using System;
using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace DivineAscension.Systems.Patches;

[HarmonyPatch]
public static class ClayFormingPatches
{
    public static event Action<IServerPlayer, ItemStack, int>? OnClayFormingFinished;

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

                // Extract clay quantity from recipe
                int clayConsumed = ExtractClayQuantity(__state, __instance.Api);

                OnClayFormingFinished?.Invoke(serverPlayer, resultStack, clayConsumed);
            }
    }

    private static int ExtractClayQuantity(ClayFormingRecipe recipe, ICoreAPI api)
    {
        try
        {
            if (recipe == null) return 0;

            // Try multiple approaches to find clay quantity
            // Approach 1: Check for QuantityLayers property (voxel count)
            var quantityLayersProperty = recipe.GetType().GetProperty("QuantityLayers");
            if (quantityLayersProperty != null)
            {
                var value = quantityLayersProperty.GetValue(recipe);
                if (value is int layers)
                {
                    api.Logger.Debug($"[DivineAscension] ClayFormingPatches: Found QuantityLayers = {layers}");
                    return layers;
                }
            }

            // Approach 2: Check for Voxels property
            var voxelsProperty = recipe.GetType().GetProperty("Voxels");
            if (voxelsProperty != null)
            {
                var value = voxelsProperty.GetValue(recipe);
                if (value != null)
                {
                    // Voxels might be an array or collection - try to get count
                    if (value is Array array)
                    {
                        int count = array.Length;
                        api.Logger.Debug($"[DivineAscension] ClayFormingPatches: Found Voxels array length = {count}");
                        return count;
                    }
                }
            }

            // Approach 3: Check for Ingredient property
            var ingredientProperty = recipe.GetType().GetProperty("Ingredient");
            if (ingredientProperty != null)
            {
                var ingredient = ingredientProperty.GetValue(recipe);
                if (ingredient != null)
                {
                    var quantityProperty = ingredient.GetType().GetProperty("Quantity");
                    if (quantityProperty != null)
                    {
                        var value = quantityProperty.GetValue(ingredient);
                        if (value is int quantity)
                        {
                            api.Logger.Debug($"[DivineAscension] ClayFormingPatches: Found Ingredient.Quantity = {quantity}");
                            return quantity;
                        }
                    }
                }
            }

            // Log all available properties for debugging
            var recipeItem = recipe.Output?.ResolvedItemstack?.Collectible?.Code?.ToString() ?? "unknown";
            api.Logger.Warning($"[DivineAscension] ClayFormingPatches: Could not find clay quantity for recipe output '{recipeItem}'. Available properties:");
            foreach (var prop in recipe.GetType().GetProperties())
            {
                api.Logger.Warning($"  - {prop.Name}: {prop.PropertyType.Name}");
            }

            return 0;
        }
        catch (Exception ex)
        {
            api.Logger.Error($"[DivineAscension] ClayFormingPatches: Error extracting clay quantity: {ex.Message}");
            return 0;
        }
    }
}