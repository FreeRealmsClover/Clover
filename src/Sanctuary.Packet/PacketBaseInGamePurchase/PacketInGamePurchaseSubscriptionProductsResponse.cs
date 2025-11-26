using System.Collections.Generic;

using Sanctuary.Core.IO;
using Sanctuary.Packet.Common.GameCommerce;

namespace Sanctuary.Packet;

public class PacketInGamePurchaseSubscriptionProductsResponse : PacketBaseInGamePurchase, ISerializablePacket
{
    public new const short OpCode = 23;

    public int ErrorCode;

    public string? Locale;

    public List<MembershipSkuData> MembershipSkuList = [];

    public PacketInGamePurchaseSubscriptionProductsResponse() : base(OpCode)
    {
    }

    public byte[] Serialize()
    {
        using var writer = new PacketWriter();

        Write(writer);

        writer.Write(ErrorCode);

        writer.Write(Locale);

        if (ErrorCode == 1)
            writer.Write(MembershipSkuList);

        return writer.Buffer;
    }
}