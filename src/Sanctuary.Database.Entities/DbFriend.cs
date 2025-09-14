using System;

namespace Sanctuary.Database.Entities;

public class DbFriend
{
    public DateTimeOffset Created { get; set; } = DateTimeOffset.UtcNow;

    public ulong FriendCharacterGuid { get; set; }
    public DbCharacter FriendCharacter { get; set; } = null!;

    public ulong CharacterGuid { get; set; }
    public DbCharacter Character { get; set; } = null!;
}