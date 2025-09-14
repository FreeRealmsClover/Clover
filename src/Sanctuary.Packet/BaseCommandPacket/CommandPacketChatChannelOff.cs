using System;

using Sanctuary.Core.IO;

namespace Sanctuary.Packet;

public class CommandPacketChatChannelOff : BaseCommandPacket, IDeserializable<CommandPacketChatChannelOff>
{
    public new const short OpCode = 33;

    public string? Name;

    public CommandPacketChatChannelOff() : base(OpCode)
    {
    }

    public static bool TryDeserialize(ReadOnlySpan<byte> data, out CommandPacketChatChannelOff value)
    {
        value = new CommandPacketChatChannelOff();

        var reader = new PacketReader(data);

        if (!value.TryRead(ref reader))
            return false;

        if (!reader.TryRead(out value.Name))
            return false;

        return reader.RemainingLength == 0;
    }
}