using System;

using Sanctuary.Core.IO;
using Sanctuary.Packet.Common;

namespace Sanctuary.Packet;

public class CommandPacketConfirmFriendResponse : BaseCommandPacket, IDeserializable<CommandPacketConfirmFriendResponse>
{
    public new const short OpCode = 17;

    public ulong Guid;

    // 0 - Accept
    // 1 - Decline
    // 2 - TimeOut
    public int Status;

    public NameData Name = new();

    public CommandPacketConfirmFriendResponse() : base(OpCode)
    {
    }

    public static bool TryDeserialize(ReadOnlySpan<byte> data, out CommandPacketConfirmFriendResponse value)
    {
        value = new CommandPacketConfirmFriendResponse();

        var reader = new PacketReader(data);

        if (!value.TryRead(ref reader))
            return false;

        if (!reader.TryRead(out value.Guid))
            return false;

        if (!reader.TryRead(out value.Status))
            return false;

        if (!value.Name.TryRead(ref reader))
            return false;

        return reader.RemainingLength == 0;
    }
}