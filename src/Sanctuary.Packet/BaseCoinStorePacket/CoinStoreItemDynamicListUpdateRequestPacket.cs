using System;

using Sanctuary.Core.IO;

namespace Sanctuary.Packet;

public class CoinStoreItemDynamicListUpdateRequestPacket : BaseCoinStorePacket, IDeserializable<CoinStoreItemDynamicListUpdateRequestPacket>
{
    public new const short OpCode = 8;

    public CoinStoreItemDynamicListUpdateRequestPacket() : base(OpCode)
    {
    }

    public static bool TryDeserialize(ReadOnlySpan<byte> data, out CoinStoreItemDynamicListUpdateRequestPacket value)
    {
        value = new CoinStoreItemDynamicListUpdateRequestPacket();

        var reader = new PacketReader(data);

        if (!value.TryRead(ref reader))
            return false;

        return reader.RemainingLength == 0;
    }
}