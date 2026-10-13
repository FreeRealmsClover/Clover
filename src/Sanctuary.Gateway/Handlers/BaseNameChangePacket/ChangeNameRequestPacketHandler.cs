using System;
using System.Linq;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Sanctuary.Core.Helpers;
using Sanctuary.Database;
using Sanctuary.Game;
using Sanctuary.Gateway.Helpers;
using Sanctuary.Packet;
using Sanctuary.Packet.Common;
using Sanctuary.Packet.Common.Attributes;

namespace Sanctuary.Gateway.Handlers;

[PacketHandler]
public static class ChangeNameRequestPacketHandler
{
    private static ILogger _logger = null!;
    private static IZoneManager _zoneManager = null!;
    private static IDbContextFactory<DatabaseContext> _dbContextFactory = null!;
    private static IResourceManager _resourceManager = null!;

    // "Character Name Change" (StringHashToValue: NameChange.CharacterItem).
    private const int CharacterNameChangeItemId = 79000;

    public static void ConfigureServices(IServiceProvider serviceProvider)
    {
        var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
        _logger = loggerFactory.CreateLogger(nameof(ChangeNameRequestPacketHandler));

        _zoneManager = serviceProvider.GetRequiredService<IZoneManager>();
        _dbContextFactory = serviceProvider.GetRequiredService<IDbContextFactory<DatabaseContext>>();
        _resourceManager = serviceProvider.GetRequiredService<IResourceManager>();
    }

    public static bool HandlePacket(GatewayConnection connection, ReadOnlySpan<byte> data)
    {
        if (!ChangeNameRequestPacket.TryDeserialize(data, out var packet))
        {
            _logger.LogError("Failed to deserialize {packet}.", nameof(ChangeNameRequestPacket));
            return false;
        }

        _logger.LogTrace("Received {name} packet. ( {packet} )", nameof(ChangeNameRequestPacket), packet);

        var nameChangeResponsePacket = new NameChangeResponsePacket
        {
            Type = packet.Type,
            Guid = packet.Guid,
            Name = packet.Name
        };

        if (packet.Type == NameChangeType.Guild)
        {
            nameChangeResponsePacket.Result = OnChangeGuildName(connection, packet);

            connection.SendTunneled(nameChangeResponsePacket);

            return true;
        }

        if (packet.Type == NameChangeType.Pet)
        {
            var pet = PetCustomizationService.Resolve(connection.Player, guid: packet.Guid);
            nameChangeResponsePacket.Result = pet is not null &&
                PetCustomizationService.Rename(connection.Player, pet, packet.Name.FullName)
                ? ChangeNameResponse.Pending : ChangeNameResponse.Error;
            _logger.LogDebug("Pet name-change request: guid={guid}, active={active}, resolved={resolved}, nameId={nameId}, textLength={length}, result={result}",
                packet.Guid, connection.Player.ActivePetId, pet?.Id, packet.Name.FirstNameId, packet.Name.FullName.Length, nameChangeResponsePacket.Result);
            connection.SendTunneled(nameChangeResponsePacket);
            return true;
        }

        if (connection.Player.Guid != packet.Guid)
        {
            _logger.LogError("Invalid player guid. {guid}", packet.Guid);

            nameChangeResponsePacket.Result = ChangeNameResponse.Error;

            connection.SendTunneled(nameChangeResponsePacket);

            return true;
        }

        nameChangeResponsePacket.Result = packet.Type switch
        {
            NameChangeType.Character => OnChangeCharacterName(connection, packet),
            _ => ChangeNameResponse.Error
        };

        connection.SendTunneled(nameChangeResponsePacket);

        return true;
    }

    private static ChangeNameResponse OnChangeCharacterName(GatewayConnection connection, ChangeNameRequestPacket packet)
    {
        if (string.IsNullOrWhiteSpace(packet.Name.FirstName)
            || packet.Name.LastName != string.Empty && string.IsNullOrWhiteSpace(packet.Name.LastName))
        {
            return ChangeNameResponse.Error;
        }

        if (packet.Name.FirstName.Length is < 3 or > 14)
            return ChangeNameResponse.Error;

        if (packet.Name.LastName != string.Empty && (packet.Name.LastName.Length is < 3 or > 14))
            return ChangeNameResponse.Error;

        if (CharacterNameHelper.ContainsIllegalCharacters(packet.Name.FullName))
        {
            return ChangeNameResponse.Error;
        }

        if (_resourceManager.NameFilter.Any(token =>
            packet.Name.FullName.Contains(token, StringComparison.OrdinalIgnoreCase)))
        {
            return ChangeNameResponse.Error;
        }

        using var dbContext = _dbContextFactory.CreateDbContext();

        var dbCharacter = dbContext.Characters.FirstOrDefault(x => x.Id == GuidHelper.GetPlayerId(connection.Player.Guid));

        if (dbCharacter is null)
            return ChangeNameResponse.Error;

        // A character rename costs one Character Name Change item (Station Cash shop).
        var nameChangeItem = connection.Player.Items.FirstOrDefault(x => x.Definition == CharacterNameChangeItemId && x.Count > 0);

        var dbNameChangeItem = nameChangeItem is null
            ? null
            : dbContext.Items.SingleOrDefault(x => x.CharacterId == dbCharacter.Id && x.Id == nameChangeItem.Id);

        if (nameChangeItem is null || dbNameChangeItem is null || dbNameChangeItem.Count <= 0)
        {
            global::Sanctuary.Game.Eye.Log("rename-denied", connection.Player, new { reason = "no name change item", wanted = packet.Name.FullName });
            return ChangeNameResponse.MissingItem;
        }

        var taken = dbContext.Characters.Any(x => x.FirstName == packet.Name.FirstName && x.LastName == packet.Name.LastName);
        if (taken)
            return ChangeNameResponse.Error;

        var oldName = connection.Player.Name.FullName;
        var itemUsedUp = dbNameChangeItem.Count <= 1;

        if (itemUsedUp)
            dbContext.Items.Remove(dbNameChangeItem);
        else
            dbNameChangeItem.Count -= 1;

        dbCharacter.FirstName = packet.Name.FirstName;
        dbCharacter.LastName = packet.Name.LastName;

        if (dbContext.SaveChanges() <= 0)
            return ChangeNameResponse.Error;

        if (itemUsedUp)
        {
            connection.Player.Items.Remove(nameChangeItem);
            connection.SendTunneled(new ClientUpdatePacketItemDelete { ItemGuid = nameChangeItem.Id });
        }
        else
        {
            nameChangeItem.Count = dbNameChangeItem.Count;
            connection.SendTunneled(new ClientUpdatePacketItemUpdate { ItemGuid = nameChangeItem.Id, Count = nameChangeItem.Count });
        }

        global::Sanctuary.Game.Eye.Log("rename", connection.Player, new { from = oldName, to = packet.Name.FullName });

        connection.Player.Name.FirstName = packet.Name.FirstName;
        connection.Player.Name.LastName = packet.Name.LastName;

        var playerUpdatePacketRenamePlayer = new PlayerUpdatePacketRenamePlayer();

        playerUpdatePacketRenamePlayer.Guid = connection.Player.Guid;
        playerUpdatePacketRenamePlayer.Name = connection.Player.Name;

        connection.Player.SendTunneledToVisible(playerUpdatePacketRenamePlayer, true);

        var friendRenamePacket = new FriendRenamePacket
        {
            Guid = connection.Player.Guid,
            Name = connection.Player.Name.FullName
        };

        foreach (var friend in connection.Player.Friends)
        {
            if (!_zoneManager.TryGetPlayer(friend.Guid, out var friendPlayer))
                continue;

            friendPlayer.SendTunneled(friendRenamePacket);
        }

        return ChangeNameResponse.Pending;
    }

    private static ChangeNameResponse OnChangeGuildName(GatewayConnection connection, ChangeNameRequestPacket packet)
    {
        if (connection.Player.GuildData is null)
            return ChangeNameResponse.Error;

        var guildGuid = connection.Player.GuildData.Guid;
        var guildName = GuildHelper.NormalizeName(packet.Name.FullName);

        if (!GuildHelper.IsValidName(guildName))
            return ChangeNameResponse.Error;

        if (GuildHelper.IsProfane(guildName, _resourceManager.NameFilter))
            return ChangeNameResponse.Error;

        using var dbContext = _dbContextFactory.CreateDbContext();

        var role = GuildHelper.GetMemberRole(dbContext, guildGuid, connection.Player.Guid);

        if (!GuildHelper.IsLeaderRole(role))
            return ChangeNameResponse.Error;

        if (GuildHelper.IsNameTaken(dbContext, guildGuid, guildName))
            return ChangeNameResponse.Error;

        var oldGuildName = connection.Player.GuildData.Name;

        if (!GuildHelper.TryConsumeNameChangeItem(dbContext, connection, GuildHelper.GuildNameChangeItemId))
        {
            global::Sanctuary.Game.Eye.Log("guild-rename-denied", connection.Player, new { reason = "no guild name change item", wanted = guildName });
            return ChangeNameResponse.MissingItem;
        }

        if (!GuildHelper.ApplyRename(_zoneManager, dbContext, connection, guildGuid, guildName, out _))
            return ChangeNameResponse.Error;

        global::Sanctuary.Game.Eye.Log("guild-rename", connection.Player, new { guild = guildGuid, from = oldGuildName, to = guildName });

        return ChangeNameResponse.Pending;
    }
}
