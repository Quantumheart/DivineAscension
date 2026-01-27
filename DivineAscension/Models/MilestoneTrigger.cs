namespace DivineAscension.Models;

/// <summary>
///     Defines the trigger condition for a milestone
/// </summary>
public class MilestoneTrigger
{
    /// <summary>
    ///     Type of trigger condition
    /// </summary>
    public MilestoneTriggerType Type { get; }

    /// <summary>
    ///     Threshold value that must be reached to trigger the milestone
    /// </summary>
    public int Threshold { get; }

    public MilestoneTrigger(MilestoneTriggerType type, int threshold)
    {
        Type = type;
        Threshold = threshold;
    }
}

/// <summary>
///     Types of milestone trigger conditions
/// </summary>
public enum MilestoneTriggerType
{
    /// <summary>
    ///     Number of religions in the civilization reaches threshold
    /// </summary>
    ReligionCount,

    /// <summary>
    ///     Number of unique deity domains in the civilization reaches threshold
    /// </summary>
    DomainCount,

    /// <summary>
    ///     Total holy sites across all religions reaches threshold
    /// </summary>
    HolySiteCount,

    /// <summary>
    ///     Total completed rituals across all religions reaches threshold
    /// </summary>
    RitualCount,

    /// <summary>
    ///     Total member count across all religions reaches threshold
    /// </summary>
    MemberCount,

    /// <summary>
    ///     Cumulative PvP kills during active wars reaches threshold
    /// </summary>
    WarKillCount,

    /// <summary>
    ///     Any holy site reaches specified tier (threshold = tier number)
    /// </summary>
    HolySiteTier,

    /// <summary>
    ///     Form diplomatic relationship (NAP or Alliance) with another civilization
    /// </summary>
    DiplomaticRelationship,

    /// <summary>
    ///     Complete all other major milestones
    /// </summary>
    AllMajorMilestones
}
