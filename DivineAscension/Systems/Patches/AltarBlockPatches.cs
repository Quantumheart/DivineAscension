using HarmonyLib;
using Vintagestory.API.Common;

namespace DivineAscension.Systems.Patches;

/// <summary>
/// Makes altar blocks interactive for the prayer system.
/// Patches Block.OnBlockInteractStart to allow OnDidUseBlock events to fire for altars.
/// </summary>
[HarmonyPatch]
public static class AltarBlockPatches
{
    /// <summary>
    /// Patch Block.OnBlockInteractStart to make altars interactive.
    /// This allows the OnDidUseBlock event to fire when players right-click altar blocks.
    /// Without this patch, altars would be non-interactive and prayers wouldn't work.
    /// </summary>
    [HarmonyPrefix]
    [HarmonyPatch(typeof(Block), nameof(Block.OnBlockInteractStart))]
    public static bool OnBlockInteractStart_Prefix(
        Block __instance,
        IWorldAccessor world,
        IPlayer byPlayer,
        BlockSelection blockSel,
        ref bool __result)
    {
        // Check if this is an altar block
        if (__instance?.Code?.Path?.StartsWith("altar") ?? false)
        {
            // Make altars interactive - returning true allows OnDidUseBlock to fire
            __result = true;
            return false; // Skip original method (we've set the result)
        }

        return true; // Continue with original method for other blocks
    }
}
