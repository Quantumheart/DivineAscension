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
                Description = "+10% tool durability, +10% ore yield.",
                RequiredFavorRank = (int)FavorRank.Initiate,
                StatModifiers = new Dictionary<string, float>
                {
                    { VintageStoryStats.ToolDurability, 0.10f },
                    { VintageStoryStats.OreDropRate, 0.10f }
                }
            },

            // Tier 2 - Disciple (500-1999 favor) - Choose Your Path
            new(BlessingIds.KhorasMasterworkTools, "Masterwork Tools", DeityType.Khoras)
            {
                Kind = BlessingKind.Player,
                Category = BlessingCategory.Utility,
                Description = "+15% tool durability (total: 25%), +10% mining speed. Requires Craftsman's Touch.",
                RequiredFavorRank = (int)FavorRank.Disciple,
                PrerequisiteBlessings = new List<string> { BlessingIds.KhorasCraftsmansTouch },
                StatModifiers = new Dictionary<string, float>
                {
                    { VintageStoryStats.ToolDurability, 0.15f },
                    { VintageStoryStats.MiningSpeed, 0.10f }
                }
            },
            new(BlessingIds.KhorasForgebornEndurance, "Forgeborn Endurance", DeityType.Khoras)
            {
                Kind = BlessingKind.Player,
                Category = BlessingCategory.Defense,
                Description = "+10% melee weapon damage, +10% max health. Requires Craftsman's Touch.",
                RequiredFavorRank = (int)FavorRank.Disciple,
                PrerequisiteBlessings = new List<string> { BlessingIds.KhorasCraftsmansTouch },
                StatModifiers = new Dictionary<string, float>
                {
                    { VintageStoryStats.MeleeWeaponsDamage, 0.10f },
                    { VintageStoryStats.MaxHealthExtraPoints, 1.10f }
                }
            },

            // Tier 3 - Zealot (2000-4999 favor) - Specialization
            new(BlessingIds.KhorasLegendarySmith, "Legendary Smith", DeityType.Khoras)
            {
                Kind = BlessingKind.Player,
                Category = BlessingCategory.Utility,
                Description = "+20% tool durability (total: 45%), +15% ore yield (total: 25%). Requires Masterwork Tools.",
                RequiredFavorRank = (int)FavorRank.Zealot,
                PrerequisiteBlessings = new List<string> { BlessingIds.KhorasMasterworkTools },
                StatModifiers = new Dictionary<string, float>
                {
                    { VintageStoryStats.ToolDurability, 0.20f },
                    { VintageStoryStats.OreDropRate, 0.15f }
                },
                SpecialEffects = new List<string> { }
            },
            new(BlessingIds.KhorasUnyielding, "Unyielding", DeityType.Khoras)
            {
                Kind = BlessingKind.Player,
                Category = BlessingCategory.Defense,
                Description = "+10% reduced armor durability loss, +15% max health (total: 25%). Requires Forgeborn Endurance.",
                RequiredFavorRank = (int)FavorRank.Zealot,
                PrerequisiteBlessings = new List<string> { BlessingIds.KhorasForgebornEndurance },
                StatModifiers = new Dictionary<string, float>
                {
                    { VintageStoryStats.ArmorDurabilityLoss, -0.10f },
                    { VintageStoryStats.MaxHealthExtraPoints, 1.15f }
                }
            },

            // Tier 4 - Champion (5000+ favor) - Capstone (requires both paths)
            new(BlessingIds.KhorasAvatarOfForge, "Avatar of the Forge", DeityType.Khoras)
            {
                Kind = BlessingKind.Player,
                Category = BlessingCategory.Utility,
                Description = "Tools repair 1 durability per 5 minutes. +10% armor walk speed. Requires both Legendary Smith and Unyielding.",
                RequiredFavorRank = (int)FavorRank.Champion,
                PrerequisiteBlessings = new List<string> { BlessingIds.KhorasLegendarySmith, BlessingIds.KhorasUnyielding },
                StatModifiers = new Dictionary<string, float>
                {
                    { VintageStoryStats.ArmorWalkSpeedAffectedness, -0.10f }
                },
                SpecialEffects = new List<string> { SpecialEffects.PassiveToolRepair1Per5Min }
            },

            // RELIGION BLESSINGS (4 total) - Shared workshop bonuses

            // Tier 1 - Fledgling (0-499 prestige) - Foundation
            new(BlessingIds.KhorasSharedWorkshop, "Shared Workshop", DeityType.Khoras)
            {
                Kind = BlessingKind.Religion,
                Category = BlessingCategory.Utility,
                Description = "+10% tool durability, +10% ore yield for all members.",
                RequiredPrestigeRank = (int)PrestigeRank.Fledgling,
                StatModifiers = new Dictionary<string, float>
                {
                    { VintageStoryStats.ToolDurability, 0.10f },
                    { VintageStoryStats.OreDropRate, 0.10f }
                }
            },

            // Tier 2 - Established (500-1999 prestige) - Coordination
            new(BlessingIds.KhorasGuildOfSmiths, "Guild of Smiths", DeityType.Khoras)
            {
                Kind = BlessingKind.Religion,
                Category = BlessingCategory.Utility,
                Description = "+15% tool durability, +15% ore yield for all. Requires Shared Workshop.",
                RequiredPrestigeRank = (int)PrestigeRank.Established,
                PrerequisiteBlessings = new List<string> { BlessingIds.KhorasSharedWorkshop },
                StatModifiers = new Dictionary<string, float>
                {
                    { VintageStoryStats.ToolDurability, 0.15f },
                    { VintageStoryStats.OreDropRate, 0.15f }
                }
            },

            // Tier 3 - Renowned (2000-4999 prestige) - Elite Force
            new(BlessingIds.KhorasMasterCraftsmen, "Master Craftsmen", DeityType.Khoras)
            {
                Kind = BlessingKind.Religion,
                Category = BlessingCategory.Utility,
                Description = "+20% tool durability, +20% ore yield, +10% armor walk speed for all. Requires Guild of Smiths.",
                RequiredPrestigeRank = (int)PrestigeRank.Renowned,
                PrerequisiteBlessings = new List<string> { BlessingIds.KhorasGuildOfSmiths },
                StatModifiers = new Dictionary<string, float>
                {
                    { VintageStoryStats.ToolDurability, 0.20f },
                    { VintageStoryStats.OreDropRate, 0.20f },
                    { VintageStoryStats.ArmorWalkSpeedAffectedness, -0.10f }
                }
            },

            // Tier 4 - Legendary (5000+ prestige) - Pantheon of Creation
            new(BlessingIds.KhorasPantheonOfCreation, "Pantheon of Creation", DeityType.Khoras)
            {
                Kind = BlessingKind.Religion,
                Category = BlessingCategory.Utility,
                Description = "+10% max health for all. Requires Master Craftsmen.",
                RequiredPrestigeRank = (int)PrestigeRank.Legendary,
                PrerequisiteBlessings = new List<string> { BlessingIds.KhorasMasterCraftsmen },
                StatModifiers = new Dictionary<string, float>
                {
                    { VintageStoryStats.MaxHealthExtraPoints, 1.10f }
                },
                SpecialEffects = new List<string> { }
            }
        };
    }

    #endregion

    #region Lysa (Hunt & Wild)

    private static List<Blessing> GetLysaBlessings()
    {
        return new List<Blessing>
        {
            // PLAYER BLESSINGS (6 total) - Hunt & Wild utility focus (v2.0.0 Utility Redesign)

            // Tier 1 - Initiate (0-499 favor)
            new(BlessingIds.LysaHuntersInstinct, "Hunter's Instinct", DeityType.Lysa)
            {
                Kind = BlessingKind.Player,
                Category = BlessingCategory.Utility,
                Description = "+15% animal and forage drops, +5% movement speed.",
                RequiredFavorRank = (int)FavorRank.Initiate,
                StatModifiers = new Dictionary<string, float>
                {
                    { VintageStoryStats.AnimalDrops, 0.15f },
                    { VintageStoryStats.ForageDropRate, 0.15f },
                    { VintageStoryStats.WalkSpeed, 0.05f }
                },
                SpecialEffects = new List<string> { }
            },

            // Tier 2 - Disciple (500-1999 favor) - Choose Path
            new(BlessingIds.LysaMasterForager, "Master Forager", DeityType.Lysa)
            {
                Kind = BlessingKind.Player,
                Category = BlessingCategory.Utility,
                Description = "+20% forage drops (total: 35%), +20% wild crop drop rate, food spoils 15% slower. Requires Hunter's Instinct.",
                RequiredFavorRank = (int)FavorRank.Disciple,
                PrerequisiteBlessings = new List<string> { BlessingIds.LysaHuntersInstinct },
                StatModifiers = new Dictionary<string, float>
                {
                    { VintageStoryStats.ForageDropRate, 0.20f },
                    { VintageStoryStats.WildCropYield, 0.20f },
                    { VintageStoryStats.FoodSpoilage, 0.15f }
                },
                SpecialEffects = new List<string> { SpecialEffects.FoodSpoilageReduction }
            },
            new(BlessingIds.LysaApexPredator, "Apex Predator", DeityType.Lysa)
            {
                Kind = BlessingKind.Player,
                Category = BlessingCategory.Combat,
                Description = "+20% animal drops (total: 35%), +10% animal harvesting speed. Requires Hunter's Instinct.",
                RequiredFavorRank = (int)FavorRank.Disciple,
                PrerequisiteBlessings = new List<string> { BlessingIds.LysaHuntersInstinct },
                StatModifiers = new Dictionary<string, float>
                {
                    { VintageStoryStats.AnimalDrops, 0.20f },
                    { VintageStoryStats.AnimalHarvestTime, 0.10f }
                },
                SpecialEffects = new List<string> { }
            },

            // Tier 3 - Zealot (2000-4999 favor) - Specialization
            new(BlessingIds.LysaAbundanceOfWild, "Abundance of the Wild", DeityType.Lysa)
            {
                Kind = BlessingKind.Player,
                Category = BlessingCategory.Utility,
                Description = "+25% forage drops (total: 60%), food spoils 25% slower (total: 40%). Requires Master Forager.",
                RequiredFavorRank = (int)FavorRank.Zealot,
                PrerequisiteBlessings = new List<string> { BlessingIds.LysaMasterForager },
                StatModifiers = new Dictionary<string, float>
                {
                    { VintageStoryStats.ForageDropRate, 0.25f },
                    { VintageStoryStats.FoodSpoilage, 0.25f }
                },
                SpecialEffects = new List<string> { SpecialEffects.FoodSpoilageReduction }
            },
            new(BlessingIds.LysaSilentDeath, "Silent Death", DeityType.Lysa)
            {
                Kind = BlessingKind.Player,
                Category = BlessingCategory.Combat,
                Description = "+15% ranged accuracy, +15% ranged damage. Requires Apex Predator.",
                RequiredFavorRank = (int)FavorRank.Zealot,
                PrerequisiteBlessings = new List<string> { BlessingIds.LysaApexPredator },
                StatModifiers = new Dictionary<string, float>
                {
                    { VintageStoryStats.RangedWeaponsAccuracy, 0.15f },
                    { VintageStoryStats.RangedWeaponsDamage, 0.15f }
                },
                SpecialEffects = new List<string> { }
            },

            // Tier 4 - Champion (5000-9999 favor) - Capstone
            new(BlessingIds.LysaAvatarOfWild, "Avatar of the Wild", DeityType.Lysa)
            {
                Kind = BlessingKind.Player,
                Category = BlessingCategory.Utility,
                Description = "+20% ranged distance, +20% reduced animal seeking range. Requires both Abundance of the Wild and Silent Death.",
                RequiredFavorRank = (int)FavorRank.Champion,
                PrerequisiteBlessings = new List<string> { BlessingIds.LysaAbundanceOfWild, BlessingIds.LysaSilentDeath },
                StatModifiers = new Dictionary<string, float>
                {
                    { VintageStoryStats.RangedWeaponsRange, 0.20f },
                    { VintageStoryStats.AnimalSeekingRange, 0.20f }
                },
                SpecialEffects = new List<string> { }
            },

            // RELIGION BLESSINGS (4 total)

            // Tier 1 - Fledgling
            new(BlessingIds.LysaHuntingParty, "Hunting Party", DeityType.Lysa)
            {
                Kind = BlessingKind.Religion,
                Category = BlessingCategory.Utility,
                Description = "All members: +15% animal and forage drops.",
                RequiredPrestigeRank = (int)PrestigeRank.Fledgling,
                StatModifiers = new Dictionary<string, float>
                {
                    { VintageStoryStats.AnimalDrops, 0.15f },
                    { VintageStoryStats.ForageDropRate, 0.15f }
                }
            },

            // Tier 2 - Established
            new(BlessingIds.LysaWildernessTribe, "Wilderness Tribe", DeityType.Lysa)
            {
                Kind = BlessingKind.Religion,
                Category = BlessingCategory.Utility,
                Description = "All members: +20% animal and forage drops; Food spoils 15% slower.",
                RequiredPrestigeRank = (int)PrestigeRank.Established,
                PrerequisiteBlessings = new List<string> { BlessingIds.LysaHuntingParty },
                StatModifiers = new Dictionary<string, float>
                {
                    { VintageStoryStats.AnimalDrops, 0.20f },
                    { VintageStoryStats.ForageDropRate, 0.20f },
                    { VintageStoryStats.FoodSpoilage, 0.15f }
                }
            },

            // Tier 3 - Renowned
            new(BlessingIds.LysaChildrenOfForest, "Children of the Forest", DeityType.Lysa)
            {
                Kind = BlessingKind.Religion,
                Category = BlessingCategory.Utility,
                Description = "All members: +25% animal and forage drops; +5% movement speed.",
                RequiredPrestigeRank = (int)PrestigeRank.Renowned,
                PrerequisiteBlessings = new List<string> { BlessingIds.LysaWildernessTribe },
                StatModifiers = new Dictionary<string, float>
                {
                    { VintageStoryStats.AnimalDrops, 0.25f },
                    { VintageStoryStats.ForageDropRate, 0.25f },
                    { VintageStoryStats.WalkSpeed, 0.05f }
                }
            },

            // Tier 4 - Legendary
            new(BlessingIds.LysaPantheonOfHunt, "Pantheon of the Hunt", DeityType.Lysa)
            {
                Kind = BlessingKind.Religion,
                Category = BlessingCategory.Utility,
                Description = "All members: +5°C temperature resistance (hot and cold).",
                RequiredPrestigeRank = (int)PrestigeRank.Legendary,
                PrerequisiteBlessings = new List<string> { BlessingIds.LysaChildrenOfForest },
                StatModifiers = new Dictionary<string, float>
                {
                    { VintageStoryStats.TemperatureResistance, 5.0f }
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
                },
                SpecialEffects = new List<string> { SpecialEffects.RareCropDiscovery }
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
                },
                SpecialEffects = new List<string> { SpecialEffects.RareCropDiscovery }
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


    #region Gaia (Pottery & Clay) - 10 Blessings (Utility-Focused)

    private static List<Blessing> GetGaiaBlessings()
    {
        return new List<Blessing>
        {
            // PLAYER BLESSINGS (6 total) - Pottery crafting & clay utility focus

            // Tier 1 - Initiate (0-499 favor) - Foundation
            new(BlessingIds.GaiaClayShaper, "Clay Shaper", DeityType.Gaia)
            {
                Kind = BlessingKind.Player,
                Category = BlessingCategory.Utility,
                Description = "Foundation for pottery crafting. +20% clay yield when digging, +10% max health.",
                RequiredFavorRank = (int)FavorRank.Initiate,
                StatModifiers = new Dictionary<string, float>
                {
                    { VintageStoryStats.ClayYield, 0.20f },
                    { VintageStoryStats.MaxHealthExtraPoints, 1.10f }
                }
            },

            // Tier 2 - Disciple (500-1999 favor) - Choose Your Path
            new(BlessingIds.GaiaMasterPotter, "Master Potter", DeityType.Gaia)
            {
                Kind = BlessingKind.Player,
                Category = BlessingCategory.Utility,
                Description =
                    "Specialize in pottery crafting. +30% chance to place an additional voxel while knapping pottery, +10% digging speed. Crafting path. Requires Clay Shaper.",
                RequiredFavorRank = (int)FavorRank.Disciple,
                PrerequisiteBlessings = new List<string> { BlessingIds.GaiaClayShaper },
                StatModifiers = new Dictionary<string, float>
                {
                    { VintageStoryStats.ClayFormingVoxelChance, 0.30f },
                    { VintageStoryStats.DiggingSpeed, 0.10f }
                }
            },
            new(BlessingIds.GaiaEarthenBuilder, "Earthen Builder", DeityType.Gaia)
            {
                Kind = BlessingKind.Player,
                Category = BlessingCategory.Utility,
                Description =
                    "Focus on utility and storage. Storage vessels +30% capacity, +15% stone yield. Utility path. Requires Clay Shaper.",
                RequiredFavorRank = (int)FavorRank.Disciple,
                PrerequisiteBlessings = new List<string> { BlessingIds.GaiaClayShaper },
                StatModifiers = new Dictionary<string, float>
                {
                    { VintageStoryStats.StorageVesselCapacity, 0.30f },
                    { VintageStoryStats.StoneYield, 0.15f }
                }
            },

            // Tier 3 - Zealot (2000-4999 favor) - Specialization
            new(BlessingIds.GaiaKilnMaster, "Kiln Master", DeityType.Gaia)
            {
                Kind = BlessingKind.Player,
                Category = BlessingCategory.Utility,
                Description =
                    "Achieve legendary pottery crafting. +40% chance to place an additional voxel while knapping pottery (total: 70%), +15% digging speed (total: 25%). Requires Master Potter.",
                RequiredFavorRank = (int)FavorRank.Zealot,
                PrerequisiteBlessings = new List<string> { BlessingIds.GaiaMasterPotter },
                StatModifiers = new Dictionary<string, float>
                {
                    { VintageStoryStats.ClayFormingVoxelChance, 0.40f },
                    { VintageStoryStats.DiggingSpeed, 0.15f }
                }
            },
            new(BlessingIds.GaiaClayArchitect, "Clay Architect", DeityType.Gaia)
            {
                Kind = BlessingKind.Player,
                Category = BlessingCategory.Utility,
                Description =
                    "Master storage and stone gathering. Storage vessels +40% capacity (total: 70%), +20% stone yield (total: 35%). Requires Earthen Builder.",
                RequiredFavorRank = (int)FavorRank.Zealot,
                PrerequisiteBlessings = new List<string> { BlessingIds.GaiaEarthenBuilder },
                StatModifiers = new Dictionary<string, float>
                {
                    { VintageStoryStats.StorageVesselCapacity, 0.40f },
                    { VintageStoryStats.StoneYield, 0.20f }
                }
            },

            // Tier 4 - Champion (5000+ favor) - Capstone (requires both paths)
            new(BlessingIds.GaiaAvatarOfClay, "Avatar of Clay", DeityType.Gaia)
            {
                Kind = BlessingKind.Player,
                Category = BlessingCategory.Utility,
                Description =
                    "Embody the mastery of clay. +10% max health. Requires both Kiln Master and Clay Architect.",
                RequiredFavorRank = (int)FavorRank.Champion,
                PrerequisiteBlessings = new List<string> { BlessingIds.GaiaKilnMaster, BlessingIds.GaiaClayArchitect },
                StatModifiers = new Dictionary<string, float>
                {
                    { VintageStoryStats.MaxHealthExtraPoints, 1.10f }
                }
            },

            // RELIGION BLESSINGS (4 total) - Shared pottery benefits

            // Tier 1 - Fledgling (0-499 prestige) - Foundation
            new(BlessingIds.GaiaPottersCircle, "Potter's Circle", DeityType.Gaia)
            {
                Kind = BlessingKind.Religion,
                Category = BlessingCategory.Utility,
                Description =
                    "Your congregation shares pottery knowledge. +15% clay yield for all members.",
                RequiredPrestigeRank = (int)PrestigeRank.Fledgling,
                StatModifiers = new Dictionary<string, float>
                {
                    { VintageStoryStats.ClayYield, 0.15f }
                }
            },

            // Tier 2 - Established (500-1999 prestige) - Coordination
            new(BlessingIds.GaiaClayGuild, "Clay Guild", DeityType.Gaia)
            {
                Kind = BlessingKind.Religion,
                Category = BlessingCategory.Utility,
                Description =
                    "A united guild of skilled potters. +20% chance to place an additional voxel while knapping pottery for all. Requires Potter's Circle.",
                RequiredPrestigeRank = (int)PrestigeRank.Established,
                PrerequisiteBlessings = new List<string> { BlessingIds.GaiaPottersCircle },
                StatModifiers = new Dictionary<string, float>
                {
                    { VintageStoryStats.ClayFormingVoxelChance, 0.20f }
                }
            },

            // Tier 3 - Renowned (2000-4999 prestige) - Elite Force
            new(BlessingIds.GaiaEarthenCommunity, "Earthen Community", DeityType.Gaia)
            {
                Kind = BlessingKind.Religion,
                Category = BlessingCategory.Utility,
                Description =
                    "A thriving pottery community. Storage vessels +25% capacity for all. Requires Clay Guild.",
                RequiredPrestigeRank = (int)PrestigeRank.Renowned,
                PrerequisiteBlessings = new List<string> { BlessingIds.GaiaClayGuild },
                StatModifiers = new Dictionary<string, float>
                {
                    { VintageStoryStats.StorageVesselCapacity, 0.25f }
                }
            },

            // Tier 4 - Legendary (5000+ prestige) - Pantheon of Clay
            new(BlessingIds.GaiaPantheonOfClay, "Pantheon of Clay", DeityType.Gaia)
            {
                Kind = BlessingKind.Religion,
                Category = BlessingCategory.Utility,
                Description =
                    "Your religion becomes legendary potters. +10% max health for all. Requires Earthen Community.",
                RequiredPrestigeRank = (int)PrestigeRank.Legendary,
                PrerequisiteBlessings = new List<string> { BlessingIds.GaiaEarthenCommunity },
                StatModifiers = new Dictionary<string, float>
                {
                    { VintageStoryStats.MaxHealthExtraPoints, 1.10f }
                }
            }
        };
    }

    #endregion
}