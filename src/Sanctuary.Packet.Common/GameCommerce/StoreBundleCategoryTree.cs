using System.Collections.Generic;

using Sanctuary.Core.IO;

namespace Sanctuary.Packet.Common.GameCommerce;

public class StoreBundleCategoryTree : ISerializableType
{
    public Dictionary<int, StoreBundleCategoryNode> Categories = [];

    public bool Sorted;

    public void Serialize(PacketWriter writer)
    {
        writer.Write(Categories);

        writer.Write(Sorted);
    }
}