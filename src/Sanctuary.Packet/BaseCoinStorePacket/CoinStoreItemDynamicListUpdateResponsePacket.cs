using System.Collections.Generic;

using Sanctuary.Core.IO;
using Sanctuary.Packet.Common;

namespace Sanctuary.Packet;

public class CoinStoreItemDynamicListUpdateResponsePacket : BaseCoinStorePacket, ISerializablePacket
{
    public new const short OpCode = 9;

    public Dictionary<int, ItemDefinitionMetaData> DynamicItems = new();

    public CoinStoreItemDynamicListUpdateResponsePacket() : base(OpCode)
    {
    }

    public byte[] Serialize()
    {
        using var writer = new PacketWriter();

        Write(writer);

        writer.Write(DynamicItems);

        return writer.Buffer;
    }
}