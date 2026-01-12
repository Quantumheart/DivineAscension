using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using DivineAscension.Constants;
using DivineAscension.Models;
using DivineAscension.Models.Enum;

namespace DivineAscension.Systems;

/// <summary>
///     Contains all blessing definitions for all domains
///     Utility-focused system: 40 blessings (4 domains × 10 blessings each)
/// </summary>
[ExcludeFromCodeCoverage]
// todo: localize the blessings
public static class BlessingDefinitions
{
    /// <summary>
    ///     Gets all blessing definitions for registration
    ///     Updated for utility-focused system (4 domains, 40 total blessings)
    /// </summary>
    public static List<Blessing> GetAllBlessings()
    {
        var blessings = new List<Blessing>();

        blessings.AddRange(GetCraftBlessings());
        blessings.AddRange(GetWildBlessings());
        blessings.AddRange(GetHarvestBlessings());
        blessings.AddRange(GetStoneBlessings());

        return blessings;
    }

    #region Craft (Forge & Craft)

    private static List<Blessing> GetCraftBlessings()
    {
        return new List<Blessing>
        {
            // PLAYER BLESSINGS (6 total) - Forge & Craft utility focus

            // Tier 1 - Initiate (0-499 favor) - Foundation
            new(BlessingIds.CraftCraftsmansTouch, "Craftsman's Touch", DeityDomain.Craft)
            {
                Kind = BlessingKind.Player,
                Category = BlessingCategory.Utility,
                Description = "+10% chance for tools to take no damage, +10% ore yield.",
                IconName = "hammer-drop",
                RequiredFavorRank = (int)FavorRank.Initiate,
                StatModifiers = new Dictionary<string, float>
                {
                    { VintageStoryStats.ToolDurability, 0.10f },
                    { VintageStoryStats.OreDropRate, 0.10f }
                }
            },

            // Tier 2 - Disciple (500-1999 favor) - Choose Your Path
            new(BlessingIds.CraftMasterworkTools, "Masterwork Tools", DeityDomain.Craft)
            {
                Kind = BlessingKind.Player,
                Category = BlessingCategory.Utility,
                Description =
                    "+15% chance for tools to take no damage (total: 25%), +10% mining speed. Requires Craftsman's Touch.",
                IconName = "miner",
                RequiredFavorRank = (int)FavorRank.Disciple,
                PrerequisiteBlessings = new List<string> { BlessingIds.CraftCraftsmansTouch },
                StatModifiers = new Dictionary<string, float>
                {
                    { VintageStoryStats.ToolDurability, 0.15f },
                    { VintageStoryStats.MiningSpeed, 0.10f }
                }
            },
            new(BlessingIds.CraftForgebornEndurance, "Forgeborn Endurance", DeityDomain.Craft)
            {
                Kind = BlessingKind.Player,
                Category = BlessingCategory.Defense,
                Description = "+10% melee weapon damage, +10% max health. Requires Craftsman's Touch.",
                IconName = "anvil",
                RequiredFavorRank = (int)FavorRank.Disciple,
                PrerequisiteBlessings = new List<string> { BlessingIds.CraftCraftsmansTouch },
                StatModifiers = new Dictionary<string, float>
                {
                    { VintageStoryStats.MeleeWeaponsDamage, 0.10f },
                    { VintageStoryStats.MaxHealthExtraPoints, 1.10f }
                }
            },

            // Tier 3 - Zealot (2000-4999 favor) - Specialization
            new(BlessingIds.CraftLegendarySmith, "Legendary Smith", DeityDomain.Craft)
            {
                Kind = BlessingKind.Player,
                Category = BlessingCategory.Utility,
                Description =
                    "+20% chance for tools to take no damage (total: 45%), +15% ore yield (total: 25%). Requires Masterwork Tools.",
                IconName = "sword-smithing",
                RequiredFavorRank = (int)FavorRank.Zealot,
                PrerequisiteBlessings = new List<string> { BlessingIds.CraftMasterworkTools },
                StatModifiers = new Dictionary<string, float>
                {
                    { VintageStoryStats.ToolDurability, 0.20f },
                    { VintageStoryStats.OreDropRate, 0.15f }
                },
                SpecialEffects = new List<string>()
            },
            new(BlessingIds.CraftUnyielding, "Unyielding", DeityDomain.Craft)
            {
                Kind = BlessingKind.Player,
                Category = BlessingCategory.Defense,
                Description =
                    "+10% reduced armor durability loss, +15% max health (total: 25%). Requires Forgeborn Endurance.",
                IconName = "shield",
                RequiredFavorRank = (int)FavorRank.Zealot,
                PrerequisiteBlessings = new List<string> { BlessingIds.CraftForgebornEndurance },
                StatModifiers = new Dictionary<string, float>
                {
                    { VintageStoryStats.ArmorDurabilityLoss, -0.10f },
                    { VintageStoryStats.MaxHealthExtraPoints, 1.15f }
                }
            },

            // Tier 4 - Champion (5000+ favor) - Capstone (requires both paths)
            new(BlessingIds.CraftAvatarOfForge, "Avatar of the Forge", DeityDomain.Craft)
            {
                Kind = BlessingKind.Player,
                Category = BlessingCategory.Utility,
                Description =
                    "Tools repair 1 durability per 5 minutes. +10% armor walk speed. Requires both Legendary Smith and Unyielding.",
                IconName = "sword-mold",
                RequiredFavorRank = (int)FavorRank.Champion,
                PrerequisiteBlessings = new List<string>
                    { BlessingIds.CraftLegendarySmith, BlessingIds.CraftUnyielding },
                StatModifiers = new Dictionary<string, float>
                {
                    { VintageStoryStats.ArmorWalkSpeedAffectedness, -0.10f }
                },
                SpecialEffects = new List<string> { SpecialEffects.PassiveToolRepair1Per5Min }
            },

            // RELIGION BLESSINGS (4 total) - Shared workshop bonuses

            // Tier 1 - Fledgling (0-499 prestige) - Foundation
            new(BlessingIds.CraftSharedWorkshop, "Shared Workshop", DeityDomain.Craft)
            {
                Kind = BlessingKind.Religion,
                Category = BlessingCategory.Utility,
                Description = "+10% chance for tools to take no damage, +10% ore yield for all members.",
                IconName = "warehouse",
                RequiredPrestigeRank = (int)PrestigeRank.Fledgling,
                StatModifiers = new Dictionary<string, float>
                {
                    { VintageStoryStats.ToolDurability, 0.10f },
                    { VintageStoryStats.OreDropRate, 0.10f }
                }
            },

            // Tier 2 - Established (500-1999 prestige) - Coordination
            new(BlessingIds.CraftGuildOfSmiths, "Guild of Smiths", DeityDomain.Craft)
            {
                Kind = BlessingKind.Religion,
                Category = BlessingCategory.Utility,
                Description =
                    "+15% chance for tools to take no damage, +15% ore yield for all. Requires Shared Workshop.",
                IconName = "blacksmith",
                RequiredPrestigeRank = (int)PrestigeRank.Established,
                PrerequisiteBlessings = new List<string> { BlessingIds.CraftSharedWorkshop },
                StatModifiers = new Dictionary<string, float>
                {
                    { VintageStoryStats.ToolDurability, 0.15f },
                    { VintageStoryStats.OreDropRate, 0.15f }
                }
            },

            // Tier 3 - Renowned (2000-4999 prestige) - Elite Force
            new(BlessingIds.CraftMasterCraftsmen, "Master Craftsmen", DeityDomain.Craft)
            {
                Kind = BlessingKind.Religion,
                Category = BlessingCategory.Utility,
                Description =
                    "+20% chance for tools to take no damage, +20% ore yield, +10% armor walk speed for all. Requires Guild of Smiths.",
                IconName = "team-upgrade",
                RequiredPrestigeRank = (int)PrestigeRank.Renowned,
                PrerequisiteBlessings = new List<string> { BlessingIds.CraftGuildOfSmiths },
                StatModifiers = new Dictionary<string, float>
                {
                    { VintageStoryStats.ToolDurability, 0.20f },
                    { VintageStoryStats.OreDropRate, 0.20f },
                    { VintageStoryStats.ArmorWalkSpeedAffectedness, -0.10f }
                }
            },

            // Tier 4 - Legendary (5000+ prestige) - Pantheon of Creation
            new(BlessingIds.CraftPantheonOfCreation, "Pantheon of Creation", DeityDomain.Craft)
            {
                Kind = BlessingKind.Religion,
                Category = BlessingCategory.Utility,
                Description = "+10% max health for all. Requires Master Craftsmen.",
                IconName = "freemasonry",
                RequiredPrestigeRank = (int)PrestigeRank.Legendary,
                PrerequisiteBlessings = new List<string> { BlessingIds.CraftMasterCraftsmen },
                StatModifiers = new Dictionary<string, float>
                {
                    { VintageStoryStats.MaxHealthExtraPoints, 1.10f }
                },
                SpecialEffects = new List<string>()
            }
        };
    }

    #endregion

    #region Wild (Hunt & Wild)

    private static List<Blessing> GetWildBlessings()
    {
        return new List<Blessing>
        {
            // PLAYER BLESSINGS (6 total) - Hunt & Wild utility focus (v2.0.0 Utility Redesign)

            // Tier 1 - Initiate (0-499 favor)
            new(BlessingIds.WildHuntersInstinct, "Hunter's Instinct", DeityDomain.Wild)
            {
                Kind = BlessingKind.Player,
                Category = BlessingCategory.Utility,
                Description = "+15% animal and forage drops, +5% movement speed.",
                IconName = "paw",
                RequiredFavorRank = (int)FavorRank.Initiate,
                StatModifiers = new Dictionary<string, float>
                {
                    { VintageStoryStats.AnimalDrops, 0.15f },
                    { VintageStoryStats.ForageDropRate, 0.15f },
                    { VintageStoryStats.WalkSpeed, 0.05f }
                },
                SpecialEffects = new List<string>()
            },

            // Tier 2 - Disciple (500-1999 favor) - Choose Path
            new(BlessingIds.WildMasterForager, "Master Forager", DeityDomain.Wild)
            {
                Kind = BlessingKind.Player,
                Category = BlessingCategory.Utility,
                Description =
                    "+20% forage drops (total: 35%), +20% wild crop drop rate, food spoils 15% slower. Requires Hunter's Instinct.",
                IconName = "fruit-bowl",
                RequiredFavorRank = (int)FavorRank.Disciple,
                PrerequisiteBlessings = new List<string> { BlessingIds.WildHuntersInstinct },
                StatModifiers = new Dictionary<string, float>
                {
                    { VintageStoryStats.ForageDropRate, 0.20f },
                    { VintageStoryStats.WildCropYield, 0.20f },
                    { VintageStoryStats.FoodSpoilage, 0.15f }
                },
                SpecialEffects = new List<string> { SpecialEffects.FoodSpoilageReduction }
            },
            new(BlessingIds.WildApexPredator, "Apex Predator", DeityDomain.Wild)
            {
                Kind = BlessingKind.Player,
                Category = BlessingCategory.Combat,
                Description =
                    "+20% animal drops (total: 35%), +10% animal harvesting speed. Requires Hunter's Instinct.",
                IconName = "tiger-head",
                RequiredFavorRank = (int)FavorRank.Disciple,
                PrerequisiteBlessings = new List<string> { BlessingIds.WildHuntersInstinct },
                StatModifiers = new Dictionary<string, float>
                {
                    { VintageStoryStats.AnimalDrops, 0.20f },
                    { VintageStoryStats.AnimalHarvestTime, 0.10f }
                },
                SpecialEffects = new List<string>()
            },

            // Tier 3 - Zealot (2000-4999 favor) - Specialization
            new(BlessingIds.WildAbundanceOfWild, "Abundance of the Wild", DeityDomain.Wild)
            {
                Kind = BlessingKind.Player,
                Category = BlessingCategory.Utility,
                Description =
                    "+25% forage drops (total: 60%), food spoils 25% slower (total: 40%). Requires Master Forager.",
                IconName = "strawberry",
                RequiredFavorRank = (int)FavorRank.Zealot,
                PrerequisiteBlessings = new List<string> { BlessingIds.WildMasterForager },
                StatModifiers = new Dictionary<string, float>
                {
                    { VintageStoryStats.ForageDropRate, 0.25f },
                    { VintageStoryStats.FoodSpoilage, 0.25f }
                },
                SpecialEffects = new List<string> { SpecialEffects.FoodSpoilageReduction }
            },
            new(BlessingIds.WildSilentDeath, "Silent Death", DeityDomain.Wild)
            {
                Kind = BlessingKind.Player,
                Category = BlessingCategory.Combat,
                Description = "+15% ranged accuracy, +15% ranged damage. Requires Apex Predator.",
                IconName = "bow-arrow",
                RequiredFavorRank = (int)FavorRank.Zealot,
                PrerequisiteBlessings = new List<string> { BlessingIds.WildApexPredator },
                StatModifiers = new Dictionary<string, float>
                {
                    { VintageStoryStats.RangedWeaponsAccuracy, 0.15f },
                    { VintageStoryStats.RangedWeaponsDamage, 0.15f }
                },
                SpecialEffects = new List<string>()
            },

            // Tier 4 - Champion (5000-9999 favor) - Capstone
            new(BlessingIds.WildAvatarOfWild, "Avatar of the Wild", DeityDomain.Wild)
            {
                Kind = BlessingKind.Player,
                Category = BlessingCategory.Utility,
                Description =
                    "+20% ranged distance, +20% reduced animal seeking range. Requires both Abundance of the Wild and Silent Death.",
                IconName = "bear-head",
                RequiredFavorRank = (int)FavorRank.Champion,
                PrerequisiteBlessings = new List<string>
                    { BlessingIds.WildAbundanceOfWild, BlessingIds.WildSilentDeath },
                StatModifiers = new Dictionary<string, float>
                {
                    { VintageStoryStats.RangedWeaponsRange, 0.20f },
                    { VintageStoryStats.AnimalSeekingRange, 0.20f }
                },
                SpecialEffects = new List<string>()
            },

            // RELIGION BLESSINGS (4 total)

            // Tier 1 - Fledgling
            new(BlessingIds.WildHuntingParty, "Hunting Party", DeityDomain.Wild)
            {
                Kind = BlessingKind.Religion,
                Category = BlessingCategory.Utility,
                Description = "All members: +15% animal and forage drops.",
                IconName = "hunting-horn",
                RequiredPrestigeRank = (int)PrestigeRank.Fledgling,
                StatModifiers = new Dictionary<string, float>
                {
                    { VintageStoryStats.AnimalDrops, 0.15f },
                    { VintageStoryStats.ForageDropRate, 0.15f }
                }
            },

            // Tier 2 - Established
            new(BlessingIds.WildWildernessTribe, "Wilderness Tribe", DeityDomain.Wild)
            {
                Kind = BlessingKind.Religion,
                Category = BlessingCategory.Utility,
                Description = "All members: +20% animal and forage drops; Food spoils 15% slower.",
                IconName = "campfire",
                RequiredPrestigeRank = (int)PrestigeRank.Established,
                PrerequisiteBlessings = new List<string> { BlessingIds.WildHuntingParty },
                StatModifiers = new Dictionary<string, float>
                {
                    { VintageStoryStats.AnimalDrops, 0.20f },
                    { VintageStoryStats.ForageDropRate, 0.20f },
                    { VintageStoryStats.FoodSpoilage, 0.15f }
                }
            },

            // Tier 3 - Renowned
            new(BlessingIds.WildChildrenOfForest, "Children of the Forest", DeityDomain.Wild)
            {
                Kind = BlessingKind.Religion,
                Category = BlessingCategory.Utility,
                Description = "All members: +25% animal and forage drops; +5% movement speed.",
                IconName = "tree",
                RequiredPrestigeRank = (int)PrestigeRank.Renowned,
                PrerequisiteBlessings = new List<string> { BlessingIds.WildWildernessTribe },
                StatModifiers = new Dictionary<string, float>
                {
                    { VintageStoryStats.AnimalDrops, 0.25f },
                    { VintageStoryStats.ForageDropRate, 0.25f },
                    { VintageStoryStats.WalkSpeed, 0.05f }
                }
            },

            // Tier 4 - Legendary
            new(BlessingIds.WildPantheonOfHunt, "Pantheon of the Hunt", DeityDomain.Wild)
            {
                Kind = BlessingKind.Religion,
                Category = BlessingCategory.Utility,
                Description = "All members: +5°C temperature resistance (hot and cold).",
                IconName = "pierced-heart",
                RequiredPrestigeRank = (int)PrestigeRank.Legendary,
                PrerequisiteBlessings = new List<string> { BlessingIds.WildChildrenOfForest },
                StatModifiers = new Dictionary<string, float>
                {
                    { VintageStoryStats.TemperatureResistance, 5.0f }
                }
            }
        };
    }

    #endregion

    #region Harvest (Agriculture & Light)

    private static List<Blessing> GetHarvestBlessings()
    {
        return new List<Blessing>
        {
            // PLAYER BLESSINGS (6 total) - Agriculture & Cooking utility focus

            // Tier 1 - Initiate (0-499 favor) - Foundation
            new(BlessingIds.HarvestSunsBlessing, "Sun's Blessing", DeityDomain.Harvest)
            {
                Kind = BlessingKind.Player,
                Category = BlessingCategory.Utility,
                Description =
                    "Light brings life and growth. +12% crop yield, +10% satiety from all food, +3°C heat resistance, light sources provide +1°C warmth radius.",
                IconName = "sunrise",
                RequiredFavorRank = (int)FavorRank.Initiate,
                StatModifiers = new Dictionary<string, float>
                {
                    { VintageStoryStats.CropYield, 0.12f },
                    { VintageStoryStats.Satiety, 0.10f },
                    { VintageStoryStats.HeatResistance, 3.0f }
                },
                SpecialEffects = new List<string> { SpecialEffects.LightWarmthBonus }
            },

            // Tier 2 - Disciple (500-1999 favor) - Choose Your Path
            new(BlessingIds.HarvestBountifulHarvest, "Bountiful Harvest", DeityDomain.Harvest)
            {
                Kind = BlessingKind.Player,
                Category = BlessingCategory.Utility,
                Description =
                    "Specialize in farming excellence. +15% crop yield, +12% satiety from crops, crops have 15% chance for bonus seeds, +15% chance to find rare crop variants. Agriculture path. Requires Sun's Blessing.",
                IconName = "wheat",
                RequiredFavorRank = (int)FavorRank.Disciple,
                PrerequisiteBlessings = new List<string> { BlessingIds.HarvestSunsBlessing },
                StatModifiers = new Dictionary<string, float>
                {
                    { VintageStoryStats.CropYield, 0.15f },
                    { VintageStoryStats.SeedDropChance, 0.15f },
                    { VintageStoryStats.RareCropChance, 0.15f }
                },
                SpecialEffects = new List<string> { SpecialEffects.RareCropDiscovery }
            },
            new(BlessingIds.HarvestBakersTouch, "Baker's Touch", DeityDomain.Harvest)
            {
                Kind = BlessingKind.Player,
                Category = BlessingCategory.Utility,
                Description =
                    "Master the art of food preparation. Cooking/baking yields +25% more food, +15% satiety from cooked food, food spoils 20% slower, +5°C heat resistance. Food preparation path. Requires Sun's Blessing.",
                IconName = "sliced-bread",
                RequiredFavorRank = (int)FavorRank.Disciple,
                PrerequisiteBlessings = new List<string> { BlessingIds.HarvestSunsBlessing },
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
            new(BlessingIds.HarvestMasterFarmer, "Master Farmer", DeityDomain.Harvest)
            {
                Kind = BlessingKind.Player,
                Category = BlessingCategory.Utility,
                Description =
                    "Achieve ultimate farming mastery. +20% crop yield, +18% satiety from crops, crops have 25% chance for bonus seeds, +30% chance to find rare crop variants, wild crops give +40% yield. Requires Bountiful Harvest.",
                IconName = "farmer",
                RequiredFavorRank = (int)FavorRank.Zealot,
                PrerequisiteBlessings = new List<string> { BlessingIds.HarvestBountifulHarvest },
                StatModifiers = new Dictionary<string, float>
                {
                    { VintageStoryStats.CropYield, 0.20f },
                    { VintageStoryStats.SeedDropChance, 0.25f },
                    { VintageStoryStats.RareCropChance, 0.30f },
                    { VintageStoryStats.WildCropYield, 0.40f }
                },
                SpecialEffects = new List<string> { SpecialEffects.RareCropDiscovery }
            },
            new(BlessingIds.HarvestDivineKitchen, "Divine Kitchen", DeityDomain.Harvest)
            {
                Kind = BlessingKind.Player,
                Category = BlessingCategory.Utility,
                Description =
                    "Create incredibly nutritious meals. Cooking yields +35% more, +25% satiety from cooked food, food spoils 30% slower, +7°C heat resistance, meals provide temporary +5% max health buff. Requires Baker's Touch.",
                IconName = "cooking-pot",
                RequiredFavorRank = (int)FavorRank.Zealot,
                PrerequisiteBlessings = new List<string> { BlessingIds.HarvestBakersTouch },
                StatModifiers = new Dictionary<string, float>
                {
                    { VintageStoryStats.CookingYield, 0.35f },
                    { VintageStoryStats.CookedFoodSatiety, 0.25f },
                    { VintageStoryStats.FoodSpoilage, 0.30f },
                    { VintageStoryStats.HeatResistance, 7.0f }
                },
                SpecialEffects = new List<string>
                    { SpecialEffects.TempHealthBuff5, SpecialEffects.FoodSpoilageReduction }
            },

            // Tier 4 - Champion (5000+ favor) - Capstone (requires both paths)
            new(BlessingIds.HarvestAvatarOfAbundance, "Avatar of Abundance", DeityDomain.Harvest)
            {
                Kind = BlessingKind.Player,
                Category = BlessingCategory.Utility,
                Description =
                    "Embody the endless bounty of the harvest. +8% movement speed, +10% max health, never suffer malnutrition penalties, can create blessed meals with powerful buffs. Requires both Master Farmer and Divine Kitchen.",
                IconName = "cornucopia",
                RequiredFavorRank = (int)FavorRank.Champion,
                PrerequisiteBlessings = new List<string>
                    { BlessingIds.HarvestMasterFarmer, BlessingIds.HarvestDivineKitchen },
                StatModifiers = new Dictionary<string, float>
                {
                    { VintageStoryStats.WalkSpeed, 0.08f },
                    { VintageStoryStats.MaxHealthExtraPoints, 1.10f }
                },
                SpecialEffects = new List<string> { SpecialEffects.NeverMalnourished, SpecialEffects.BlessedMeals }
            },

            // RELIGION BLESSINGS (4 total) - Shared agricultural bonuses

            // Tier 1 - Fledgling (0-499 prestige)
            new(BlessingIds.HarvestCommunityFarm, "Community Farm", DeityDomain.Harvest)
            {
                Kind = BlessingKind.Religion,
                Category = BlessingCategory.Utility,
                Description =
                    "Your congregation shares agricultural knowledge. +10% crop yield, +8% satiety from all food for all members.",
                IconName = "greenhouse",
                RequiredPrestigeRank = (int)PrestigeRank.Fledgling,
                StatModifiers = new Dictionary<string, float>
                {
                    { VintageStoryStats.CropYield, 0.10f },
                    { VintageStoryStats.Satiety, 0.08f }
                }
            },

            // Tier 2 - Established (500-1999 prestige)
            new(BlessingIds.HarvestHarvestFestival, "Harvest Festival", DeityDomain.Harvest)
            {
                Kind = BlessingKind.Religion,
                Category = BlessingCategory.Utility,
                Description =
                    "Celebrate abundant harvests together. +15% crop yield, +12% satiety from all food, food spoils 10% slower for all. Requires Community Farm.",
                IconName = "sun-priest",
                RequiredPrestigeRank = (int)PrestigeRank.Established,
                PrerequisiteBlessings = new List<string> { BlessingIds.HarvestCommunityFarm },
                StatModifiers = new Dictionary<string, float>
                {
                    { VintageStoryStats.CropYield, 0.15f },
                    { VintageStoryStats.Satiety, 0.12f },
                    { VintageStoryStats.FoodSpoilage, 0.10f }
                }
            },

            // Tier 3 - Renowned (2000-4999 prestige)
            new(BlessingIds.HarvestLandOfPlenty, "Land of Plenty", DeityDomain.Harvest)
            {
                Kind = BlessingKind.Religion,
                Category = BlessingCategory.Utility,
                Description =
                    "Your land becomes legendary for its bounty. +22% crop yield, +18% satiety from all food, food spoils 18% slower, +5°C heat resistance for all. Requires Harvest Festival.",
                IconName = "field",
                RequiredPrestigeRank = (int)PrestigeRank.Renowned,
                PrerequisiteBlessings = new List<string> { BlessingIds.HarvestHarvestFestival },
                StatModifiers = new Dictionary<string, float>
                {
                    { VintageStoryStats.CropYield, 0.22f },
                    { VintageStoryStats.Satiety, 0.18f },
                    { VintageStoryStats.FoodSpoilage, 0.18f },
                    { VintageStoryStats.HeatResistance, 5.0f }
                }
            },

            // Tier 4 - Legendary (5000+ prestige) - Pantheon of Light
            new(BlessingIds.HarvestPantheonOfLight, "Pantheon of Light", DeityDomain.Harvest)
            {
                Kind = BlessingKind.Religion,
                Category = BlessingCategory.Utility,
                IconName = "sun",
                Description =
                    "Your religion becomes the source of endless bounty. +30% crop yield, +20% satiety from all food, food spoils 25% slower, +8°C heat resistance, religion can build Sacred Granary for all. Requires Land of Plenty.",
                RequiredPrestigeRank = (int)PrestigeRank.Legendary,
                PrerequisiteBlessings = new List<string> { BlessingIds.HarvestLandOfPlenty },
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


    #region Stone (Pottery & Clay)

    private static List<Blessing> GetStoneBlessings()
    {
        return new List<Blessing>
        {
            // PLAYER BLESSINGS (6 total) - Pottery crafting & clay utility focus

            // Tier 1 - Initiate (0-499 favor) - Foundation
            new(BlessingIds.StoneClayShaper, "Clay Shaper", DeityDomain.Stone)
            {
                Kind = BlessingKind.Player,
                Category = BlessingCategory.Utility,
                Description = "Foundation for pottery crafting. +20% clay yield when digging, +10% max health.",
                IconName = "dig-hole",
                RequiredFavorRank = (int)FavorRank.Initiate,
                StatModifiers = new Dictionary<string, float>
                {
                    { VintageStoryStats.ClayYield, 0.20f },
                    { VintageStoryStats.MaxHealthExtraPoints, 1.10f }
                }
            },

            // Tier 2 - Disciple (500-1999 favor) - Choose Your Path
            new(BlessingIds.StoneMasterPotter, "Master Potter", DeityDomain.Stone)
            {
                Kind = BlessingKind.Player,
                Category = BlessingCategory.Utility,
                Description =
                    "Specialize in pottery crafting. +10% chance to craft duplicate pottery items on completion, +10% digging speed. Crafting path. Requires Clay Shaper.",
                IconName = "painted-pottery",
                RequiredFavorRank = (int)FavorRank.Disciple,
                PrerequisiteBlessings = new List<string> { BlessingIds.StoneClayShaper },
                StatModifiers = new Dictionary<string, float>
                {
                    { VintageStoryStats.PotteryBatchCompletionChance, 0.25f },
                    { VintageStoryStats.DiggingSpeed, 0.10f }
                },
                SpecialEffects = new List<string> { SpecialEffects.PotteryBatchCompletionBonus }
            },
            new(BlessingIds.StoneEarthenBuilder, "Earthen Builder", DeityDomain.Stone)
            {
                Kind = BlessingKind.Player,
                Category = BlessingCategory.Utility,
                Description =
                    "Focus on resilience and stonework. +15% armor effectiveness, +15% stone yield. Utility path. Requires Clay Shaper.",
                IconName = "clay-brick",
                RequiredFavorRank = (int)FavorRank.Disciple,
                PrerequisiteBlessings = new List<string> { BlessingIds.StoneClayShaper },
                StatModifiers = new Dictionary<string, float>
                {
                    { VintageStoryStats.ArmorEffectiveness, 0.15f },
                    { VintageStoryStats.StoneYield, 0.15f }
                }
            },

            // Tier 3 - Zealot (2000-4999 favor) - Specialization
            new(BlessingIds.StoneKilnMaster, "Kiln Master", DeityDomain.Stone)
            {
                Kind = BlessingKind.Player,
                Category = BlessingCategory.Utility,
                Description =
                    "Achieve legendary pottery crafting. +15% chance to craft duplicate pottery items (total: 45%), +15% digging speed (total: 25%). Requires Master Potter.",
                IconName = "fire-bowl",
                RequiredFavorRank = (int)FavorRank.Zealot,
                PrerequisiteBlessings = new List<string> { BlessingIds.StoneMasterPotter },
                StatModifiers = new Dictionary<string, float>
                {
                    { VintageStoryStats.PotteryBatchCompletionChance, 0.35f },
                    { VintageStoryStats.DiggingSpeed, 0.15f }
                },
                SpecialEffects = new List<string> { SpecialEffects.PotteryBatchCompletionBonus }
            },
            new(BlessingIds.StoneClayArchitect, "Clay Architect", DeityDomain.Stone)
            {
                Kind = BlessingKind.Player,
                Category = BlessingCategory.Utility,
                Description =
                    "Master fortification and stone gathering. +20% armor effectiveness (total: 35%), +20% stone yield (total: 35%). Requires Earthen Builder.",
                IconName = "concrete-bag",
                RequiredFavorRank = (int)FavorRank.Zealot,
                PrerequisiteBlessings = new List<string> { BlessingIds.StoneEarthenBuilder },
                StatModifiers = new Dictionary<string, float>
                {
                    { VintageStoryStats.ArmorEffectiveness, 0.20f },
                    { VintageStoryStats.StoneYield, 0.20f }
                }
            },

            // Tier 4 - Champion (5000+ favor) - Capstone (requires both paths)
            new(BlessingIds.StoneAvatarOfClay, "Avatar of Clay", DeityDomain.Stone)
            {
                Kind = BlessingKind.Player,
                Category = BlessingCategory.Utility,
                Description =
                    "Embody the mastery of clay. +10% max health. Requires both Kiln Master and Clay Architect.",
                IconName = "clay-golem",
                RequiredFavorRank = (int)FavorRank.Champion,
                PrerequisiteBlessings =
                    new List<string> { BlessingIds.StoneKilnMaster, BlessingIds.StoneClayArchitect },
                StatModifiers = new Dictionary<string, float>
                {
                    { VintageStoryStats.MaxHealthExtraPoints, 1.10f }
                }
            },

            // RELIGION BLESSINGS (4 total) - Shared pottery benefits

            // Tier 1 - Fledgling (0-499 prestige) - Foundation
            new(BlessingIds.StonePottersCircle, "Potter's Circle", DeityDomain.Stone)
            {
                Kind = BlessingKind.Religion,
                Category = BlessingCategory.Utility,
                Description =
                    "Your congregation shares pottery knowledge. +15% clay yield for all members.",
                IconName = "cycle",
                RequiredPrestigeRank = (int)PrestigeRank.Fledgling,
                StatModifiers = new Dictionary<string, float>
                {
                    { VintageStoryStats.ClayYield, 0.15f }
                }
            },

            // Tier 2 - Established (500-1999 prestige)
            new(BlessingIds.StoneClayGuild, "Clay Guild", DeityDomain.Stone)
            {
                Kind = BlessingKind.Religion,
                Category = BlessingCategory.Utility,
                Description =
                    "A united guild of skilled potters. +5% batch completion chance (craft duplicate pottery) for all. Requires Potter's Circle.",
                IconName = "team-upgrade",
                RequiredPrestigeRank = (int)PrestigeRank.Established,
                PrerequisiteBlessings = new List<string> { BlessingIds.StonePottersCircle },
                StatModifiers = new Dictionary<string, float>
                {
                    { VintageStoryStats.PotteryBatchCompletionChance, 0.20f }
                },
                SpecialEffects = new List<string> { SpecialEffects.PotteryBatchCompletionBonus }
            },

            // Tier 3 - Renowned (2000-4999 prestige)
            new(BlessingIds.StoneEarthenCommunity, "Earthen Community", DeityDomain.Stone)
            {
                Kind = BlessingKind.Religion,
                Category = BlessingCategory.Utility,
                Description =
                    "A thriving fortified community. +15% armor effectiveness for all. Requires Clay Guild.",
                IconName = "armor-upgrade",
                RequiredPrestigeRank = (int)PrestigeRank.Renowned,
                PrerequisiteBlessings = new List<string> { BlessingIds.StoneClayGuild },
                StatModifiers = new Dictionary<string, float>
                {
                    { VintageStoryStats.ArmorEffectiveness, 0.15f }
                }
            },

            // Tier 4 - Legendary (5000+ prestige) - Pantheon of Clay
            new(BlessingIds.StonePantheonOfClay, "Pantheon of Clay", DeityDomain.Stone)
            {
                Kind = BlessingKind.Religion,
                Category = BlessingCategory.Utility,
                Description =
                    "Your religion becomes legendary potters. +10% max health for all. Requires Earthen Community.",
                IconName = "mineral-heart",
                RequiredPrestigeRank = (int)PrestigeRank.Legendary,
                PrerequisiteBlessings = new List<string> { BlessingIds.StoneEarthenCommunity },
                StatModifiers = new Dictionary<string, float>
                {
                    { VintageStoryStats.MaxHealthExtraPoints, 1.10f }
                }
            }
        };
    }

    #endregion
}