using System.Collections.Generic;

using Sanctuary.Core.IO;

namespace Sanctuary.Packet.Common.GameCommerce;

public class StoreBundleGroupDefinition : ISerializableType
{
    public int Id { get; set; }

    public int NameId { get; set; }
    public int DescriptionId { get; set; }

    public int Status { get; set; }

    public ImageDataDefinition Image { get; set; } = new();

    public bool IsExclusive { get; set; }

    public Dictionary<int, Entry> Entries { get; set; } = [];

    public class Entry : ISerializableType
    {
        public int StoreBundleId { get; set; }
        public int DisplayOrder { get; set; }

        public void Serialize(PacketWriter writer)
        {
            writer.Write(StoreBundleId);
            writer.Write(DisplayOrder);
        }
    }

    public void Serialize(PacketWriter writer)
    {
        writer.Write(Id);

        writer.Write(NameId);
        writer.Write(DescriptionId);

        Image.Serialize(writer);

        writer.Write(IsExclusive);

        writer.Write(Status);

        writer.Write(Entries);
    }
}