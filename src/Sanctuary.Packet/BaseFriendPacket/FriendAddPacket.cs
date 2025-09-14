using Sanctuary.Core.IO;
using Sanctuary.Packet.Common;

namespace Sanctuary.Packet;

public class FriendAddPacket : BaseFriendPacket, ISerializablePacket
{
    public new const byte OpCode = 6;

    public FriendData Data = new();

    public FriendAddPacket() : base(OpCode)
    {
    }

    public byte[] Serialize()
    {
        using var writer = new PacketWriter();

        Write(writer);

        Data.Serialize(writer);

        return writer.Buffer;
    }
}