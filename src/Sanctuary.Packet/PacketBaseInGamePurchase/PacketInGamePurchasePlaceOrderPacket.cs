using System;

using Sanctuary.Core.IO;
using Sanctuary.Packet.Common.GameCommerce;

namespace Sanctuary.Packet;

public class PacketInGamePurchasePlaceOrderPacket : PacketBaseInGamePurchase, IDeserializable<PacketInGamePurchasePlaceOrderPacket>
{
    public new const short OpCode = 3;

    public ClientInGamePurchaseOrder Order = new();

    public PacketInGamePurchasePlaceOrderPacket() : base(OpCode)
    {
    }

    public static bool TryDeserialize(ReadOnlySpan<byte> data, out PacketInGamePurchasePlaceOrderPacket value)
    {
        value = new PacketInGamePurchasePlaceOrderPacket();

        var reader = new PacketReader(data);

        if (!value.TryRead(ref reader))
            return false;

        if (!value.Order.TryRead(ref reader))
            return false;

        return reader.RemainingLength == 0;
    }
}