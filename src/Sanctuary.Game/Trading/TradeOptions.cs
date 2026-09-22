using System;

namespace Sanctuary.Game.Trading;

public sealed class TradeOptions
{
    public TimeSpan InviteTimeout { get; init; } = TimeSpan.FromSeconds(60);

    public TimeSpan SessionTimeout { get; init; } = TimeSpan.FromMinutes(5);

    public TimeSpan ConfirmationDelay { get; init; } = TimeSpan.FromSeconds(10);

    public int MaximumDistinctItemsPerPlayer { get; init; } = 50;
}
