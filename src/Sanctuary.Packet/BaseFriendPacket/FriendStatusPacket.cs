using Sanctuary.Core.IO;
using Sanctuary.Packet.Common;

namespace Sanctuary.Packet;

public class FriendStatusPacket : BaseFriendPacket, ISerializablePacket
{
    public new const byte OpCode = 9;

    public string? Name;

    public ulong Guid;

    public FriendStatus Status = new();

    public FriendStatusPacket() : base(OpCode)
    {
    }

    public byte[] Serialize()
    {
        using var writer = new PacketWriter();

        Write(writer);

        writer.Write(Name);

        writer.Write(Guid);

        Status.Serialize(writer);

        return writer.Buffer;
    }
}