using System;

using Sanctuary.Core.IO;
using Sanctuary.Packet.Common;

namespace Sanctuary.Packet;

public class InventoryPacketEquipByItemRecord : BaseInventoryPacket, IDeserializable<InventoryPacketEquipByItemRecord>
{
    public new const short OpCode = 8;

    public ItemRecord ItemRecord = new();
    public int ProfileId;
    public int Slot;

    public InventoryPacketEquipByItemRecord() : base(OpCode)
    {
    }

    public static bool TryDeserialize(ReadOnlySpan<byte> data, out InventoryPacketEquipByItemRecord value)
    {
        value = new InventoryPacketEquipByItemRecord();

        var reader = new PacketReader(data);

        if (!value.TryRead(ref reader))
            return false;

        if (!value.ItemRecord.TryRead(ref reader))
            return false;

        if (!reader.TryRead(out value.ProfileId))
            return false;

        if (!reader.TryRead(out value.Slot))
            return false;

        return reader.RemainingLength == 0;
    }
}