using Sanctuary.Core.IO;

namespace Sanctuary.Packet;

public class PacketInGamePurchaseEnableMarketplace : PacketBaseInGamePurchase, ISerializablePacket
{
    public new const short OpCode = 24;

    public bool Enabled;

    public PacketInGamePurchaseEnableMarketplace() : base(OpCode)
    {
    }

    public byte[] Serialize()
    {
        using var writer = new PacketWriter();

        Write(writer);

        writer.Write(Enabled);

        return writer.Buffer;
    }
}