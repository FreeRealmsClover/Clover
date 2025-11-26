using Sanctuary.Core.IO;

namespace Sanctuary.Packet;

public class AbilityPacketFailed : BaseAbilityPacket, ISerializablePacket
{
    public new const short OpCode = 1;

    public int StringId;

    public AbilityPacketFailed() : base(OpCode)
    {
    }

    public byte[] Serialize()
    {
        using var writer = new PacketWriter();

        Write(writer);

        writer.Write(StringId);

        return writer.Buffer;
    }
}