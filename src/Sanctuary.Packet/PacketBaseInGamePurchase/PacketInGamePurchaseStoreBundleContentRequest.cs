using System;
using System.Collections.Generic;

using Sanctuary.Core.IO;

namespace Sanctuary.Packet;

public class PacketInGamePurchaseStoreBundleContentRequest : PacketBaseInGamePurchase, IDeserializable<PacketInGamePurchaseStoreBundleContentRequest>
{
    public new const short OpCode = 27;

    public List<RequestData> Requests = new();

    public class RequestData : IDeserializableType
    {
        public int StoreId;
        public int BundleId;

        public List<int> MarketingItemIds = [];

        public bool TryRead(ref PacketReader reader)
        {
            if (!reader.TryRead(out StoreId))
                return false;

            if (!reader.TryRead(out BundleId))
                return false;

            if (!reader.TryRead(out MarketingItemIds))
                return false;

            return true;
        }
    }

    public PacketInGamePurchaseStoreBundleContentRequest() : base(OpCode)
    {
    }

    public static bool TryDeserialize(ReadOnlySpan<byte> data, out PacketInGamePurchaseStoreBundleContentRequest value)
    {
        value = new PacketInGamePurchaseStoreBundleContentRequest();

        var reader = new PacketReader(data);

        if (!value.TryRead(ref reader))
            return false;

        if (!reader.TryReadList(out value.Requests))
            return false;

        return reader.RemainingLength == 0;
    }
}