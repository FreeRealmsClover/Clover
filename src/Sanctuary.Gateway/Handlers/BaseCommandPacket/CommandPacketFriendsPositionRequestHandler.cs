using System;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Sanctuary.Game;
using Sanctuary.Packet;
using Sanctuary.Packet.Common.Attributes;

namespace Sanctuary.Gateway.Handlers;

[PacketHandler]
public static class CommandPacketFriendsPositionRequestHandler
{
    private static ILogger _logger = null!;
    private static IZoneManager _zoneManager = null!;

    public static void ConfigureServices(IServiceProvider serviceProvider)
    {
        var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
        _logger = loggerFactory.CreateLogger(nameof(CommandPacketFriendsPositionRequestHandler));

        _zoneManager = serviceProvider.GetRequiredService<IZoneManager>();
    }

    public static bool HandlePacket(GatewayConnection connection)
    {
        var friendUpdatePositionsPacket = new FriendUpdatePositionsPacket();

        foreach (var friend in connection.Player.Friends)
        {
            if (!_zoneManager.StartingZone.TryGetPlayer(friend.Guid, out var friendPlayer))
                continue;

            if (!friendPlayer.Visible)
                continue;

            friendUpdatePositionsPacket.Entries.Add(new FriendUpdatePositionsPacket.Entry
            {
                Guid = friendPlayer.Guid,

                LocationX = friendPlayer.Position.X,
                LocationZ = friendPlayer.Position.Z
            });
        }

        if (friendUpdatePositionsPacket.Entries.Count > 0)
            connection.SendTunneled(friendUpdatePositionsPacket);

        return true;
    }
}