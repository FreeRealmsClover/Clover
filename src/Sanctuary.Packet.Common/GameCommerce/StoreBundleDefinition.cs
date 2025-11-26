using Sanctuary.Core.IO;

namespace Sanctuary.Packet.Common.GameCommerce;

public class StoreBundleDefinition : MarketingBundleDefinition
{
    public int StoreId { get; set; }

    public int CategoryGroupId { get; set; }

    public int MaxPerOrder { get; set; }

    public int DisplayOrder { get; set; }

    public int CategoryDisplayOrder { get; set; }

    public int DiscountType { get; set; }

    public int SkuId { get; set; }

    public int DiscountValue { get; set; }

    public int Unknown3 { get; set; }
    public int Unknown4 { get; set; }

    public byte TermsAndConditionsId { get; set; }
    public bool New { get; set; }
    public bool IsViewOnly { get; set; }
    public bool IsVisible { get; set; }

    public override void Serialize(PacketWriter writer)
    {
        base.Serialize(writer);

        writer.Write(StoreId);

        writer.Write(CategoryGroupId);

        writer.Write(IsVisible);

        writer.Write(MaxPerOrder);

        writer.Write(DisplayOrder);

        writer.Write(CategoryDisplayOrder);

        writer.Write(DiscountType);

        writer.Write(SkuId);

        writer.Write(DiscountValue);

        writer.Write(Unknown3);
        writer.Write(Unknown4);

        writer.Write(TermsAndConditionsId);

        writer.Write(New);
        writer.Write(IsViewOnly);
    }
}