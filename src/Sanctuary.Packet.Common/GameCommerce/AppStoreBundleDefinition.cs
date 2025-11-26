using Sanctuary.Core.IO;

namespace Sanctuary.Packet.Common.GameCommerce;

public class AppStoreBundleDefinition : StoreBundleDefinition
{
    public int MemberDiscount { get; set; }
    public int LimitedSeriesId { get; set; }
    public bool IsMembersOnly { get; set; }
    public int VipRank { get; set; }
    public bool IsMerchantOnly { get; set; }
    public int MembersOnlyPrice { get; set; }
    public int SalePrice { get; set; }
    public bool IsLimitedTime { get; set; }
    public int ForceCanPreview { get; set; }
    public int PreviewAnimId { get; set; }

    public override void Serialize(PacketWriter writer)
    {
        base.Serialize(writer);

        writer.Write(MemberDiscount);

        writer.Write(LimitedSeriesId);

        writer.Write(IsMembersOnly);

        writer.Write(VipRank);

        writer.Write(MembersOnlyPrice);

        writer.Write(SalePrice);

        writer.Write(IsMerchantOnly);
        writer.Write(IsLimitedTime);

        writer.Write(ForceCanPreview);

        writer.Write(PreviewAnimId);
    }
}