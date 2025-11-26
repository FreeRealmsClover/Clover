using System;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Sanctuary.Packet;
using Sanctuary.Packet.Common.Attributes;

namespace Sanctuary.Gateway.Handlers;

[PacketHandler]
public static class CoinStoreItemDynamicListUpdateRequestPacketHandler
{
    private static ILogger _logger = null!;

    public static void ConfigureServices(IServiceProvider serviceProvider)
    {
        var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
        _logger = loggerFactory.CreateLogger(nameof(CoinStoreItemDynamicListUpdateRequestPacketHandler));
    }

    public static bool HandlePacket(GatewayConnection connection, ReadOnlySpan<byte> data)
    {
        if (!CoinStoreItemDynamicListUpdateRequestPacket.TryDeserialize(data, out var packet))
        {
            _logger.LogError("Failed to deserialize {packet}.", nameof(CoinStoreItemDynamicListUpdateRequestPacket));
            return false;
        }

        _logger.LogTrace("Received {name} packet. ( {packet} )", nameof(CoinStoreItemDynamicListUpdateRequestPacket), packet);

        // TODO: Dynamic Items?
        var coinStoreItemDynamicListUpdateResponsePacket = new CoinStoreItemDynamicListUpdateResponsePacket();

        connection.SendTunneled(coinStoreItemDynamicListUpdateResponsePacket);

        return true;
    }
}