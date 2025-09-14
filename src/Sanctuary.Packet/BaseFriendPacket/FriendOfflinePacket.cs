using Sanctuary.Core.IO;

namespace Sanctuary.Packet;

public class FriendOfflinePacket : BaseFriendPacket, ISerializablePacket
{
    public new const byte OpCode = 3;

    public ulong Guid;

    public FriendOfflinePacket() : base(OpCode)
    {
    }

    public byte[] Serialize()
    {
        using var writer = new PacketWriter();

        Write(writer);

        writer.Write(Guid);

        return writer.Buffer;
    }
}