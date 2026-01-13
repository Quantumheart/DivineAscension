using System;
using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace DivineAscension.Systems.Patches;

[HarmonyPatch]
public static class MushroomPatches
{
    /// <summary>
    /// Event fired when a mushroom block is harvested (drops are calculated).
    /// Provides the player, block, and mushroom type from Variant["mushroom"].
    /// </summary>
    public static event Action<IServerPlayer, Block, string?>? OnMushroomHarvested;

    /// <summary>
    /// Patch BlockMushroom.GetDrops - called when a mushroom is broken and drops are calculated.
    /// </summary>
    [HarmonyPatch(typeof(BlockMushroom), nameof(BlockMushroom.GetDrops))]
    [HarmonyPostfix]
    public static void Postfix_GetDrops(
        BlockMushroom __instance,
        IWorldAccessor world,
        BlockPos pos,
        IPlayer byPlayer)
    {
        // Only fire on server side
        if (world.Api.Side != EnumAppSide.Server) return;
        if (byPlayer is not IServerPlayer serverPlayer) return;

        // Get mushroom type from Variant
        __instance.Variant.TryGetValue("mushroom", out var mushroomType);

        OnMushroomHarvested?.Invoke(serverPlayer, __instance, mushroomType);
    }
}