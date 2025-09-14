using Sanctuary.Core.IO;
using Sanctuary.Packet.Common;

namespace Sanctuary.Packet;

public class TellEchoPacket : BaseChatPacket, ISerializablePacket
{
    public new const short OpCode = 5;

    public NameData Name = new();

    public string? Message = null!;

    public TellEchoPacket() : base(OpCode)
    {
    }

    public byte[] Serialize()
    {
        using var writer = new PacketWriter();

        base.Write(writer);

        Name.Serialize(writer);

        writer.Write(Message);

        return writer.Buffer;
    }
}