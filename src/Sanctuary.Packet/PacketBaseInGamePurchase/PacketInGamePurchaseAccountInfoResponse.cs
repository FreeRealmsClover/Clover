using Sanctuary.Core.IO;

namespace Sanctuary.Packet;

public class PacketInGamePurchaseAccountInfoResponse : PacketBaseInGamePurchase, ISerializablePacket
{
    public new const short OpCode = 26;

    public int ErrorCode;

    public string? DefaultCountryCode;
    public string? DefaultCurrencyCode;

    public bool IsParentalPasswordEnabled;

    public PacketInGamePurchaseAccountInfoResponse() : base(OpCode)
    {
    }

    public byte[] Serialize()
    {
        using var writer = new PacketWriter();

        Write(writer);

        writer.Write(ErrorCode);

        writer.Write(DefaultCountryCode);

        writer.Write(DefaultCurrencyCode);
        writer.Write(IsParentalPasswordEnabled);

        return writer.Buffer;
    }
}