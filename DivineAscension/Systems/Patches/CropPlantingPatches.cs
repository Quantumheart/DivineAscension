using System;
using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace DivineAscension.Systems.Patches;

[HarmonyPatch]
public static class CropPlantingPatches
{
    public static event Action<IServerPlayer, Block>? OnCropPlanted;

    public static void ClearSubscribers()
    {
        OnCropPlanted = null;
    }

    /// <summary>
    /// Patch BlockEntityFarmland.TryPlant - this is called when player plants crops on farmland
    /// </summary>
    [HarmonyPatch(typeof(BlockEntityFarmland), "TryPlant")]
    [HarmonyPostfix]
    public static void Postfix_TryPlant(
        BlockEntityFarmland __instance,
        Block block,
        ItemSlot itemslot,
        EntityAgent byEntity,
        BlockSelection blockSel,
        bool __result)
    {
        var api = __instance.Api;

        // Only proceed if planting was successful and we're on the server
        if (!__result)
        {
            api?.Logger?.Debug("[CropPlantingPatches] TryPlant returned false");
            return;
        }

        if (api?.Side != EnumAppSide.Server)
        {
            api?.Logger?.Debug("[CropPlantingPatches] Not on server side");
            return;
        }

        // Get the player from the EntityAgent
        var player = byEntity as EntityPlayer;
        if (player?.Player is not IServerPlayer serverPlayer)
        {
            api?.Logger?.Debug(
                $"[CropPlantingPatches] Could not get IServerPlayer from EntityAgent: {byEntity?.GetType().Name}");
            return;
        }

        // Check if the planted block is a crop
        if (block is not BlockCrop)
        {
            api?.Logger?.Debug(
                $"[CropPlantingPatches] Block is not a crop: {block?.GetType().Name}, Code: {block?.Code?.Path}");
            return;
        }

        api?.Logger?.Notification(
            $"[CropPlantingPatches] Crop planted by {serverPlayer.PlayerName}: {block.Code?.Path}");
        OnCropPlanted?.Invoke(serverPlayer, block);
    }
}