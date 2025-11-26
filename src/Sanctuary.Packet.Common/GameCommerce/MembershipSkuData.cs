using Sanctuary.Core.IO;

namespace Sanctuary.Packet.Common.GameCommerce;

public class MembershipSkuData : ISerializableType
{
    public string? Sku;

    public string? Name;
    public string? Description;

    public string? PreviewLegalText;

    public string? MarketingDescription;

    public string? ProductLegalText;

    public int PriceLocalCurrency;
    public int PriceStationCash;

    public int Unknown9;

    public bool IsVatTaxable;

    public void Serialize(PacketWriter writer)
    {
        writer.Write(Sku);

        writer.Write(Name);
        writer.Write(Description);

        writer.Write(PreviewLegalText);

        writer.Write(MarketingDescription);

        writer.Write(ProductLegalText);

        writer.Write(PriceLocalCurrency);

        writer.Write(PriceStationCash);

        writer.Write(Unknown9);

        writer.Write(IsVatTaxable);
    }
}