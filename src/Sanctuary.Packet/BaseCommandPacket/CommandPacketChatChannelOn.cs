using System;

using Sanctuary.Core.IO;

namespace Sanctuary.Packet;

public class CommandPacketChatChannelOn : BaseCommandPacket, IDeserializable<CommandPacketChatChannelOn>
{
    public new const short OpCode = 32;

    public string? Name;

    public CommandPacketChatChannelOn() : base(OpCode)
    {
    }

    public static bool TryDeserialize(ReadOnlySpan<byte> data, out CommandPacketChatChannelOn value)
    {
        value = new CommandPacketChatChannelOn();

        var reader = new PacketReader(data);

        if (!value.TryRead(ref reader))
            return false;

        if (!reader.TryRead(out value.Name))
            return false;

        return reader.RemainingLength == 0;
    }
}