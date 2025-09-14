using Sanctuary.Core.IO;
using Sanctuary.Packet.Common;

namespace Sanctuary.Packet;

public class CommandPacketConfirmFriendRequest : BaseCommandPacket, ISerializablePacket
{
    public new const short OpCode = 16;

    public ulong Guid;

    public NameData Name = new();

    public bool Unknown;

    public CommandPacketConfirmFriendRequest() : base(OpCode)
    {
    }

    public byte[] Serialize()
    {
        using var writer = new PacketWriter();

        base.Write(writer);

        writer.Write(Guid);

        Name.Serialize(writer);

        writer.Write(Unknown);

        return writer.Buffer;
    }
}