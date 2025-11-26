using System;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Sanctuary.Packet;
using Sanctuary.Packet.Common.Attributes;

namespace Sanctuary.Gateway.Handlers;

[PacketHandler]
public static class AbilityPacketClientRequestStartAbilityHandler
{
    private static ILogger _logger = null!;

    public static void ConfigureServices(IServiceProvider serviceProvider)
    {
        var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
        _logger = loggerFactory.CreateLogger(nameof(AbilityPacketClientRequestStartAbilityHandler));
    }

    public static bool HandlePacket(GatewayConnection connection, ReadOnlySpan<byte> data)
    {
        if (!AbilityPacketClientRequestStartAbility.TryDeserialize(data, out var packet))
        {
            _logger.LogError("Failed to deserialize {packet}.", nameof(AbilityPacketClientRequestStartAbility));
            return false;
        }

        _logger.LogTrace("Received {name} packet. ( {packet} )", nameof(AbilityPacketClientRequestStartAbility), packet);

        var abilityPacketFailed = new AbilityPacketFailed
        {
            // You can't use that ability right now.
            StringId = 3079
        };

        connection.SendTunneled(abilityPacketFailed);

        return true;
    }
}