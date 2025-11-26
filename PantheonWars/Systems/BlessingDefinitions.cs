using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using PantheonWars.Constants;
using PantheonWars.Models;
using PantheonWars.Models.Enum;
using Vintagestory.GameContent;

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

    #region Khoras (Forge & Craft) - 10 Blessings (Utility-Focused)

    private static List<Blessing> GetKhorasBlessings()
    {
        return new List<Blessing>
        {
            // PLAYER BLESSINGS (6 total) - Forge & Craft utility focus

            // Tier 1 - Initiate (0-499 favor) - Foundation
            new(BlessingIds.KhorasCraftsmansTouch, "Craftsman's Touch", DeityType.Khoras)
            {
                Kind = BlessingKind.Player,
                Type = EnumTraitType.Positive,
                Category = BlessingCategory.Utility,
                Description = "Your devotion to the forge strengthens your craft. Tools/weapons lose durability 10% slower, +10% ore yield when mining, +3°C cold resistance.",
                RequiredFavorRank = (int)FavorRank.Initiate,
                StatModifiers = new Dictionary<string, float>
                {
                    { VintageStoryStats.ToolDurability, 0.10f },
                    { VintageStoryStats.OreYield, 0.10f },
                    { VintageStoryStats.ColdResistance, 3.0f }
                }
            },

            // Tier 2 - Disciple (500-1999 favor) - Choose Your Path
            new(BlessingIds.KhorasMasterworkTools, "Masterwork Tools", DeityType.Khoras)
            {
                Kind = BlessingKind.Player,
                Category = BlessingCategory.Utility,
                Description =
                    "Master the craft of tool-making. Tools last 15% longer, +8% mining/chopping speed, -15% tool repair costs. Utility path. Requires Craftsman's Touch.",
                RequiredFavorRank = (int)FavorRank.Disciple,
                PrerequisiteBlessings = new List<string> { BlessingIds.KhorasCraftsmansTouch },
                StatModifiers = new Dictionary<string, float>
                {
                    { VintageStoryStats.ToolDurability, 0.15f },
                    { VintageStoryStats.MiningSpeed, 0.08f },
                    { VintageStoryStats.ChoppingSpeed, 0.08f },
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
                    { VintageStoryStats.OreYield, 0.15f },
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
                    "Embody the eternal forge. Tools repair 1 durability per 5 minutes in inventory, -10% material costs for smithing, +12% mining/chopping speed. Requires both Legendary Smith and Unyielding.",
                RequiredFavorRank = (int)FavorRank.Champion,
                PrerequisiteBlessings = new List<string> { BlessingIds.KhorasLegendarySmith, BlessingIds.KhorasUnyielding },
                StatModifiers = new Dictionary<string, float>
                {
                    { VintageStoryStats.SmithingCostReduction, 0.10f },
                    { VintageStoryStats.MiningSpeed, 0.12f },
                    { VintageStoryStats.ChoppingSpeed, 0.12f }
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
                    { VintageStoryStats.OreYield, 0.08f }
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
                    { VintageStoryStats.OreYield, 0.12f },
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
                    { VintageStoryStats.OreYield, 0.15f },
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
                    { VintageStoryStats.OreYield, 0.20f },
                    { VintageStoryStats.ColdResistance, 8.0f },
                    { VintageStoryStats.MiningSpeed, 0.10f },
                    { VintageStoryStats.ChoppingSpeed, 0.10f }
                },
                SpecialEffects = new List<string> { SpecialEffects.PassiveToolRepair1Per10Min }
            }
        };
    }

    #endregion

    #region Lysa (Hunt) - 10 Blessings (Refactored)

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
                Description = "Foundation for wilderness survival. Double harvest chance on bushes/plants +10%, +5% movement speed, see animal tracks highlighted.",
                RequiredFavorRank = (int)FavorRank.Initiate,
                StatModifiers = new Dictionary<string, float>
                {
                    { VintageStoryStats.DoubleHarvestChance, 0.10f },
                    { VintageStoryStats.WalkSpeed, 0.05f }
                },
                SpecialEffects = new List<string> { SpecialEffects.TrackingVision }
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
                    { VintageStoryStats.DoubleHarvestChance, 0.12f },
                    { VintageStoryStats.FoodSpoilage, 0.15f },
                    { VintageStoryStats.Satiety, 0.10f } // Note: Logic assumes Satiety applies generally or handled specially
                },
                SpecialEffects = new List<string> { SpecialEffects.FoodSpoilageReduction }
            },
            new(BlessingIds.LysaApexPredator, "Apex Predator", DeityType.Lysa)
            {
                Kind = BlessingKind.Player,
                Category = BlessingCategory.Combat,
                Description = "Focuses on hunting efficiency and stealth. +12% damage vs animals, animal drops +20%, sneak movement 25% quieter. Hunting Path. Requires Hunter's Instinct.",
                RequiredFavorRank = (int)FavorRank.Disciple,
                PrerequisiteBlessings = new List<string> { BlessingIds.LysaHuntersInstinct },
                StatModifiers = new Dictionary<string, float>
                {
                    { VintageStoryStats.AnimalDamage, 0.12f },
                    { VintageStoryStats.AnimalDrops, 0.20f },
                    { VintageStoryStats.ToolDurability, 0.15f } // Bow/Spear durability
                },
                SpecialEffects = new List<string> { SpecialEffects.StealthMovementQuiet }
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
                    { VintageStoryStats.Satiety, 0.15f } 
                },
                SpecialEffects = new List<string> { SpecialEffects.RareForageChance, SpecialEffects.FoodSpoilageReduction }
            },
            new(BlessingIds.LysaSilentDeath, "Silent Death", DeityType.Lysa)
            {
                Kind = BlessingKind.Player,
                Category = BlessingCategory.Combat,
                Description = "Master hunter. +18% damage vs animals, animal drops +25%, arrows/spears 8% chance to not consume, animals detect you 40% less easily. Requires Apex Predator.",
                RequiredFavorRank = (int)FavorRank.Zealot,
                PrerequisiteBlessings = new List<string> { BlessingIds.LysaApexPredator },
                StatModifiers = new Dictionary<string, float>
                {
                    { VintageStoryStats.AnimalDamage, 0.18f },
                    { VintageStoryStats.AnimalDrops, 0.25f }
                },
                SpecialEffects = new List<string> { SpecialEffects.AmmoConservation, SpecialEffects.StealthBonus }
            },

            // Tier 4 - Champion (5000-9999 favor) - Capstone
            new(BlessingIds.LysaAvatarOfWild, "Avatar of the Wild", DeityType.Lysa)
            {
                Kind = BlessingKind.Player,
                Category = BlessingCategory.Utility,
                Description = "True master of the wilderness. +8% movement speed, +8°C temperature resistance, compass always visible. Requires both Abundance of the Wild and Silent Death.",
                RequiredFavorRank = (int)FavorRank.Champion,
                PrerequisiteBlessings = new List<string> { BlessingIds.LysaAbundanceOfWild, BlessingIds.LysaSilentDeath },
                StatModifiers = new Dictionary<string, float>
                {
                    { VintageStoryStats.WalkSpeed, 0.08f },
                    { VintageStoryStats.TemperatureResistance, 8.0f }
                },
                SpecialEffects = new List<string> { SpecialEffects.CompassAlwaysVisible, SpecialEffects.TemperatureResistance }
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
                    { VintageStoryStats.WalkSpeed, 0.06f },
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
                    { VintageStoryStats.WalkSpeed, 0.08f },
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

    #region Aethra (Light) - 10 Blessings (Refactored)

    private static List<Blessing> GetAethraBlessings()
    {
        return new List<Blessing>
        {
            // PLAYER BLESSINGS (6 total) - Divine protection and healing

            // Tier 1 - Initiate (0-499 favor) - Foundation
            new(BlessingIds.AethraDivineGrace, "Divine Grace", DeityType.Aethra)
            {
                Kind = BlessingKind.Player,
                Category = BlessingCategory.Utility,
                Description =
                    "The light blesses you with divine vitality. +10% max health, +12% healing effectiveness.",
                RequiredFavorRank = (int)FavorRank.Initiate,
                StatModifiers = new Dictionary<string, float>
                {
                    { VintageStoryStats.MaxHealthExtraPoints, 1.10f },
                    { VintageStoryStats.HealingEffectiveness, 0.12f }
                }
            },

            // Tier 2 - Disciple (500-1999 favor) - Choose Your Path
            new(BlessingIds.AethraRadiantStrike, "Radiant Strike", DeityType.Aethra)
            {
                Kind = BlessingKind.Player,
                Category = BlessingCategory.Combat,
                Description =
                    "Your attacks radiate holy energy. +12% melee damage, +10% ranged damage, heal 5% on hit. Offense path. Requires Divine Grace.",
                RequiredFavorRank = (int)FavorRank.Disciple,
                PrerequisiteBlessings = new List<string> { BlessingIds.AethraDivineGrace },
                StatModifiers = new Dictionary<string, float>
                {
                    { VintageStoryStats.MeleeWeaponsDamage, 0.12f },
                    { VintageStoryStats.RangedWeaponsDamage, 0.10f }
                },
                SpecialEffects = new List<string> { SpecialEffects.Lifesteal3 }
            },
            new(BlessingIds.AethraBlessedShield, "Blessed Shield", DeityType.Aethra)
            {
                Kind = BlessingKind.Player,
                Category = BlessingCategory.Defense,
                Description =
                    "Light shields you from harm. +18% armor, +15% max health, 8% damage reduction. Defense path. Requires Divine Grace.",
                RequiredFavorRank = (int)FavorRank.Disciple,
                PrerequisiteBlessings = new List<string> { BlessingIds.AethraDivineGrace },
                StatModifiers = new Dictionary<string, float>
                {
                    { VintageStoryStats.MeleeWeaponArmor, 0.18f },
                    { VintageStoryStats.MaxHealthExtraPoints, 1.15f }
                },
                SpecialEffects = new List<string> { SpecialEffects.DamageReduction10 }
            },

            // Tier 3 - Zealot (2000-4999 favor) - Specialization
            new(BlessingIds.AethraPurifyingLight, "Purifying Light", DeityType.Aethra)
            {
                Kind = BlessingKind.Player,
                Category = BlessingCategory.Combat,
                Description =
                    "Unleash devastating holy power. +22% melee damage, +18% ranged damage, heal 12% on hit, AoE healing pulse. Requires Radiant Strike.",
                RequiredFavorRank = (int)FavorRank.Zealot,
                PrerequisiteBlessings = new List<string> { BlessingIds.AethraRadiantStrike },
                StatModifiers = new Dictionary<string, float>
                {
                    { VintageStoryStats.MeleeWeaponsDamage, 0.22f },
                    { VintageStoryStats.RangedWeaponsDamage, 0.18f }
                },
                SpecialEffects = new List<string> { SpecialEffects.Lifesteal10 }
            },
            new(BlessingIds.AethraAegisOfLight, "Aegis of Light", DeityType.Aethra)
            {
                Kind = BlessingKind.Player,
                Category = BlessingCategory.Defense,
                Description =
                    "Become nearly invincible with divine protection. +28% armor, +25% max health, 15% damage reduction, +18% healing. Requires Blessed Shield.",
                RequiredFavorRank = (int)FavorRank.Zealot,
                PrerequisiteBlessings = new List<string> { BlessingIds.AethraBlessedShield },
                StatModifiers = new Dictionary<string, float>
                {
                    { VintageStoryStats.MeleeWeaponArmor, 0.28f },
                    { VintageStoryStats.MaxHealthExtraPoints, 1.25f },
                    { VintageStoryStats.HealingEffectiveness, 0.18f }
                },
                SpecialEffects = new List<string> { SpecialEffects.DamageReduction10 }
            },

            // Tier 4 - Champion (5000-9999 favor) - Capstone (requires both paths)
            new(BlessingIds.AethraAvatarOfLight, "Avatar of Light", DeityType.Aethra)
            {
                Kind = BlessingKind.Player,
                Category = BlessingCategory.Combat,
                Description =
                    "Embody divine radiance. +15% all stats, +20% healing, radiant aura heals allies, smite enemies. Requires both Purifying Light and Aegis of Light.",
                RequiredFavorRank = (int)FavorRank.Champion,
                PrerequisiteBlessings = new List<string> { BlessingIds.AethraPurifyingLight, BlessingIds.AethraAegisOfLight },
                StatModifiers = new Dictionary<string, float>
                {
                    { VintageStoryStats.MeleeWeaponsDamage, 0.15f },
                    { VintageStoryStats.RangedWeaponsDamage, 0.15f },
                    { VintageStoryStats.MeleeWeaponArmor, 0.15f },
                    { VintageStoryStats.MaxHealthExtraPoints, 1.15f },
                    { VintageStoryStats.HealingEffectiveness, 0.20f }
                },
                SpecialEffects = new List<string> { SpecialEffects.Lifesteal15 }
            },

            // RELIGION BLESSINGS (4 total) - Divine congregation

            // Tier 1 - Fledgling (0-499 prestige) - Foundation
            new(BlessingIds.AethraBlessingOfLight, "Blessing of Light", DeityType.Aethra)
            {
                Kind = BlessingKind.Religion,
                Category = BlessingCategory.Utility,
                Description =
                    "Your congregation is blessed by divine light. +8% max health, +10% healing effectiveness for all members.",
                RequiredPrestigeRank = (int)PrestigeRank.Fledgling,
                StatModifiers = new Dictionary<string, float>
                {
                    { VintageStoryStats.MaxHealthExtraPoints, 1.08f },
                    { VintageStoryStats.HealingEffectiveness, 0.10f }
                }
            },

            // Tier 2 - Established (500-1999 prestige) - Coordination
            new(BlessingIds.AethraDivineSanctuary, "Divine Sanctuary", DeityType.Aethra)
            {
                Kind = BlessingKind.Religion,
                Category = BlessingCategory.Defense,
                Description =
                    "Sacred protection shields all. +12% armor, +10% max health, +12% healing for all. Requires Blessing of Light.",
                RequiredPrestigeRank = (int)PrestigeRank.Established,
                PrerequisiteBlessings = new List<string> { BlessingIds.AethraBlessingOfLight },
                StatModifiers = new Dictionary<string, float>
                {
                    { VintageStoryStats.MeleeWeaponArmor, 0.12f },
                    { VintageStoryStats.MaxHealthExtraPoints, 1.10f },
                    { VintageStoryStats.HealingEffectiveness, 0.12f }
                }
            },

            // Tier 3 - Renowned (2000-4999 prestige) - Elite Force
            new(BlessingIds.AethraSacredBond, "Sacred Bond", DeityType.Aethra)
            {
                Kind = BlessingKind.Religion,
                Category = BlessingCategory.Combat,
                Description =
                    "Divine unity empowers the congregation. +15% armor, +15% max health, +15% healing, +10% all damage for all. Requires Divine Sanctuary.",
                RequiredPrestigeRank = (int)PrestigeRank.Renowned,
                PrerequisiteBlessings = new List<string> { BlessingIds.AethraDivineSanctuary },
                StatModifiers = new Dictionary<string, float>
                {
                    { VintageStoryStats.MeleeWeaponArmor, 0.15f },
                    { VintageStoryStats.MaxHealthExtraPoints, 1.15f },
                    { VintageStoryStats.HealingEffectiveness, 0.15f },
                    { VintageStoryStats.MeleeWeaponsDamage, 0.10f },
                    { VintageStoryStats.RangedWeaponsDamage, 0.10f }
                }
            },

            // Tier 4 - Legendary (5000-9999 prestige) - Divine Temple
            new(BlessingIds.AethraCathedralOfLight, "Cathedral of Light", DeityType.Aethra)
            {
                Kind = BlessingKind.Religion,
                Category = BlessingCategory.Combat,
                Description =
                    "Your religion becomes a beacon of divine power. +20% armor, +20% max health, +20% healing, +15% all damage, +8% movement for all. Divine sanctuary ability. Requires Sacred Bond.",
                RequiredPrestigeRank = (int)PrestigeRank.Legendary,
                PrerequisiteBlessings = new List<string> { BlessingIds.AethraSacredBond },
                StatModifiers = new Dictionary<string, float>
                {
                    { VintageStoryStats.MeleeWeaponArmor, 0.20f },
                    { VintageStoryStats.MaxHealthExtraPoints, 1.20f },
                    { VintageStoryStats.HealingEffectiveness, 0.20f },
                    { VintageStoryStats.MeleeWeaponsDamage, 0.15f },
                    { VintageStoryStats.RangedWeaponsDamage, 0.15f },
                    { VintageStoryStats.WalkSpeed, 0.08f }
                }
            }
        };
    }

    #endregion


    #region Gaia (Earth) - 10 Blessings (Refactored)

    private static List<Blessing> GetGaiaBlessings()
    {
        return new List<Blessing>
        {
            // PLAYER BLESSINGS (6 total) - Earth defender

            // Tier 1 - Initiate (0-499 favor) - Foundation
            new(BlessingIds.GaiaEarthenResilience, "Earthen Resilience", DeityType.Gaia)
            {
                Kind = BlessingKind.Player,
                Category = BlessingCategory.Defense,
                Description =
                    "Earth's strength flows through you. +15% max health, +10% armor, +8% healing effectiveness.",
                RequiredFavorRank = (int)FavorRank.Initiate,
                StatModifiers = new Dictionary<string, float>
                {
                    { VintageStoryStats.MaxHealthExtraPoints, 1.15f },
                    { VintageStoryStats.MeleeWeaponArmor, 0.10f },
                    { VintageStoryStats.HealingEffectiveness, 0.08f }
                }
            },

            // Tier 2 - Disciple (500-1999 favor) - Choose Your Path
            new(BlessingIds.GaiaStoneForm, "Stone Form", DeityType.Gaia)
            {
                Kind = BlessingKind.Player,
                Category = BlessingCategory.Defense,
                Description =
                    "Become as unyielding as stone. +22% armor, +18% max health, 10% damage reduction. Defense path. Requires Earthen Resilience.",
                RequiredFavorRank = (int)FavorRank.Disciple,
                PrerequisiteBlessings = new List<string> { BlessingIds.GaiaEarthenResilience },
                StatModifiers = new Dictionary<string, float>
                {
                    { VintageStoryStats.MeleeWeaponArmor, 0.22f },
                    { VintageStoryStats.MaxHealthExtraPoints, 1.18f }
                },
                SpecialEffects = new List<string> { SpecialEffects.DamageReduction10 }
            },
            new(BlessingIds.GaiaNaturesBlessing, "Nature's Blessing", DeityType.Gaia)
            {
                Kind = BlessingKind.Player,
                Category = BlessingCategory.Utility,
                Description =
                    "Nature restores you constantly. +20% max health, +18% healing effectiveness, slow passive regeneration. Regeneration path. Requires Earthen Resilience.",
                RequiredFavorRank = (int)FavorRank.Disciple,
                PrerequisiteBlessings = new List<string> { BlessingIds.GaiaEarthenResilience },
                StatModifiers = new Dictionary<string, float>
                {
                    { VintageStoryStats.MaxHealthExtraPoints, 1.20f },
                    { VintageStoryStats.HealingEffectiveness, 0.18f }
                }
            },

            // Tier 3 - Zealot (2000-4999 favor) - Specialization
            new(BlessingIds.GaiaMountainGuard, "Mountain Guard", DeityType.Gaia)
            {
                Kind = BlessingKind.Player,
                Category = BlessingCategory.Defense,
                Description =
                    "Stand immovable like a mountain. +32% armor, +28% max health, 15% damage reduction, +10% melee damage. Requires Stone Form.",
                RequiredFavorRank = (int)FavorRank.Zealot,
                PrerequisiteBlessings = new List<string> { BlessingIds.GaiaStoneForm },
                StatModifiers = new Dictionary<string, float>
                {
                    { VintageStoryStats.MeleeWeaponArmor, 0.32f },
                    { VintageStoryStats.MaxHealthExtraPoints, 1.28f },
                    { VintageStoryStats.MeleeWeaponsDamage, 0.10f }
                },
                SpecialEffects = new List<string> { SpecialEffects.DamageReduction10 }
            },
            new(BlessingIds.GaiaLifebloom, "Lifebloom", DeityType.Gaia)
            {
                Kind = BlessingKind.Player,
                Category = BlessingCategory.Utility,
                Description =
                    "Life flourishes around you. +30% max health, +28% healing effectiveness, strong passive regeneration, heal nearby allies. Requires Nature's Blessing.",
                RequiredFavorRank = (int)FavorRank.Zealot,
                PrerequisiteBlessings = new List<string> { BlessingIds.GaiaNaturesBlessing },
                StatModifiers = new Dictionary<string, float>
                {
                    { VintageStoryStats.MaxHealthExtraPoints, 1.30f },
                    { VintageStoryStats.HealingEffectiveness, 0.28f }
                }
            },

            // Tier 4 - Champion (5000-9999 favor) - Capstone (requires both paths)
            new(BlessingIds.GaiaAvatarOfEarth, "Avatar of Earth", DeityType.Gaia)
            {
                Kind = BlessingKind.Player,
                Category = BlessingCategory.Defense,
                Description =
                    "Embody the eternal earth. +25% armor, +35% max health, +30% healing, 15% damage reduction, earthen aura protects and heals. Requires both Mountain Guard and Lifebloom.",
                RequiredFavorRank = (int)FavorRank.Champion,
                PrerequisiteBlessings = new List<string> { BlessingIds.GaiaMountainGuard, BlessingIds.GaiaLifebloom },
                StatModifiers = new Dictionary<string, float>
                {
                    { VintageStoryStats.MeleeWeaponArmor, 0.25f },
                    { VintageStoryStats.MaxHealthExtraPoints, 1.35f },
                    { VintageStoryStats.HealingEffectiveness, 0.30f },
                    { VintageStoryStats.MeleeWeaponsDamage, 0.15f }
                },
                SpecialEffects = new List<string> { SpecialEffects.DamageReduction10 }
            },

            // RELIGION BLESSINGS (4 total) - Earth wardens

            // Tier 1 - Fledgling (0-499 prestige) - Foundation
            new(BlessingIds.GaiaEarthwardens, "Earthwardens", DeityType.Gaia)
            {
                Kind = BlessingKind.Religion,
                Category = BlessingCategory.Defense,
                Description =
                    "Your congregation stands as guardians of the earth. +10% max health, +8% armor for all members.",
                RequiredPrestigeRank = (int)PrestigeRank.Fledgling,
                StatModifiers = new Dictionary<string, float>
                {
                    { VintageStoryStats.MaxHealthExtraPoints, 1.10f },
                    { VintageStoryStats.MeleeWeaponArmor, 0.08f }
                }
            },

            // Tier 2 - Established (500-1999 prestige) - Coordination
            new(BlessingIds.GaiaLivingFortress, "Living Fortress", DeityType.Gaia)
            {
                Kind = BlessingKind.Religion,
                Category = BlessingCategory.Defense,
                Description =
                    "United, you become an impenetrable fortress. +15% max health, +12% armor, +10% healing for all. Requires Earthwardens.",
                RequiredPrestigeRank = (int)PrestigeRank.Established,
                PrerequisiteBlessings = new List<string> { BlessingIds.GaiaEarthwardens },
                StatModifiers = new Dictionary<string, float>
                {
                    { VintageStoryStats.MaxHealthExtraPoints, 1.15f },
                    { VintageStoryStats.MeleeWeaponArmor, 0.12f },
                    { VintageStoryStats.HealingEffectiveness, 0.10f }
                }
            },

            // Tier 3 - Renowned (2000-4999 prestige) - Elite Force
            new(BlessingIds.GaiaNaturesWrath, "Nature's Wrath", DeityType.Gaia)
            {
                Kind = BlessingKind.Religion,
                Category = BlessingCategory.Combat,
                Description =
                    "Nature defends its own with fury. +20% max health, +18% armor, +15% healing, +12% melee damage for all. Requires Living Fortress.",
                RequiredPrestigeRank = (int)PrestigeRank.Renowned,
                PrerequisiteBlessings = new List<string> { BlessingIds.GaiaLivingFortress },
                StatModifiers = new Dictionary<string, float>
                {
                    { VintageStoryStats.MaxHealthExtraPoints, 1.20f },
                    { VintageStoryStats.MeleeWeaponArmor, 0.18f },
                    { VintageStoryStats.HealingEffectiveness, 0.15f },
                    { VintageStoryStats.MeleeWeaponsDamage, 0.12f }
                }
            },

            // Tier 4 - Legendary (5000-9999 prestige) - World Tree
            new(BlessingIds.GaiaWorldTree, "World Tree", DeityType.Gaia)
            {
                Kind = BlessingKind.Religion,
                Category = BlessingCategory.Defense,
                Description =
                    "Your religion becomes the eternal world tree. +30% max health, +25% armor, +22% healing, +18% melee damage for all. Massive regeneration aura. Requires Nature's Wrath.",
                RequiredPrestigeRank = (int)PrestigeRank.Legendary,
                PrerequisiteBlessings = new List<string> { BlessingIds.GaiaNaturesWrath },
                StatModifiers = new Dictionary<string, float>
                {
                    { VintageStoryStats.MaxHealthExtraPoints, 1.30f },
                    { VintageStoryStats.MeleeWeaponArmor, 0.25f },
                    { VintageStoryStats.HealingEffectiveness, 0.22f },
                    { VintageStoryStats.MeleeWeaponsDamage, 0.18f }
                }
            }
        };
    }

    #endregion
}