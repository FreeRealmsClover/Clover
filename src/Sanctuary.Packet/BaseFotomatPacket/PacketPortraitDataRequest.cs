using System;

using Sanctuary.Core.IO;

namespace Sanctuary.Packet;

public class PacketPortraitDataRequest : BaseFotomatPacket, IDeserializable<PacketPortraitDataRequest>
{
    public new const short OpCode = 2;

    public ulong Guid;

    public string? Provider;

    public PacketPortraitDataRequest() : base(OpCode)
    {
    }

    public static bool TryDeserialize(ReadOnlySpan<byte> data, out PacketPortraitDataRequest value)
    {
        value = new PacketPortraitDataRequest();

        var reader = new PacketReader(data);

        if (!value.TryRead(ref reader))
            return false;

        if (!reader.TryRead(out value.Guid))
            return false;

        if (!reader.TryRead(out value.Provider))
            return false;

        return reader.RemainingLength == 0;
    }
}