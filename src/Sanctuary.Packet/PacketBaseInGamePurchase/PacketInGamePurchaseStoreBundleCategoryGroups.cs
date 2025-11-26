using System.Collections.Generic;

using Sanctuary.Core.IO;
using Sanctuary.Packet.Common.GameCommerce;

namespace Sanctuary.Packet;

public class PacketInGamePurchaseStoreBundleCategoryGroups : PacketBaseInGamePurchase, ISerializablePacket
{
    public new const short OpCode = 6;

    public Dictionary<int, StoreBundleCategoryGroupDefinition> CategoryGroups = [];

    public PacketInGamePurchaseStoreBundleCategoryGroups() : base(OpCode)
    {
    }

    public byte[] Serialize()
    {
        using var writer = new PacketWriter();

        Write(writer);

        writer.Write(CategoryGroups);

        return writer.Buffer;
    }
}