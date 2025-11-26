using System;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Sanctuary.Packet;
using Sanctuary.Packet.Common.Attributes;
using Sanctuary.Packet.Common.GameCommerce;

namespace Sanctuary.Gateway.Handlers;

[PacketHandler]
public static class PacketInGamePurchaseCountryCodesRequestHandler
{
    private static ILogger _logger = null!;

    public static void ConfigureServices(IServiceProvider serviceProvider)
    {
        var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
        _logger = loggerFactory.CreateLogger(nameof(PacketBaseInGamePurchaseHandler));
    }

    public static bool HandlePacket(GatewayConnection connection, ReadOnlySpan<byte> data)
    {
        if (!PacketInGamePurchaseCountryCodesRequest.TryDeserialize(data, out var packet))
        {
            _logger.LogError("Failed to deserialize {packet}.", nameof(PacketInGamePurchaseCountryCodesRequest));
            return false;
        }

        _logger.LogTrace("Received {name} packet. ( {packet} )", nameof(PacketInGamePurchaseCountryCodesRequest), packet);

        var packetInGamePurchaseCountryCodesResponse = new PacketInGamePurchaseCountryCodesResponse
        {
            ErrorCode = 1,
            Locale = "en_US"
        };

        packetInGamePurchaseCountryCodesResponse.Countries.Add(new CountryCode
        {
            Code = "US",
            Language = "",
            Locale = "United States",
            Name = "United States",
        });

        connection.SendTunneled(packetInGamePurchaseCountryCodesResponse);

        return true;
    }
}