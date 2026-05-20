using System.Collections.Generic;
using DivineAscension.Models.Enum;

namespace DivineAscension.GUI.Models.Hud;

/// <summary>
///     Per-deity slice rendered as a single bar in the rank HUD.
/// </summary>
public readonly record struct DeityRankSlice(
    DeityDomain Domain,
    string RankName,
    string NextRankName,
    int TotalFavorEarned,
    int FavorRequiredForNext,
    float Progress,
    bool IsMaxRank,
    bool IsPatron);

/// <summary>
///     Immutable view model for the rank progress HUD overlay (multi-deity).
/// </summary>
public readonly record struct RankProgressHudViewModel(
    IReadOnlyList<DeityRankSlice> Deities,
    string PrestigeRankName,
    string NextPrestigeRankName,
    int CurrentPrestige,
    int PrestigeRequiredForNext,
    float PrestigeProgress,
    bool IsPrestigeMaxRank,
    int SpendableFavor,
    DeityDomain PatronDomain,
    bool HasReligion,
    bool CollapsedToPatron,
    float ScreenWidth,
    float ScreenHeight,
    bool IsVisible);
