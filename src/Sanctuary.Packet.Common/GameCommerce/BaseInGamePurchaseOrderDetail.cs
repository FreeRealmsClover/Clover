using Sanctuary.Core.IO;

namespace Sanctuary.Packet.Common.GameCommerce;

public class BaseInGamePurchaseOrderDetail : IDeserializableType
{
    public int StoreId;
    public int StoreBundleId;

    public int Quantity;

    public string? Tint;
    public string? Unknown;

    public bool TryRead(ref PacketReader reader)
    {
        if (!reader.TryRead(out StoreBundleId))
            return false;

        if (!reader.TryRead(out StoreId))
            return false;

        if (!reader.TryRead(out Quantity))
            return false;

        if (!reader.TryRead(out Tint))
            return false;

        if (!reader.TryRead(out Unknown))
            return false;

        return true;
    }
}