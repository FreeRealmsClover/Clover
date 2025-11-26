using System.Collections.Generic;

using Sanctuary.Core.IO;

namespace Sanctuary.Packet.Common.GameCommerce;

public class StoreBundleCategoryGroupDefinition : ISerializableType
{
    public int Id { get; set; }

    public List<int> CategoryIds { get; set; } = [];

    public void Serialize(PacketWriter writer)
    {
        writer.Write(Id);

        writer.Write(CategoryIds);
    }
}