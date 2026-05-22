using System.Collections.Generic;
using DivineAscension.Network;

namespace DivineAscension.GUI.Models.Civilization.Milestones;

/// <summary>
///     ViewModel for the Chronicles ledger chapter (II.vii).
/// </summary>
public readonly struct CivilizationMilestoneViewModel(
    string realmName,
    int rank,
    List<MilestoneProgressDto> milestones,
    CivilizationBonusesDto bonuses,
    bool isLoading,
    string? errorMsg,
    float scrollY,
    float x,
    float y,
    float width,
    float height)
{
    /// <summary>Realm name displayed in the chapter title strip and prose intro.</summary>
    public string RealmName { get; } = realmName;

    public int Rank { get; } = rank;

    public List<MilestoneProgressDto> Milestones { get; } = milestones;

    public CivilizationBonusesDto Bonuses { get; } = bonuses;

    public bool IsLoading { get; } = isLoading;

    public string? ErrorMsg { get; } = errorMsg;

    public float ScrollY { get; } = scrollY;

    public float X { get; } = x;

    public float Y { get; } = y;

    public float Width { get; } = width;

    public float Height { get; } = height;
}
