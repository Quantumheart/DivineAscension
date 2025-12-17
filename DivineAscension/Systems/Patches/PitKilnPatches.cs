using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.GameContent;

namespace PantheonWars.Systems.Patches;

[HarmonyPatch]
public static class PitKilnPatches
{
    // Deduplicate by kiln position + burn end time (cycle key) instead of time window
    private static readonly HashSet<string> _completedCycles = new();

    private static readonly ConditionalWeakTable<BlockEntityPitKiln, string> _kilnOwners = new();
    public static event Action<string, List<ItemStack>>? OnPitKilnFired;

    public static void ClearSubscribers()
    {
        OnPitKilnFired = null;
        _completedCycles.Clear();
    }

    [HarmonyPatch(typeof(BlockEntityPitKiln), "TryIgnite")]
    [HarmonyPostfix]
    public static void PostFix_TryIgnite(BlockEntityPitKiln __instance, IPlayer? byPlayer)
    {
        if (byPlayer is not null) _kilnOwners.AddOrUpdate(__instance, byPlayer.PlayerUID);
    }

    [HarmonyPatch(typeof(BlockEntityPitKiln), "ToTreeAttributes")]
    [HarmonyPostfix]
    public static void Postfix_ToTreeAttributes(BlockEntityPitKiln __instance, ITreeAttribute tree)
    {
        if (_kilnOwners.TryGetValue(__instance, out var uid)) tree.SetString("pw_ownerUid", uid);
    }

    [HarmonyPatch(typeof(BlockEntityPitKiln), "FromTreeAttributes")]
    [HarmonyPostfix]
    public static void Postfix_FromTreeAttributes(BlockEntityPitKiln __instance, ITreeAttribute tree)
    {
        var uid = tree.GetString("pw_ownerUid");
        if (!string.IsNullOrEmpty(uid)) _kilnOwners.AddOrUpdate(__instance, uid);
    }

    [HarmonyPatch(typeof(BlockEntityPitKiln), "OnFired")]
    [HarmonyPrefix]
    public static void Prefix_OnFired(BlockEntityPitKiln __instance, out List<ItemStack> __state)
    {
        __state = new List<ItemStack>();

        // Ensure we are on server side
        if (__instance?.Api == null || __instance.Api.Side != EnumAppSide.Server) return;

        // Store copies of items in slots 0-3
        for (var i = 0; i < 4; i++)
        {
            var slot = __instance.Inventory[i];
            if (!slot.Empty)
                __state.Add(slot.Itemstack.Clone());
            else
                __state.Add(null!);
        }
    }

    [HarmonyPatch(typeof(BlockEntityPitKiln), "OnFired")]
    [HarmonyPostfix]
    public static void Postfix_OnFired(BlockEntityPitKiln __instance, List<ItemStack> __state)
    {
        // Ensure we are on server side
        if (__instance?.Api == null || __instance.Api.Side != EnumAppSide.Server) return;

        // Build deterministic cycle key: kiln position + burn end time
        var cycleKey =
            $"{__instance.Pos.X},{__instance.Pos.Y},{__instance.Pos.Z}@{__instance.BurningUntilTotalHours.ToString("F3", CultureInfo.InvariantCulture)}";
        if (!_completedCycles.Add(cycleKey))
        {
            __instance.Api.Logger.Warning(
                $"[DivineAscension] Prevented duplicate Pit Kiln firing event at {__instance.Pos} for cycle {__instance.BurningUntilTotalHours:F3}");
            return;
        }

        var firedItems = new List<ItemStack>();

        // Compare items to detect changes (successful firing)
        for (var i = 0; i < 4; i++)
        {
            var currentSlot = __instance.Inventory[i];
            var originalStack = i < __state.Count ? __state[i] : null;

            if (originalStack != null && !currentSlot.Empty)
                // If the item type changed, it was fired.
                if (originalStack.Collectible.Code != currentSlot.Itemstack.Collectible.Code)
                    firedItems.Add(currentSlot.Itemstack.Clone());
        }

        // If items were fired, trigger event
        if (firedItems.Count > 0)
            if (_kilnOwners.TryGetValue(__instance, out var uid))
            {
                // Log debug info
                __instance.Api.Logger.Debug(
                    $"[DivineAscension] PitKilnPatches: Firing event for {uid} with {firedItems.Count} items at {__instance.Pos}");
                OnPitKilnFired?.Invoke(uid, firedItems);
            }
    }
}