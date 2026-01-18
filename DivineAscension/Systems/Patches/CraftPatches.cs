using System;
using DivineAscension.Constants;
using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;

namespace DivineAscension.Systems.Patches;

[HarmonyPatch]
public static class CraftPatches
{
    // Patch for Tool Durability
    // Target: CollectibleObject.DamageItem
    // Logic: Reduce damage amount based on ToolDurability stat
    [HarmonyPatch(typeof(CollectibleObject), "DamageItem")]
    [HarmonyPrefix]
    public static void Prefix_DamageItem(IWorldAccessor world, Entity byEntity, ItemSlot itemslot, ref int amount)
    {
        if (byEntity == null || amount <= 0) return;

        // Only apply to items with mining/tool properties
        if (itemslot?.Itemstack?.Collectible is not Item item) return;
        if (item.Tool == null) return; // Not a tool

        // Get tool durability bonus (e.g. 0.10 for 10%)
        // VS returns 1.0 as base; blessings add to it (1.10 = 10% bonus)
        double durabilityBonus = byEntity.Stats.GetBlended(VintageStoryStats.ToolDurability) - 1.0;
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

    // Note: Ore yield bonus is handled natively by Vintage Story's oreDropRate stat.
    // No Harmony patch needed - the stat modifier applied by BlessingEffectSystem
    // is automatically processed by the game's Block.GetDrops() method.
}