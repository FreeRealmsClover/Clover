using Sanctuary.Core.IO;

namespace Sanctuary.Packet;

public class PacketInGamePurchasePreviewOrderResponse : PacketBaseInGamePurchase, ISerializablePacket
{
    public new const short OpCode = 2;

    public int OrderTrackingId;

    // 2, 4, 6, 7 - marketplace_error_order_failure
    // 5 - marketplace_error_balance_not_enough
    // 8 - marketplace_error_invalid_coupon
    // X - marketplace_error_request_unavailable
    public int Result;

    public int Discount;

    public int Total;

    public PacketInGamePurchasePreviewOrderResponse() : base(OpCode)
    {
    }

    public byte[] Serialize()
    {
        using var writer = new PacketWriter();

        Write(writer);

        writer.Write(OrderTrackingId);

        writer.Write(Result);
        writer.Write(Discount);
        writer.Write(Total);

        return writer.Buffer;
    }
}