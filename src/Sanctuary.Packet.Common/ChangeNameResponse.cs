namespace Sanctuary.Packet.Common;

public enum ChangeNameResponse
{
    MissingItem = -4,
    // ServiceUnavailable = -3,
    Requested = -2,
    // ServiceUnavailable = -1,
    ServiceUnavailable = 0,
    Pending = 1,
    Error = 2,
    PendingRename = 3,
    // Error = 4,
    AlreadyInProgress = 5
}