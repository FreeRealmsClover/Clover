using Sanctuary.Core.IO;

namespace Sanctuary.Packet.Common.GameCommerce;

public class StateCode : ISerializableType
{
    public string? Code;
    public string? Name;

    public void Serialize(PacketWriter writer)
    {
        writer.Write(Code);
        writer.Write(Name);
    }
}