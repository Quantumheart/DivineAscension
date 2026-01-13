using System;
using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace DivineAscension.Systems.Patches;

[HarmonyPatch]
public static class BlockCropPatches
{
    /// <summary>
    /// Event fired when a crop block is harvested (drops are calculated).
    /// Provides the player and typed BlockCrop reference.
    /// </summary>
    public static event Action<IServerPlayer, BlockCrop>? OnCropHarvested;

    /// <summary>
    /// Patch BlockCrop.GetDrops - called when a crop is broken and drops are calculated.
    /// </summary>
    [HarmonyPatch(typeof(BlockCrop), nameof(BlockCrop.GetDrops))]
    [HarmonyPostfix]
    public static void Postfix_GetDrops(
        BlockCrop __instance,
        IWorldAccessor world,
        BlockPos pos,
        IPlayer byPlayer)
    {
        // Only fire on server side
        if (world.Api.Side != EnumAppSide.Server) return;
        if (byPlayer is not IServerPlayer serverPlayer) return;

        OnCropHarvested?.Invoke(serverPlayer, __instance);
    }
}