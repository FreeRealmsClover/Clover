using Sanctuary.Core.IO;

namespace Sanctuary.Packet;

public class PacketInGamePurchaseStoreBundleBase : PacketBaseInGamePurchase
{
    public new const short OpCode = 5;

    private int SubOpCode;

    public int StoreId;

    public PacketInGamePurchaseStoreBundleBase(int subOpCode) : base(OpCode)
    {
        SubOpCode = subOpCode;
    }

    public override void Write(PacketWriter writer)
    {
        base.Write(writer);

        writer.Write(SubOpCode);

        writer.Write(StoreId);
    }
}