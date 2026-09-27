using System;

namespace Sanctuary.Database.Entities;

// Written by Robbie (the Discord bot) when a mod action needs to reach a
// player who might currently be online in-game - Robbie has no direct
// connection to the running Gateway process, so it queues a row here
// instead. Sanctuary.Gateway polls this table (see PendingModActionService)
// and applies the action live if the target is currently connected, then
// marks it processed. The actual ban/mute state itself always lives on
// DbUser.LockedUntil/MutedUntil (set directly by Robbie) - this table only
// exists to make an already-online session feel the effect immediately
// instead of waiting for their next login.
public sealed class DbPendingModAction
{
    public int Id { get; set; }

    // "Kick", "Mute", or "Ban" - kept as a string rather than an enum column
    // so a future action type never needs a migration just to widen a
    // stored enum's underlying values.
    public required string ActionType { get; set; }

    public required ulong TargetUserId { get; set; }

    // Only meaningful for Mute/Ban (mirrors whatever Robbie already wrote to
    // DbUser.MutedUntil/LockedUntil). Null for Kick, and null for a
    // permanent ban.
    public DateTimeOffset? Until { get; set; }

    // Discord username of whoever issued the action, purely for logging /
    // troubleshooting if something looks wrong later.
    public required string IssuedBy { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public bool Processed { get; set; }
    public DateTimeOffset? ProcessedAt { get; set; }
}
