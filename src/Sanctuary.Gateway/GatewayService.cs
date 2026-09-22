using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Sanctuary.Core.Configuration;
using Sanctuary.Database;
using Sanctuary.Game;
using Sanctuary.Packet.Common.Extensions;
using Sanctuary.Scripting;
using Sanctuary.UdpLibrary.Enumerations;

namespace Sanctuary.Gateway;

public class GatewayService : BackgroundService
{
    private readonly ILogger _logger;
    private readonly LoginClient _client;
    private readonly GatewayServer _server;
    private readonly IZoneManager _zoneManager;
    private readonly GatewayServerOptions _options;
    private readonly IServiceProvider _serviceProvider;
    private readonly IResourceManager _resourceManager;
    private readonly IScriptManager _scriptManager;
    private readonly IInteractionManager _interactionManager;
    private readonly IChatCommandManager _chatCommandManager;
    private readonly IHostApplicationLifetime _hostApplicationLifetime;
    private readonly IDbContextFactory<DatabaseContext> _dbContextFactory;

    private Timer? _playerCountTimer;
    private static readonly TimeSpan PlayerCountInterval = TimeSpan.FromSeconds(60);

    public GatewayService(
        ILogger<GatewayService> logger,
        LoginClient client,
        GatewayServer server,
        IOptions<GatewayServerOptions> options,
        IZoneManager zoneManager,
        IServiceProvider serviceProvider,
        IResourceManager resourceManager,
        IScriptManager scriptManager,
        IInteractionManager interactionManager,
        IChatCommandManager ChatCommandManager,
        IDbContextFactory<DatabaseContext> dbContextFactory,
        IHostApplicationLifetime hostApplicationLifetime)
    {
        _logger = logger;
        _client = client;
        _server = server;
        _options = options.Value;
        _zoneManager = zoneManager;
        _serviceProvider = serviceProvider;
        _resourceManager = resourceManager;
        _scriptManager = scriptManager;
        _interactionManager = interactionManager;
        _chatCommandManager = ChatCommandManager;
        _dbContextFactory = dbContextFactory;
        _hostApplicationLifetime = hostApplicationLifetime;
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _server.OnStopping();

        _playerCountTimer?.Dispose();
        _playerCountTimer = null;

        await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
    }

    // Writes the current online-player count to Logs/player_count.json every
    // PlayerCountInterval, so external tools (e.g. the Discord bots) can read
    // it without needing a live connection into the game server.
    private void WritePlayerCount(object? state)
    {
        try
        {
            var count = _zoneManager.StartingZone.Players.Count();

            var payload = JsonSerializer.Serialize(new
            {
                player_count = count,
                updated_at = DateTime.UtcNow.ToString("o")
            });

            var directory = Path.Combine(AppContext.BaseDirectory, "Logs");
            Directory.CreateDirectory(directory);

            var path = Path.Combine(directory, "player_count.json");
            var tempPath = path + ".tmp";

            File.WriteAllText(tempPath, payload);
            File.Move(tempPath, path, overwrite: true);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to write player_count.json.");
        }
    }

    protected override Task ExecuteAsync(CancellationToken cancellationToken)
    {
        // Check we can connect to the database.
        using var dbContext = _dbContextFactory.CreateDbContext();

        if (!dbContext.Database.CanConnect())
        {
            _logger.LogCritical("Cannot start {server}, failed to connect to database.", nameof(GatewayServer));

            _hostApplicationLifetime.StopApplication();

            return Task.CompletedTask;
        }

        // Load resources.
        if (!_resourceManager.Load())
        {
            _logger.LogCritical("Cannot start {server}, failed to load resources.", nameof(GatewayServer));

            _hostApplicationLifetime.StopApplication();

            return Task.CompletedTask;
        }

        // Load scripts.
        if (!_scriptManager.Load())
        {
            _logger.LogCritical("Cannot start {server}, failed to load scripts.", nameof(GatewayServer));

            _hostApplicationLifetime.StopApplication();

            return Task.CompletedTask;
        }

        // Load zones.
        if (!_zoneManager.Load())
        {
            _logger.LogCritical("Cannot start {server}, failed to load zones.", nameof(GatewayServer));

            _hostApplicationLifetime.StopApplication();

            return Task.CompletedTask;
        }

        // Load interactions.
        if (!_interactionManager.Load())
        {
            _logger.LogCritical("Cannot start {server}, failed to load interactions.", nameof(GatewayServer));

            _hostApplicationLifetime.StopApplication();

            return Task.CompletedTask;
        }

        // Load commands.
        if (!_chatCommandManager.Load())
        {
            _logger.LogCritical("Cannot start {server}, failed to load commands.", nameof(GatewayServer));

            _hostApplicationLifetime.StopApplication();

            return Task.CompletedTask;
        }

        // Register services on static packet handlers.
        _serviceProvider.ConfigurePacketHandlers();

        // Connect to the Login Server.

        var clientConnection = _client.EstablishConnection(_options.LoginGatewayAddress);

        if (clientConnection is null)
        {
            _logger.LogCritical("Cannot start {client}. Failed to create client connection.", nameof(LoginClient));

            _hostApplicationLifetime.StopApplication();

            return Task.CompletedTask;
        }

        _logger.LogInformation($"{nameof(GatewayServer)} started and is listening on port '{_options.Port}'.");

        _server.OnStarted();

        _playerCountTimer = new Timer(WritePlayerCount, null, TimeSpan.Zero, PlayerCountInterval);

        // Main server loop.
        while (!cancellationToken.IsCancellationRequested && clientConnection.Status != Status.Disconnected)
        {
            var hadData = false;
            hadData |= _server.GiveTime();
            hadData |= _client.GiveTime();

            if (!hadData)
                Thread.Sleep(1);
        }

        return Task.CompletedTask;
    }
}