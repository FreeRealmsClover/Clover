using Sanctuary.Core.IO;
using Sanctuary.Packet.Common.GameCommerce;

namespace Sanctuary.Packet;

public class PacketInGamePurchaseStoreBundles : PacketInGamePurchaseStoreBundleBase, ISerializablePacket
{
    public new const int OpCode = 1;

    public StoreDefinition Store = new();

    public PacketInGamePurchaseStoreBundles() : base(OpCode)
    {
    }

    public byte[] Serialize()
    {
        using var writer = new PacketWriter();

        Write(writer);

        Store.Serialize(writer);

        return writer.Buffer;
    }
}