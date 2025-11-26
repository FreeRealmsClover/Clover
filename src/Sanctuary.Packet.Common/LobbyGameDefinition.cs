using System.Collections.Generic;

using Sanctuary.Core.IO;

namespace Sanctuary.Packet.Common;

public class LobbyGameDefinition : ISerializableType
{
    // public Dictionary<int, ?> Unknown = [];
    // public Dictionary<int, ?> Unknown2 = [];
    // public Dictionary<int, ?> Unknown3 = [];
    // public Dictionary<int, ?> Unknown4 = [];

    public Dictionary<int, LobbyGameEntry> GameEntries = [];

    public class LobbyGameEntry : ISerializableType
    {
        public int Id;

        // 1 - Kart Race
        // 2 - Demo Derby
        // 3 - Soccer
        // 4 - Chess
        // 5 - Checkers
        public int Type;

        public int Unknown;

        public int NameId;

        public void Serialize(PacketWriter writer)
        {
            writer.Write(Id);

            writer.Write(Type);

            writer.Write(Unknown);

            writer.Write(NameId);
        }
    }

    public void Serialize(PacketWriter writer)
    {
        writer.Write(GameEntries);

        writer.Write(0); // Unknown Count
        writer.Write(0); // Unknown2 Count
        writer.Write(0); // Unknown3 Count
        writer.Write(0); // Unknown4 Count
    }
}