using System;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Sanctuary.Packet;
using Sanctuary.Packet.Common.Attributes;

namespace Sanctuary.Gateway.Handlers;

[PacketHandler]
public static class PacketInGamePurchaseAccountInfoRequestHandler
{
    private static ILogger _logger = null!;

    public static void ConfigureServices(IServiceProvider serviceProvider)
    {
        var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
        _logger = loggerFactory.CreateLogger(nameof(PacketBaseInGamePurchaseHandler));
    }

    public static bool HandlePacket(GatewayConnection connection, ReadOnlySpan<byte> data)
    {
        if (!PacketInGamePurchaseAccountInfoRequest.TryDeserialize(data, out var packet))
        {
            _logger.LogError("Failed to deserialize {packet}.", nameof(PacketInGamePurchaseAccountInfoRequest));
            return false;
        }

        _logger.LogTrace("Received {name} packet. ( {packet} )", nameof(PacketInGamePurchaseAccountInfoRequest), packet);

        var packetInGamePurchaseAccountInfoResponse = new PacketInGamePurchaseAccountInfoResponse
        {
            ErrorCode = 1,
            DefaultCountryCode = "US",
            DefaultCurrencyCode = "USD"
        };

        connection.SendTunneled(packetInGamePurchaseAccountInfoResponse);

        return true;
    }
}