using System;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Sanctuary.Packet;
using Sanctuary.Packet.Common.Attributes;

namespace Sanctuary.Gateway.Handlers;

[PacketHandler]
public static class CoinStoreItemDefinitionsRequestPacketHandler
{
    private static ILogger _logger = null!;

    public static void ConfigureServices(IServiceProvider serviceProvider)
    {
        var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
        _logger = loggerFactory.CreateLogger(nameof(CoinStoreItemDefinitionsRequestPacketHandler));
    }

    public static bool HandlePacket(GatewayConnection connection, ReadOnlySpan<byte> data)
    {
        if (!CoinStoreItemDefinitionsRequestPacket.TryDeserialize(data, out var packet))
        {
            _logger.LogError("Failed to deserialize {packet}.", nameof(CoinStoreItemDefinitionsRequestPacket));
            return false;
        }

        _logger.LogTrace("Received {name} packet. ( {packet} )", nameof(CoinStoreItemDefinitionsRequestPacket), packet);

        var coinStoreItemDefinitionsResponsePacket = new CoinStoreItemDefinitionsResponsePacket();

        coinStoreItemDefinitionsResponsePacket.Success = true;

        coinStoreItemDefinitionsResponsePacket.ItemDefinitions = packet.ItemDefinitions;

        connection.SendTunneled(coinStoreItemDefinitionsResponsePacket);

        return true;
    }
}