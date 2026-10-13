using Sanctuary.Core.IO;

namespace Sanctuary.Packet;

public class PetSetNamePacket : ISerializablePacket
{
    public const byte OpCode = 10;
    public ulong PetGuid;
    public string Name = string.Empty;
    public bool IsAccepted = true;
    public byte[] Serialize()
    {
        using var writer = new PacketWriter();
        writer.Write(PetListPacket.BaseOpCode);
        writer.Write(OpCode);
        writer.Write(PetGuid);
        writer.Write(Name);
        writer.Write(IsAccepted);
        return writer.Buffer;
    }
}
