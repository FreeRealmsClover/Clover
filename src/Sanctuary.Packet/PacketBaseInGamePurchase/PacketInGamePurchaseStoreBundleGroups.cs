using System.Collections.Generic;

using Sanctuary.Core.IO;
using Sanctuary.Packet.Common.GameCommerce;

namespace Sanctuary.Packet;

public class PacketInGamePurchaseStoreBundleGroups : PacketBaseInGamePurchase, ISerializablePacket
{
    public new const short OpCode = 9;

    public Dictionary<int, StoreBundleGroupDefinition> BundleGroups = [];

    public PacketInGamePurchaseStoreBundleGroups() : base(OpCode)
    {
    }

    public byte[] Serialize()
    {
        using var writer = new PacketWriter();

        Write(writer);

        writer.Write(BundleGroups);

        return writer.Buffer;
    }
}