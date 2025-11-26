using System.Collections.Generic;

using Sanctuary.Core.IO;

namespace Sanctuary.Packet;

public class CoinStoreItemDefinitionsResponsePacket : BaseCoinStorePacket, ISerializablePacket
{
    public new const short OpCode = 3;

    public bool Success;

    public List<int> ItemDefinitions = [];

    public CoinStoreItemDefinitionsResponsePacket() : base(OpCode)
    {
    }

    public byte[] Serialize()
    {
        using var writer = new PacketWriter();

        Write(writer);

        writer.Write(Success);
        writer.Write(ItemDefinitions);

        return writer.Buffer;
    }
}