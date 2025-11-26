using System;

using Sanctuary.Core.IO;

namespace Sanctuary.Packet;

public class PacketInGamePurchaseSubscriptionProductsRequest : PacketBaseInGamePurchase, IDeserializable<PacketInGamePurchaseSubscriptionProductsRequest>
{
    public new const short OpCode = 22;

    public string? Locale;
    public string? CurrencyCode;

    public PacketInGamePurchaseSubscriptionProductsRequest() : base(OpCode)
    {
    }

    public static bool TryDeserialize(ReadOnlySpan<byte> data, out PacketInGamePurchaseSubscriptionProductsRequest value)
    {
        value = new PacketInGamePurchaseSubscriptionProductsRequest();

        var reader = new PacketReader(data);

        if (!value.TryRead(ref reader))
            return false;

        if (!reader.TryRead(out value.Locale))
            return false;

        if (!reader.TryRead(out value.CurrencyCode))
            return false;

        return reader.RemainingLength == 0;
    }
}