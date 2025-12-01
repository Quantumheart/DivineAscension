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
    public const string RangedWeaponsAccuracy = "rangedWeaponsAccuracy";
    public const string RangedWeaponsRange = "rangedWeaponsRange";

    // Defense Stats
    public const string MeleeWeaponArmor = "meleeWeaponArmor";
    public const string MaxHealthExtraPoints = "maxhealthExtraPoints";

    // Movement Stats
    public const string WalkSpeed = "walkspeed";

    // Utility Stats
    public const string HealingEffectiveness = "healingeffectivness";

    // Khoras (Forge & Craft) Stats
    public const string ToolDurability = "toolDurability";
    public const string OreDropRate = "oreDropRate";
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
    public const string AnimalDrops = "animalLootDropRate";
    public const string ForageDropRate = "forageDropRate";
    public const string FoodSpoilage = "foodSpoilage";
    public const string Satiety = "satiety";
    public const string TemperatureResistance = "temperatureResistance";
    public const string AnimalHarvestTime = "animalHarvestingTime";
    public const string ForagingYield = "foragingYield";

    // Aethra (Agriculture & Cooking) Stats
    public const string CropYield = "cropYield";
    public const string SeedDropChance = "seedDropChance";
    public const string CookingYield = "cookingYield";
    public const string HeatResistance = "heatResistance";
    public const string RareCropChance = "rareCropChance";
    public const string WildCropYield = "wildCropYield";
    public const string CookedFoodSatiety = "cookedFoodSatiety";

    // Gaia (Pottery & Clay) Stats
    public const string StoneYield = "stoneYield";
    public const string ClayYield = "clayYield";
    public const string ClayFormingVoxelChance = "clayFormingVoxelChance";
    public const string StorageVesselCapacity = "storageVesselCapacity";
    public const string DiggingSpeed = "diggingSpeed";

    // Legacy Gaia Stats (kept for compatibility)
    public const string PickDurability = "pickDurability";
    public const string FallDamageReduction = "fallDamageReduction";
    public const string RareStoneChance = "rareStoneChance";
    public const string OreInStoneChance = "oreInStoneChance";
    public const string GravelYield = "gravelYield";

    // Other
    public const string AnimalSeekingRange = "animalSeekingRange";
}