using Sanctuary.Core.IO;

namespace Sanctuary.Packet.Common.GameCommerce;

public class StoreBundleCategoryNode : StoreBundleCategoryDefinition
{
    public int ParentId { get; set; }
    public int Count { get; set; }
    public int DisplayOrder { get; set; }

    public override void Serialize(PacketWriter writer)
    {
        base.Serialize(writer);

        writer.Write(ParentId);

        writer.Write(Count);

        writer.Write(DisplayOrder);
    }
}