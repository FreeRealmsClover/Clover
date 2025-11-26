using System;

using Sanctuary.Core.IO;

namespace Sanctuary.Packet;

public class InventoryPacketUseStyleCard : BaseInventoryPacket, IDeserializable<InventoryPacketUseStyleCard>
{
    public new const short OpCode = 10;

    public int ItemGuid;

    public InventoryPacketUseStyleCard() : base(OpCode)
    {
    }

    public static bool TryDeserialize(ReadOnlySpan<byte> data, out InventoryPacketUseStyleCard value)
    {
        value = new InventoryPacketUseStyleCard();

        var reader = new PacketReader(data);

        if (!value.TryRead(ref reader))
            return false;

        if (!reader.TryRead(out value.ItemGuid))
            return false;

        return reader.RemainingLength == 0;
    }
}