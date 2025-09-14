using Sanctuary.Core.IO;

namespace Sanctuary.Packet.Common;

public class FriendData : ISerializableType
{
    public NameData Name = new();

    public ulong Guid;

    public bool Online;

    public ulong Unknown;

    public bool IsLocal;
    public bool IsInStaticZone;
    public bool InEncounter;

    public float LocationX;
    public float LocationZ;

    public int Popularity;

    public FriendStatus Status = new();

    public void Serialize(PacketWriter writer)
    {
        Name.Serialize(writer);

        writer.Write(Guid);

        writer.Write(Online);

        if (Online)
        {
            writer.Write(InEncounter);

            Status.Serialize(writer);

            if (!InEncounter)
            {
                writer.Write(LocationX);
                writer.Write(LocationZ);
            }

            writer.Write(Popularity);

            writer.Write(IsLocal);
        }
        else
        {
            writer.Write(Unknown);
        }

        writer.Write(IsInStaticZone);
    }
}