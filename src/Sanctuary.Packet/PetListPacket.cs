using System.Collections.Generic;

using Sanctuary.Core.IO;
using Sanctuary.Packet.Common;

namespace Sanctuary.Packet;

public class PetListPacket : ISerializablePacket
{
    public const short BaseOpCode = 53;
    public const byte OpCode = 5;
    public List<ClientPetData> Pets = [];
    public int ActivePetId;
    public ulong ActivePetGuid;

    public static bool IsListRequest(System.ReadOnlySpan<byte> data)
    {
        var reader = new PacketReader(data);
        if (!reader.TryRead(out short baseOpCode) || baseOpCode != BaseOpCode ||
            !reader.TryRead(out byte subOpCode))
            return false;

        // The original client also uses a padded header: 53, 0, subopcode, 0.
        if (subOpCode == 0)
        {
            if (!reader.TryRead(out byte padding) || padding != 0 ||
                !reader.TryRead(out short paddedSubOpCode) || paddedSubOpCode < 0 || paddedSubOpCode > 255)
                return false;
            subOpCode = (byte)paddedSubOpCode;
        }

        return subOpCode is 1 or 2 or OpCode or 24;
    }

    public byte[] Serialize()
    {
        using var writer = new PacketWriter();
        writer.Write(BaseOpCode);
        writer.Write(OpCode);
        writer.Write(Pets.Count);
        foreach (var pet in Pets)
        {
            writer.Write(pet.Id);
            pet.Serialize(writer);
        }
        writer.Write(ActivePetId);
        writer.Write(ActivePetGuid);
        return writer.Buffer;
    }
}
