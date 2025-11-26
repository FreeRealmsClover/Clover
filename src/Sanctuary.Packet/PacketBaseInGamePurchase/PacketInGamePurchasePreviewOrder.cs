using System;

using Sanctuary.Core.IO;
using Sanctuary.Packet.Common.GameCommerce;

namespace Sanctuary.Packet;

public class PacketInGamePurchasePreviewOrder : PacketBaseInGamePurchase, IDeserializable<PacketInGamePurchasePreviewOrder>
{
    public new const short OpCode = 1;

    public ClientInGamePurchaseOrder Order = new();

    private string? Unused;

    public PacketInGamePurchasePreviewOrder() : base(OpCode)
    {
    }

    public static bool TryDeserialize(ReadOnlySpan<byte> data, out PacketInGamePurchasePreviewOrder value)
    {
        value = new PacketInGamePurchasePreviewOrder();

        var reader = new PacketReader(data);

        if (!value.TryRead(ref reader))
            return false;

        if (!value.Order.TryRead(ref reader))
            return false;

        if (!reader.TryRead(out value.Unused))
            return false;

        return reader.RemainingLength == 0;
    }
}