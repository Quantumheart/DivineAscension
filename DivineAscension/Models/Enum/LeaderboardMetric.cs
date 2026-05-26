namespace DivineAscension.Models.Enum;

/// <summary>
///     The measure by which realms are weighed on the Standing of Realms
///     leaderboard. Each value is a selectable board.
/// </summary>
public enum LeaderboardMetric
{
    /// <summary>Civilization rank (Nascent → Eternal).</summary>
    Standing = 0,

    /// <summary>War kill count.</summary>
    Conquest = 1,

    /// <summary>Age since founding.</summary>
    Endurance = 2,

    /// <summary>Milestones completed.</summary>
    Deeds = 3
}
