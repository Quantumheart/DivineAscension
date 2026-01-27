using System.Collections.Generic;

namespace DivineAscension.Models;

/// <summary>
///     DTO for deserializing milestone JSON file structure
/// </summary>
public class MilestoneFileDto
{
    /// <summary>
    ///     Schema version for migration support
    /// </summary>
    public int Version { get; set; } = 1;

    /// <summary>
    ///     List of milestone definitions
    /// </summary>
    public List<MilestoneJsonDto> Milestones { get; set; } = new();
}

/// <summary>
///     DTO for deserializing individual milestone definitions from JSON
/// </summary>
public class MilestoneJsonDto
{
    /// <summary>
    ///     Unique identifier for the milestone
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    ///     Display name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    ///     Description text
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    ///     Milestone type: "major" or "minor"
    /// </summary>
    public string Type { get; set; } = "major";

    /// <summary>
    ///     Trigger configuration
    /// </summary>
    public MilestoneTriggerDto Trigger { get; set; } = new();

    /// <summary>
    ///     Rank reward for major milestones (typically 1)
    /// </summary>
    public int RankReward { get; set; } = 0;

    /// <summary>
    ///     One-time prestige payout
    /// </summary>
    public int PrestigePayout { get; set; } = 0;

    /// <summary>
    ///     Permanent benefit configuration (optional)
    /// </summary>
    public MilestoneBenefitDto? PermanentBenefit { get; set; }

    /// <summary>
    ///     Temporary benefit configuration (optional)
    /// </summary>
    public MilestoneTemporaryBenefitDto? TemporaryBenefit { get; set; }
}

/// <summary>
///     DTO for milestone trigger configuration
/// </summary>
public class MilestoneTriggerDto
{
    /// <summary>
    ///     Trigger type: "religion_count", "domain_count", "holy_site_count",
    ///     "ritual_count", "member_count", "war_kill_count", "holy_site_tier",
    ///     "diplomatic_relationship", "all_major_milestones"
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    ///     Threshold value to trigger milestone
    /// </summary>
    public int Threshold { get; set; } = 0;
}

/// <summary>
///     DTO for milestone permanent benefit configuration
/// </summary>
public class MilestoneBenefitDto
{
    /// <summary>
    ///     Benefit type: "prestige_multiplier", "favor_multiplier", "conquest_multiplier",
    ///     "holy_site_slot", "unlock_blessing", "all_rewards_multiplier"
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    ///     Amount of the benefit (as decimal for multipliers, e.g., 0.05 = 5%)
    /// </summary>
    public float Amount { get; set; } = 0f;

    /// <summary>
    ///     Blessing ID to unlock (for unlock_blessing type)
    /// </summary>
    public string? BlessingId { get; set; }
}

/// <summary>
///     DTO for milestone temporary benefit configuration
/// </summary>
public class MilestoneTemporaryBenefitDto
{
    /// <summary>
    ///     Benefit type (same as permanent benefits)
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    ///     Amount of the benefit
    /// </summary>
    public float Amount { get; set; } = 0f;

    /// <summary>
    ///     Duration in days
    /// </summary>
    public int DurationDays { get; set; } = 0;
}
