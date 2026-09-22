using System;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Sanctuary.Game;
using Sanctuary.Packet;
using Sanctuary.Packet.Common.Attributes;

namespace Sanctuary.Gateway.Handlers;

[PacketHandler]
public static class BaseTradePacketHandler
{
    private static ILogger _logger = null!;
    private static ITradeManager _tradeManager = null!;

    public static void ConfigureServices(IServiceProvider serviceProvider)
    {
        var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
        _logger = loggerFactory.CreateLogger(nameof(BaseTradePacketHandler));
        _tradeManager = serviceProvider.GetRequiredService<ITradeManager>();
    }

    public static bool HandlePacket(GatewayConnection connection, ReadOnlySpan<byte> data)
    {
        if (connection.Player is null || !BaseTradePacket.TryDeserialize(data, out var command) || command is null)
        {
            _logger.LogWarning("Rejected a malformed ordinary trade packet.");
            return false;
        }

        _tradeManager.HandleClientCommand(connection.Player, command);
        return true;
    }
}
