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

    // Whether Email has been confirmed via the link the Website emails at
    // registration. Login is refused for any account where this is false -
    // both the website login (AuthEndpoints.LoginHandlerAsync) and the
    // actual game client login (LoginRequestHandler.cs), the same way both
    // already refuse an account with no linked Discord. Existing accounts
    // from before this feature existed are grandfathered in as confirmed
    // by the migration that adds this column (defaultValue: true) - only
    // registrations made after that point start out unconfirmed.
    public bool EmailConfirmed { get; set; }

    // Set at registration, cleared once /confirm-email succeeds. Null for
    // any account that's already confirmed, including grandfathered ones
    // that never got a token in the first place.
    public string? EmailConfirmationToken { get; set; }

    // Set by /forgot-password, cleared by /reset-password. A token past
    // PasswordResetTokenExpires is treated as invalid by
    // ResetPasswordHandlerAsync - there's no background cleanup job, an
    // expired-but-still-stored token is just never accepted.
    public string? PasswordResetToken { get; set; }
    public DateTimeOffset? PasswordResetTokenExpires { get; set; }

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
