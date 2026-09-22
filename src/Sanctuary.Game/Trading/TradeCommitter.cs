using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using Sanctuary.Core.Helpers;
using Sanctuary.Core.IO;
using Sanctuary.Database;
using Sanctuary.Database.Entities;
using Sanctuary.Game.Entities;
using Sanctuary.Packet;
using Sanctuary.Packet.Common;

namespace Sanctuary.Game.Trading;

/// <summary>
/// Persists both sides of an ordinary inventory trade as one transaction and
/// updates the two live players only after the durable commit succeeds.
/// </summary>
public sealed class TradeCommitter : ITradeCommitter
{
    private const int TradingCardItemType = 7;
    private static readonly TimeSpan MutationGuardTimeout = TimeSpan.FromSeconds(5);

    private readonly IDbContextFactory<DatabaseContext> _dbContextFactory;
    private readonly IResourceManager _resourceManager;
    private readonly ILogger<TradeCommitter> _logger;
    private readonly TradeOptions _options;

    public TradeCommitter(
        IDbContextFactory<DatabaseContext> dbContextFactory,
        IResourceManager resourceManager,
        ILogger<TradeCommitter> logger,
        TradeOptions options)
    {
        _dbContextFactory = dbContextFactory;
        _resourceManager = resourceManager;
        _logger = logger;
        _options = options;
    }

    public ValueTask<TradeCommitResult> CommitAsync(
        TradeCommitRequest request,
        CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return ValueTask.FromResult(TradeCommitResult.Fail(
                TradeCommitFailureKind.ParticipantChanged,
                "A trade participant disconnected or started zoning before commit."));
        }

        // Connection mutation guards are thread-affine. Run the complete frozen
        // transaction synchronously on a worker so no await can change threads.
        return new ValueTask<TradeCommitResult>(Task.Run(
            () => Commit(request, cancellationToken),
            CancellationToken.None));
    }

    private TradeCommitResult Commit(TradeCommitRequest request, CancellationToken cancellationToken)
    {
        if (request?.First?.Player is not Player firstPlayer
            || request.Second?.Player is not Player secondPlayer
            || request.First.OfferedItems is null
            || request.Second.OfferedItems is null
            || ReferenceEquals(firstPlayer, secondPlayer)
            || firstPlayer.Guid == secondPlayer.Guid
            || request.First.OfferedItems.Count > _options.MaximumDistinctItemsPerPlayer
            || request.Second.OfferedItems.Count > _options.MaximumDistinctItemsPerPlayer)
        {
            return TradeCommitResult.Fail(
                TradeCommitFailureKind.InvalidRequest,
                "The trade commit request is invalid.");
        }

        Player guardFirst = firstPlayer.Guid < secondPlayer.Guid ? firstPlayer : secondPlayer;
        Player guardSecond = ReferenceEquals(guardFirst, firstPlayer) ? secondPlayer : firstPlayer;

        long started = Stopwatch.GetTimestamp();
        var spinner = new SpinWait();
        while (Stopwatch.GetElapsedTime(started) < MutationGuardTimeout)
        {
            if (guardFirst.TryAcquireTradeMutationGuard(TimeSpan.Zero, out IDisposable? firstLease))
            {
                using (firstLease)
                {
                    if (guardSecond.TryAcquireTradeMutationGuard(TimeSpan.Zero, out IDisposable? secondLease))
                    {
                        using (secondLease)
                        {
                            if (cancellationToken.IsCancellationRequested)
                            {
                                return TradeCommitResult.Fail(
                                    TradeCommitFailureKind.ParticipantChanged,
                                    "A trade participant disconnected or started zoning before commit.");
                            }

                            return CommitFrozen(request, firstPlayer, secondPlayer);
                        }
                    }
                }
            }

            // Never wait for one player's guard while retaining the other's;
            // ordinary handlers may legitimately send across connections.
            spinner.SpinOnce();
        }

        return TradeCommitResult.Fail(
            TradeCommitFailureKind.MutationFreezeTimeout,
            "Timed out while freezing both trade participants.");
    }

    private TradeCommitResult CommitFrozen(
        TradeCommitRequest request,
        Player firstPlayer,
        Player secondPlayer)
    {
        if (!firstPlayer.IsConnectedForTrade || !secondPlayer.IsConnectedForTrade)
        {
            return TradeCommitResult.Fail(
                TradeCommitFailureKind.ParticipantChanged,
                "A trade participant disconnected before commit.",
                !firstPlayer.IsConnectedForTrade ? firstPlayer.Guid : secondPlayer.Guid);
        }

        if (!IsEligiblePair(firstPlayer, secondPlayer))
        {
            return TradeCommitResult.Fail(
                TradeCommitFailureKind.ParticipantChanged,
                "A trade participant left the shared world context.");
        }

        TradeTransferPlan? transferPlan = null;
        TradeCommitResult? validationFailure = null;
        bool commitAttempted = false;

        try
        {
            using DatabaseContext dbContext = _dbContextFactory.CreateDbContext();
            var executionStrategy = dbContext.Database.CreateExecutionStrategy();
            int executionAttempts = 0;

            executionStrategy.Execute(() =>
            {
                // Retrying an exchange after an ambiguous provider failure could
                // duplicate a durable transfer. The strategy wrapper is required
                // by retry-enabled providers, but a second invocation is refused.
                if (++executionAttempts != 1)
                    throw new InvalidOperationException("Automatic trade transaction retry was suppressed.");

                using var transaction = dbContext.Database.BeginTransaction(IsolationLevel.Serializable);

                ulong firstCharacterId = GuidHelper.GetPlayerId(firstPlayer.Guid);
                ulong secondCharacterId = GuidHelper.GetPlayerId(secondPlayer.Guid);

                List<DbCharacter> characters = dbContext.Characters
                    .Include(character => character.Items)
                        .ThenInclude(item => item.Profiles)
                    .AsSplitQuery()
                    .Where(character =>
                        character.Id == firstCharacterId || character.Id == secondCharacterId)
                    .ToList();

                DbCharacter? firstCharacter = characters.SingleOrDefault(
                    character => character.Id == firstCharacterId);
                DbCharacter? secondCharacter = characters.SingleOrDefault(
                    character => character.Id == secondCharacterId);

                if (firstCharacter is null || secondCharacter is null)
                {
                    validationFailure = TradeCommitResult.Fail(
                        TradeCommitFailureKind.CharacterMissing,
                        "A trade participant is missing from the database.",
                        firstCharacter is null ? firstPlayer.Guid : secondPlayer.Guid);
                    return;
                }

                TradeCommitResult firstSnapshotResult = TryBuildSnapshot(
                    firstPlayer,
                    firstCharacter,
                    out TradePartySnapshot? firstSnapshot);
                if (!firstSnapshotResult.Succeeded)
                {
                    validationFailure = firstSnapshotResult;
                    return;
                }

                TradeCommitResult secondSnapshotResult = TryBuildSnapshot(
                    secondPlayer,
                    secondCharacter,
                    out TradePartySnapshot? secondSnapshot);
                if (!secondSnapshotResult.Succeeded)
                {
                    validationFailure = secondSnapshotResult;
                    return;
                }

                var planningRequest = new TradePlanningRequest(
                    firstSnapshot!,
                    request.First.OfferedItems,
                    request.First.OfferedCoins,
                    secondSnapshot!,
                    request.Second.OfferedItems,
                    request.Second.OfferedCoins);

                TradeCommitResult planningResult = TradeTransferPlanner.TryPlan(
                    planningRequest,
                    out TradeTransferPlan? plannedTransfer);
                if (!planningResult.Succeeded)
                {
                    validationFailure = planningResult;
                    return;
                }

                transferPlan = plannedTransfer!;
                ApplyDatabasePlan(dbContext, firstCharacter, transferPlan.First);
                ApplyDatabasePlan(dbContext, secondCharacter, transferPlan.Second);

                dbContext.SaveChanges();
                commitAttempted = true;
                transaction.Commit();
            });

            if (validationFailure is not null)
                return validationFailure;
        }
        catch (Exception exception)
        {
            if (commitAttempted)
            {
                _logger.LogCritical(
                    exception,
                    "The outcome of a player-trade commit is uncertain. ( First: {FirstGuid}, Second: {SecondGuid} )",
                    firstPlayer.Guid,
                    secondPlayer.Guid);
                return TradeCommitResult.Fail(
                    TradeCommitFailureKind.CommitOutcomeUncertain,
                    "The durable outcome is uncertain; both clients must reload.",
                    durable: true);
            }

            _logger.LogError(
                exception,
                "Atomic player-trade persistence failed before commit. No runtime state was changed.");
            return TradeCommitResult.Fail(
                TradeCommitFailureKind.Persistence,
                "The trade transaction failed and was rolled back.");
        }

        try
        {
            ApplyRuntimePlan(firstPlayer, transferPlan!.First);
            ApplyRuntimePlan(secondPlayer, transferPlan.Second);
            return TradeCommitResult.Success;
        }
        catch (Exception exception)
        {
            _logger.LogCritical(
                exception,
                "A durable player trade committed, but runtime synchronization failed. ( First: {FirstGuid}, Second: {SecondGuid} )",
                firstPlayer.Guid,
                secondPlayer.Guid);
            return TradeCommitResult.Fail(
                TradeCommitFailureKind.DurableRuntimeFailure,
                "The transaction committed, but both clients require a clean reload.",
                durable: true);
        }
    }

    private TradeCommitResult TryBuildSnapshot(
        Player player,
        DbCharacter character,
        out TradePartySnapshot? snapshot)
    {
        snapshot = null;

        if (character.Coins != player.Coins)
        {
            return TradeCommitResult.Fail(
                TradeCommitFailureKind.CoinMismatch,
                "Runtime and database coin balances differ.",
                player.Guid);
        }

        Dictionary<int, DbItem> databaseItems;
        Dictionary<int, ClientItem> runtimeItems;
        try
        {
            databaseItems = character.Items.ToDictionary(item => item.Id);
            runtimeItems = player.Items.ToDictionary(item => item.Id);
        }
        catch (ArgumentException)
        {
            return TradeCommitResult.Fail(
                TradeCommitFailureKind.InventoryMismatch,
                "Runtime or database inventory contains duplicate item IDs.",
                player.Guid);
        }

        if (databaseItems.Count != runtimeItems.Count)
        {
            return TradeCommitResult.Fail(
                TradeCommitFailureKind.InventoryMismatch,
                "Runtime and database inventory sizes differ.",
                player.Guid);
        }

        var items = new List<TradeStackSnapshot>(runtimeItems.Count);
        foreach ((int itemId, ClientItem runtimeItem) in runtimeItems)
        {
            if (!databaseItems.TryGetValue(itemId, out DbItem? databaseItem)
                || databaseItem.CharacterId != character.Id
                || databaseItem.Definition != runtimeItem.Definition
                || databaseItem.Tint != runtimeItem.Tint
                || databaseItem.Count != runtimeItem.Count)
            {
                return TradeCommitResult.Fail(
                    TradeCommitFailureKind.InventoryMismatch,
                    "A runtime stack differs from its database row.",
                    player.Guid,
                    itemId,
                    runtimeItem.Definition,
                    runtimeItem.Tint);
            }

            bool known = _resourceManager.ClientItemDefinitions.TryGetValue(
                runtimeItem.Definition,
                out ClientItemDefinition? definition);

            int reservedCount = Math.Clamp(runtimeItem.ConsumedCount, 0, runtimeItem.Count);
            bool runtimeProfileReference = player.Profiles.Any(profile =>
                profile.Items.Values.Any(profileItem => profileItem.Id == itemId));
            bool databaseProfileReference = databaseItem.Profiles.Count != 0;
            if (runtimeProfileReference || databaseProfileReference)
                reservedCount = Math.Max(reservedCount, 1);

            items.Add(new TradeStackSnapshot(
                itemId,
                runtimeItem.Definition,
                runtimeItem.Tint,
                runtimeItem.Count,
                reservedCount,
                known,
                definition is null || definition.NoTrade || definition.Type == TradingCardItemType,
                PetProtected: false,
                definition?.MaxStackSize ?? 0));
        }

        snapshot = new TradePartySnapshot(player.Guid, player.Coins, items);
        return TradeCommitResult.Validated;
    }

    private static bool IsEligiblePair(Player first, Player second)
    {
        if (!ReferenceEquals(first.Zone, second.Zone)
            || first.Ignores.Any(entry => entry.Guid == second.Guid)
            || second.Ignores.Any(entry => entry.Guid == first.Guid))
        {
            return false;
        }

        return first.VisiblePlayers.TryGetValue(second.Guid, out Player? firstView)
            && ReferenceEquals(firstView, second)
            && second.VisiblePlayers.TryGetValue(first.Guid, out Player? secondView)
            && ReferenceEquals(secondView, first);
    }

    private static void ApplyDatabasePlan(
        DatabaseContext dbContext,
        DbCharacter character,
        TradePartyPlan plan)
    {
        Dictionary<int, DbItem> existing = character.Items.ToDictionary(item => item.Id);
        Dictionary<int, TradePlannedStack> final = plan.FinalItems.ToDictionary(item => item.ItemId);

        foreach (DbItem item in existing.Values.Where(item => !final.ContainsKey(item.Id)).ToArray())
            dbContext.Items.Remove(item);

        foreach (TradePlannedStack planned in final.Values)
        {
            if (existing.TryGetValue(planned.ItemId, out DbItem? item))
            {
                if (item.Definition != planned.Definition || item.Tint != planned.Tint)
                    throw new InvalidOperationException("A trade attempted to repurpose an existing item ID.");

                item.Count = planned.Count;
            }
            else
            {
                character.Items.Add(new DbItem
                {
                    Id = planned.ItemId,
                    CharacterId = character.Id,
                    Definition = planned.Definition,
                    Tint = planned.Tint,
                    Count = planned.Count
                });
            }
        }

        character.Coins = plan.FinalCoins;
    }

    private void ApplyRuntimePlan(Player player, TradePartyPlan plan)
    {
        Dictionary<int, ClientItem> existing = player.Items.ToDictionary(item => item.Id);
        Dictionary<int, TradePlannedStack> final = plan.FinalItems.ToDictionary(item => item.ItemId);

        ClientItem[] deleted = existing.Values
            .Where(item => !final.ContainsKey(item.Id))
            .ToArray();
        (ClientItem Item, TradePlannedStack Planned)[] updated = final.Values
            .Where(planned =>
                existing.TryGetValue(planned.ItemId, out ClientItem? item)
                && item.Count != planned.Count)
            .Select(planned => (existing[planned.ItemId], planned))
            .ToArray();
        TradePlannedStack[] added = final.Values
            .Where(planned => !existing.ContainsKey(planned.ItemId))
            .ToArray();
        int previousCoins = player.Coins;

        foreach (ClientItem item in deleted)
            player.Items.Remove(item);

        foreach ((ClientItem item, TradePlannedStack planned) in updated)
            item.Count = planned.Count;

        var addedItems = new List<(ClientItem Item, ClientItemDefinition Definition)>();
        foreach (TradePlannedStack planned in added)
        {
            if (!_resourceManager.ClientItemDefinitions.TryGetValue(
                    planned.Definition,
                    out ClientItemDefinition? definition))
            {
                throw new InvalidOperationException(
                    $"Item definition {planned.Definition} disappeared after commit.");
            }

            var item = new ClientItem
            {
                Id = planned.ItemId,
                Definition = planned.Definition,
                Tint = planned.Tint,
                Count = planned.Count
            };
            player.Items.Add(item);
            addedItems.Add((item, definition));
        }

        player.Coins = plan.FinalCoins;

        foreach (ClientItem item in deleted)
        {
            player.SendTunneled(new ClientUpdatePacketItemDelete
            {
                ItemGuid = item.Id
            });
        }

        foreach ((ClientItem item, _) in updated)
        {
            player.SendTunneled(new ClientUpdatePacketItemUpdate
            {
                ItemGuid = item.Id,
                Count = item.Count,
                ConsumedCount = item.ConsumedCount,
                AbilityCount = item.AbilityCount
            });
        }

        foreach ((ClientItem item, ClientItemDefinition definition) in addedItems)
        {
            using var writer = new PacketWriter();
            item.Serialize(writer);
            definition.Serialize(writer);
            player.SendTunneled(new ClientUpdatePacketItemAdd
            {
                Payload = writer.Buffer
            });
        }

        if (previousCoins != player.Coins)
        {
            player.SendTunneled(new ClientUpdatePacketCoinCount
            {
                Coins = player.Coins
            });
        }
    }
}
