using System;

using Sanctuary.Core.IO;
using Sanctuary.Packet.Common;

namespace Sanctuary.Packet;

public class CommandPacketIgnoreRequest : BaseCommandPacket, IDeserializable<CommandPacketIgnoreRequest>
{
    public new const short OpCode = 30;

    public NameData Name = new NameData();

    public bool Ignore;

    public CommandPacketIgnoreRequest() : base(OpCode)
    {
    }

    public static bool TryDeserialize(ReadOnlySpan<byte> data, out CommandPacketIgnoreRequest value)
    {
        value = new CommandPacketIgnoreRequest();

        var reader = new PacketReader(data);

        if (!value.TryRead(ref reader))
            return false;

        if (!value.Name.TryRead(ref reader))
            return false;

        if (!reader.TryRead(out value.Ignore))
            return false;

        return reader.RemainingLength == 0;
    }
}