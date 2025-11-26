using System;

using Sanctuary.Core.IO;

namespace Sanctuary.Packet;

public class InventoryPacketItemActionBarAssign : BaseInventoryPacket, IDeserializable<InventoryPacketItemActionBarAssign>
{
    public new const short OpCode = 6;

    public int Slot;
    public int Guid;

    public InventoryPacketItemActionBarAssign() : base(OpCode)
    {
    }

    public static bool TryDeserialize(ReadOnlySpan<byte> data, out InventoryPacketItemActionBarAssign value)
    {
        value = new InventoryPacketItemActionBarAssign();

        var reader = new PacketReader(data);

        if (!value.TryRead(ref reader))
            return false;

        if (!reader.TryRead(out value.Slot))
            return false;

        if (!reader.TryRead(out value.Guid))
            return false;

        return reader.RemainingLength == 0;
    }
}