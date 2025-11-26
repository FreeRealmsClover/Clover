using Sanctuary.Core.IO;

namespace Sanctuary.Packet.Common.GameCommerce;

public class WalletInfo : ISerializableType
{
    public bool Unknown;

    public int StationCash;

    public int CreditCardId;

    public string? CurrencyCode;
    public string? Unknown5;

    public bool International;

    public void Serialize(PacketWriter writer)
    {
        writer.Write(Unknown);

        writer.Write(StationCash);

        writer.Write(CreditCardId);

        writer.Write(CurrencyCode, 8);
        writer.Write(Unknown5, 8);

        writer.Write(International);
    }
}