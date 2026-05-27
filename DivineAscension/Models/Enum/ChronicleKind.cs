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
    Disbanded = 7
}
