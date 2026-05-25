using DivineAscension.Models.Enum;

namespace DivineAscension.GUI.Models.Blessing.Tab;

/// <summary>
///     Per-deity counters rendered in the cross-deity summary strip above the deity selector.
/// </summary>
public readonly record struct DeityBlessingSummary(
    DeityDomain Domain,
    int FavorRank,
    int CurrentFavor,
    int TotalFavorEarned,
    int FavorRequiredForNext,
    bool IsMaxRank,
    int UnlockedPlayer,
    int TotalPlayer,
    int UnlockedReligion,
    int TotalReligion,
    bool IsPatron,
    bool IsActive);
