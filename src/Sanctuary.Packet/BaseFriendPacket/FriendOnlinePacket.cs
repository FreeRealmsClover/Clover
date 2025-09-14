using Sanctuary.Core.IO;

namespace Sanctuary.Packet;

public class FriendOnlinePacket : BaseFriendPacket, ISerializablePacket
{
    public new const byte OpCode = 2;

    public string? Name;

    public ulong Guid;

    public bool IsLocal;

    public FriendOnlinePacket() : base(OpCode)
    {
    }

    public byte[] Serialize()
    {
        using var writer = new PacketWriter();

        Write(writer);

        writer.Write(Name);

        writer.Write(Guid);

        writer.Write(IsLocal);

        return writer.Buffer;
    }
}