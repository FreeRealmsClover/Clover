namespace Sanctuary.Packet.Common;

public enum FriendMessageType : byte
{
    FriendAddRequestAccepted = 1,
    FriendAddRequestDeclined,
    FriendAddRequestTimedOut,
    NoTeleportToFriendCombat,
    FriendAddRequested,
    NoTeleportFriendOffline,
    TeleportedCombat,
    NoFriendSpam,
    FriendNameChanged,
    NoTeleportToPlayer,
    NoTeleportToFriendMiniGame
}