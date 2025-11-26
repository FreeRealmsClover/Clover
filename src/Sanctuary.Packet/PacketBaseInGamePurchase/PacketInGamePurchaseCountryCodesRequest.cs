using System;

using Sanctuary.Core.IO;

namespace Sanctuary.Packet;

public class PacketInGamePurchaseCountryCodesRequest : PacketBaseInGamePurchase, IDeserializable<PacketInGamePurchaseCountryCodesRequest>
{
    public new const short OpCode = 20;

    public string? Locale;

    public PacketInGamePurchaseCountryCodesRequest() : base(OpCode)
    {
    }

    public static bool TryDeserialize(ReadOnlySpan<byte> data, out PacketInGamePurchaseCountryCodesRequest value)
    {
        value = new PacketInGamePurchaseCountryCodesRequest();

        var reader = new PacketReader(data);

        if (!value.TryRead(ref reader))
            return false;

        if (!reader.TryRead(out value.Locale))
            return false;

        return reader.RemainingLength == 0;
    }
}