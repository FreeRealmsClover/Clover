using System;
using System.Collections.Generic;

using Sanctuary.Core.IO;

namespace Sanctuary.Packet;

public class CoinStoreItemDefinitionsRequestPacket : BaseCoinStorePacket, IDeserializable<CoinStoreItemDefinitionsRequestPacket>
{
    public new const short OpCode = 2;

    public List<int> ItemDefinitions = [];

    public CoinStoreItemDefinitionsRequestPacket() : base(OpCode)
    {
    }

    public static bool TryDeserialize(ReadOnlySpan<byte> data, out CoinStoreItemDefinitionsRequestPacket value)
    {
        value = new CoinStoreItemDefinitionsRequestPacket();

        var reader = new PacketReader(data);

        if (!value.TryRead(ref reader))
            return false;

        if (!reader.TryRead(out value.ItemDefinitions))
            return false;

        return reader.RemainingLength == 0;
    }
}