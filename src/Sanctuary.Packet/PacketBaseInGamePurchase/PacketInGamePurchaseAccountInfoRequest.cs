using System;

using Sanctuary.Core.IO;

namespace Sanctuary.Packet;

public class PacketInGamePurchaseAccountInfoRequest : PacketBaseInGamePurchase, IDeserializable<PacketInGamePurchaseAccountInfoRequest>
{
    public new const short OpCode = 25;

    public string? Locale;

    public PacketInGamePurchaseAccountInfoRequest() : base(OpCode)
    {
    }

    public static bool TryDeserialize(ReadOnlySpan<byte> data, out PacketInGamePurchaseAccountInfoRequest value)
    {
        value = new PacketInGamePurchaseAccountInfoRequest();

        var reader = new PacketReader(data);

        if (!value.TryRead(ref reader))
            return false;

        if (!reader.TryRead(out value.Locale))
            return false;

        return reader.RemainingLength == 0;
    }
}