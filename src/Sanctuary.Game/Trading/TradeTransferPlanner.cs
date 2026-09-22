using System.Collections.Generic;
using System.Linq;

namespace Sanctuary.Game.Trading;

internal readonly record struct TradeStackKey(int Definition, int Tint);

internal sealed record TradeStackSnapshot(
    int ItemId,
    int Definition,
    int Tint,
    int Count,
    int ReservedCount,
    bool DefinitionKnown,
    bool NoTrade,
    bool PetProtected,
    int MaxStackSize);

internal sealed record TradePartySnapshot(
    ulong PlayerGuid,
    int Coins,
    IReadOnlyList<TradeStackSnapshot> Items);

internal sealed record TradePlanningRequest(
    TradePartySnapshot First,
    IReadOnlyDictionary<int, int> FirstOfferedItems,
    int FirstOfferedCoins,
    TradePartySnapshot Second,
    IReadOnlyDictionary<int, int> SecondOfferedItems,
    int SecondOfferedCoins);

internal sealed record TradePlannedStack(int ItemId, int Definition, int Tint, int Count);

internal sealed record TradePartyPlan(
    ulong PlayerGuid,
    int FinalCoins,
    IReadOnlyList<TradePlannedStack> FinalItems);

internal sealed record TradeTransferPlan(TradePartyPlan First, TradePartyPlan Second);

internal static class TradeTransferPlanner
{
    internal static TradeCommitResult TryPlan(
        TradePlanningRequest request,
        out TradeTransferPlan? plan)
    {
        plan = null;
        if (request.First.PlayerGuid == request.Second.PlayerGuid)
        {
            return TradeCommitResult.Fail(
                TradeCommitFailureKind.InvalidRequest,
                "A player cannot trade with itself.",
                request.First.PlayerGuid);
        }

        TradeCommitResult firstValidation = ValidateParty(
            request.First,
            request.FirstOfferedItems,
            request.FirstOfferedCoins);
        if (!firstValidation.Succeeded)
        {
            return firstValidation;
        }

        TradeCommitResult secondValidation = ValidateParty(
            request.Second,
            request.SecondOfferedItems,
            request.SecondOfferedCoins);
        if (!secondValidation.Succeeded)
        {
            return secondValidation;
        }

        if (!TryCalculateCoins(
                request.First,
                request.FirstOfferedCoins,
                request.SecondOfferedCoins,
                out int firstCoins,
                out TradeCommitResult? firstCoinFailure))
        {
            return firstCoinFailure!;
        }

        if (!TryCalculateCoins(
                request.Second,
                request.SecondOfferedCoins,
                request.FirstOfferedCoins,
                out int secondCoins,
                out TradeCommitResult? secondCoinFailure))
        {
            return secondCoinFailure!;
        }

        Dictionary<TradeStackKey, long> firstOutgoing = QuantitiesByKey(
            request.First,
            request.FirstOfferedItems);
        Dictionary<TradeStackKey, long> secondOutgoing = QuantitiesByKey(
            request.Second,
            request.SecondOfferedItems);

        TradeCommitResult firstPlanResult = TryPlanParty(
            request.First,
            firstCoins,
            firstOutgoing,
            secondOutgoing,
            request.Second,
            out TradePartyPlan? firstPlan);
        if (!firstPlanResult.Succeeded)
        {
            return firstPlanResult;
        }

        TradeCommitResult secondPlanResult = TryPlanParty(
            request.Second,
            secondCoins,
            secondOutgoing,
            firstOutgoing,
            request.First,
            out TradePartyPlan? secondPlan);
        if (!secondPlanResult.Succeeded)
        {
            return secondPlanResult;
        }

        plan = new TradeTransferPlan(firstPlan!, secondPlan!);
        return TradeCommitResult.Validated;
    }

    private static TradeCommitResult ValidateParty(
        TradePartySnapshot party,
        IReadOnlyDictionary<int, int> offeredItems,
        int offeredCoins)
    {
        if (offeredCoins < 0 || offeredCoins > party.Coins)
        {
            return TradeCommitResult.Fail(
                TradeCommitFailureKind.CoinMismatch,
                "The coin offer no longer matches the player's balance.",
                party.PlayerGuid);
        }

        var itemIds = new HashSet<int>();
        var keys = new HashSet<TradeStackKey>();
        foreach (TradeStackSnapshot item in party.Items)
        {
            var key = new TradeStackKey(item.Definition, item.Tint);
            if (item.ItemId <= 0
                || item.Count <= 0
                || item.ReservedCount < 0
                || item.ReservedCount > item.Count
                || !itemIds.Add(item.ItemId)
                || !keys.Add(key))
            {
                return TradeCommitResult.Fail(
                    TradeCommitFailureKind.InventoryMismatch,
                    "The inventory contains an invalid or duplicate stack.",
                    party.PlayerGuid,
                    item.ItemId,
                    item.Definition,
                    item.Tint);
            }

            if (!item.DefinitionKnown)
            {
                return TradeCommitResult.Fail(
                    TradeCommitFailureKind.UnknownDefinition,
                    "An inventory stack has no client item definition.",
                    party.PlayerGuid,
                    item.ItemId,
                    item.Definition,
                    item.Tint);
            }
        }

        foreach ((int itemId, int count) in offeredItems)
        {
            TradeStackSnapshot? item = party.Items.FirstOrDefault(candidate => candidate.ItemId == itemId);
            if (item is null || count <= 0 || count > item.Count)
            {
                return TradeCommitResult.Fail(
                    TradeCommitFailureKind.InventoryMismatch,
                    "An offered stack no longer matches the inventory.",
                    party.PlayerGuid,
                    itemId,
                    item?.Definition,
                    item?.Tint);
            }

            if (item.NoTrade)
            {
                return TradeCommitResult.Fail(
                    TradeCommitFailureKind.NoTrade,
                    "The offered stack is not eligible for ordinary player trading.",
                    party.PlayerGuid,
                    item.ItemId,
                    item.Definition,
                    item.Tint);
            }

            if (item.PetProtected)
            {
                return TradeCommitResult.Fail(
                    TradeCommitFailureKind.PetProtected,
                    "The offered stack is protected pet state.",
                    party.PlayerGuid,
                    item.ItemId,
                    item.Definition,
                    item.Tint);
            }

            int available = item.Count - item.ReservedCount;
            if (count > available)
            {
                return TradeCommitResult.Fail(
                    TradeCommitFailureKind.ReservedItem,
                    "The offered quantity includes reserved units.",
                    party.PlayerGuid,
                    item.ItemId,
                    item.Definition,
                    item.Tint);
            }
        }

        return TradeCommitResult.Validated;
    }

    private static bool TryCalculateCoins(
        TradePartySnapshot party,
        int outgoing,
        int incoming,
        out int finalCoins,
        out TradeCommitResult? failure)
    {
        long calculated = (long)party.Coins - outgoing + incoming;
        if (calculated is < 0 or > int.MaxValue)
        {
            finalCoins = default;
            failure = TradeCommitResult.Fail(
                TradeCommitFailureKind.CoinOverflow,
                "The resulting coin balance is outside the supported range.",
                party.PlayerGuid);
            return false;
        }

        finalCoins = (int)calculated;
        failure = null;
        return true;
    }

    private static Dictionary<TradeStackKey, long> QuantitiesByKey(
        TradePartySnapshot party,
        IReadOnlyDictionary<int, int> offeredItems)
    {
        Dictionary<int, TradeStackSnapshot> byId = party.Items.ToDictionary(item => item.ItemId);
        var result = new Dictionary<TradeStackKey, long>();
        foreach ((int itemId, int count) in offeredItems)
        {
            TradeStackSnapshot item = byId[itemId];
            var key = new TradeStackKey(item.Definition, item.Tint);
            result.TryGetValue(key, out long current);
            result[key] = checked(current + count);
        }

        return result;
    }

    private static TradeCommitResult TryPlanParty(
        TradePartySnapshot party,
        int finalCoins,
        IReadOnlyDictionary<TradeStackKey, long> outgoing,
        IReadOnlyDictionary<TradeStackKey, long> incoming,
        TradePartySnapshot incomingSource,
        out TradePartyPlan? partyPlan)
    {
        partyPlan = null;
        Dictionary<TradeStackKey, TradeStackSnapshot> existing =
            party.Items.ToDictionary(item => new TradeStackKey(item.Definition, item.Tint));
        var allKeys = new HashSet<TradeStackKey>(existing.Keys);
        allKeys.UnionWith(outgoing.Keys);
        allKeys.UnionWith(incoming.Keys);

        int nextItemId = party.Items.Count == 0 ? 0 : party.Items.Max(item => item.ItemId);
        var finalItems = new List<TradePlannedStack>(allKeys.Count);
        foreach (TradeStackKey key in allKeys.OrderBy(key => key.Definition).ThenBy(key => key.Tint))
        {
            existing.TryGetValue(key, out TradeStackSnapshot? original);
            outgoing.TryGetValue(key, out long outgoingCount);
            incoming.TryGetValue(key, out long incomingCount);
            long originalCount = original?.Count ?? 0;
            long finalCount = originalCount - outgoingCount + incomingCount;
            if (finalCount < 0 || finalCount > int.MaxValue)
            {
                return StackOverflow(party.PlayerGuid, key, original?.ItemId);
            }

            if (finalCount == 0)
            {
                continue;
            }

            TradeStackSnapshot? definitionSource = original
                ?? incomingSource.Items.FirstOrDefault(
                    item => item.Definition == key.Definition && item.Tint == key.Tint);
            int maxStackSize = definitionSource?.MaxStackSize ?? -1;

            if (maxStackSize > 0
                && finalCount > maxStackSize
                && finalCount > originalCount)
            {
                return StackOverflow(party.PlayerGuid, key, original?.ItemId);
            }

            int itemId;
            if (original is not null)
            {
                itemId = original.ItemId;
            }
            else
            {
                if (nextItemId == int.MaxValue)
                {
                    return TradeCommitResult.Fail(
                        TradeCommitFailureKind.ItemIdOverflow,
                        "The recipient has no available character-local item ID.",
                        party.PlayerGuid,
                        responsibleDefinition: key.Definition,
                        responsibleTint: key.Tint);
                }

                itemId = ++nextItemId;
            }

            finalItems.Add(new TradePlannedStack(itemId, key.Definition, key.Tint, (int)finalCount));
        }

        partyPlan = new TradePartyPlan(
            party.PlayerGuid,
            finalCoins,
            finalItems.OrderBy(item => item.ItemId).ToArray());
        return TradeCommitResult.Validated;
    }

    private static TradeCommitResult StackOverflow(
        ulong playerGuid,
        TradeStackKey key,
        int? itemId) =>
        TradeCommitResult.Fail(
            TradeCommitFailureKind.StackOverflow,
            "Definition and tint folding would exceed the recipient's stack limit.",
            playerGuid,
            itemId,
            key.Definition,
            key.Tint);
}
