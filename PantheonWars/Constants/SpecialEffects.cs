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

    #region Lysa (Hunt & Wild) Effects

    /// <summary>
    ///     Rare Forage Chance: Increased chance to find rare items when foraging
    /// </summary>
    public const string RareForageChance = "rare_forage_chance";

    /// <summary>
    ///     Food Spoilage Reduction: Food spoils slower in inventory
    /// </summary>
    public const string FoodSpoilageReduction = "food_spoilage_reduction";

    /// <summary>
    ///     Temperature Resistance: Passive body temperature regulation
    /// </summary>
    public const string TemperatureResistance = "temperature_resistance";
    
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

    #region Aethra (Agriculture & Light) Effects

    /// <summary>
    ///     Light Warmth Bonus: Light sources provide increased warmth radius
    /// </summary>
    public const string LightWarmthBonus = "light_warmth_bonus";

    /// <summary>
    ///     Never Malnourished: Immune to malnutrition penalties
    /// </summary>
    public const string NeverMalnourished = "never_malnourished";

    /// <summary>
    ///     Blessed Meals: Can create meals with powerful temporary buffs
    /// </summary>
    public const string BlessedMeals = "blessed_meals";

    /// <summary>
    ///     Temporary Health Buff: Meals provide +5% max health temporarily
    /// </summary>
    public const string TempHealthBuff5 = "temp_health_buff_5";

    /// <summary>
    ///     Sacred Granary: Religion can build special food storage structure
    /// </summary>
    public const string SacredGranary = "sacred_granary";

    #endregion

    #region Gaia (Earth & Stone) Effects

    /// <summary>
    ///     Overburdened Immunity: Immune to slowness from being overburdened (first tier)
    /// </summary>
    public const string OverburdenedImmunity = "overburdened_immunity";

    #endregion
}