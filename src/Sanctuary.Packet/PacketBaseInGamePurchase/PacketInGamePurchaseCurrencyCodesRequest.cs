using System;

using Sanctuary.Core.IO;

namespace Sanctuary.Packet;

public class PacketInGamePurchaseCurrencyCodesRequest : PacketBaseInGamePurchase, IDeserializable<PacketInGamePurchaseCurrencyCodesRequest>
{
    public new const short OpCode = 16;

    public string? Locale;

    public PacketInGamePurchaseCurrencyCodesRequest() : base(OpCode)
    {
    }

    public static bool TryDeserialize(ReadOnlySpan<byte> data, out PacketInGamePurchaseCurrencyCodesRequest value)
    {
        value = new PacketInGamePurchaseCurrencyCodesRequest();

        var reader = new PacketReader(data);

        if (!value.TryRead(ref reader))
            return false;

        if (!reader.TryRead(out value.Locale))
            return false;

        return reader.RemainingLength == 0;
    }
}