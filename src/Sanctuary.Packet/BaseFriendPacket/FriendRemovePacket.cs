using Sanctuary.Core.IO;

namespace Sanctuary.Packet;

public class FriendRemovePacket : BaseFriendPacket, ISerializablePacket
{
    public new const byte OpCode = 7;

    public ulong Guid;

    public FriendRemovePacket() : base(OpCode)
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