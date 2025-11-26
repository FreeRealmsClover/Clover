using Sanctuary.Core.IO;

namespace Sanctuary.Packet.Common;

public class PlayerCustomizationData : ISerializableType
{
    public int Id;

    public string? StringParam;
    public int Param;

    public int ItemId;

    public void Serialize(PacketWriter writer)
    {
        writer.Write(Id);

        writer.Write(StringParam);
        writer.Write(Param);

        writer.Write(ItemId);
    }
}