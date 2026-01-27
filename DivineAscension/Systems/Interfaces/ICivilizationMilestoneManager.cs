using System;
using System.Collections.Generic;
using DivineAscension.Models;

namespace DivineAscension.Systems.Interfaces;

/// <summary>
///     Manages civilization milestone progression and rewards
/// </summary>
public interface ICivilizationMilestoneManager
{
    /// <summary>
    ///     Event fired when a milestone is unlocked
    /// </summary>
    event Action<string, string>? OnMilestoneUnlocked;

    /// <summary>
    ///     Event fired when civilization rank increases
    /// </summary>
    event Action<string, int>? OnRankIncreased;

    /// <summary>
    ///     Initializes the milestone manager and subscribes to events
    /// </summary>
    void Initialize();

    /// <summary>
    ///     Cleans up event subscriptions
    /// </summary>
    void Dispose();

    /// <summary>
    ///     Checks if a specific milestone is completed for a civilization
    /// </summary>
    /// <param name="civId">Civilization ID</param>
    /// <param name="milestoneId">Milestone ID</param>
    /// <returns>True if the milestone is completed</returns>
    bool IsMilestoneCompleted(string civId, string milestoneId);

    /// <summary>
    ///     Gets the civilization's current rank
    /// </summary>
    /// <param name="civId">Civilization ID</param>
    /// <returns>Current rank (0 if not found)</returns>
    int GetCivilizationRank(string civId);

    /// <summary>
    ///     Gets all completed milestone IDs for a civilization
    /// </summary>
    /// <param name="civId">Civilization ID</param>
    /// <returns>Set of completed milestone IDs</returns>
    IReadOnlySet<string> GetCompletedMilestones(string civId);

    /// <summary>
    ///     Gets the active bonuses for a civilization based on completed milestones
    /// </summary>
    /// <param name="civId">Civilization ID</param>
    /// <returns>Computed bonuses from all completed milestones</returns>
    CivilizationBonuses GetActiveBonuses(string civId);

    /// <summary>
    ///     Gets progress information for a specific milestone
    /// </summary>
    /// <param name="civId">Civilization ID</param>
    /// <param name="milestoneId">Milestone ID</param>
    /// <returns>Progress toward the milestone, or null if not found</returns>
    MilestoneProgress? GetMilestoneProgress(string civId, string milestoneId);

    /// <summary>
    ///     Gets progress information for all milestones
    /// </summary>
    /// <param name="civId">Civilization ID</param>
    /// <returns>Dictionary of milestone ID to progress</returns>
    Dictionary<string, MilestoneProgress> GetAllMilestoneProgress(string civId);

    /// <summary>
    ///     Triggers milestone checks for a civilization
    ///     Called by other systems when state changes occur
    /// </summary>
    /// <param name="civId">Civilization ID</param>
    void CheckMilestones(string civId);

    /// <summary>
    ///     Records a PvP kill during an active war for the war_heroes milestone
    /// </summary>
    /// <param name="civId">Civilization ID of the killer</param>
    void RecordWarKill(string civId);

    /// <summary>
    ///     Gets the bonus holy site slots granted by milestones
    /// </summary>
    /// <param name="civId">Civilization ID</param>
    /// <returns>Number of bonus holy site slots</returns>
    int GetBonusHolySiteSlots(string civId);
}
