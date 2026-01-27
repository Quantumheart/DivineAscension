namespace DivineAscension.Models;

/// <summary>
///     Defines a permanent benefit granted by completing a milestone
/// </summary>
public class MilestoneBenefit
{
    /// <summary>
    ///     Type of benefit
    /// </summary>
    public MilestoneBenefitType Type { get; }

    /// <summary>
    ///     Amount of the benefit (percentage as decimal for multipliers, e.g., 0.05 = 5%)
    /// </summary>
    public float Amount { get; }

    /// <summary>
    ///     Blessing ID to unlock (only used for UnlockBlessing type)
    /// </summary>
    public string? BlessingId { get; }

    public MilestoneBenefit(MilestoneBenefitType type, float amount, string? blessingId = null)
    {
        Type = type;
        Amount = amount;
        BlessingId = blessingId;
    }
}

/// <summary>
///     Defines a temporary benefit granted by completing a milestone
/// </summary>
public class MilestoneTemporaryBenefit
{
    /// <summary>
    ///     Type of benefit
    /// </summary>
    public MilestoneBenefitType Type { get; }

    /// <summary>
    ///     Amount of the benefit (percentage as decimal for multipliers)
    /// </summary>
    public float Amount { get; }

    /// <summary>
    ///     Duration of the benefit in days
    /// </summary>
    public int DurationDays { get; }

    public MilestoneTemporaryBenefit(MilestoneBenefitType type, float amount, int durationDays)
    {
        Type = type;
        Amount = amount;
        DurationDays = durationDays;
    }
}

/// <summary>
///     Types of milestone benefits
/// </summary>
public enum MilestoneBenefitType
{
    /// <summary>
    ///     Multiplier applied to prestige generation (e.g., 0.05 = +5%)
    /// </summary>
    PrestigeMultiplier,

    /// <summary>
    ///     Multiplier applied to favor generation (e.g., 0.10 = +10%)
    /// </summary>
    FavorMultiplier,

    /// <summary>
    ///     Multiplier applied to conquest/PvP rewards (e.g., 0.05 = +5%)
    /// </summary>
    ConquestMultiplier,

    /// <summary>
    ///     Increases holy site slot cap for all religions in civilization
    /// </summary>
    HolySiteSlot,

    /// <summary>
    ///     Unlocks a civilization-wide blessing
    /// </summary>
    UnlockBlessing,

    /// <summary>
    ///     Multiplier applied to all rewards (both favor and prestige)
    /// </summary>
    AllRewardsMultiplier
}
