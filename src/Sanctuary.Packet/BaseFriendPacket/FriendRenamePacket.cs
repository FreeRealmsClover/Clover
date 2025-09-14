using Sanctuary.Core.IO;

namespace Sanctuary.Packet;

public class FriendRenamePacket : BaseFriendPacket, ISerializablePacket
{
    public new const byte OpCode = 10;

    public string? Name;

    public ulong Guid;

    public FriendRenamePacket() : base(OpCode)
    {
    }

    public byte[] Serialize()
    {
        using var writer = new PacketWriter();

        Write(writer);

        writer.Write(Name);

        writer.Write(Guid);

        return writer.Buffer;
    }
}