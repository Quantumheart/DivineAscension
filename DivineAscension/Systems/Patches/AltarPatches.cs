using System;
using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace DivineAscension.Systems.Patches;

/// <summary>
/// Harmony patches for altar block interactions and destruction.
/// Fires events when players interact with or destroy altar blocks (code starts with "altar").
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
    /// Event fired when a player breaks an altar block.
    /// Provides the server player and block position for holy site deconsecration.
    /// </summary>
    public static event Action<IServerPlayer, BlockPos>? OnAltarBroken;

    /// <summary>
    /// Clears event subscribers. Called during server initialization to prevent stale subscriptions.
    /// </summary>
    public static void ClearSubscribers()
    {
        OnAltarUsed = null;
        OnAltarBroken = null;
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

    /// <summary>
    /// Patches Block.OnBlockBroken to detect altar destruction.
    /// Fires before the block is removed so we can access its data.
    /// </summary>
    [HarmonyPatch(typeof(Block), nameof(Block.OnBlockBroken))]
    [HarmonyPrefix]
    public static void Prefix_OnBlockBroken(
        Block __instance,
        IWorldAccessor world,
        BlockPos pos,
        IPlayer byPlayer)
    {
        // Only process on server side
        if (world?.Side != EnumAppSide.Server)
            return;

        // Check if block and player are valid
        if (__instance?.Code?.Path == null || byPlayer == null)
            return;

        // Check if this is an altar block
        if (!__instance.Code.Path.StartsWith("altar"))
            return;

        // Fire event for altar destruction
        if (byPlayer is IServerPlayer serverPlayer)
        {
            OnAltarBroken?.Invoke(serverPlayer, pos);
        }
    }
}