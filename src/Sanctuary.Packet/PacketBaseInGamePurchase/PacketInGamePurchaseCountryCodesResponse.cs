using System.Collections.Generic;

using Sanctuary.Core.IO;
using Sanctuary.Packet.Common.GameCommerce;

namespace Sanctuary.Packet;

public class PacketInGamePurchaseCountryCodesResponse : PacketBaseInGamePurchase, ISerializablePacket
{
    public new const short OpCode = 21;

    public int ErrorCode;

    public string? Locale;

    public List<CountryCode> Countries = [];

    public PacketInGamePurchaseCountryCodesResponse() : base(OpCode)
    {
    }

    public byte[] Serialize()
    {
        using var writer = new PacketWriter();

        Write(writer);

        writer.Write(ErrorCode);

        writer.Write(Locale);

        if (ErrorCode == 1)
            writer.Write(Countries);

        return writer.Buffer;
    }
}