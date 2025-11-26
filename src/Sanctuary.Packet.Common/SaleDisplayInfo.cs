using Sanctuary.Core.IO;

namespace Sanctuary.Packet.Common;

public class SaleDisplayInfo : ISerializableType
{
    public int Id;

    public int IconId;
    public int TintId;

    public int TitleId;
    public int BodyId;

    public int SecondsLeft;

    public int Unknown;

    public bool IsMembership;

    public void Serialize(PacketWriter writer)
    {
        writer.Write(Id);

        writer.Write(IconId);
        writer.Write(TintId);

        writer.Write(TitleId);
        writer.Write(BodyId);

        writer.Write(SecondsLeft);

        writer.Write(Unknown);

        writer.Write(IsMembership);
    }
}