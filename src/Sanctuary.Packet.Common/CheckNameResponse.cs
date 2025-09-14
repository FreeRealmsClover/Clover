namespace Sanctuary.Packet.Common;

public enum CheckNameResponse
{
    NotLeader = -9,
    FoundItem = -8,
    MissingHouse = -7,
    MissingPet = -6,
    MissingItem = -5,
    AlreadyPendingRequest = -4,
    // ServiceUnavailable = -3,
    Requested = -2,
    // ServiceUnavailable = -1,
    ServiceUnavailable = 0,
    Available = 1,
    Taken = 2,
    Profane = 3,
    Invalid = 4,
    IncorrectLength = 5,
    // ServiceUnavailable = 6,
    FirstNameTooShort = 7,
    LastNameTooShort = 8,
    FirstNameTooLong = 9,
    LastNameTooLong = 10,
    IllegalCharacters = 11
}