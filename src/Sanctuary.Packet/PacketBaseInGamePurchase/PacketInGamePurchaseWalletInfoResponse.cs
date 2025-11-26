using Sanctuary.Core.IO;
using Sanctuary.Packet.Common.GameCommerce;

namespace Sanctuary.Packet;

public class PacketInGamePurchaseWalletInfoResponse : PacketBaseInGamePurchase, ISerializablePacket
{
    public new const short OpCode = 11;

    public int ErrorCode;

    public WalletInfo WalletInfo = new();

    public PacketInGamePurchaseWalletInfoResponse() : base(OpCode)
    {
    }

    public byte[] Serialize()
    {
        using var writer = new PacketWriter();

        Write(writer);

        writer.Write(ErrorCode);

        WalletInfo.Serialize(writer);

        return writer.Buffer;
    }
}