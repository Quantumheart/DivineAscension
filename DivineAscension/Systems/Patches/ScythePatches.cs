using System;
using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace DivineAscension.Systems.Patches;

[HarmonyPatch]
public static class ScythePatches
{
    /// <summary>
    /// Event fired when a scythe (or shears) breaks a block.
    /// Fired BEFORE the block is actually broken, so the block data is still available.
    /// </summary>
    public static event Action<IServerPlayer, Block>? OnScytheHarvest;

    public static void ClearSubscribers()
    {
        OnScytheHarvest = null;
    }

    /// <summary>
    /// Patch ItemShears.breakMultiBlock - this is called for each block broken by scythe/shears.
    /// Using Prefix to capture block data before it's destroyed.
    /// </summary>
    [HarmonyPatch(typeof(ItemShears), "breakMultiBlock")]
    [HarmonyPrefix]
    public static void Prefix_breakMultiBlock(
        BlockPos pos,
        IPlayer plr)
    {
        // Only fire on server side
        if (plr?.Entity?.World?.Api?.Side != EnumAppSide.Server) return;
        if (plr is not IServerPlayer serverPlayer) return;

        // Get block BEFORE it's broken
        var block = plr.Entity.World.BlockAccessor.GetBlock(pos);
        if (block?.Code == null) return;

        OnScytheHarvest?.Invoke(serverPlayer, block);
    }
}