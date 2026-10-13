using System;

using Sanctuary.Core.IO;

namespace Sanctuary.Packet;

public static class PetPacketHeader
{
    public static bool TryRead(ReadOnlySpan<byte> data, out PacketReader reader, out byte opCode)
    {
        reader = new PacketReader(data);
        opCode = 0;
        if (!reader.TryRead(out short baseOpCode) || baseOpCode != PetListPacket.BaseOpCode ||
            !reader.TryRead(out opCode))
            return false;
        if (opCode != 0)
            return true;
        if (!reader.TryRead(out byte padding) || padding != 0 ||
            !reader.TryRead(out short paddedOpCode) || paddedOpCode is < 1 or > 255 ||
            !reader.TryRead(out short reserved) || reserved != 0 || reader.RemainingLength < 4)
            return false;
        reader.Advance(4);
        opCode = (byte)paddedOpCode;
        return true;
    }
}

public readonly record struct PetRenameRequest(int PetId, ulong PetGuid, string Name)
{
    public static bool TryDeserialize(ReadOnlySpan<byte> data, out PetRenameRequest request)
    {
        request = default;
        if (data.Length > 512 || !PetPacketHeader.TryRead(data, out var reader, out var opCode) || opCode is not (6 or 10))
            return false;
        var payload = reader.RemainingSpan;
        if (opCode == 10)
        {
            var guidReader = new PacketReader(payload);
            if (guidReader.TryRead(out ulong guid) && guidReader.TryRead(out string guidName) &&
                (guidReader.RemainingLength == 0 || guidReader.RemainingLength == 1 && guidReader.TryRead(out bool _)))
            {
                request = new PetRenameRequest(0, guid, guidName);
                return true;
            }
            var idReader = new PacketReader(payload);
            if (idReader.TryRead(out int id) && idReader.TryRead(out string idName) && idReader.RemainingLength == 0)
            {
                request = new PetRenameRequest(id, 0, idName);
                return true;
            }
        }
        var nameReader = new PacketReader(payload);
        if (!nameReader.TryRead(out string name) || nameReader.RemainingLength != 0)
            return false;
        request = new PetRenameRequest(0, 0, name);
        return true;
    }
}

public readonly record struct PetClothingRequest(int PetId, int Slot, int ItemId)
{
    public static bool TryDeserialize(ReadOnlySpan<byte> data, out PetClothingRequest request)
    {
        request = default;
        if (!PetPacketHeader.TryRead(data, out var reader, out var opCode) || opCode is not (11 or 16 or 21 or 35))
            return false;
        var mode = -1;
        if (reader.RemainingLength % 4 == 1)
        {
            reader.TryRead(out byte byteMode);
            mode = byteMode;
        }
        if (opCode == 35 || opCode == 11 && reader.RemainingLength == 4)
        {
            if (!reader.TryRead(out int itemId) || itemId <= 0 || reader.RemainingLength != 0)
                return false;
            request = new PetClothingRequest(0, 0, itemId);
            return true;
        }
        if (reader.RemainingLength == 12)
        {
            reader.TryRead(out int petId);
            reader.TryRead(out int slot);
            reader.TryRead(out int itemId);
            request = new PetClothingRequest(petId, slot, itemId);
            return petId >= 0 && itemId >= 0;
        }
        if (opCode == 11 && reader.RemainingLength == 8)
        {
            reader.TryRead(out int first);
            reader.TryRead(out int second);
            if (mode == 1) // "None" button in the original equipment UI.
                request = new PetClothingRequest(0, first is 1 or 3 or 5 or 6 ? first : second, 0);
            else
                request = first is 1 or 3 or 5 or 6
                    ? new PetClothingRequest(0, first, second)
                    : new PetClothingRequest(0, second, first);
            return request.ItemId >= 0;
        }
        return false;
    }
}
