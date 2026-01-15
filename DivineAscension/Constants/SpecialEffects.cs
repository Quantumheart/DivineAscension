using System.Diagnostics.CodeAnalysis;

namespace DivineAscension.Constants;

/// <summary>
///     Constants for special effect identifiers used in blessings.
///     These effects are not yet implemented but are reserved for future functionality.
///     Use these constants instead of hardcoded strings to prevent typos and enable refactoring.
/// </summary>
[ExcludeFromCodeCoverage]
public static class SpecialEffects
{
    #region Wild (Hunt & Wild) Effects

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

    #region Craft (Forge & Craft) Effects

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

    #region Harvest (Agriculture & Light) Effects

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

    /// <summary>
    ///     Rare Crop Discovery: Chance to discover rare crop variants on harvest
    /// </summary>
    public const string RareCropDiscovery = "rare_crop_discovery";

    #endregion

    #region Conquest (Domination & Victory) Effects

    /// <summary>
    ///     Battle Fury: Damage increases temporarily after each kill
    /// </summary>
    public const string BattleFury = "battle_fury";

    /// <summary>
    ///     Bloodlust: Heal a small amount when defeating enemies
    /// </summary>
    public const string Bloodlust = "bloodlust";

    /// <summary>
    ///     Warcry: Intimidate nearby hostile creatures, reducing their attack damage
    /// </summary>
    public const string Warcry = "warcry";

    /// <summary>
    ///     Last Stand: When health drops below 25%, gain temporary damage reduction
    /// </summary>
    public const string LastStand = "last_stand";

    #endregion

    #region Stone (Earth & Stone) Effects

    /// <summary>
    ///     Overburdened Immunity: Immune to slowness from being overburdened (first tier)
    /// </summary>
    public const string OverburdenedImmunity = "overburdened_immunity";

    /// <summary>
    ///     Clay Forming Voxel Bonus: Chance to place an additional voxel during clay forming (legacy)
    /// </summary>
    public const string ClayFormingVoxelBonus = "clay_forming_voxel_bonus";

    /// <summary>
    ///     Pottery Batch Completion Bonus: Chance to craft a duplicate pottery item on completion
    /// </summary>
    public const string PotteryBatchCompletionBonus = "pottery_batch_completion_bonus";

    #endregion
}