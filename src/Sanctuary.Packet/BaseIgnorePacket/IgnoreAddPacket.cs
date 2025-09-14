using Sanctuary.Core.IO;
using Sanctuary.Packet.Common;

namespace Sanctuary.Packet;

public class IgnoreAddPacket : BaseIgnorePacket, ISerializablePacket
{
    public new const byte OpCode = 2;

    public IgnoreData Ignore = new IgnoreData();

    public IgnoreAddPacket() : base(OpCode)
    {
    }

    public byte[] Serialize()
    {
        using var writer = new PacketWriter();

        Write(writer);

        Ignore.Serialize(writer);

        return writer.Buffer;
    }
}