using System;

using Sanctuary.Core.IO;
using Sanctuary.Packet.Common;

namespace Sanctuary.Packet;

public class CoinStoreSellToClientRequestPacket : BaseCoinStorePacket, IDeserializable<CoinStoreSellToClientRequestPacket>
{
    public new const short OpCode = 4;

    public ulong MerchantGuid;
    public int MerchantUnknown;

    public ItemRecord ItemRecord = new();

    public int Quantity;

    public CoinStoreSellToClientRequestPacket() : base(OpCode)
    {
    }

    public static bool TryDeserialize(ReadOnlySpan<byte> data, out CoinStoreSellToClientRequestPacket value)
    {
        value = new CoinStoreSellToClientRequestPacket();

        var reader = new PacketReader(data);

        if (!value.TryRead(ref reader))
            return false;

        if (!reader.TryRead(out value.MerchantGuid))
            return false;

        if (!reader.TryRead(out value.MerchantUnknown))
            return false;

        if (!value.ItemRecord.TryRead(ref reader))
            return false;

        if (!reader.TryRead(out value.Quantity))
            return false;

        return reader.RemainingLength == 0;
    }
}