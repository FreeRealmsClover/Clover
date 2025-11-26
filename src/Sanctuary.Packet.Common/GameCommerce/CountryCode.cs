using Sanctuary.Core.IO;

namespace Sanctuary.Packet.Common.GameCommerce;

public class CountryCode : ISerializableType
{
    public string? Code;
    public string? Language;
    public string? Locale;
    public string? Name;

    public void Serialize(PacketWriter writer)
    {
        writer.Write(Code);
        writer.Write(Language);
        writer.Write(Locale);
        writer.Write(Name);
    }
}