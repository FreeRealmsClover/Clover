namespace Sanctuary.Packet;

public class PacketInGamePurchaseStateCodesRequest : PacketBaseInGamePurchase
{
    public new const short OpCode = 18;

    public PacketInGamePurchaseStateCodesRequest() : base(OpCode)
    {
    }
}