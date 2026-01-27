namespace DivineAscension.Models;

/// <summary>
///     Defines a civilization milestone that can be unlocked through gameplay
/// </summary>
public class MilestoneDefinition
{
    /// <summary>
    ///     Unique identifier for the milestone
    /// </summary>
    public string MilestoneId { get; }

    /// <summary>
    ///     Display name of the milestone
    /// </summary>
    public string Name { get; }

    /// <summary>
    ///     Description of what the milestone represents
    /// </summary>
    public string Description { get; }

    /// <summary>
    ///     Whether this is a major milestone (grants rank) or minor (one-time payout only)
    /// </summary>
    public MilestoneType Type { get; }

    /// <summary>
    ///     Trigger condition for unlocking this milestone
    /// </summary>
    public MilestoneTrigger Trigger { get; }

    /// <summary>
    ///     Rank increase for major milestones (typically 1)
    /// </summary>
    public int RankReward { get; }

    /// <summary>
    ///     One-time prestige payout to founding religion
    /// </summary>
    public int PrestigePayout { get; }

    /// <summary>
    ///     Permanent benefit granted by this milestone (null if none)
    /// </summary>
    public MilestoneBenefit? PermanentBenefit { get; }

    /// <summary>
    ///     Temporary benefit granted by this milestone (null if none)
    /// </summary>
    public MilestoneTemporaryBenefit? TemporaryBenefit { get; }

    public MilestoneDefinition(
        string milestoneId,
        string name,
        string description,
        MilestoneType type,
        MilestoneTrigger trigger,
        int rankReward,
        int prestigePayout,
        MilestoneBenefit? permanentBenefit = null,
        MilestoneTemporaryBenefit? temporaryBenefit = null)
    {
        MilestoneId = milestoneId;
        Name = name;
        Description = description;
        Type = type;
        Trigger = trigger;
        RankReward = rankReward;
        PrestigePayout = prestigePayout;
        PermanentBenefit = permanentBenefit;
        TemporaryBenefit = temporaryBenefit;
    }
}

/// <summary>
///     Type of milestone - determines whether it advances rank
/// </summary>
public enum MilestoneType
{
    /// <summary>
    ///     Major milestone - advances civilization rank by 1
    /// </summary>
    Major,

    /// <summary>
    ///     Minor milestone - provides one-time payout without rank advancement
    /// </summary>
    Minor
}
