using System;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Sanctuary.Packet;
using Sanctuary.Packet.Common.Attributes;

namespace Sanctuary.Gateway.Handlers;

[PacketHandler]
public static class PacketInGamePurchasePreviewOrderHandler
{
    private static ILogger _logger = null!;

    public static void ConfigureServices(IServiceProvider serviceProvider)
    {
        var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
        _logger = loggerFactory.CreateLogger(nameof(PacketBaseInGamePurchaseHandler));
    }

    public static bool HandlePacket(GatewayConnection connection, ReadOnlySpan<byte> data)
    {
        if (!PacketInGamePurchasePreviewOrder.TryDeserialize(data, out var packet))
        {
            _logger.LogError("Failed to deserialize {packet}.", nameof(PacketInGamePurchasePreviewOrder));
            return false;
        }

        _logger.LogTrace("Received {name} packet. ( {packet} )", nameof(PacketInGamePurchasePreviewOrder), packet);

        var packetInGamePurchasePreviewOrderResponse = new PacketInGamePurchasePreviewOrderResponse
        {
            Result = 8
        };

        if (packet.Order.CouponCode == "OSFR")
        {
            packetInGamePurchasePreviewOrderResponse.Result = 1;
            packetInGamePurchasePreviewOrderResponse.Discount = 0;
            packetInGamePurchasePreviewOrderResponse.Total = 0;
        }

        connection.SendTunneled(packetInGamePurchasePreviewOrderResponse);

        return true;
    }
}