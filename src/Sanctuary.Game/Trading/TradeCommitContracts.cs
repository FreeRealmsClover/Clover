using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Sanctuary.Game.Entities;

namespace Sanctuary.Game.Trading;

public interface ITradeCommitter
{
    ValueTask<TradeCommitResult> CommitAsync(
        TradeCommitRequest request,
        CancellationToken cancellationToken = default);
}

public sealed record TradeCommitRequest(TradeCommitParty First, TradeCommitParty Second);

public sealed record TradeCommitParty(
    Player Player,
    IReadOnlyDictionary<int, int> OfferedItems,
    int OfferedCoins)
{
    public ulong PlayerGuid => Player.Guid;
}

public enum TradeCommitFailureKind
{
    None,
    Disabled,
    Configuration,
    InvalidRequest,
    MutationFreezeUnavailable,
    MutationFreezeTimeout,
    ParticipantChanged,
    CharacterMissing,
    InventoryMismatch,
    CoinMismatch,
    UnknownDefinition,
    NoTrade,
    ReservedItem,
    PetProtected,
    StackOverflow,
    CoinOverflow,
    ItemIdOverflow,
    Persistence,
    CommitOutcomeUncertain,
    DurableRuntimeFailure
}

public sealed record TradeCommitResult(
    bool Succeeded,
    bool Durable,
    TradeCommitFailureKind FailureKind,
    string Message,
    ulong? ResponsiblePlayerGuid = null,
    int? ResponsibleItemGuid = null,
    int? ResponsibleDefinition = null,
    int? ResponsibleTint = null)
{
    public static TradeCommitResult Success { get; } =
        new(true, true, TradeCommitFailureKind.None, string.Empty);

    internal static TradeCommitResult Validated { get; } =
        new(true, false, TradeCommitFailureKind.None, string.Empty);

    public static TradeCommitResult Fail(
        TradeCommitFailureKind failureKind,
        string message,
        ulong? responsiblePlayerGuid = null,
        int? responsibleItemGuid = null,
        int? responsibleDefinition = null,
        int? responsibleTint = null,
        bool durable = false) =>
        new(
            false,
            durable,
            failureKind,
            message,
            responsiblePlayerGuid,
            responsibleItemGuid,
            responsibleDefinition,
            responsibleTint);
}
