using System;
using DivineAscension.Constants;
using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;

namespace DivineAscension.Systems.Patches;

[HarmonyPatch]
public static class KhorasPatches
{
    // Patch for Tool Durability
    // Target: CollectibleObject.DamageItem
    // Logic: Reduce damage amount based on ToolDurability stat
    [HarmonyPatch(typeof(CollectibleObject), "DamageItem")]
    [HarmonyPrefix]
    public static void Prefix_DamageItem(IWorldAccessor world, Entity byEntity, ItemSlot itemslot, ref int amount)
    {
        if (byEntity == null || amount <= 0) return;

        // Get tool durability bonus (e.g. 0.10 for 10%)
        double durabilityBonus = byEntity.Stats.GetBlended(VintageStoryStats.ToolDurability);
        if (durabilityBonus <= 0) return;

        // Calculate reduction
        // Example: amount = 1, bonus = 0.10. reduce = 0.10. 
        // We want 10% chance to reduce by 1 (making it 0).

        var reduceAmount = amount * (float)durabilityBonus;
        var reduceInt = (int)reduceAmount;
        var remainder = reduceAmount - reduceInt;

        if (world.Rand.NextDouble() < remainder) reduceInt++;

        amount = Math.Max(0, amount - reduceInt);
    }

    // Patch for Ore Yield
    // Target: Block.GetDrops
    // Data Source: Checks 'oreYield' stat defined in BlessingDefinitions.cs and applied by BlessingEffectSystem.cs
    // Logic: Increase stack size of ore drops based on OreYield stat
    [HarmonyPatch(typeof(Block), "GetDrops")]
    [HarmonyPostfix]
    public static void Postfix_GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, ref ItemStack[] __result)
    {
        if (byPlayer?.Entity == null || __result == null || __result.Length == 0) return;

        // Get ore yield bonus
        double yieldBonus = byPlayer.Entity.Stats.GetBlended(VintageStoryStats.OreDropRate);
        if (yieldBonus <= 0) return;

        // Check if block is an ore block (simple check by code)
        var block = world.BlockAccessor.GetBlock(pos);
        if (block == null) return;

        // We only apply to "ore-" blocks to avoid infinite wood/dirt duping
        if (!block.Code.Path.StartsWith("ore-")) return;

        foreach (var stack in __result)
        {
            if (stack == null) continue;

            // Only boost nuggets/chunks/ores
            if (!stack.Collectible.Code.Path.Contains("ore") &&
                !stack.Collectible.Code.Path.Contains("nugget") &&
                !stack.Collectible.Code.Path.Contains("chunk")) continue;

            // Calculate extra
            var extra = stack.StackSize * (float)yieldBonus;
            var extraInt = (int)extra;

            if (world.Rand.NextDouble() < extra - extraInt) extraInt++;

            if (extraInt > 0) stack.StackSize += extraInt;
        }
    }
}