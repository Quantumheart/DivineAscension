using System;
using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace DivineAscension.Systems.Patches;

[HarmonyPatch]
public static class FlowerPatches
{
    /// <summary>
    /// Event fired when a flower block is broken.
    /// Provides the player, block, flower type from Variant["type"], and whether harvested with scythe.
    /// </summary>
    public static event Action<IServerPlayer, Block, string?, bool>? OnFlowerHarvested;

    public static void ClearSubscribers()
    {
        OnFlowerHarvested = null;
    }

    /// <summary>
    /// Patch BlockPlant.GetDrops - fires for all plants, we filter for flowers.
    /// </summary>
    [HarmonyPatch(typeof(BlockPlant), nameof(BlockPlant.GetDrops))]
    [HarmonyPostfix]
    public static void Postfix_GetDrops(
        BlockPlant __instance,
        IWorldAccessor world,
        BlockPos pos,
        IPlayer byPlayer)
    {
        // Only fire on server side
        if (world.Api.Side != EnumAppSide.Server) return;
        if (byPlayer is not IServerPlayer serverPlayer) return;

        // Only fire for flower blocks
        if (__instance.FirstCodePart() != "flower") return;

        // Detect if harvested with scythe/shears for batching optimization
        var activeSlot = byPlayer.InventoryManager?.ActiveHotbarSlot;
        var activeItem = activeSlot?.Itemstack?.Collectible;
        bool isScytheHarvest = activeItem is ItemShears;

        // Get flower type from Variant
        __instance.Variant.TryGetValue("type", out var flowerType);

        OnFlowerHarvested?.Invoke(serverPlayer, __instance, flowerType, isScytheHarvest);
    }
}