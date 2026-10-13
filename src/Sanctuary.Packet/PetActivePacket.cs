using Sanctuary.Core.IO;

namespace Sanctuary.Packet;

public class PetActivePacket : ISerializablePacket
{
    public int PetId;
    public ulong PetGuid;

    public byte[] Serialize()
    {
        using var writer = new PacketWriter();
        writer.Write(PetListPacket.BaseOpCode);
        writer.Write((byte)9);
        writer.Write(PetId);
        writer.Write(PetGuid);
        return writer.Buffer;
    }
}
