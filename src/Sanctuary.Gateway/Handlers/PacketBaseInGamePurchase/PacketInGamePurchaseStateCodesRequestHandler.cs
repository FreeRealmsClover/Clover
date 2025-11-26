using System;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Sanctuary.Packet;
using Sanctuary.Packet.Common.Attributes;
using Sanctuary.Packet.Common.GameCommerce;

namespace Sanctuary.Gateway.Handlers;

[PacketHandler]
public static class PacketInGamePurchaseStateCodesRequestHandler
{
    private static ILogger _logger = null!;

    public static void ConfigureServices(IServiceProvider serviceProvider)
    {
        var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
        _logger = loggerFactory.CreateLogger(nameof(PacketBaseInGamePurchaseHandler));
    }

    public static bool HandlePacket(GatewayConnection connection)
    {
        _logger.LogTrace("Received {name} packet.", nameof(PacketInGamePurchaseStateCodesRequest));

        var packetInGamePurchaseStateCodesResponse = new PacketInGamePurchaseStateCodesResponse
        {
            ErrorCode = 1
        };

        packetInGamePurchaseStateCodesResponse.States.Add(new StateCode
        {
            Code = "WA",
            Name = "Washington"
        });

        connection.SendTunneled(packetInGamePurchaseStateCodesResponse);

        return true;
    }
}