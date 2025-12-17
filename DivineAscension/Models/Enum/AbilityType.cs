namespace PantheonWars.Models.Enum;

/// <summary>
///     Categories of abilities for organizational purposes
/// </summary>
public enum AbilityType
{
    /// <summary>
    ///     Abilities that provide positive effects to the caster or allies
    /// </summary>
    Buff,

    /// <summary>
    ///     Abilities that apply negative effects to enemies
    /// </summary>
    Debuff,

    /// <summary>
    ///     Abilities that deal direct damage
    /// </summary>
    Damage,

    /// <summary>
    ///     Utility abilities (mobility, vision, etc.)
    /// </summary>
    Utility,

    /// <summary>
    ///     Defensive abilities that reduce incoming damage or provide protection
    /// </summary>
    Defensive
}