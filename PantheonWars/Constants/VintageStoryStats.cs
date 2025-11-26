using System.Diagnostics.CodeAnalysis;

namespace PantheonWars.Constants;

/// <summary>
///     Constants for Vintage Story's stat system.
///     These are the actual stat names used by VS's entity.Stats API.
///     Use these constants in blessing definitions to ensure consistency.
/// </summary>
[ExcludeFromCodeCoverage]
public static class VintageStoryStats
{
    // Combat Stats
    public const string MeleeWeaponsDamage = "meleeWeaponsDamage";
    public const string RangedWeaponsDamage = "rangedWeaponsDamage";
    public const string MeleeWeaponsSpeed = "meleeWeaponsSpeed";

    // Defense Stats
    public const string MeleeWeaponArmor = "meleeWeaponArmor";
    public const string MaxHealthExtraPoints = "maxhealthExtraPoints";

    // Movement Stats
    public const string WalkSpeed = "walkspeed";

    // Utility Stats
    public const string HealingEffectiveness = "healingeffectivness";

    // Khoras (Forge & Craft) Stats
    public const string ToolDurability = "toolDurability";
    public const string OreYield = "oreYield";
    public const string ColdResistance = "coldResistance";
    public const string MiningSpeed = "miningSpeedMul";
    public const string RepairCostReduction = "repairCostReduction";
    public const string RepairEfficiency = "repairEfficiency";
    public const string SmithingCostReduction = "smithingCostReduction";
    public const string MetalArmorBonus = "metalArmorBonus";
    public const string HungerRate = "hungerrate";

    // Lysa (Hunt & Wild) Stats
    public const string DoubleHarvestChance = "doubleHarvestChance";
    public const string AnimalDamage = "animalDamage";
    public const string AnimalDrops = "animalDrops";
    public const string FoodSpoilage = "foodSpoilage";
    public const string Satiety = "satiety";
    public const string TemperatureResistance = "temperatureResistance";
    public const string HarvestSpeed = "harvestSpeed";
    public const string ForagingYield = "foragingYield";
}