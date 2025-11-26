using System;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Sanctuary.Core.IO;
using Sanctuary.Packet;
using Sanctuary.Packet.Common;
using Sanctuary.Packet.Common.Attributes;

namespace Sanctuary.Gateway.Handlers;

[PacketHandler]
public static class LobbyGameDefinitionPacketDefinitionsRequestHandler
{
    private static ILogger _logger = null!;

    public static void ConfigureServices(IServiceProvider serviceProvider)
    {
        var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
        _logger = loggerFactory.CreateLogger(nameof(LobbyGameDefinitionPacketDefinitionsRequestHandler));
    }

    public static bool HandlePacket(GatewayConnection connection)
    {
        _logger.LogTrace("Received {name} packet.", nameof(LobbyGameDefinitionPacketDefinitionsRequest));

        var lobbyGameDefinition = new LobbyGameDefinition();

        lobbyGameDefinition.GameEntries.Add(1, new LobbyGameDefinition.LobbyGameEntry
        {
            Id = 1,
            Type = 1,
            Unknown = 37,
            NameId = 3030 // Test Lobby Game
        });

        using var writer = new PacketWriter();

        lobbyGameDefinition.Serialize(writer);

        var lobbyGameDefinitionPacketDefinitionsResponse = new LobbyGameDefinitionPacketDefinitionsResponse();

        lobbyGameDefinitionPacketDefinitionsResponse.Payload = writer.Buffer;

        connection.SendTunneled(lobbyGameDefinitionPacketDefinitionsResponse);

        return true;
    }
}