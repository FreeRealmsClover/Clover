using System.Collections.Generic;

using Sanctuary.Core.IO;

namespace Sanctuary.Packet.Common.GameCommerce;

public class StoreDefinition : ISerializableType
{
    public int Id { get; set; }
    public int NameId { get; set; }
    public int DescriptionId { get; set; }

    public ImageDataDefinition Image { get; set; } = new();

    public Dictionary<int, AppStoreBundleDefinition> Bundles { get; set; } = [];

    public void Serialize(PacketWriter writer)
    {
        writer.Write(Id);

        writer.Write(NameId);
        writer.Write(DescriptionId);

        Image.Serialize(writer);

        writer.Write(Bundles);
    }
}