using System;

using Sanctuary.Core.IO;
using Sanctuary.Packet.Common;

namespace Sanctuary.Packet;

public class CommandPacketRemoveFriendRequest : BaseCommandPacket, IDeserializable<CommandPacketRemoveFriendRequest>
{
    public new const short OpCode = 15;

    public NameData Name = new();

    public CommandPacketRemoveFriendRequest() : base(OpCode)
    {
    }

    public static bool TryDeserialize(ReadOnlySpan<byte> data, out CommandPacketRemoveFriendRequest value)
    {
        value = new CommandPacketRemoveFriendRequest();

        var reader = new PacketReader(data);

        if (!value.TryRead(ref reader))
            return false;

        if (!value.Name.TryRead(ref reader))
            return false;

        return reader.RemainingLength == 0;
    }
}