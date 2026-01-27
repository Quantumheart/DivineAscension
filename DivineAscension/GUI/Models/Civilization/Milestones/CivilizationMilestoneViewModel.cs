using System.Collections.Generic;
using DivineAscension.Network;

namespace DivineAscension.GUI.Models.Civilization.Milestones;

/// <summary>
///     ViewModel for the Milestones sub-tab in the Civilization tab.
///     Contains all data needed to render milestone progress.
/// </summary>
public readonly struct CivilizationMilestoneViewModel(
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
    /// <summary>
    ///     Current civilization rank based on completed milestones
    /// </summary>
    public int Rank { get; } = rank;

    /// <summary>
    ///     All milestones with their progress
    /// </summary>
    public List<MilestoneProgressDto> Milestones { get; } = milestones;

    /// <summary>
    ///     Active bonuses from completed milestones
    /// </summary>
    public CivilizationBonusesDto Bonuses { get; } = bonuses;

    /// <summary>
    ///     Whether milestone data is currently loading
    /// </summary>
    public bool IsLoading { get; } = isLoading;

    /// <summary>
    ///     Error message if loading failed
    /// </summary>
    public string? ErrorMsg { get; } = errorMsg;

    /// <summary>
    ///     Current scroll position
    /// </summary>
    public float ScrollY { get; } = scrollY;

    /// <summary>
    ///     X coordinate of the content area
    /// </summary>
    public float X { get; } = x;

    /// <summary>
    ///     Y coordinate of the content area
    /// </summary>
    public float Y { get; } = y;

    /// <summary>
    ///     Width of the content area
    /// </summary>
    public float Width { get; } = width;

    /// <summary>
    ///     Height of the content area
    /// </summary>
    public float Height { get; } = height;
}
