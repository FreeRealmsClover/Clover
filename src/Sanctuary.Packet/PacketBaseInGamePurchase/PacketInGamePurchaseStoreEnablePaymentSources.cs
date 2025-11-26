using Sanctuary.Core.IO;

namespace Sanctuary.Packet;

public class PacketInGamePurchaseStoreEnablePaymentSources : PacketBaseInGamePurchase, ISerializablePacket
{
    public new const short OpCode = 33;

    public bool Sms;
    public bool Paypal;

    public PacketInGamePurchaseStoreEnablePaymentSources() : base(OpCode)
    {
    }

    public byte[] Serialize()
    {
        using var writer = new PacketWriter();

        Write(writer);

        writer.Write(Sms);
        writer.Write(Paypal);

        return writer.Buffer;
    }
}