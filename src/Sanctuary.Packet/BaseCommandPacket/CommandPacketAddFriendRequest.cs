using System;

using Sanctuary.Core.IO;

namespace Sanctuary.Packet;

public class CommandPacketAddFriendRequest : BaseCommandPacket, IDeserializable<CommandPacketAddFriendRequest>
{
    public new const short OpCode = 14;

    public string? Name;

    public CommandPacketAddFriendRequest() : base(OpCode)
    {
    }

    public static bool TryDeserialize(ReadOnlySpan<byte> data, out CommandPacketAddFriendRequest value)
    {
        value = new CommandPacketAddFriendRequest();

        var reader = new PacketReader(data);

        if (!value.TryRead(ref reader))
            return false;

        if (!reader.TryRead(out value.Name))
            return false;

        return reader.RemainingLength == 0;
    }
}