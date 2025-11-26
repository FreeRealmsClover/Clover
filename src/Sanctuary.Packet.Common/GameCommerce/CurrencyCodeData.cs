using Sanctuary.Core.IO;

namespace Sanctuary.Packet.Common.GameCommerce;

public class CurrencyCodeData : ISerializableType
{
    public string? Code;
    public string? Symbol;
    public string? Separator;
    public string? Name;

    public int MinorUnit;
    public int Decimals;

    public bool Unknown;

    public void Serialize(PacketWriter writer)
    {
        writer.Write(Code);
        writer.Write(Symbol);
        writer.Write(Separator);
        writer.Write(Name);

        writer.Write(MinorUnit);
        writer.Write(Decimals);

        writer.Write(Unknown);
    }
}