using System;
using DivineAscension.Constants;
using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace DivineAscension.Systems.Patches;

/// <summary>
///     Harmony patches for stone and clay yield bonuses
/// </summary>
[HarmonyPatch]
public static class StonePatches
{
    /// <summary>
    ///     Applies StoneYield and ClayYield stat bonuses to block drops
    /// </summary>
    [HarmonyPatch(typeof(Block), nameof(Block.OnBlockBroken))]
    [HarmonyPrefix]
    public static void Prefix_OnBlockBroken(
        Block __instance,
        IWorldAccessor world,
        BlockPos pos,
        IPlayer byPlayer,
        ref float dropQuantityMultiplier)
    {
        // Only process on server side
        if (world?.Side != EnumAppSide.Server) return;
        if (byPlayer?.Entity?.Stats == null) return;

        var block = __instance;
        if (block?.Code == null) return;

        var path = block.Code.Path.ToLowerInvariant();

        // Apply StoneYield bonus for stone blocks
        if (IsStoneBlock(path))
        {
            var stoneYieldBonus = byPlayer.Entity.Stats.GetBlended(VintageStoryStats.StoneYield);
            if (stoneYieldBonus > 0)
            {
                // WeightedSum blending: base is 1.0, so we already have the multiplier
                // e.g., if stat is 1.20, we want 20% bonus, so multiply by 1.20
                dropQuantityMultiplier *= (float)stoneYieldBonus;

                world.Logger.Debug(
                    $"[DivineAscension] StoneYield applied: {stoneYieldBonus:F2}x for {block.Code.Path} (player: {byPlayer.PlayerName})");
            }
        }
        // Apply ClayYield bonus for clay blocks
        else if (IsClayBlock(path))
        {
            var clayYieldBonus = byPlayer.Entity.Stats.GetBlended(VintageStoryStats.ClayYield);
            if (clayYieldBonus > 0)
            {
                // WeightedSum blending: base is 1.0
                dropQuantityMultiplier *= (float)clayYieldBonus;

                world.Logger.Debug(
                    $"[DivineAscension] ClayYield applied: {clayYieldBonus:F2}x for {block.Code.Path} (player: {byPlayer.PlayerName})");
            }
        }
    }

    /// <summary>
    ///     Checks if a block is a stone block
    /// </summary>
    private static bool IsStoneBlock(string path)
    {
        // Check if it's a rock/stone block but NOT ore (ores use OreDropRate)
        if (path.StartsWith("ore-", StringComparison.Ordinal))
            return false; // Ore blocks use OreDropRate stat

        // Common stone types
        var stoneTypes = new[]
        {
            "granite", "andesite", "basalt", "peridotite", "limestone",
            "sandstone", "claystone", "chalk", "chert", "suevite",
            "phyllite", "slate", "conglomerate", "shale", "marble",
            "obsidian"
        };

        foreach (var stoneType in stoneTypes)
        {
            if (path.Contains(stoneType))
                return true;
        }

        // Generic "rock" or "stone" blocks (but not cobblestone items)
        if ((path.StartsWith("rock-") || path.StartsWith("stone-") ||
             path.Contains("-rock-") || path.Contains("-stone-")) &&
            !path.Contains("cobblestone"))
        {
            return true;
        }

        return false;
    }

    /// <summary>
    ///     Checks if a block is a clay block
    /// </summary>
    private static bool IsClayBlock(string path)
    {
        // Clay blocks and clay-bearing soils
        // Note: This is for digging clay, not pottery items
        if (path.Contains("clay") && !path.Contains("fired") && !path.Contains("brick"))
        {
            // Raw clay blocks: clay-blue, clay-fire, soil-medium-clay, etc.
            return true;
        }

        return false;
    }
}