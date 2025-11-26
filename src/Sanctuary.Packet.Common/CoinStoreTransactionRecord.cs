using System;

using Sanctuary.Core.IO;

namespace Sanctuary.Packet.Common;

public class CoinStoreTransactionRecord : ISerializableType
{
    // 1 - Buy
    // 2 - Sell
    public int Type;

    public ulong MerchantGuid;

    public int Id;
    public DateTimeOffset Timestamp;

    public ItemRecord ItemRecord = new();

    public int Quantity;
    public int QuantityRemaining;

    public int Price;

    public void Serialize(PacketWriter writer)
    {
        writer.Write(Type);

        writer.Write(MerchantGuid);

        writer.Write(Id);
        writer.Write(Timestamp);

        ItemRecord.Serialize(writer);

        writer.Write(Quantity);

        writer.Write(Price);

        writer.Write(QuantityRemaining);
    }
}