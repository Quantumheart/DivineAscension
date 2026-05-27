namespace DivineAscension.Models.Enum;

/// <summary>
///     The category of a significant civilization event recorded in its chronicle.
///     Persisted as an int via ProtoBuf — append new members, never reorder.
/// </summary>
public enum ChronicleKind
{
    Founded = 0,
    ReligionJoined = 1,
    ReligionLeft = 2,
    MilestoneAwarded = 3,
    WarDeclared = 4,
    PeaceSigned = 5,
    AllianceFormed = 6,
    Disbanded = 7,

    // Religion chronicle (#373). Shared with the civ chronicle above; append only.
    FirstHolySite = 8,
    BlessingUnlocked = 9,
    Schism = 10,
    FounderTransferred = 11,
    JoinedCivilization = 12,
    LeftCivilization = 13,
    WarParticipation = 14,
    Sainted = 15
}
