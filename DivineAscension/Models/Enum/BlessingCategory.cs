namespace PantheonWars.Models.Enum;

/// <summary>
///     Category of blessing - used for organization and filtering
/// </summary>
public enum BlessingCategory
{
    /// <summary>
    ///     Combat-focused blessings - damage, attack speed, critical hits
    /// </summary>
    Combat,

    /// <summary>
    ///     Defensive blessings - resistances, health, shields
    /// </summary>
    Defense,

    /// <summary>
    ///     Mobility blessings - movement speed, jumping, agility
    /// </summary>
    Mobility,

    /// <summary>
    ///     Utility blessings - crafting, gathering, special mechanics
    /// </summary>
    Utility,

    /// <summary>
    ///     Economic blessings - trade, resource generation, wealth
    /// </summary>
    Economic,

    /// <summary>
    ///     Territory blessings - land control, building bonuses, area effects
    /// </summary>
    Territory
}