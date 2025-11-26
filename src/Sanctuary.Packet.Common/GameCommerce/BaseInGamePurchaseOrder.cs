using Sanctuary.Core.IO;

namespace Sanctuary.Packet.Common.GameCommerce;

public class BaseInGamePurchaseOrder : IDeserializableType
{
    public string? CustomerUserKey;
    public string? RecipientUserKey;

    public int OrderTrackingId;

    public string? CustomerLocaleString;
    public string? CouponCode;

    public string? Unknown;
    public string? Unknown2;

    public virtual bool TryRead(ref PacketReader reader)
    {
        if (!reader.TryRead(out CustomerUserKey))
            return false;

        if (!reader.TryRead(out RecipientUserKey))
            return false;

        if (!reader.TryRead(out OrderTrackingId))
            return false;

        if (!reader.TryRead(out CustomerLocaleString))
            return false;

        if (!reader.TryRead(out CouponCode))
            return false;

        if (!reader.TryRead(out Unknown))
            return false;

        if (!reader.TryRead(out Unknown2))
            return false;

        return true;
    }
}