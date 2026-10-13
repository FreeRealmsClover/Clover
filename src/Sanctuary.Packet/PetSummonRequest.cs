using System;

using Sanctuary.Core.IO;

namespace Sanctuary.Packet;

public readonly record struct PetSummonRequest(byte OpCode, int Id, bool Summon)
{
    public static bool TryDeserialize(ReadOnlySpan<byte> data, out PetSummonRequest request)
    {
        request = default;
        var reader = new PacketReader(data);
        if (!reader.TryRead(out short baseOpCode) || baseOpCode != PetListPacket.BaseOpCode ||
            !reader.TryRead(out byte subOpCode))
            return false;

        if (subOpCode == 0)
        {
            if (!reader.TryRead(out byte padding) || padding != 0 ||
                !reader.TryRead(out short paddedOpCode) || paddedOpCode is not (4 or 9 or 32) ||
                !reader.TryRead(out short reserved) || reserved != 0 || reader.RemainingLength < 4)
                return false;
            reader.Advance(4); // Payload discriminator and padding, as in the original pet packets.
            subOpCode = (byte)paddedOpCode;
        }

        if (subOpCode is not (4 or 9 or 32) || !reader.TryRead(out int id) || id < 0)
            return false;

        var status = 1;
        if (reader.RemainingLength == 1)
        {
            if (!reader.TryRead(out byte byteStatus))
                return false;
            status = byteStatus;
        }
        else if (reader.RemainingLength == 4)
        {
            if (!reader.TryRead(out status))
                return false;
        }
        else if (reader.RemainingLength != 0)
            return false;

        if (status is not (0 or 1) || (status == 1 && id == 0) || (subOpCode == 32 && status != 1))
            return false;

        request = new PetSummonRequest(subOpCode, id, status == 1);
        return true;
    }
}
