using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using PantheonWars.Constants;
using PantheonWars.Models;
using PantheonWars.Models.Enum;
using Vintagestory.GameContent;

namespace PantheonWars.Systems;

/// <summary>
///     Contains all blessing definitions for all deities
///     3-deity system: Aethra (Light/Good), Gaia (Nature/Neutral), Morthen (Shadow & Death/Evil)
///     Total: 30 blessings (3 deities × 10 blessings each)
/// </summary>
[ExcludeFromCodeCoverage]
public static class BlessingDefinitions
{
    /// <summary>
    ///     Gets all blessing definitions for registration
    /// </summary>
    public static List<Blessing> GetAllBlessings()
    {
        var blessings = new List<Blessing>();

        // Universal utility blessings (MVP 1 - religion-only system)
        blessings.AddRange(GetUniversalUtilityBlessings());

        // Legacy deity-specific blessings (kept for backwards compatibility)
        blessings.AddRange(GetAethraBlessings());
        blessings.AddRange(GetGaiaBlessings());
        blessings.AddRange(GetMorthenBlessings());

        return blessings;
    }

    #region Universal Utility Blessings (MVP 1)

    /// <summary>
    ///     Gets universal utility blessings for the religion-only system.
    ///     These are deity-agnostic and focus on economy/utility rather than combat.
    /// </summary>
    private static List<Blessing> GetUniversalUtilityBlessings()
    {
        return new List<Blessing>
        {
            // TIER 1 - Starter Blessings (pick 2 at religion creation)

            new(BlessingIds.EfficientMiner, "Efficient Miner", DeityType.None)
            {
                Kind = BlessingKind.Religion,
                Category = BlessingCategory.Utility,
                Description = "Your religion's miners work faster. +15% mining speed for all members.",
                RequiredPrestigeRank = (int)PrestigeRank.Fledgling,
                StatModifiers = new Dictionary<string, float>
                {
                    { VintageStoryStats.MiningSpeedMul, 0.15f }
                }
            },

            new(BlessingIds.SwiftTraveler, "Swift Traveler", DeityType.None)
            {
                Kind = BlessingKind.Religion,
                Category = BlessingCategory.Utility,
                Description = "Your religion's members move swiftly. +10% movement speed for all members.",
                RequiredPrestigeRank = (int)PrestigeRank.Fledgling,
                StatModifiers = new Dictionary<string, float>
                {
                    { VintageStoryStats.WalkSpeed, 0.10f }
                }
            },

            new(BlessingIds.HardyConstitution, "Hardy Constitution", DeityType.None)
            {
                Kind = BlessingKind.Religion,
                Category = BlessingCategory.Utility,
                Description = "Your religion's members have strong constitutions. -15% hunger rate for all members.",
                RequiredPrestigeRank = (int)PrestigeRank.Fledgling,
                StatModifiers = new Dictionary<string, float>
                {
                    { VintageStoryStats.HungerRate, -0.15f }
                }
            },

            new(BlessingIds.BountifulHarvest, "Bountiful Harvest", DeityType.None)
            {
                Kind = BlessingKind.Religion,
                Category = BlessingCategory.Utility,
                Description = "Your religion's farmers are blessed. +10% max health (representing overall wellness) for all members.",
                RequiredPrestigeRank = (int)PrestigeRank.Fledgling,
                StatModifiers = new Dictionary<string, float>
                {
                    { VintageStoryStats.MaxHealthExtraPoints, 2.0f } // +2 health points
                }
            },

            // TIER 2 - Mid-game Blessings (pick 3 at 500 prestige)

            new(BlessingIds.MasterCrafter, "Master Crafter", DeityType.None)
            {
                Kind = BlessingKind.Religion,
                Category = BlessingCategory.Utility,
                Description = "Your religion produces quality goods. +20% mining speed, +5% max health for all members.",
                RequiredPrestigeRank = (int)PrestigeRank.Established,
                StatModifiers = new Dictionary<string, float>
                {
                    { VintageStoryStats.MiningSpeedMul, 0.20f },
                    { VintageStoryStats.MaxHealthExtraPoints, 1.0f }
                }
            },

            new(BlessingIds.NaturesLarder, "Nature's Larder", DeityType.None)
            {
                Kind = BlessingKind.Religion,
                Category = BlessingCategory.Utility,
                Description = "Your religion never goes hungry. -25% hunger rate, +15% healing effectiveness for all members.",
                RequiredPrestigeRank = (int)PrestigeRank.Established,
                StatModifiers = new Dictionary<string, float>
                {
                    { VintageStoryStats.HungerRate, -0.25f },
                    { VintageStoryStats.HealingEffectiveness, 0.15f }
                }
            },

            new(BlessingIds.IronWill, "Iron Will", DeityType.None)
            {
                Kind = BlessingKind.Religion,
                Category = BlessingCategory.Defense,
                Description = "Your religion's members are resilient. +15% max health, +10% armor for all members.",
                RequiredPrestigeRank = (int)PrestigeRank.Established,
                StatModifiers = new Dictionary<string, float>
                {
                    { VintageStoryStats.MaxHealthExtraPoints, 3.0f },
                    { VintageStoryStats.MeleeWeaponArmor, 0.10f }
                }
            },

            new(BlessingIds.QuickHands, "Quick Hands", DeityType.None)
            {
                Kind = BlessingKind.Religion,
                Category = BlessingCategory.Utility,
                Description = "Your religion's members work efficiently. +15% movement speed, +10% mining speed for all members.",
                RequiredPrestigeRank = (int)PrestigeRank.Established,
                StatModifiers = new Dictionary<string, float>
                {
                    { VintageStoryStats.WalkSpeed, 0.15f },
                    { VintageStoryStats.MiningSpeedMul, 0.10f }
                }
            }
        };
    }

    #endregion

    #region Aethra (Light) - 10 Blessings

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
                SpecialEffects = new List<string> { SpecialEffectIds.Lifesteal3 }
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
                SpecialEffects = new List<string> { SpecialEffectIds.DamageReduction10 }
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
                SpecialEffects = new List<string> { SpecialEffectIds.Lifesteal10 }
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
                SpecialEffects = new List<string> { SpecialEffectIds.DamageReduction10 }
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
                SpecialEffects = new List<string> { SpecialEffectIds.Lifesteal15 }
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

    #region Gaia (Nature) - 10 Blessings

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
                SpecialEffects = new List<string> { SpecialEffectIds.DamageReduction10 }
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
                SpecialEffects = new List<string> { SpecialEffectIds.DamageReduction10 }
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
                SpecialEffects = new List<string> { SpecialEffectIds.DamageReduction10 }
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

    #region Morthen (Shadow & Death) - 10 Blessings

    private static List<Blessing> GetMorthenBlessings()
    {
        return new List<Blessing>
        {
            // PLAYER BLESSINGS (6 total) - Shadow reaper and death magic

            // Tier 1 - Initiate (0-499 favor) - Foundation
            new(BlessingIds.MorthenDeathsEmbrace, "Death's Embrace", DeityType.Morthen)
            {
                Kind = BlessingKind.Player,
                Category = BlessingCategory.Combat,
                Description =
                    "Death and shadow empower your strikes. +10% melee damage, +10% max health, minor lifesteal.",
                RequiredFavorRank = (int)FavorRank.Initiate,
                StatModifiers = new Dictionary<string, float>
                {
                    { VintageStoryStats.MeleeWeaponsDamage, 0.10f },
                    { VintageStoryStats.MaxHealthExtraPoints, 1.10f }
                },
                SpecialEffects = new List<string> { SpecialEffectIds.Lifesteal3 }
            },

            // Tier 2 - Disciple (500-1999 favor) - Choose Your Path
            new(BlessingIds.MorthenSoulReaper, "Soul Reaper", DeityType.Morthen)
            {
                Kind = BlessingKind.Player,
                Category = BlessingCategory.Combat,
                Description =
                    "Harvest souls from the shadows with dark magic. +15% melee damage, +10% lifesteal, attacks apply poison. Offense path. Requires Death's Embrace.",
                RequiredFavorRank = (int)FavorRank.Disciple,
                PrerequisiteBlessings = new List<string> { BlessingIds.MorthenDeathsEmbrace },
                StatModifiers = new Dictionary<string, float> { { VintageStoryStats.MeleeWeaponsDamage, 0.15f } },
                SpecialEffects = new List<string> { SpecialEffectIds.Lifesteal10, SpecialEffectIds.PoisonDot }
            },
            new(BlessingIds.MorthenUndying, "Undying", DeityType.Morthen)
            {
                Kind = BlessingKind.Player,
                Category = BlessingCategory.Defense,
                Description =
                    "Resist death itself. +20% max health, +15% armor, +10% health regeneration. Defense path. Requires Death's Embrace.",
                RequiredFavorRank = (int)FavorRank.Disciple,
                PrerequisiteBlessings = new List<string> { BlessingIds.MorthenDeathsEmbrace },
                StatModifiers = new Dictionary<string, float>
                {
                    { VintageStoryStats.MaxHealthExtraPoints, 1.20f },
                    { VintageStoryStats.MeleeWeaponArmor, 0.15f },
                    { VintageStoryStats.HealingEffectiveness, 0.10f }
                }
            },

            // Tier 3 - Zealot (2000-4999 favor) - Specialization
            new(BlessingIds.MorthenPlagueBearer, "Plague Bearer", DeityType.Morthen)
            {
                Kind = BlessingKind.Player,
                Category = BlessingCategory.Combat,
                Description =
                    "Spread pestilence and decay from the shadows. +25% melee damage, +15% lifesteal, plague aura weakens enemies. Requires Soul Reaper.",
                RequiredFavorRank = (int)FavorRank.Zealot,
                PrerequisiteBlessings = new List<string> { BlessingIds.MorthenSoulReaper },
                StatModifiers = new Dictionary<string, float> { { VintageStoryStats.MeleeWeaponsDamage, 0.25f } },
                SpecialEffects = new List<string>
                    { SpecialEffectIds.Lifesteal15, SpecialEffectIds.PoisonDotStrong, SpecialEffectIds.PlagueAura }
            },
            new(BlessingIds.MorthenDeathless, "Deathless", DeityType.Morthen)
            {
                Kind = BlessingKind.Player,
                Category = BlessingCategory.Defense,
                Description =
                    "Transcend mortality. +30% max health, +25% armor, +20% health regen, death resistance. Requires Undying.",
                RequiredFavorRank = (int)FavorRank.Zealot,
                PrerequisiteBlessings = new List<string> { BlessingIds.MorthenUndying },
                StatModifiers = new Dictionary<string, float>
                {
                    { VintageStoryStats.MaxHealthExtraPoints, 1.30f },
                    { VintageStoryStats.MeleeWeaponArmor, 0.25f },
                    { VintageStoryStats.HealingEffectiveness, 0.20f }
                },
                SpecialEffects = new List<string> { SpecialEffectIds.DamageReduction10 }
            },

            // Tier 4 - Champion (5000-9999 favor) - Capstone (requires both paths)
            new(BlessingIds.MorthenLordOfDeath, "Lord of Shadow & Death", DeityType.Morthen)
            {
                Kind = BlessingKind.Player,
                Category = BlessingCategory.Combat,
                Description =
                    "Command death and darkness itself. +15% all stats, +10% attack speed, death aura, execute low health enemies. Requires both Plague Bearer and Deathless.",
                RequiredFavorRank = (int)FavorRank.Champion,
                PrerequisiteBlessings = new List<string> { BlessingIds.MorthenPlagueBearer, BlessingIds.MorthenDeathless },
                StatModifiers = new Dictionary<string, float>
                {
                    { VintageStoryStats.MeleeWeaponsDamage, 0.15f },
                    { VintageStoryStats.MeleeWeaponArmor, 0.15f },
                    { VintageStoryStats.MaxHealthExtraPoints, 1.15f },
                    { VintageStoryStats.MeleeWeaponsSpeed, 0.10f },
                    { VintageStoryStats.HealingEffectiveness, 0.15f }
                },
                SpecialEffects = new List<string>
                    { SpecialEffectIds.DeathAura, SpecialEffectIds.ExecuteThreshold, SpecialEffectIds.Lifesteal20 }
            },

            // RELIGION BLESSINGS (4 total) - Shadow cult & necromancy

            // Tier 1 - Fledgling (0-499 prestige) - Foundation
            new(BlessingIds.MorthenShadowCult, "Shadow Cult", DeityType.Morthen)
            {
                Kind = BlessingKind.Religion,
                Category = BlessingCategory.Combat,
                Description =
                    "Your congregation embraces the darkness. +8% melee damage, +8% max health for all members.",
                RequiredPrestigeRank = (int)PrestigeRank.Fledgling,
                StatModifiers = new Dictionary<string, float>
                {
                    { VintageStoryStats.MeleeWeaponsDamage, 0.08f },
                    { VintageStoryStats.MaxHealthExtraPoints, 1.08f }
                }
            },

            // Tier 2 - Established (500-1999 prestige) - Coordination
            new(BlessingIds.MorthenNecromanticCovenant, "Necromantic Covenant", DeityType.Morthen)
            {
                Kind = BlessingKind.Religion,
                Category = BlessingCategory.Combat,
                Description =
                    "Dark pact strengthens all with shadow magic. +12% melee damage, +10% armor, +8% health regen for all. Requires Shadow Cult.",
                RequiredPrestigeRank = (int)PrestigeRank.Established,
                PrerequisiteBlessings = new List<string> { BlessingIds.MorthenShadowCult },
                StatModifiers = new Dictionary<string, float>
                {
                    { VintageStoryStats.MeleeWeaponsDamage, 0.12f },
                    { VintageStoryStats.MeleeWeaponArmor, 0.10f },
                    { VintageStoryStats.HealingEffectiveness, 0.08f }
                }
            },

            // Tier 3 - Renowned (2000-4999 prestige) - Elite Force
            new(BlessingIds.MorthenDeathlessLegion, "Deathless Legion", DeityType.Morthen)
            {
                Kind = BlessingKind.Religion,
                Category = BlessingCategory.Combat,
                Description =
                    "Unkillable army of shadow warriors. +18% melee damage, +15% armor, +15% max health, +12% regen for all. Requires Necromantic Covenant.",
                RequiredPrestigeRank = (int)PrestigeRank.Renowned,
                PrerequisiteBlessings = new List<string> { BlessingIds.MorthenNecromanticCovenant },
                StatModifiers = new Dictionary<string, float>
                {
                    { VintageStoryStats.MeleeWeaponsDamage, 0.18f },
                    { VintageStoryStats.MeleeWeaponArmor, 0.15f },
                    { VintageStoryStats.MaxHealthExtraPoints, 1.15f },
                    { VintageStoryStats.HealingEffectiveness, 0.12f }
                }
            },

            // Tier 4 - Legendary (5000-9999 prestige) - Empire of Darkness
            new(BlessingIds.MorthenEmpireOfDarkness, "Empire of Darkness", DeityType.Morthen)
            {
                Kind = BlessingKind.Religion,
                Category = BlessingCategory.Combat,
                Description =
                    "Your religion rules over death and shadow. +25% melee damage, +20% armor, +20% max health, +18% regen, +10% attack speed for all. Death mark ability. Requires Deathless Legion.",
                RequiredPrestigeRank = (int)PrestigeRank.Legendary,
                PrerequisiteBlessings = new List<string> { BlessingIds.MorthenDeathlessLegion },
                StatModifiers = new Dictionary<string, float>
                {
                    { VintageStoryStats.MeleeWeaponsDamage, 0.25f },
                    { VintageStoryStats.MeleeWeaponArmor, 0.20f },
                    { VintageStoryStats.MaxHealthExtraPoints, 1.20f },
                    { VintageStoryStats.HealingEffectiveness, 0.18f },
                    { VintageStoryStats.MeleeWeaponsSpeed, 0.10f }
                },
                SpecialEffects = new List<string> { SpecialEffectIds.ReligionDeathMark }
            }
        };
    }

    #endregion
}
