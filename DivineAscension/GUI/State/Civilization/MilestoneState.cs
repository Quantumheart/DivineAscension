using System.Collections.Generic;
using DivineAscension.GUI.Interfaces;
using DivineAscension.Network;

namespace DivineAscension.GUI.State.Civilization;

/// <summary>
///     State for milestone progress display in the civilization tab
/// </summary>
public class MilestoneState : IState
{
    /// <summary>
    ///     Civilization ID for which milestone data is loaded
    /// </summary>
    public string? CivId { get; set; }

    /// <summary>
    ///     Current civilization rank
    /// </summary>
    public int Rank { get; set; }

    /// <summary>
    ///     List of completed milestone IDs
    /// </summary>
    public List<string> CompletedMilestones { get; set; } = new();

    /// <summary>
    ///     Progress data for all milestones
    /// </summary>
    public List<MilestoneProgressDto> Progress { get; set; } = new();

    /// <summary>
    ///     Active bonuses from completed milestones
    /// </summary>
    public CivilizationBonusesDto Bonuses { get; set; } = new();

    /// <summary>
    ///     Whether milestone data is currently loading
    /// </summary>
    public bool IsLoading { get; set; }

    /// <summary>
    ///     Error message if loading failed
    /// </summary>
    public string? ErrorMsg { get; set; }

    /// <summary>
    ///     Scroll position for the milestone list
    /// </summary>
    public float ScrollY { get; set; }

    /// <summary>
    ///     Updates state from a milestone progress response packet
    /// </summary>
    public void UpdateFromPacket(MilestoneProgressResponsePacket packet)
    {
        CivId = packet.CivId;
        Rank = packet.Rank;
        CompletedMilestones = packet.CompletedMilestones;
        Progress = packet.Progress;
        Bonuses = packet.Bonuses;
        IsLoading = false;
        ErrorMsg = null;
    }

    public void Reset()
    {
        CivId = null;
        Rank = 0;
        CompletedMilestones.Clear();
        Progress.Clear();
        Bonuses = new CivilizationBonusesDto();
        IsLoading = false;
        ErrorMsg = null;
        ScrollY = 0f;
    }
}
