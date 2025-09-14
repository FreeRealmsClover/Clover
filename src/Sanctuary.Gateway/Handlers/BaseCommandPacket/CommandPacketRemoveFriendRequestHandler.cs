using System;
using System.Linq;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Sanctuary.Database;
using Sanctuary.Game;
using Sanctuary.Packet;
using Sanctuary.Packet.Common;
using Sanctuary.Packet.Common.Attributes;

namespace Sanctuary.Gateway.Handlers;

[PacketHandler]
public static class CommandPacketRemoveFriendRequestHandler
{
    private static ILogger _logger = null!;
    private static IZoneManager _zoneManager = null!;
    private static IDbContextFactory<DatabaseContext> _dbContextFactory = null!;

    public static void ConfigureServices(IServiceProvider serviceProvider)
    {
        var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
        _logger = loggerFactory.CreateLogger(nameof(CommandPacketRemoveFriendRequestHandler));

        _zoneManager = serviceProvider.GetRequiredService<IZoneManager>();
        _dbContextFactory = serviceProvider.GetRequiredService<IDbContextFactory<DatabaseContext>>();
    }

    public static bool HandlePacket(GatewayConnection connection, ReadOnlySpan<byte> data)
    {
        if (!CommandPacketRemoveFriendRequest.TryDeserialize(data, out var packet))
        {
            _logger.LogError("Failed to deserialize {packet}.", nameof(CommandPacketRemoveFriendRequest));
            return false;
        }

        _logger.LogTrace("Received {name} packet. ( {packet} )", nameof(CommandPacketRemoveFriendRequest), packet);

        Handle(connection, packet.Name);

        return true;
    }

    public static void Handle(GatewayConnection connection, NameData name)
    {
        using var dbContext = _dbContextFactory.CreateDbContext();

        var dbCharacterToRemove = dbContext.Characters
            .AsNoTracking()
            .Include(x => x.Friends)
                .ThenInclude(x => x.FriendCharacter)
            .FirstOrDefault(x => x.FullName == name.FullName);

        if (dbCharacterToRemove is null)
            return;

        var dbFriendsToRemove = dbContext.Friends.Where(x => (x.CharacterGuid == dbCharacterToRemove.Guid &&
                                                              x.FriendCharacterGuid == connection.Player.Guid) ||
                                                             (x.FriendCharacterGuid == dbCharacterToRemove.Guid &&
                                                             x.CharacterGuid == connection.Player.Guid));

        if (dbFriendsToRemove.ExecuteDelete() <= 0)
            return;

        connection.Player.Friends.RemoveAll(x => x.Guid == dbCharacterToRemove.Guid);

        connection.Player.SendTunneled(new FriendRemovePacket
        {
            Guid = dbCharacterToRemove.Guid
        });

        if (_zoneManager.TryGetPlayer(dbCharacterToRemove.Guid, out var player))
        {
            player.Friends.RemoveAll(x => x.Guid == connection.Player.Guid);

            player.SendTunneled(new FriendRemovePacket
            {
                Guid = connection.Player.Guid
            });
        }
    }
}