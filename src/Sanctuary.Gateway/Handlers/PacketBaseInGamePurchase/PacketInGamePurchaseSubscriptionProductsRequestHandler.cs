using System;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Sanctuary.Packet;
using Sanctuary.Packet.Common.Attributes;
using Sanctuary.Packet.Common.GameCommerce;

namespace Sanctuary.Gateway.Handlers;

[PacketHandler]
public static class PacketInGamePurchaseSubscriptionProductsRequestHandler
{
    private static ILogger _logger = null!;

    public static void ConfigureServices(IServiceProvider serviceProvider)
    {
        var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
        _logger = loggerFactory.CreateLogger(nameof(PacketBaseInGamePurchaseHandler));
    }

    public static bool HandlePacket(GatewayConnection connection, ReadOnlySpan<byte> data)
    {
        if (!PacketInGamePurchaseSubscriptionProductsRequest.TryDeserialize(data, out var packet))
        {
            _logger.LogError("Failed to deserialize {packet}.", nameof(PacketInGamePurchaseSubscriptionProductsRequest));
            return false;
        }

        _logger.LogTrace("Received {name} packet. ( {packet} )", nameof(PacketInGamePurchaseSubscriptionProductsRequest), packet);

        var packetInGamePurchaseSubscriptionProductsResponse = new PacketInGamePurchaseSubscriptionProductsResponse
        {
            ErrorCode = 1,
            Locale = packet.Locale,
            MembershipSkuList =
            {
                new MembershipSkuData
                {
                    Sku = "FRREL-SU-IR0309-MEMBER-01MON",
                    Name = "1 Month",
                    Description = "Recurring Free Realms Membership",
                    PriceLocalCurrency = 499,
                    PriceStationCash = 499,
                    IsVatTaxable = true
                },
                new MembershipSkuData
                {
                    Sku = "FRREL-SU-IR0309-MEMBER-03MON",
                    Name = "3 Month",
                    Description = "Recurring Free Realms Membership",
                    MarketingDescription = "10% Savings",
                    PriceLocalCurrency = 1299,
                    PriceStationCash = 1299,
                    IsVatTaxable = true
                },
                new MembershipSkuData
                {
                    Sku = "FRREL-SU-IR0309-MEMBER-06MON",
                    Name = "6 Month",
                    Description = "Recurring Free Realms Membership",
                    PriceLocalCurrency = 2450,
                    PriceStationCash = 2450,
                    IsVatTaxable = true
                },
                new MembershipSkuData
                {
                    Sku = "FRREL-SU-IR0810-NEWMBR-LIFE",
                    Name = "Lifetime",
                    Description = "Best Value!",
                    MarketingDescription = "Best Value!",
                    ProductLegalText = "Bill me now to upgrade my subscription to a Lifetime Membership. As a reward for upgrading to Lifetime Membership, SOE will convert the remaining time on your existing subscription into Station Cash and these funds will be available in your wallet upon completion of the transaction.",
                    PreviewLegalText = "SOE will credit your wallet with Station Cash based on the remaining of the membership time attached to your subscription.",
                    PriceLocalCurrency = 3499,
                    PriceStationCash = 3499,
                    IsVatTaxable = true
                },
                new MembershipSkuData
                {
                    Sku = "FRREL-SU-IR0309-MEMBER-12MON",
                    Name = "12 Month",
                    Description = "Recurring Free Realms Membership",
                    PriceLocalCurrency = 3950,
                    PriceStationCash = 3950,
                    IsVatTaxable = true
                }
            }
        };

        connection.SendTunneled(packetInGamePurchaseSubscriptionProductsResponse);

        return true;
    }
}