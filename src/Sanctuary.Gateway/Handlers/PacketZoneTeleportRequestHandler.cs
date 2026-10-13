using System;
using System.Numerics;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Sanctuary.Game;
using Sanctuary.Packet;
using Sanctuary.Packet.Common.Attributes;

namespace Sanctuary.Gateway.Handlers;

[PacketHandler]
public static class PacketZoneTeleportRequestHandler
{
    private static ILogger _logger = null!;
    private static IResourceManager _resourceManager = null!;

    // CloverPoiSpawnOffset / CloverPoiDropOnTop tuning, in meters.
    private const float PoiClearance = 1.5f;
    private const float PoiDropHeight = 3.0f;

    // Launch pads are flat, landing on them already works.
    private const int LaunchPadIconId = 28411;

    public static void ConfigureServices(IServiceProvider serviceProvider)
    {
        var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
        _logger = loggerFactory.CreateLogger(nameof(PacketZoneTeleportRequestHandler));

        _resourceManager = serviceProvider.GetRequiredService<IResourceManager>();
    }

    public static bool HandlePacket(GatewayConnection connection, Span<byte> data)
    {
        if (!PacketZoneTeleportRequest.TryDeserialize(data, out var packet))
        {
            _logger.LogError("Failed to deserialize {packet}.", nameof(PacketZoneTeleportRequest));
            return false;
        }

        _logger.LogTrace("Received {name} packet. ( {packet} )", nameof(PacketZoneTeleportRequest), packet);

        if (!_resourceManager.PointOfInterests.TryGetValue(packet.Id, out var pointOfInterest))
        {
            _logger.LogError("Invalid point of interest.");
            return true;
        }

        var rotationX = MathF.Cos(pointOfInterest.Heading);
        var rotationZ = MathF.Sin(pointOfInterest.Heading);

        var position = pointOfInterest.SpawnPosition;
        var rotation = new Quaternion(rotationZ, 0f, rotationX, 0f);

        // CloverPoiSpawnOffset: activity points (chess tables, cooking tables, launch pads...)
        // usually have no spawn spot of their own, so the player lands inside the object.
        // CloverPoiDropOnTop: drop in from above the object rather than stepping out to the
        // side, which could land players outside walls or off ledges.
        if (pointOfInterest.NotificationType == global::Sanctuary.Packet.Common.PointOfInterestNotificationType.Local
            && pointOfInterest.IconId != LaunchPadIconId)
        {
            var dx = position.X - pointOfInterest.Position.X;
            var dz = position.Z - pointOfInterest.Position.Z;

            if (dx * dx + dz * dz < PoiClearance * PoiClearance)
            {
                position = new Vector4(
                    pointOfInterest.Position.X,
                    pointOfInterest.Position.Y + PoiDropHeight,
                    pointOfInterest.Position.Z,
                    position.W);
            }
        }

        global::Sanctuary.Game.Eye.Log("teleport", connection.Player, new { kind = "point", point = packet.Id, to = new[] { position.X, position.Y, position.Z } });

        connection.Player.UpdatePosition(position, rotation, updateZoneArea: false);
        connection.Player.ResetMovementValidation();

        var clientUpdatePacketUpdateLocation = new ClientUpdatePacketUpdateLocation
        {
            Position = position,
            Rotation = rotation,
            Teleport = true
        };

        connection.SendTunneled(clientUpdatePacketUpdateLocation);

        return true;
    }
}
