using System;
using System.Linq;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Sanctuary.Packet.Common;
using Sanctuary.Core.IO;
using Sanctuary.Packet;
using Sanctuary.Packet.Common.Attributes;

namespace Sanctuary.Gateway.Handlers;

[PacketHandler]
public static class PetListPacketHandler
{
    private static ILogger _logger = null!;
    public static void ConfigureServices(IServiceProvider serviceProvider)
    {
        _logger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(PetListPacketHandler));
        PetCustomizationService.ConfigureServices(serviceProvider);
    }

    public static bool HandlePacket(GatewayConnection connection, PacketReader reader)
    {
        if (PetRenameRequest.TryDeserialize(reader.Span, out var rename))
        {
            var pet = PetCustomizationService.Resolve(connection.Player, rename.PetId, rename.PetGuid);
            var accepted = pet is not null && PetCustomizationService.Rename(connection.Player, pet, rename.Name);
            _logger.LogDebug("Pet rename request: id={id}, guid={guid}, active={active}, resolved={resolved}, accepted={accepted}",
                rename.PetId, rename.PetGuid, connection.Player.ActivePetId, pet?.Id, accepted);
            connection.SendTunneled(new NameChangeResponsePacket
            {
                Type = NameChangeType.Pet,
                // Pet lists serialize the inventory ID, not the server-only synthetic GUID.
                Guid = rename.PetGuid != 0 ? rename.PetGuid : (ulong)(pet?.Id ?? 0),
                Name = new NameData { FirstName = rename.Name },
                Result = accepted ? ChangeNameResponse.Pending : ChangeNameResponse.Error
            });
            if (!accepted)
                connection.SendTunneled(new PetSetNamePacket { PetGuid = rename.PetGuid, Name = rename.Name, IsAccepted = false });
            return true;
        }
        if (PetClothingRequest.TryDeserialize(reader.Span, out var clothing))
        {
            var pet = PetCustomizationService.Resolve(connection.Player, clothing.PetId);
            return pet is not null && PetCustomizationService.Equip(connection.Player, pet, clothing.Slot, clothing.ItemId);
        }
        if (PetSummonRequest.TryDeserialize(reader.Span, out var request))
        {
            if (!request.Summon)
            {
                PetCompanionService.Dismiss(connection.Player);
                PetCompanionService.SendActiveState(connection.Player);
                return true;
            }

            var pet = request.OpCode == 32
                ? connection.Player.Pets.FirstOrDefault(pet => pet.ItemDefinitionId == request.Id)
                : connection.Player.Pets.FirstOrDefault(pet => pet.Id == request.Id);
            if (pet is null)
                return false;
            return PetCompanionService.Summon(connection.Player, pet);
        }
        if (!PetListPacket.IsListRequest(reader.Span))
        {
            if (PetPacketHeader.TryRead(reader.Span, out _, out var unknownOpcode) && unknownOpcode is 6 or 10)
                _logger.LogWarning("Unrecognized pet rename payload: opcode={opcode}, size={size}, payload={payload}",
                    unknownOpcode, reader.Span.Length, Convert.ToHexString(reader.Span[..Math.Min(reader.Span.Length, 512)]));
            return false;
        }

        PetCompanionService.SendActiveState(connection.Player);
        if (PetPacketHeader.TryRead(reader.Span, out _, out var opcode) && opcode == 24)
            PetCustomizationService.SendWardrobe(connection.Player);
        return true;
    }
}
