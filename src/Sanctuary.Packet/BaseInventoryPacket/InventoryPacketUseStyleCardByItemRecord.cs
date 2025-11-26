using System;

using Sanctuary.Core.IO;

namespace Sanctuary.Packet;

public class InventoryPacketUseStyleCardByItemRecord : BaseInventoryPacket, IDeserializable<InventoryPacketUseStyleCardByItemRecord>
{
    public new const short OpCode = 12;

    public int ItemDefinitionId;

    public InventoryPacketUseStyleCardByItemRecord() : base(OpCode)
    {
    }

    public static bool TryDeserialize(ReadOnlySpan<byte> data, out InventoryPacketUseStyleCardByItemRecord value)
    {
        value = new InventoryPacketUseStyleCardByItemRecord();

        var reader = new PacketReader(data);

        if (!value.TryRead(ref reader))
            return false;

        if (!reader.TryRead(out value.ItemDefinitionId))
            return false;

        return reader.RemainingLength == 0;
    }
}