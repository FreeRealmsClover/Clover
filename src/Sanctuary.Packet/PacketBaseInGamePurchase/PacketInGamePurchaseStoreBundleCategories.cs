using Sanctuary.Core.IO;
using Sanctuary.Packet.Common.GameCommerce;

namespace Sanctuary.Packet;

public class PacketInGamePurchaseStoreBundleCategories : PacketBaseInGamePurchase, ISerializablePacket
{
    public new const short OpCode = 7;

    public StoreBundleCategoryTree CategoryTree = new();

    public PacketInGamePurchaseStoreBundleCategories() : base(OpCode)
    {
    }

    public byte[] Serialize()
    {
        using var writer = new PacketWriter();

        Write(writer);

        CategoryTree.Serialize(writer);

        return writer.Buffer;
    }
}