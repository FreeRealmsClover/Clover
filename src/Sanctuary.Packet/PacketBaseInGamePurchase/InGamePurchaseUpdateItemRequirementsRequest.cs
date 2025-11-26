namespace Sanctuary.Packet;

public class InGamePurchaseUpdateItemRequirementsRequest : PacketBaseInGamePurchase
{
    public new const short OpCode = 38;

    public InGamePurchaseUpdateItemRequirementsRequest() : base(OpCode)
    {
    }
}