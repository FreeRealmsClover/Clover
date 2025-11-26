using Sanctuary.Core.IO;

namespace Sanctuary.Packet.Common.GameCommerce;

public class ImageDataDefinition : ISerializableType
{
    public string? Image { get; set; }
    public string? Tint { get; set; }

    public void Serialize(PacketWriter writer)
    {
        writer.Write(Image);
        writer.Write(Tint);
    }
}