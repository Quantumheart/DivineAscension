namespace DivineAscension.Models.Enum;

/// <summary>
///     Result of an Add or Remove custom feast attempt (#422). Wire-stable —
///     append only.
/// </summary>
public enum FeastDayErrorCode
{
    None = 0,
    UnknownReligion = 1,
    NotFounder = 2,
    NameEmpty = 3,
    NameTooLong = 4,
    NameProfanity = 5,
    InvalidDate = 6,
    PrestigeLocked = 7,
    OnCooldown = 8,
    TooCloseToExistingFeast = 9,
    CapReached = 10,
    NotFound = 11
}
