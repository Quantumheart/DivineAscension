using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using PantheonWars.Constants;
using PantheonWars.Models;
using PantheonWars.Models.Enum;

namespace PantheonWars.Systems;

/// <summary>
///     Contains all blessing definitions for all deities
///     Utility-focused system: 40 blessings (4 deities × 10 blessings each)
/// </summary>
[ExcludeFromCodeCoverage]
public static class BlessingDefinitions
{
    /// <summary>
    ///     Gets all blessing definitions for registration
    ///     Updated for utility-focused system (4 deities, 40 total blessings)
    /// </summary>
    public static List<Blessing> GetAllBlessings()
    {
        var blessings = new List<Blessing>();

        blessings.AddRange(GetKhorasBlessings());
        blessings.AddRange(GetLysaBlessings());
        blessings.AddRange(GetAethraBlessings());
        blessings.AddRange(GetGaiaBlessings());

        return blessings;
    }

    #region Khoras (Forge & Craft)

    private static List<Blessing> GetKhorasBlessings()
    {
        return new List<Blessing>
        {
            // PLAYER BLESSINGS (6 total) - Forge & Craft utility focus

            // Tier 1 - Initiate (0-499 favor) - Foundation
            new(BlessingIds.KhorasCraftsmansTouch, "Craftsman's Touch", DeityType.Khoras)
            {
                Kind = BlessingKind.Player,
                Category = BlessingCategory.Utility,
                Description = "Your devotion to the forge strengthens your craft. Tools/weapons lose durability 10% slower, +10% ore yield when mining, +3°C cold resistance.",
                RequiredFavorRank = (int)FavorRank.Initiate,
                StatModifiers = new Dictionary<string, float>
                {
                    { VintageStoryStats.ToolDurability, 0.10f },
                    { VintageStoryStats.OreDropRate, 0.10f },
                    { VintageStoryStats.ColdResistance, 3.0f }
                }
            },

            // Tier 2 - Disciple (500-1999 favor) - Choose Your Path
            new(BlessingIds.KhorasMasterworkTools, "Masterwork Tools", DeityType.Khoras)
            {
                Kind = BlessingKind.Player,
                Category = BlessingCategory.Utility,
                Description =
                    "Master the craft of tool-making. Tools last 15% longer, +8% mining speed, -15% tool repair costs. Utility path. Requires Craftsman's Touch.",
                RequiredFavorRank = (int)FavorRank.Disciple,
                PrerequisiteBlessings = new List<string> { BlessingIds.KhorasCraftsmansTouch },
                StatModifiers = new Dictionary<string, float>
                {
                    { VintageStoryStats.ToolDurability, 0.15f },
                    { VintageStoryStats.MiningSpeed, 0.08f },
                    { VintageStoryStats.RepairCostReduction, 0.15f }
                }
            },
            new(BlessingIds.KhorasForgebornEndurance, "Forgeborn Endurance", DeityType.Khoras)
            {
                Kind = BlessingKind.Player,
                Category = BlessingCategory.Defense,
                Description =
                    "The forge's heat tempers your body. +5°C cold resistance, +10% max health, +10% armor from metal equipment. Survival path. Requires Craftsman's Touch.",
                RequiredFavorRank = (int)FavorRank.Disciple,
                PrerequisiteBlessings = new List<string> { BlessingIds.KhorasCraftsmansTouch },
                StatModifiers = new Dictionary<string, float>
                {
                    { VintageStoryStats.ColdResistance, 5.0f },
                    { VintageStoryStats.MaxHealthExtraPoints, 1.10f },
                    { VintageStoryStats.MetalArmorBonus, 0.10f }
                }
            },

            // Tier 3 - Zealot (2000-4999 favor) - Specialization
            new(BlessingIds.KhorasLegendarySmith, "Legendary Smith", DeityType.Khoras)
            {
                Kind = BlessingKind.Player,
                Category = BlessingCategory.Utility,
                Description =
                    "Achieve legendary smithing mastery. Tools last 20% longer, +15% ore yield, 10% chance to save materials when smithing, tool repairs restore +25% more durability. Requires Masterwork Tools.",
                RequiredFavorRank = (int)FavorRank.Zealot,
                PrerequisiteBlessings = new List<string> { BlessingIds.KhorasMasterworkTools },
                StatModifiers = new Dictionary<string, float>
                {
                    { VintageStoryStats.ToolDurability, 0.20f },
                    { VintageStoryStats.OreDropRate, 0.15f },
                    { VintageStoryStats.RepairEfficiency, 0.25f }
                },
                SpecialEffects = new List<string> { SpecialEffects.MaterialSaveChance10 }
            },
            new(BlessingIds.KhorasUnyielding, "Unyielding", DeityType.Khoras)
            {
                Kind = BlessingKind.Player,
                Category = BlessingCategory.Defense,
                Description =
                    "Become as unyielding as the anvil. +7°C cold resistance, +15% max health, +15% armor from all equipment, hunger/satiety depletes 8% slower. Requires Forgeborn Endurance.",
                RequiredFavorRank = (int)FavorRank.Zealot,
                PrerequisiteBlessings = new List<string> { BlessingIds.KhorasForgebornEndurance },
                StatModifiers = new Dictionary<string, float>
                {
                    { VintageStoryStats.ColdResistance, 7.0f },
                    { VintageStoryStats.MaxHealthExtraPoints, 1.15f },
                    { VintageStoryStats.MeleeWeaponArmor, 0.15f },
                    { VintageStoryStats.HungerRate, -0.08f }
                }
            },

            // Tier 4 - Champion (5000+ favor) - Capstone (requires both paths)
            new(BlessingIds.KhorasAvatarOfForge, "Avatar of the Forge", DeityType.Khoras)
            {
                Kind = BlessingKind.Player,
                Category = BlessingCategory.Utility,
                Description =
                    "Embody the eternal forge. Tools repair 1 durability per 5 minutes in inventory, -10% material costs for smithing, +12% mining speed. Requires both Legendary Smith and Unyielding.",
                RequiredFavorRank = (int)FavorRank.Champion,
                PrerequisiteBlessings = new List<string> { BlessingIds.KhorasLegendarySmith, BlessingIds.KhorasUnyielding },
                StatModifiers = new Dictionary<string, float>
                {
                    { VintageStoryStats.SmithingCostReduction, 0.10f },
                    { VintageStoryStats.MiningSpeed, 0.12f },
                },
                SpecialEffects = new List<string> { SpecialEffects.PassiveToolRepair1Per5Min }
            },

            // RELIGION BLESSINGS (4 total) - Shared workshop bonuses

            // Tier 1 - Fledgling (0-499 prestige) - Foundation
            new(BlessingIds.KhorasSharedWorkshop, "Shared Workshop", DeityType.Khoras)
            {
                Kind = BlessingKind.Religion,
                Category = BlessingCategory.Utility,
                Description =
                    "Your congregation shares tools and knowledge. +8% tool durability, +8% ore yield for all members.",
                RequiredPrestigeRank = (int)PrestigeRank.Fledgling,
                StatModifiers = new Dictionary<string, float>
                {
                    { VintageStoryStats.ToolDurability, 0.08f },
                    { VintageStoryStats.OreDropRate, 0.08f }
                }
            },

            // Tier 2 - Established (500-1999 prestige) - Coordination
            new(BlessingIds.KhorasGuildOfSmiths, "Guild of Smiths", DeityType.Khoras)
            {
                Kind = BlessingKind.Religion,
                Category = BlessingCategory.Utility,
                Description =
                    "A united guild of master craftsmen. +12% tool durability, +12% ore yield, +4°C cold resistance for all. Requires Shared Workshop.",
                RequiredPrestigeRank = (int)PrestigeRank.Established,
                PrerequisiteBlessings = new List<string> { BlessingIds.KhorasSharedWorkshop },
                StatModifiers = new Dictionary<string, float>
                {
                    { VintageStoryStats.ToolDurability, 0.12f },
                    { VintageStoryStats.OreDropRate, 0.12f },
                    { VintageStoryStats.ColdResistance, 4.0f }
                }
            },

            // Tier 3 - Renowned (2000-4999 prestige) - Elite Force
            new(BlessingIds.KhorasMasterCraftsmen, "Master Craftsmen", DeityType.Khoras)
            {
                Kind = BlessingKind.Religion,
                Category = BlessingCategory.Utility,
                Description =
                    "Elite artisans of legendary skill. +18% tool durability, +15% ore yield, +6°C cold resistance, -10% repair costs for all. Requires Guild of Smiths.",
                RequiredPrestigeRank = (int)PrestigeRank.Renowned,
                PrerequisiteBlessings = new List<string> { BlessingIds.KhorasGuildOfSmiths },
                StatModifiers = new Dictionary<string, float>
                {
                    { VintageStoryStats.ToolDurability, 0.18f },
                    { VintageStoryStats.OreDropRate, 0.15f },
                    { VintageStoryStats.ColdResistance, 6.0f },
                    { VintageStoryStats.RepairCostReduction, 0.10f }
                }
            },

            // Tier 4 - Legendary (5000+ prestige) - Pantheon of Creation
            new(BlessingIds.KhorasPantheonOfCreation, "Pantheon of Creation", DeityType.Khoras)
            {
                Kind = BlessingKind.Religion,
                Category = BlessingCategory.Utility,
                Description =
                    "Your religion becomes legendary creators. +25% tool durability, +20% ore yield, +8°C cold resistance, +10% mining/chopping speed, passive tool repair (1/10min) for all. Requires Master Craftsmen.",
                RequiredPrestigeRank = (int)PrestigeRank.Legendary,
                PrerequisiteBlessings = new List<string> { BlessingIds.KhorasMasterCraftsmen },
                StatModifiers = new Dictionary<string, float>
                {
                    { VintageStoryStats.ToolDurability, 0.25f },
                    { VintageStoryStats.OreDropRate, 0.20f },
                    { VintageStoryStats.ColdResistance, 8.0f },
                    { VintageStoryStats.MiningSpeed, 0.10f },
                },
                SpecialEffects = new List<string> { SpecialEffects.PassiveToolRepair1Per10Min }
            }
        };
    }

    #endregion

    #region Lysa (Hunt)

    private static List<Blessing> GetLysaBlessings()
    {
        return new List<Blessing>
        {
            // PLAYER BLESSINGS (6 total) - Hunt & Wild utility focus

            // Tier 1 - Initiate (0-499 favor)
            new(BlessingIds.LysaHuntersInstinct, "Hunter's Instinct", DeityType.Lysa)
            {
                Kind = BlessingKind.Player,
                Category = BlessingCategory.Utility,
                Description = "Foundation for wilderness survival. 5% more animal and forage drops, +2% movement speed, and harvest 10% faster.",
                RequiredFavorRank = (int)FavorRank.Initiate,
                StatModifiers = new Dictionary<string, float>
                {
                    { VintageStoryStats.AnimalDrops, 0.05f },
                    { VintageStoryStats.ForageDropRate, 0.05f},
                    { VintageStoryStats.AnimalHarvestTime, 0.10f },
                    { VintageStoryStats.WalkSpeed, 0.02f }
                },
                SpecialEffects = new List<string> {  }
            },

            // Tier 2 - Disciple (500-1999 favor) - Choose Your Path
            new(BlessingIds.LysaMasterForager, "Master Forager", DeityType.Lysa)
            {
                Kind = BlessingKind.Player,
                Category = BlessingCategory.Utility,
                Description = "Specializes in plant gathering and food preservation. Double harvest chance +12%, food spoils 15% slower, +10% satiety from foraged foods. Gathering Path. Requires Hunter's Instinct.",
                RequiredFavorRank = (int)FavorRank.Disciple,
                PrerequisiteBlessings = new List<string> { BlessingIds.LysaHuntersInstinct },
                StatModifiers = new Dictionary<string, float>
                {
                    { VintageStoryStats.ForageDropRate, 0.20f },
                    { VintageStoryStats.FoodSpoilage, 0.15f },
                    { VintageStoryStats.Satiety, 0.10f } // Note: Logic assumes Satiety applies generally or handled specially
                },
                SpecialEffects = new List<string> { SpecialEffects.FoodSpoilageReduction }
            },
            new(BlessingIds.LysaApexPredator, "Apex Predator", DeityType.Lysa)
            {
                Kind = BlessingKind.Player,
                Category = BlessingCategory.Combat,
                Description = "Focuses on hunting efficiency and stealth. +12% damage vs animals, animal drops +20%, animals detect you less +20% and harvest 15% faster. Hunting Path. Requires Hunter's Instinct.",
                RequiredFavorRank = (int)FavorRank.Disciple,
                PrerequisiteBlessings = new List<string> { BlessingIds.LysaHuntersInstinct },
                StatModifiers = new Dictionary<string, float>
                {
                    { VintageStoryStats.AnimalDamage, 0.12f },
                    { VintageStoryStats.AnimalDrops, 0.20f },
                    { VintageStoryStats.AnimalSeekingRange, 0.20f},
                    { VintageStoryStats.ToolDurability, 0.15f } // Bow/Spear durability
                },
                SpecialEffects = new List<string> {  }
            },

            // Tier 3 - Zealot (2000-4999 favor) - Specialization
            new(BlessingIds.LysaAbundanceOfWild, "Abundance of the Wild", DeityType.Lysa)
            {
                Kind = BlessingKind.Player,
                Category = BlessingCategory.Utility,
                Description = "Master gatherer. Double harvest chance +15%, find rare herbs/mushrooms 50% more often, food spoils 25% slower, +15% satiety. Requires Master Forager.",
                RequiredFavorRank = (int)FavorRank.Zealot,
                PrerequisiteBlessings = new List<string> { BlessingIds.LysaMasterForager },
                StatModifiers = new Dictionary<string, float>
                {
                    { VintageStoryStats.DoubleHarvestChance, 0.15f },
                    { VintageStoryStats.FoodSpoilage, 0.25f },
                    { VintageStoryStats.Satiety, 0.15f },
                    {VintageStoryStats.ForageDropRate, 0.20f}
                },
                SpecialEffects = new List<string> { SpecialEffects.RareForageChance, SpecialEffects.FoodSpoilageReduction }
            },
            new(BlessingIds.LysaSilentDeath, "Tree walker", DeityType.Lysa)
            {
                Kind = BlessingKind.Player,
                Category = BlessingCategory.Combat,
                Description = "Master hunter. +18% damage vs animals, animal drops +25%, animals detect you 40% less easily. Requires Apex Predator.",
                RequiredFavorRank = (int)FavorRank.Zealot,
                PrerequisiteBlessings = new List<string> { BlessingIds.LysaApexPredator },
                StatModifiers = new Dictionary<string, float>
                {
                    { VintageStoryStats.AnimalDamage, 0.18f },
                    { VintageStoryStats.AnimalSeekingRange, 0.40f },
                    { VintageStoryStats.AnimalDrops, 0.25f }
                },
                SpecialEffects = new List<string> { }
            },

            // Tier 4 - Champion (5000-9999 favor) - Capstone
            new(BlessingIds.LysaAvatarOfWild, "Avatar of the Wild", DeityType.Lysa)
            {
                Kind = BlessingKind.Player,
                Category = BlessingCategory.Utility,
                Description = "True master of the wilderness. +8°C temperature resistance. Requires both Abundance of the Wild and Silent Death.",
                RequiredFavorRank = (int)FavorRank.Champion,
                PrerequisiteBlessings = new List<string> { BlessingIds.LysaAbundanceOfWild, BlessingIds.LysaSilentDeath },
                StatModifiers = new Dictionary<string, float>
                {
                    { VintageStoryStats.AnimalDrops, 0.10f },
                    { VintageStoryStats.AnimalSeekingRange, 0.20f },
                    { VintageStoryStats.AnimalHarvestTime, 0.20f },
                    { VintageStoryStats.ForageDropRate, 0.10f },
                    { VintageStoryStats.TemperatureResistance, 8.0f }
                },
                SpecialEffects = new List<string> { SpecialEffects.TemperatureResistance }
            },

            // RELIGION BLESSINGS (4 total)

            // Tier 1 - Fledgling
            new(BlessingIds.LysaHuntingParty, "Hunting Party", DeityType.Lysa)
            {
                Kind = BlessingKind.Religion,
                Category = BlessingCategory.Utility,
                Description = "A group of hunters and gatherers. +10% double harvest chance, +12% animal drops, +5% movement speed for all members.",
                RequiredPrestigeRank = (int)PrestigeRank.Fledgling,
                StatModifiers = new Dictionary<string, float>
                {
                    { VintageStoryStats.DoubleHarvestChance, 0.10f },
                    { VintageStoryStats.AnimalDrops, 0.12f },
                    { VintageStoryStats.WalkSpeed, 0.05f }
                }
            },

            // Tier 2 - Established
            new(BlessingIds.LysaWildernessTribe, "Wilderness Tribe", DeityType.Lysa)
            {
                Kind = BlessingKind.Religion,
                Category = BlessingCategory.Utility,
                Description = "A true tribe that thrives in the wilderness. +15% double harvest chance, +18% animal drops, +6% movement speed, food spoils 12% slower.",
                RequiredPrestigeRank = (int)PrestigeRank.Established,
                PrerequisiteBlessings = new List<string> { BlessingIds.LysaHuntingParty },
                StatModifiers = new Dictionary<string, float>
                {
                    { VintageStoryStats.DoubleHarvestChance, 0.15f },
                    { VintageStoryStats.AnimalDrops, 0.18f },
                    { VintageStoryStats.FoodSpoilage, 0.12f }
                }
            },

            // Tier 3 - Renowned
            new(BlessingIds.LysaChildrenOfForest, "Children of the Forest", DeityType.Lysa)
            {
                Kind = BlessingKind.Religion,
                Category = BlessingCategory.Utility,
                Description = "Legendary wilderness survivors. +22% double harvest chance, +25% animal drops, +8% movement speed, +10% satiety, +5°C temperature resistance.",
                RequiredPrestigeRank = (int)PrestigeRank.Renowned,
                PrerequisiteBlessings = new List<string> { BlessingIds.LysaWildernessTribe },
                StatModifiers = new Dictionary<string, float>
                {
                    { VintageStoryStats.DoubleHarvestChance, 0.22f },
                    { VintageStoryStats.AnimalDrops, 0.25f },
                    { VintageStoryStats.Satiety, 0.10f },
                    { VintageStoryStats.TemperatureResistance, 5.0f }
                }
            },

            // Tier 4 - Legendary
            new(BlessingIds.LysaPantheonOfHunt, "Pantheon of the Hunt", DeityType.Lysa)
            {
                Kind = BlessingKind.Religion,
                Category = BlessingCategory.Utility,
                Description = "The ultimate wilderness tribe. +30% double harvest chance, +35% animal drops, +10% movement speed, +15% satiety, +8°C temperature resistance.",
                RequiredPrestigeRank = (int)PrestigeRank.Legendary,
                PrerequisiteBlessings = new List<string> { BlessingIds.LysaChildrenOfForest },
                StatModifiers = new Dictionary<string, float>
                {
                    { VintageStoryStats.DoubleHarvestChance, 0.30f },
                    { VintageStoryStats.AnimalDrops, 0.35f },
                    { VintageStoryStats.WalkSpeed, 0.10f },
                    { VintageStoryStats.Satiety, 0.15f },
                    { VintageStoryStats.TemperatureResistance, 8.0f }
                }
            }
        };
    }

    #endregion

    #region Aethra (Agriculture & Light)

    private static List<Blessing> GetAethraBlessings()
    {
        return new List<Blessing>
        {
            // PLAYER BLESSINGS (6 total) - Agriculture & Cooking utility focus

            // Tier 1 - Initiate (0-499 favor) - Foundation
            new(BlessingIds.AethraSunsBlessing, "Sun's Blessing", DeityType.Aethra)
            {
                Kind = BlessingKind.Player,
                Category = BlessingCategory.Utility,
                Description = "Light brings life and growth. +12% crop yield, +10% satiety from all food, +3°C heat resistance, light sources provide +1°C warmth radius.",
                RequiredFavorRank = (int)FavorRank.Initiate,
                StatModifiers = new Dictionary<string, float>
                {
                    { VintageStoryStats.CropYield, 0.12f },
                    { VintageStoryStats.Satiety, 0.10f },
                    { VintageStoryStats.HeatResistance, 3.0f },
                },
                SpecialEffects = new List<string> { SpecialEffects.LightWarmthBonus }
            },

            // Tier 2 - Disciple (500-1999 favor) - Choose Your Path
            new(BlessingIds.AethraBountifulHarvest, "Bountiful Harvest", DeityType.Aethra)
            {
                Kind = BlessingKind.Player,
                Category = BlessingCategory.Utility,
                Description =
                    "Specialize in farming excellence. +15% crop yield, +12% satiety from crops, crops have 15% chance for bonus seeds, +15% chance to find rare crop variants. Agriculture path. Requires Sun's Blessing.",
                RequiredFavorRank = (int)FavorRank.Disciple,
                PrerequisiteBlessings = new List<string> { BlessingIds.AethraSunsBlessing },
                StatModifiers = new Dictionary<string, float>
                {
                    { VintageStoryStats.CropYield, 0.15f },
                    { VintageStoryStats.SeedDropChance, 0.15f },
                    { VintageStoryStats.RareCropChance, 0.15f }
                }
            },
            new(BlessingIds.AethraBakersTouch, "Baker's Touch", DeityType.Aethra)
            {
                Kind = BlessingKind.Player,
                Category = BlessingCategory.Utility,
                Description =
                    "Master the art of food preparation. Cooking/baking yields +25% more food, +15% satiety from cooked food, food spoils 20% slower, +5°C heat resistance. Food preparation path. Requires Sun's Blessing.",
                RequiredFavorRank = (int)FavorRank.Disciple,
                PrerequisiteBlessings = new List<string> { BlessingIds.AethraSunsBlessing },
                StatModifiers = new Dictionary<string, float>
                {
                    { VintageStoryStats.CookingYield, 0.25f },
                    { VintageStoryStats.CookedFoodSatiety, 0.15f },
                    { VintageStoryStats.FoodSpoilage, 0.20f },
                    { VintageStoryStats.HeatResistance, 5.0f }
                },
                SpecialEffects = new List<string> { SpecialEffects.FoodSpoilageReduction }
            },

            // Tier 3 - Zealot (2000-4999 favor) - Specialization
            new(BlessingIds.AethraMasterFarmer, "Master Farmer", DeityType.Aethra)
            {
                Kind = BlessingKind.Player,
                Category = BlessingCategory.Utility,
                Description =
                    "Achieve ultimate farming mastery. +20% crop yield, +18% satiety from crops, crops have 25% chance for bonus seeds, +30% chance to find rare crop variants, wild crops give +40% yield. Requires Bountiful Harvest.",
                RequiredFavorRank = (int)FavorRank.Zealot,
                PrerequisiteBlessings = new List<string> { BlessingIds.AethraBountifulHarvest },
                StatModifiers = new Dictionary<string, float>
                {
                    { VintageStoryStats.CropYield, 0.20f },
                    { VintageStoryStats.SeedDropChance, 0.25f },
                    { VintageStoryStats.RareCropChance, 0.30f },
                    { VintageStoryStats.WildCropYield, 0.40f }
                }
            },
            new(BlessingIds.AethraDivineKitchen, "Divine Kitchen", DeityType.Aethra)
            {
                Kind = BlessingKind.Player,
                Category = BlessingCategory.Utility,
                Description =
                    "Create incredibly nutritious meals. Cooking yields +35% more, +25% satiety from cooked food, food spoils 30% slower, +7°C heat resistance, meals provide temporary +5% max health buff. Requires Baker's Touch.",
                RequiredFavorRank = (int)FavorRank.Zealot,
                PrerequisiteBlessings = new List<string> { BlessingIds.AethraBakersTouch },
                StatModifiers = new Dictionary<string, float>
                {
                    { VintageStoryStats.CookingYield, 0.35f },
                    { VintageStoryStats.CookedFoodSatiety, 0.25f },
                    { VintageStoryStats.FoodSpoilage, 0.30f },
                    { VintageStoryStats.HeatResistance, 7.0f }
                },
                SpecialEffects = new List<string> { SpecialEffects.TempHealthBuff5, SpecialEffects.FoodSpoilageReduction }
            },

            // Tier 4 - Champion (5000+ favor) - Capstone (requires both paths)
            new(BlessingIds.AethraAvatarOfAbundance, "Avatar of Abundance", DeityType.Aethra)
            {
                Kind = BlessingKind.Player,
                Category = BlessingCategory.Utility,
                Description =
                    "Embody the endless bounty of the harvest. +8% movement speed, +10% max health, never suffer malnutrition penalties, can create blessed meals with powerful buffs. Requires both Master Farmer and Divine Kitchen.",
                RequiredFavorRank = (int)FavorRank.Champion,
                PrerequisiteBlessings = new List<string> { BlessingIds.AethraMasterFarmer, BlessingIds.AethraDivineKitchen },
                StatModifiers = new Dictionary<string, float>
                {
                    { VintageStoryStats.WalkSpeed, 0.08f },
                    { VintageStoryStats.MaxHealthExtraPoints, 1.10f }
                },
                SpecialEffects = new List<string> { SpecialEffects.NeverMalnourished, SpecialEffects.BlessedMeals }
            },

            // RELIGION BLESSINGS (4 total) - Shared agricultural bonuses

            // Tier 1 - Fledgling (0-499 prestige) - Foundation
            new(BlessingIds.AethraCommunityFarm, "Community Farm", DeityType.Aethra)
            {
                Kind = BlessingKind.Religion,
                Category = BlessingCategory.Utility,
                Description =
                    "Your congregation shares agricultural knowledge. +10% crop yield, +8% satiety from all food for all members.",
                RequiredPrestigeRank = (int)PrestigeRank.Fledgling,
                StatModifiers = new Dictionary<string, float>
                {
                    { VintageStoryStats.CropYield, 0.10f },
                    { VintageStoryStats.Satiety, 0.08f }
                }
            },

            // Tier 2 - Established (500-1999 prestige) - Coordination
            new(BlessingIds.AethraHarvestFestival, "Harvest Festival", DeityType.Aethra)
            {
                Kind = BlessingKind.Religion,
                Category = BlessingCategory.Utility,
                Description =
                    "Celebrate abundant harvests together. +15% crop yield, +12% satiety from all food, food spoils 10% slower for all. Requires Community Farm.",
                RequiredPrestigeRank = (int)PrestigeRank.Established,
                PrerequisiteBlessings = new List<string> { BlessingIds.AethraCommunityFarm },
                StatModifiers = new Dictionary<string, float>
                {
                    { VintageStoryStats.CropYield, 0.15f },
                    { VintageStoryStats.Satiety, 0.12f },
                    { VintageStoryStats.FoodSpoilage, 0.10f }
                }
            },

            // Tier 3 - Renowned (2000-4999 prestige) - Elite Force
            new(BlessingIds.AethraLandOfPlenty, "Land of Plenty", DeityType.Aethra)
            {
                Kind = BlessingKind.Religion,
                Category = BlessingCategory.Utility,
                Description =
                    "Your land becomes legendary for its bounty. +22% crop yield, +18% satiety from all food, food spoils 18% slower, +5°C heat resistance for all. Requires Harvest Festival.",
                RequiredPrestigeRank = (int)PrestigeRank.Renowned,
                PrerequisiteBlessings = new List<string> { BlessingIds.AethraHarvestFestival },
                StatModifiers = new Dictionary<string, float>
                {
                    { VintageStoryStats.CropYield, 0.22f },
                    { VintageStoryStats.Satiety, 0.18f },
                    { VintageStoryStats.FoodSpoilage, 0.18f },
                    { VintageStoryStats.HeatResistance, 5.0f }
                }
            },

            // Tier 4 - Legendary (5000+ prestige) - Pantheon of Light
            new(BlessingIds.AethraPantheonOfLight, "Pantheon of Light", DeityType.Aethra)
            {
                Kind = BlessingKind.Religion,
                Category = BlessingCategory.Utility,
                Description =
                    "Your religion becomes the source of endless bounty. +30% crop yield, +20% satiety from all food, food spoils 25% slower, +8°C heat resistance, religion can build Sacred Granary for all. Requires Land of Plenty.",
                RequiredPrestigeRank = (int)PrestigeRank.Legendary,
                PrerequisiteBlessings = new List<string> { BlessingIds.AethraLandOfPlenty },
                StatModifiers = new Dictionary<string, float>
                {
                    { VintageStoryStats.CropYield, 0.30f },
                    { VintageStoryStats.Satiety, 0.20f },
                    { VintageStoryStats.FoodSpoilage, 0.25f },
                    { VintageStoryStats.HeatResistance, 8.0f }
                },
                SpecialEffects = new List<string> { SpecialEffects.SacredGranary }
            }
        };
    }

    #endregion


    #region Gaia (Earth & Stone) - 10 Blessings (Utility-Focused)

    private static List<Blessing> GetGaiaBlessings()
    {
        return new List<Blessing>
        {
            // PLAYER BLESSINGS (6 total) - Earth resources & endurance utility focus

            // Tier 1 - Initiate (0-499 favor) - Foundation
            new(BlessingIds.GaiaEarthenFoundation, "Earthen Foundation", DeityType.Gaia)
            {
                Kind = BlessingKind.Player,
                Category = BlessingCategory.Utility,
                Description = "Draw strength from the earth. +10% stone/clay/gravel yield when mining, +10% max health, -15% fall damage.",
                RequiredFavorRank = (int)FavorRank.Initiate,
                StatModifiers = new Dictionary<string, float>
                {
                    { VintageStoryStats.StoneYield, 0.10f },
                    { VintageStoryStats.ClayYield, 0.10f },
                    { VintageStoryStats.GravelYield, 0.10f },
                    { VintageStoryStats.MaxHealthExtraPoints, 1.10f },
                    { VintageStoryStats.FallDamageReduction, 0.15f }
                }
            },

            // Tier 2 - Disciple (500-1999 favor) - Choose Your Path
            new(BlessingIds.GaiaQuarryman, "Quarryman", DeityType.Gaia)
            {
                Kind = BlessingKind.Player,
                Category = BlessingCategory.Utility,
                Description =
                    "Specialize in stone extraction. +12% stone/clay/gravel yield, +20% chance to find granite/marble/other stone types, mining picks last 15% longer. Resource path. Requires Earthen Foundation.",
                RequiredFavorRank = (int)FavorRank.Disciple,
                PrerequisiteBlessings = new List<string> { BlessingIds.GaiaEarthenFoundation },
                StatModifiers = new Dictionary<string, float>
                {
                    { VintageStoryStats.StoneYield, 0.12f },
                    { VintageStoryStats.ClayYield, 0.12f },
                    { VintageStoryStats.GravelYield, 0.12f },
                    { VintageStoryStats.RareStoneChance, 0.20f },
                    { VintageStoryStats.PickDurability, 0.15f }
                }
            },
            new(BlessingIds.GaiaMountainsEndurance, "Mountain's Endurance", DeityType.Gaia)
            {
                Kind = BlessingKind.Player,
                Category = BlessingCategory.Defense,
                Description =
                    "Become as tough as the mountains. +15% max health, -20% fall damage, +8% armor from all equipment, hunger depletes 10% slower. Survival path. Requires Earthen Foundation.",
                RequiredFavorRank = (int)FavorRank.Disciple,
                PrerequisiteBlessings = new List<string> { BlessingIds.GaiaEarthenFoundation },
                StatModifiers = new Dictionary<string, float>
                {
                    { VintageStoryStats.MaxHealthExtraPoints, 1.15f },
                    { VintageStoryStats.FallDamageReduction, 0.20f },
                    { VintageStoryStats.MeleeWeaponArmor, 0.08f },
                    { VintageStoryStats.HungerRate, -0.10f }
                }
            },

            // Tier 3 - Zealot (2000-4999 favor) - Specialization
            new(BlessingIds.GaiaMasterQuarryman, "Master Quarryman", DeityType.Gaia)
            {
                Kind = BlessingKind.Player,
                Category = BlessingCategory.Utility,
                Description =
                    "Achieve mastery of stone extraction. +15% stone/clay/gravel yield, +35% chance to find rare stones, mining picks last 25% longer, +15% chance to find surface copper/tin when mining stone. Requires Quarryman.",
                RequiredFavorRank = (int)FavorRank.Zealot,
                PrerequisiteBlessings = new List<string> { BlessingIds.GaiaQuarryman },
                StatModifiers = new Dictionary<string, float>
                {
                    { VintageStoryStats.StoneYield, 0.15f },
                    { VintageStoryStats.ClayYield, 0.15f },
                    { VintageStoryStats.GravelYield, 0.15f },
                    { VintageStoryStats.RareStoneChance, 0.35f },
                    { VintageStoryStats.PickDurability, 0.25f },
                    { VintageStoryStats.OreInStoneChance, 0.15f }
                }
            },
            new(BlessingIds.GaiaUnshakeable, "Unshakeable", DeityType.Gaia)
            {
                Kind = BlessingKind.Player,
                Category = BlessingCategory.Defense,
                Description =
                    "Become nearly indestructible. +20% max health, -25% fall damage, +12% armor from all equipment, hunger depletes 15% slower, +5°C cold resistance. Requires Mountain's Endurance.",
                RequiredFavorRank = (int)FavorRank.Zealot,
                PrerequisiteBlessings = new List<string> { BlessingIds.GaiaMountainsEndurance },
                StatModifiers = new Dictionary<string, float>
                {
                    { VintageStoryStats.MaxHealthExtraPoints, 1.20f },
                    { VintageStoryStats.FallDamageReduction, 0.25f },
                    { VintageStoryStats.MeleeWeaponArmor, 0.12f },
                    { VintageStoryStats.HungerRate, -0.15f },
                    { VintageStoryStats.ColdResistance, 5.0f }
                }
            },

            // Tier 4 - Champion (5000+ favor) - Capstone (requires both paths)
            new(BlessingIds.GaiaAvatarOfEarth, "Avatar of Earth", DeityType.Gaia)
            {
                Kind = BlessingKind.Player,
                Category = BlessingCategory.Utility,
                Description =
                    "Embody the eternal strength of the earth. +5% movement speed, +8°C cold resistance, immune to slowness from being overburdened. Requires both Master Quarryman and Unshakeable.",
                RequiredFavorRank = (int)FavorRank.Champion,
                PrerequisiteBlessings = new List<string> { BlessingIds.GaiaMasterQuarryman, BlessingIds.GaiaUnshakeable },
                StatModifiers = new Dictionary<string, float>
                {
                    { VintageStoryStats.WalkSpeed, 0.05f },
                    { VintageStoryStats.ColdResistance, 8.0f }
                },
                SpecialEffects = new List<string> { SpecialEffects.OverburdenedImmunity }
            },

            // RELIGION BLESSINGS (4 total) - Shared earth benefits

            // Tier 1 - Fledgling (0-499 prestige) - Foundation
            new(BlessingIds.GaiaStoneCircle, "Stone Circle", DeityType.Gaia)
            {
                Kind = BlessingKind.Religion,
                Category = BlessingCategory.Utility,
                Description =
                    "Your congregation gathers in ancient stone circles. +8% stone/clay/gravel yield, +8% max health for all members.",
                RequiredPrestigeRank = (int)PrestigeRank.Fledgling,
                StatModifiers = new Dictionary<string, float>
                {
                    { VintageStoryStats.StoneYield, 0.08f },
                    { VintageStoryStats.ClayYield, 0.08f },
                    { VintageStoryStats.GravelYield, 0.08f },
                    { VintageStoryStats.MaxHealthExtraPoints, 1.08f }
                }
            },

            // Tier 2 - Established (500-1999 prestige) - Coordination
            new(BlessingIds.GaiaEarthWardens, "Earth Wardens", DeityType.Gaia)
            {
                Kind = BlessingKind.Religion,
                Category = BlessingCategory.Utility,
                Description =
                    "Guardians of the earth united. +12% stone/clay/gravel yield, +12% max health, -15% fall damage for all. Requires Stone Circle.",
                RequiredPrestigeRank = (int)PrestigeRank.Established,
                PrerequisiteBlessings = new List<string> { BlessingIds.GaiaStoneCircle },
                StatModifiers = new Dictionary<string, float>
                {
                    { VintageStoryStats.StoneYield, 0.12f },
                    { VintageStoryStats.ClayYield, 0.12f },
                    { VintageStoryStats.GravelYield, 0.12f },
                    { VintageStoryStats.MaxHealthExtraPoints, 1.12f },
                    { VintageStoryStats.FallDamageReduction, 0.15f }
                }
            },

            // Tier 3 - Renowned (2000-4999 prestige) - Elite Force
            new(BlessingIds.GaiaMountainsChildren, "Mountain's Children", DeityType.Gaia)
            {
                Kind = BlessingKind.Religion,
                Category = BlessingCategory.Utility,
                Description =
                    "Your religion becomes children of the mountains. +18% stone/clay/gravel yield, +18% max health, -22% fall damage, +8% armor for all. Requires Earth Wardens.",
                RequiredPrestigeRank = (int)PrestigeRank.Renowned,
                PrerequisiteBlessings = new List<string> { BlessingIds.GaiaEarthWardens },
                StatModifiers = new Dictionary<string, float>
                {
                    { VintageStoryStats.StoneYield, 0.18f },
                    { VintageStoryStats.ClayYield, 0.18f },
                    { VintageStoryStats.GravelYield, 0.18f },
                    { VintageStoryStats.MaxHealthExtraPoints, 1.18f },
                    { VintageStoryStats.FallDamageReduction, 0.22f },
                    { VintageStoryStats.MeleeWeaponArmor, 0.08f }
                }
            },

            // Tier 4 - Legendary (5000+ prestige) - Pantheon of Stone
            new(BlessingIds.GaiaPantheonOfStone, "Pantheon of Stone", DeityType.Gaia)
            {
                Kind = BlessingKind.Religion,
                Category = BlessingCategory.Utility,
                Description =
                    "Your religion becomes legendary stone workers. +25% stone/clay/gravel yield, +25% max health, -30% fall damage, +12% armor, +5°C cold resistance for all. Requires Mountain's Children.",
                RequiredPrestigeRank = (int)PrestigeRank.Legendary,
                PrerequisiteBlessings = new List<string> { BlessingIds.GaiaMountainsChildren },
                StatModifiers = new Dictionary<string, float>
                {
                    { VintageStoryStats.StoneYield, 0.25f },
                    { VintageStoryStats.ClayYield, 0.25f },
                    { VintageStoryStats.GravelYield, 0.25f },
                    { VintageStoryStats.MaxHealthExtraPoints, 1.25f },
                    { VintageStoryStats.FallDamageReduction, 0.30f },
                    { VintageStoryStats.MeleeWeaponArmor, 0.12f },
                    { VintageStoryStats.ColdResistance, 5.0f }
                }
            }
        };
    }

    #endregion
}