using System;

using Sanctuary.Core.IO;

namespace Sanctuary.Packet;

public class InventoryPacketPreviewStyleCard : BaseInventoryPacket, IDeserializable<InventoryPacketPreviewStyleCard>
{
    public new const short OpCode = 11;

    public int Id;

    public InventoryPacketPreviewStyleCard() : base(OpCode)
    {
    }

    public static bool TryDeserialize(ReadOnlySpan<byte> data, out InventoryPacketPreviewStyleCard value)
    {
        value = new InventoryPacketPreviewStyleCard();

        var reader = new PacketReader(data);

        if (!value.TryRead(ref reader))
            return false;

        if (!reader.TryRead(out value.Id))
            return false;

        return reader.RemainingLength == 0;
    }
}