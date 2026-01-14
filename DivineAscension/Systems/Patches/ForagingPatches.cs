using System;
using System.Reflection;
using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace DivineAscension.Systems.Patches;

[HarmonyPatch]
public static class ForagingPatches
{
    // Cache the private field for performance
    private static readonly FieldInfo _harvestTimeField =
        AccessTools.Field(typeof(BlockBehaviorHarvestable), "harvestTime");

    // ThreadStatic ensures thread safety for parallel world ticks
    [ThreadStatic] private static Block? _capturedBlock;

    /// <summary>
    /// Event fired when a harvestable block is picked.
    /// Provides the player, block selection, and the original block (captured before state change).
    /// </summary>
    public static event Action<IServerPlayer?, BlockSelection?, Block?>? Picked;

    public static void ClearSubscribers()
    {
        Picked = null;
    }

    /// <summary>
    /// Prefix: Capture the block BEFORE harvest changes its state.
    /// </summary>
    [HarmonyPatch(typeof(BlockBehaviorHarvestable), "OnBlockInteractStop")]
    [HarmonyPrefix]
    public static void Prefix_OnBlockInteractStop(
        IWorldAccessor world,
        BlockSelection blockSel)
    {
        _capturedBlock = null;
        if (blockSel?.Position == null || world == null) return;

        // Capture the block in its current state (before harvest changes it)
        _capturedBlock = world.BlockAccessor.GetBlock(blockSel.Position);
    }

    /// <summary>
    /// Postfix: Fire event with the captured block.
    /// </summary>
    [HarmonyPatch(typeof(BlockBehaviorHarvestable), "OnBlockInteractStop")]
    [HarmonyPostfix]
    public static void Postfix_OnBlockInteractStop(
        BlockBehaviorHarvestable __instance,
        float secondsUsed,
        IWorldAccessor world,
        IPlayer byPlayer,
        BlockSelection blockSel,
        ref EnumHandling handled)
    {
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (blockSel == null || world == null || byPlayer == null)
        {
            _capturedBlock = null;
            return;
        }

        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (_harvestTimeField != null)
        {
            var harvestTimeValue = _harvestTimeField.GetValue(__instance);
            if (harvestTimeValue is float harvestTime && secondsUsed > harvestTime)
                Picked?.Invoke(byPlayer as IServerPlayer, blockSel, _capturedBlock);
        }

        _capturedBlock = null;
    }
}