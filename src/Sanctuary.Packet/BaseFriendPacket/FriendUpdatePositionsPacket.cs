using System.Collections.Generic;

using Sanctuary.Core.IO;

namespace Sanctuary.Packet;

public class FriendUpdatePositionsPacket : BaseFriendPacket, ISerializablePacket
{
    public new const byte OpCode = 5;

    public class Entry : ISerializableType
    {
        public ulong Guid;

        public string? Name;

        public bool InEncounter;

        public float LocationX;
        public float LocationZ;

        public void Serialize(PacketWriter writer)
        {
            writer.Write(Guid);

            writer.Write(Name);

            writer.Write(InEncounter);

            if (!InEncounter)
            {
                writer.Write(LocationX);
                writer.Write(LocationZ);
            }
        }
    }

    public List<Entry> Entries = new();

    public FriendUpdatePositionsPacket() : base(OpCode)
    {
    }

    public byte[] Serialize()
    {
        using var writer = new PacketWriter();

        Write(writer);

        writer.Write(Entries);

        return writer.Buffer;
    }
}