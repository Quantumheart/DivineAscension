using System.Diagnostics.CodeAnalysis;

namespace DivineAscension.Constants;

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
    public const string RangedWeaponsAccuracy = "rangedWeaponsAcc";
    public const string RangedWeaponsRange = "rangedWeaponsRange";

    // War (Blood & Battle) Stats
    public const string KillHealthRestore = "killHealthRestore";
    public const string DamageReduction = "damageReduction";
    public const string CriticalHitChance = "criticalHitChance";
    public const string CriticalHitDamage = "criticalHitDamage";

    // Defense Stats
    public const string MeleeWeaponArmor = "meleeWeaponArmor";
    public const string MaxHealthExtraPoints = "maxhealthExtraPoints";
    public const string MaxHealthExtraMultiplier = "maxhealthExtraMultiplier";
    public const string ArmorEffectiveness = "armorEffectiveness";

    // Movement Stats
    public const string WalkSpeed = "walkspeed";

    // Utility Stats
    public const string HealingEffectiveness = "healingeffectivness";

    // Craft (Forge & Craft) Stats
    public const string ToolDurability = "toolDurability";
    public const string OreDropRate = "oreDropRate";
    public const string ColdResistance = "coldResistance";
    public const string MiningSpeed = "miningSpeedMul";
    public const string RepairCostReduction = "repairCostReduction";
    public const string RepairEfficiency = "repairEfficiency";
    public const string SmithingCostReduction = "smithingCostReduction";
    public const string MetalArmorBonus = "metalArmorBonus";
    public const string HungerRate = "hungerrate";
    public const string ArmorDurabilityLoss = "armorDurabilityLoss";
    public const string ArmorWalkSpeedAffectedness = "armorWalkSpeedAffectedness";

    // Wild (Hunt & Wild) Stats
    public const string DoubleHarvestChance = "doubleHarvestChance";
    public const string AnimalDamage = "animalDamage";
    public const string AnimalDrops = "animalLootDropRate";
    public const string ForageDropRate = "forageDropRate";
    public const string FoodSpoilage = "foodSpoilage";
    public const string Satiety = "satiety";
    public const string TemperatureResistance = "temperatureResistance";
    public const string AnimalHarvestTime = "animalHarvestingTime";
    public const string ForagingYield = "foragingYield";

    // Harvest (Agriculture & Cooking) Stats
    public const string CropYield = "cropYield";
    public const string SeedDropChance = "seedDropChance";
    public const string CookingYield = "cookingYield";
    public const string HeatResistance = "heatResistance";
    public const string RareCropChance = "rareCropChance";
    public const string WildCropYield = "wildCropDropRate";
    public const string CookedFoodSatiety = "cookedFoodSatiety";

    // Stone (Pottery & Clay) Stats
    public const string StoneYield = "stoneYield";
    public const string ClayYield = "clayYield";
    public const string ClayFormingVoxelChance = "clayFormingVoxelChance";
    public const string PotteryBatchCompletionChance = "potteryBatchCompletionChance";
    public const string StorageVesselCapacity = "storageVesselCapacity";
    public const string DiggingSpeed = "diggingSpeed";

    // Legacy Stone Stats (kept for compatibility)
    public const string PickDurability = "pickDurability";
    public const string FallDamageReduction = "fallDamageReduction";
    public const string RareStoneChance = "rareStoneChance";
    public const string OreInStoneChance = "oreInStoneChance";
    public const string GravelYield = "gravelYield";

    // Other
    public const string AnimalSeekingRange = "animalSeekingRange";

    #region CombatOverhaul Compatible Stats

    // CombatOverhaul-compatible tier bonus stats
    // These stats are read by CombatOverhaul's projectile system for damage calculations
    public const string MeleeDamageTierBonusSlashing = "meleeDamageTierBonusSlashingAttack";
    public const string MeleeDamageTierBonusPiercing = "meleeDamageTierBonusPiercingAttack";
    public const string MeleeDamageTierBonusBlunt = "meleeDamageTierBonusBluntAttack";
    public const string RangedDamageTierBonusSlashing = "rangedDamageTierBonusSlashingAttack";
    public const string RangedDamageTierBonusPiercing = "rangedDamageTierBonusPiercingAttack";
    public const string RangedDamageTierBonusBlunt = "rangedDamageTierBonusBluntAttack";

    // CombatOverhaul armor penalty reduction stats
    // These reduce the penalties applied by wearing armor
    public const string ArmorManipulationSpeedAffectedness = "armorManipulationSpeedAffectedness";
    public const string ArmorHungerRateAffectedness = "armorHungerRateAffectedness";

    // CombatOverhaul movement/utility stats
    public const string ManipulationSpeed = "manipulationSpeed";
    public const string SteadyAim = "steadyAim";

    // CombatOverhaul combat stats
    public const string MechanicalsDamage = "mechanicalsDamage";

    // CombatOverhaul body zone damage factors
    // These modify damage taken to specific body parts (default values in comments)
    public const string PlayerHeadDamageFactor = "playerHeadDamageFactor"; // Default: 2.0x
    public const string PlayerFaceDamageFactor = "playerFaceDamageFactor"; // Default: 1.5x
    public const string PlayerNeckDamageFactor = "playerNeckDamageFactor"; // Default: 2.0x
    public const string PlayerTorsoDamageFactor = "playerTorsoDamageFactor"; // Default: 1.0x
    public const string PlayerArmsDamageFactor = "playerArmsDamageFactor"; // Default: 0.5x
    public const string PlayerLegsDamageFactor = "playerLegsDamageFactor"; // Default: 0.5x
    public const string PlayerHandsDamageFactor = "playerHandsDamageFactor"; // Default: 0.5x
    public const string PlayerFeetDamageFactor = "playerFeetDamageFactor"; // Default: 0.5x

    // CombatOverhaul weapon proficiencies
    // These affect attack speed (melee) or reload/draw speed (ranged)
    // Typical values: 0.3 = +30% speed, 0.5 = +50% speed, -0.3 = -30% speed
    public const string BowsProficiency = "bowsProficiency";
    public const string CrossbowsProficiency = "crossbowsProficiency";
    public const string FirearmsProficiency = "firearmsProficiency";
    public const string OneHandedSwordsProficiency = "oneHandedSwordsProficiency";
    public const string TwoHandedSwordsProficiency = "twoHandedSwordsProficiency";
    public const string SpearsProficiency = "spearsProficiency";
    public const string JavelinsProficiency = "javelinsProficiency";
    public const string MacesProficiency = "macesProficiency";
    public const string ClubsProficiency = "clubsProficiency";
    public const string HalberdsProficiency = "halberdsProficiency";
    public const string AxesProficiency = "axesProficiency";
    public const string QuarterstaffProficiency = "quarterstaffProficiency";
    public const string SlingsProficiency = "slingsProficiency";

    // CombatOverhaul second chance mechanic
    public const string SecondChanceCooldown = "secondChanceCooldown";
    public const string SecondChanceGracePeriod = "secondChanceGracePeriod";

    #endregion
}