using Sanctuary.Core.IO;

namespace Sanctuary.Packet.Common;

public class ItemDefinitionMetaData : ISerializableType
{
    public int Id { get; set; }
    public int CategoryId { get; set; }
    public int TintGroupId { get; set; }
    public bool BuyDisabled { get; set; }
    public bool CoinStoreOnly { get; set; }
    public bool Hidden { get; set; }

    public void Serialize(PacketWriter writer)
    {
        writer.Write(Id);
        writer.Write(CategoryId);
        writer.Write(TintGroupId);
        writer.Write(BuyDisabled);
        writer.Write(CoinStoreOnly);
        writer.Write(Hidden);
    }
}