using System;
using System.Collections.Generic;
using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace DivineAscension.Systems.Patches;

[HarmonyPatch]
public static class BlockCropPatches
{
    // De-duplication: track recently harvested positions to prevent double-firing
    // Key: "playerUID:x,y,z" - uses HashSet since we only need to know if we've seen it
    private static readonly HashSet<string> _recentHarvests = new();
    private static DateTime _lastCleanup = DateTime.UtcNow;

    /// <summary>
    /// Event fired when a crop block is harvested (drops are calculated).
    /// Provides the player, typed BlockCrop reference, position, and whether harvested with scythe.
    /// </summary>
    public static event Action<IServerPlayer, BlockCrop, BlockPos, bool>? OnCropHarvested;

    public static void ClearSubscribers()
    {
        OnCropHarvested = null;
        _recentHarvests.Clear();
    }

    /// <summary>
    /// Patch BlockCrop.GetDrops - called when a crop is broken and drops are calculated.
    /// </summary>
    [HarmonyPatch(typeof(BlockCrop), nameof(BlockCrop.GetDrops))]
    [HarmonyPostfix]
    public static void Postfix_GetDrops(
        BlockCrop __instance,
        IWorldAccessor world,
        BlockPos pos,
        IPlayer byPlayer,
        float dropQuantityMultiplier = 1f)
    {
        // Only fire on server side
        if (world.Api.Side != EnumAppSide.Server) return;
        if (byPlayer is not IServerPlayer serverPlayer) return;

        // Clean up the set periodically (every 5 seconds) to prevent memory growth
        // Must happen BEFORE the dedup check
        var now = DateTime.UtcNow;
        if ((now - _lastCleanup).TotalSeconds > 5)
        {
            _recentHarvests.Clear();
            _lastCleanup = now;
        }

        // De-duplicate: GetDrops is called twice per harvest (calculate then spawn)
        var key = $"{serverPlayer.PlayerUID}:{pos.X},{pos.Y},{pos.Z}";
        if (!_recentHarvests.Add(key))
            return;

        // Detect if harvested with scythe/shears for batching optimization
        var activeSlot = byPlayer.InventoryManager?.ActiveHotbarSlot;
        var activeItem = activeSlot?.Itemstack?.Collectible;
        bool isScytheHarvest = activeItem is ItemShears;

        OnCropHarvested?.Invoke(serverPlayer, __instance, pos, isScytheHarvest);
    }
}