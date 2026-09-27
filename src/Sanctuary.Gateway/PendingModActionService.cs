using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using Sanctuary.Database;
using Sanctuary.Database.Entities;
using Sanctuary.Game;

namespace Sanctuary.Gateway;

// Robbie (the Discord bot) has no direct connection into this running
// process, so a Discord-issued kick/mute/ban that needs to affect an
// already-connected player reaches here indirectly: Robbie writes a row to
// PendingModActions, and this service polls for unprocessed ones and
// applies them live if the target happens to be online right now. The
// persisted ban/mute state always lives on DbUser.LockedUntil /
// DbUser.MutedUntil (set directly by Robbie) - this only makes an
// already-open session feel the effect immediately instead of waiting for
// their next login/reconnect. A few seconds of delay here is fine; nothing
// about this needs to be instant.
public class PendingModActionService : BackgroundService
{
    private readonly ILogger _logger;
    private readonly IZoneManager _zoneManager;
    private readonly IDbContextFactory<DatabaseContext> _dbContextFactory;

    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(5);

    public PendingModActionService(
        ILogger<PendingModActionService> logger,
        IZoneManager zoneManager,
        IDbContextFactory<DatabaseContext> dbContextFactory)
    {
        _logger = logger;
        _zoneManager = zoneManager;
        _dbContextFactory = dbContextFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                ProcessPendingActions();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to process pending mod actions.");
            }

            try
            {
                await Task.Delay(PollInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Expected on shutdown.
            }
        }
    }

    private void ProcessPendingActions()
    {
        using DatabaseContext dbContext = _dbContextFactory.CreateDbContext();

        var pendingActions = dbContext.PendingModActions
            .Where(action => !action.Processed)
            .OrderBy(action => action.CreatedAt)
            .ToList();

        if (pendingActions.Count == 0)
            return;

        foreach (var action in pendingActions)
        {
            try
            {
                Apply(dbContext, action);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to apply pending mod action {Id} ({ActionType}) for user {UserId}.",
                    action.Id, action.ActionType, action.TargetUserId);
            }

            action.Processed = true;
            action.ProcessedAt = DateTimeOffset.UtcNow;
        }

        dbContext.SaveChanges();
    }

    private void Apply(DatabaseContext dbContext, DbPendingModAction action)
    {
        var characterNames = dbContext.Characters
            .Where(character => character.UserId == action.TargetUserId)
            .Select(character => character.FullName)
            .ToList();

        foreach (var characterName in characterNames)
        {
            if (!_zoneManager.TryGetPlayer(characterName, out var player))
                continue;

            switch (action.ActionType)
            {
                case "Kick":
                case "Ban":
                    player.Disconnect();
                    break;
                case "Mute":
                    player.MutedUntil = action.Until;
                    break;
            }

            _logger.LogInformation("Applied pending mod action {ActionType} to online player {Player} (issued by {IssuedBy}).",
                action.ActionType, characterName, action.IssuedBy);
        }
    }
}
