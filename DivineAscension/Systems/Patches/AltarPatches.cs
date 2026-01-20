using System;
using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace DivineAscension.Systems.Patches;

/// <summary>
/// Harmony patch for altar block interactions.
/// Fires events when players interact with altar blocks (code starts with "altar").
/// </summary>
[HarmonyPatch]
public static class AltarPatches
{
    /// <summary>
    /// Event fired when a player interacts with an altar block.
    /// Provides the server player and block selection for prayer handling.
    /// </summary>
    public static event Action<IServerPlayer, BlockSelection>? OnAltarUsed;

    /// <summary>
    /// Clears event subscribers. Called during server initialization to prevent stale subscriptions.
    /// </summary>
    public static void ClearSubscribers()
    {
        OnAltarUsed = null;
    }

    /// <summary>
    /// Patches Block.OnBlockInteractStart to detect altar interactions.
    /// Only fires the event for blocks whose code path starts with "altar".
    /// </summary>
    [HarmonyPatch(typeof(Block), nameof(Block.OnBlockInteractStart))]
    [HarmonyPostfix]
    public static void Postfix_OnBlockInteractStart(
        Block __instance,
        IWorldAccessor world,
        IPlayer byPlayer,
        BlockSelection blockSel,
        ref bool __result)
    {
        // Only process on server side
        if (world?.Api?.Side != EnumAppSide.Server)
            return;

        // Only fire event if interaction was successful and block is an altar
        if (!__result || __instance?.Code?.Path == null)
            return;

        // Check if this is an altar block
        if (!__instance.Code.Path.StartsWith("altar"))
            return;

        // Fire event for altar interaction
        if (byPlayer is IServerPlayer serverPlayer && blockSel != null)
        {
            OnAltarUsed?.Invoke(serverPlayer, blockSel);
        }
    }
}