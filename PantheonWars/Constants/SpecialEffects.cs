using System.Diagnostics.CodeAnalysis;

namespace PantheonWars.Constants;

/// <summary>
///     Constants for special effect identifiers used in blessings.
///     These effects are not yet implemented but are reserved for future functionality.
///     Use these constants instead of hardcoded strings to prevent typos and enable refactoring.
/// </summary>
[ExcludeFromCodeCoverage]
public static class SpecialEffects
{
    #region Damage Reduction Effects

    /// <summary>
    ///     Damage Reduction: Reduce incoming damage by 10%
    /// </summary>
    public const string DamageReduction10 = "damage_reduction_10";

    #endregion

    #region Lifesteal Effects

    /// <summary>
    ///     Lifesteal: Heal for 3% of damage dealt
    /// </summary>
    public const string Lifesteal3 = "lifesteal_3";

    /// <summary>
    ///     Lifesteal: Heal for 10% of damage dealt
    /// </summary>
    public const string Lifesteal10 = "lifesteal_10";

    /// <summary>
    ///     Lifesteal: Heal for 15% of damage dealt
    /// </summary>
    public const string Lifesteal15 = "lifesteal_15";

    /// <summary>
    ///     Lifesteal: Heal for 20% of damage dealt
    /// </summary>
    public const string Lifesteal20 = "lifesteal_20";

    #endregion

    #region Damage Over Time Effects

    /// <summary>
    ///     Poison: Apply weak poison damage over time
    /// </summary>
    public const string PoisonDot = "poison_dot";

    /// <summary>
    ///     Strong Poison: Apply strong poison damage over time
    /// </summary>
    public const string PoisonDotStrong = "poison_dot_strong";

    /// <summary>
    ///     Plague Aura: Nearby enemies take periodic poison damage
    /// </summary>
    public const string PlagueAura = "plague_aura";

    #endregion

    #region Critical Strike Effects

    /// <summary>
    ///     Critical Chance: 10% chance to deal critical damage
    /// </summary>
    public const string CriticalChance10 = "critical_chance_10";

    /// <summary>
    ///     Critical Chance: 20% chance to deal critical damage
    /// </summary>
    public const string CriticalChance20 = "critical_chance_20";

    /// <summary>
    ///     Headshot Bonus: Increased damage on headshots
    /// </summary>
    public const string HeadshotBonus = "headshot_bonus";

    #endregion

    #region Combat Special Effects

    /// <summary>
    ///     AoE Cleave: Melee attacks hit multiple enemies in an arc
    /// </summary>
    public const string AoeCleave = "aoe_cleave";

    /// <summary>
    ///     Multishot: Ranged attacks fire multiple projectiles
    /// </summary>
    public const string Multishot = "multishot";

    /// <summary>
    ///     Death Aura: Enemies near you take periodic damage
    /// </summary>
    public const string DeathAura = "death_aura";

    /// <summary>
    ///     Execute Threshold: Instant kill enemies below 15% health
    /// </summary>
    public const string ExecuteThreshold = "execute_threshold";

    #endregion

    #region Utility Effects

    /// <summary>
    ///     Stealth Bonus: Reduced detection range by enemies
    /// </summary>
    public const string StealthBonus = "stealth_bonus";

    /// <summary>
    ///     Tracking Vision: Enhanced ability to track animals and players
    /// </summary>
    public const string TrackingVision = "tracking_vision";

    /// <summary>
    ///     Animal Companion: Summon a companion animal to fight alongside you
    /// </summary>
    public const string AnimalCompanion = "animal_companion";

    #endregion

    #region Lysa (Hunt & Wild) Effects

    /// <summary>
    ///     Compass Always Visible: The compass overlay is always visible
    /// </summary>
    public const string CompassAlwaysVisible = "compass_always_visible";

    /// <summary>
    ///     Rare Forage Chance: Increased chance to find rare items when foraging
    /// </summary>
    public const string RareForageChance = "rare_forage_chance";

    /// <summary>
    ///     Stealth Movement Quiet: Sneaking is quieter
    /// </summary>
    public const string StealthMovementQuiet = "stealth_movement_quiet";

    /// <summary>
    ///     Ammo Conservation: Chance to not consume arrows/spears
    /// </summary>
    public const string AmmoConservation = "ammo_conservation";
    
    #endregion

    #region Religion-Specific Effects

    /// <summary>
    ///     War Cry: Religion-wide buff that boosts damage for all members temporarily
    /// </summary>
    public const string ReligionWarCry = "religion_war_cry";

    /// <summary>
    ///     Pack Tracking: Religion members can see each other's positions and tracks
    /// </summary>
    public const string ReligionPackTracking = "religion_pack_tracking";

    /// <summary>
    ///     Death Mark: Religion-wide debuff on enemies that increases damage taken
    /// </summary>
    public const string ReligionDeathMark = "religion_death_mark";

    #endregion

    #region Khoras (Forge & Craft) Effects

    /// <summary>
    ///     Material Save: 10% chance to save materials when smithing
    /// </summary>
    public const string MaterialSaveChance10 = "material_save_chance_10";

    /// <summary>
    ///     Passive Tool Repair: Tools repair 1 durability per 5 minutes in inventory
    /// </summary>
    public const string PassiveToolRepair1Per5Min = "passive_tool_repair_1per5min";

    /// <summary>
    ///     Passive Tool Repair (Slow): Tools repair 1 durability per 10 minutes in inventory
    /// </summary>
    public const string PassiveToolRepair1Per10Min = "passive_tool_repair_1per10min";

    #endregion
}