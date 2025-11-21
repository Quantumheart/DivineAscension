using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using PantheonWars.Constants;
using PantheonWars.Models;
using PantheonWars.Models.Enum;
using Vintagestory.GameContent;

namespace PantheonWars.Systems;

/// <summary>
///     Contains all blessing definitions for the religion-only system.
///     Universal blessings available to all religions regardless of deity.
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
}
