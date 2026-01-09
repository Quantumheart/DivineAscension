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

    public static Action<IServerPlayer?, BlockSelection> Picked { get; set; }

    /// <summary>
    /// Patch BlockBehaviorHarvestable.OnBlockInteractStop - this is called when player forages
    /// </summary>
    [HarmonyPatch(typeof(BlockBehaviorHarvestable), "OnBlockInteractStop")]
    [HarmonyPostfix]
    public static void Postfix_GetDrops(
        BlockBehaviorHarvestable __instance,
        float secondsUsed,
        IWorldAccessor world,
        IPlayer byPlayer,
        BlockSelection blockSel,
        ref EnumHandling handled)
    {
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (blockSel == null || world == null || byPlayer == null)
            return;
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (_harvestTimeField != null)
        {
            float harvestTime = (float)_harvestTimeField.GetValue(__instance);
            if (secondsUsed > harvestTime)
                Picked.Invoke(byPlayer as IServerPlayer, blockSel);
        }
    }
}