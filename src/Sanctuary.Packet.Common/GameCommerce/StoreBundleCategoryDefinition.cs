using Sanctuary.Core.IO;

namespace Sanctuary.Packet.Common.GameCommerce;

public class StoreBundleCategoryDefinition : ISerializableType
{
    public int Id { get; set; }

    public int NameId { get; set; }

    public ImageDataDefinition Image { get; set; } = new();

    public virtual void Serialize(PacketWriter writer)
    {
        writer.Write(Id);

        writer.Write(NameId);

        Image.Serialize(writer);
    }
}