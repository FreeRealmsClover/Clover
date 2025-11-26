using System.Collections.Generic;

using Sanctuary.Core.IO;
using Sanctuary.Packet.Common.GameCommerce;

namespace Sanctuary.Packet;

public class PacketInGamePurchaseCurrencyCodesResponse : PacketBaseInGamePurchase, ISerializablePacket
{
    public new const short OpCode = 17;

    public int ErrorCode;

    public string? Locale;

    public Dictionary<uint, CurrencyCodeData> Currencies = [];

    public PacketInGamePurchaseCurrencyCodesResponse() : base(OpCode)
    {
    }

    public byte[] Serialize()
    {
        using var writer = new PacketWriter();

        Write(writer);

        writer.Write(ErrorCode);

        writer.Write(Locale);

        if (ErrorCode == 1)
            writer.Write(Currencies);

        return writer.Buffer;
    }
}