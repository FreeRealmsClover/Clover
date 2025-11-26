using System;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Sanctuary.Core.Helpers;
using Sanctuary.Packet;
using Sanctuary.Packet.Common.Attributes;
using Sanctuary.Packet.Common.GameCommerce;

namespace Sanctuary.Gateway.Handlers;

[PacketHandler]
public static class PacketInGamePurchaseCurrencyCodesRequestHandler
{
    private static ILogger _logger = null!;

    public static void ConfigureServices(IServiceProvider serviceProvider)
    {
        var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
        _logger = loggerFactory.CreateLogger(nameof(PacketBaseInGamePurchaseHandler));
    }

    public static bool HandlePacket(GatewayConnection connection, ReadOnlySpan<byte> data)
    {
        if (!PacketInGamePurchaseCurrencyCodesRequest.TryDeserialize(data, out var packet))
        {
            _logger.LogError("Failed to deserialize {packet}.", nameof(PacketInGamePurchaseCurrencyCodesRequest));
            return false;
        }

        _logger.LogTrace("Received {name} packet. ( {packet} )", nameof(PacketInGamePurchaseCurrencyCodesRequest), packet);

        var packetInGamePurchaseCurrencyCodesResponse = new PacketInGamePurchaseCurrencyCodesResponse
        {
            ErrorCode = 1,
            Locale = "en_US"
        };

        var currencyCodeData = new CurrencyCodeData
        {
            Code = "USD",
            Symbol = "$",
            Separator = ".",
            Name = "US Dollar",
            MinorUnit = 100,
            Decimals = 2,
            Unknown = false,
        };

        packetInGamePurchaseCurrencyCodesResponse.Currencies.Add(
            JenkinsHelper.OneAtATimeHash(currencyCodeData.Code), currencyCodeData);

        connection.SendTunneled(packetInGamePurchaseCurrencyCodesResponse);

        return true;
    }
}