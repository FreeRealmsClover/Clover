using Sanctuary.Core.IO;
using Sanctuary.Packet.Common;

namespace Sanctuary.Packet;

public class FriendMessagePacket : BaseFriendPacket, ISerializablePacket
{
    public new const byte OpCode = 8;

    public FriendMessageType Type;

    public ulong Guid;

    public NameData Name = new();

    /// <summary>
    /// Used by <see cref="FriendMessageType.FriendNameChanged"/>
    /// </summary>
    public NameData NewName = new();

    public FriendMessagePacket() : base(OpCode)
    {
    }

    public byte[] Serialize()
    {
        using var writer = new PacketWriter();

        Write(writer);

        writer.Write(Type);

        writer.Write(Guid);

        Name.Serialize(writer);
        NewName.Serialize(writer);

        return writer.Buffer;
    }
}