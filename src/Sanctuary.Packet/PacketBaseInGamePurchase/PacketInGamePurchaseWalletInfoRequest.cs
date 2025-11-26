namespace Sanctuary.Packet;

public class PacketInGamePurchaseWalletInfoRequest : PacketBaseInGamePurchase
{
    public new const short OpCode = 10;

    public PacketInGamePurchaseWalletInfoRequest() : base(OpCode)
    {
    }
}