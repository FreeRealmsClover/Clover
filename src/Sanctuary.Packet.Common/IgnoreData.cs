using Sanctuary.Core.IO;

namespace Sanctuary.Packet.Common;

public class IgnoreData : ISerializableType
{
    public ulong Guid;

    public string? Name;

    public void Serialize(PacketWriter writer)
    {
        writer.Write(Guid);
        writer.Write(Name);
    }
}