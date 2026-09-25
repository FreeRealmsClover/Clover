using System;
using System.Collections.Generic;

namespace Sanctuary.Database.Entities;

public sealed class DbUser
{
    public ulong Id { get; set; }

    public required string Username { get; set; }
    public required string Password { get; set; }

    // Nullable so the migration adding this column doesn't break on rows
    // registered before Email existed. New registrations always set it
    // (enforced by RegisterRequestModel), so in practice this is only ever
    // null for pre-existing accounts.
    public string? Email { get; set; }

    public string? Session { get; set; }
    public DateTimeOffset? SessionCreated { get; set; }

    public int MaxCharacters { get; set; }

    public bool IsMember { get; set; }
    public bool IsAdmin { get; set; }
    public bool IsMod { get; set; }

    public DateTimeOffset? LockedUntil { get; set; }
    public DateTimeOffset? MutedUntil { get; set; }

    public DateTimeOffset Created { get; set; }
    public DateTimeOffset? LastLogin { get; set; }

    // Set only by Robbie (the Discord bot) via its own verification flow in
    // #verify - never at registration. A null DiscordId means the account
    // isn't linked yet, which LoginRequestHandler checks and refuses to log
    // in for. DiscordUsername is stored purely for display/moderation
    // convenience (e.g. showing it in #status or mod tooling) - DiscordId is
    // the actual source of truth since a Discord username can change.
    public ulong? DiscordId { get; set; }
    public string? DiscordUsername { get; set; }

    public ICollection<DbCharacter> Characters { get; set; } = [];
}