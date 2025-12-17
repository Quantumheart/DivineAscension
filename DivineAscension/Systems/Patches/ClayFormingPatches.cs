using System;
using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace DivineAscension.Systems.Patches;

[HarmonyPatch]
public static class ClayFormingPatches
{
    public static event Action<IServerPlayer, ItemStack>? OnClayFormingFinished;

    [HarmonyPatch(typeof(BlockEntityClayForm), "CheckIfFinished")]
    [HarmonyPrefix]
    public static void Prefix_CheckIfFinished(BlockEntityClayForm __instance, out ClayFormingRecipe? __state)
    {
        // Capture the recipe before it might be cleared by the method
        __state = __instance.SelectedRecipe;
    }

    [HarmonyPatch(typeof(BlockEntityClayForm), "CheckIfFinished")]
    [HarmonyPostfix]
    public static void Postfix_CheckIfFinished(BlockEntityClayForm __instance, IPlayer byPlayer,
        ClayFormingRecipe? __state)
    {
        // Ensure we are on server side
        if (__instance?.Api == null || __instance.Api.Side != EnumAppSide.Server) return;

        // If we didn't have a recipe start with, ignore
        if (__state == null) return;

        // If the recipe is now null in the instance, it means CheckIfFinished successfully completed
        // the crafting and cleared the recipe field.
        if (__instance.SelectedRecipe == null)
            if (byPlayer is IServerPlayer serverPlayer)
            {
                var resultStack = __state.Output.ResolvedItemstack.Clone();
                OnClayFormingFinished?.Invoke(serverPlayer, resultStack);
            }
    }
}