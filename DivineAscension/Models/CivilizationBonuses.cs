namespace DivineAscension.Models;

/// <summary>
///     Represents the active bonuses for a civilization from completed milestones
/// </summary>
public class CivilizationBonuses
{
    /// <summary>
    ///     Multiplier for prestige generation (1.0 = no bonus, 1.05 = +5%)
    /// </summary>
    public float PrestigeMultiplier { get; init; } = 1.0f;

    /// <summary>
    ///     Multiplier for favor generation (1.0 = no bonus, 1.10 = +10%)
    /// </summary>
    public float FavorMultiplier { get; init; } = 1.0f;

    /// <summary>
    ///     Multiplier for conquest/PvP rewards (1.0 = no bonus, 1.05 = +5%)
    /// </summary>
    public float ConquestMultiplier { get; init; } = 1.0f;

    /// <summary>
    ///     Additional holy site slots granted to all religions in the civilization
    /// </summary>
    public int BonusHolySiteSlots { get; init; } = 0;

    /// <summary>
    ///     Whether there are any active bonuses
    /// </summary>
    public bool HasAnyBonus =>
        PrestigeMultiplier > 1.0f ||
        FavorMultiplier > 1.0f ||
        ConquestMultiplier > 1.0f ||
        BonusHolySiteSlots > 0;

    /// <summary>
    ///     Default bonuses (no modifiers)
    /// </summary>
    public static CivilizationBonuses None { get; } = new();
}
