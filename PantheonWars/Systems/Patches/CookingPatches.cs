using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace PantheonWars.Systems.Patches;

/// <summary>
/// Cooking completion event source. Harmony hooks will be added to firepit/crock in a later change.
/// For now this class declares the event contract for systems to subscribe to.
/// </summary>
[HarmonyPatch]
public static class CookingPatches
{
    public static event Action<string, ItemStack, BlockPos>? OnMealCooked;

    public static void ClearSubscribers()
    {
        OnMealCooked = null;
    }

    // Internal helper for future patches to safely invoke the event
    internal static void RaiseMealCooked(string playerUid, ItemStack stack, BlockPos pos)
    {
        try { OnMealCooked?.Invoke(playerUid, stack, pos); }
        catch { /* ignore listener errors */ }
    }

    // --- Firepit owner tracking -------------------------------------------------
    private static readonly ConditionalWeakTable<BlockEntityFirepit, string> _firepitOwners = new();

    [HarmonyPatch(typeof(BlockEntityFirepit), "TryIgnite")]
    [HarmonyPostfix]
    public static void Postfix_Firepit_TryIgnite(BlockEntityFirepit __instance, IPlayer? byPlayer)
    {
        if (byPlayer is null) return;
        _firepitOwners.AddOrUpdate(__instance, byPlayer.PlayerUID);
    }

    [HarmonyPatch(typeof(BlockEntityFirepit), "ToTreeAttributes")]
    [HarmonyPostfix]
    public static void Postfix_Firepit_ToTreeAttributes(BlockEntityFirepit __instance, ITreeAttribute tree)
    {
        if (_firepitOwners.TryGetValue(__instance, out var uid))
        {
            tree.SetString("pw_ownerUid", uid);
        }
    }

    [HarmonyPatch(typeof(BlockEntityFirepit), "FromTreeAttributes")]
    [HarmonyPostfix]
    public static void Postfix_Firepit_FromTreeAttributes(BlockEntityFirepit __instance, ITreeAttribute tree)
    {
        string uid = tree.GetString("pw_ownerUid");
        if (!string.IsNullOrEmpty(uid))
        {
            _firepitOwners.AddOrUpdate(__instance, uid);
        }
    }

    // Capture inventory state before cooking completes to identify cooked output
    [HarmonyPatch(typeof(BlockEntityFirepit), "OnCookingComplete")]
    [HarmonyPrefix]
    public static void Prefix_Firepit_OnCookingComplete(BlockEntityFirepit __instance, out List<ItemStack?> __state)
    {
        __state = new List<ItemStack?>();
        if (__instance?.Api == null || __instance.Api.Side != EnumAppSide.Server) return;
        var inv = __instance.Inventory;
        if (inv == null) return;
        int count = inv.Count;
        for (int i = 0; i < count; i++)
        {
            var slot = inv[i];
            __state.Add(slot?.Empty == false ? slot.Itemstack.Clone() : null);
        }
    }

    [HarmonyPatch(typeof(BlockEntityFirepit), "OnCookingComplete")]
    [HarmonyPostfix]
    public static void Postfix_Firepit_OnCookingComplete(BlockEntityFirepit __instance, List<ItemStack?> __state)
    {
        if (__instance?.Api == null || __instance.Api.Side != EnumAppSide.Server) return;

        if (!_firepitOwners.TryGetValue(__instance, out var uid) || string.IsNullOrEmpty(uid))
        {
            // No attribution allowed per requirements
            return;
        }

        var inv = __instance.Inventory;
        if (inv == null) return;

        int count = Math.Min(__state?.Count ?? 0, inv.Count);
        for (int i = 0; i < count; i++)
        {
            var before = __state[i];
            var afterSlot = inv[i];
            if (afterSlot == null || afterSlot.Empty) continue;
            var after = afterSlot.Itemstack;

            // Consider it cooked if collectible code changed
            if (before == null || before.Collectible?.Code != after.Collectible?.Code)
            {
                RaiseMealCooked(uid, after.Clone(), __instance.Pos);
            }
        }
    }

    // Some versions cook via TryCook, so capture that as well
    [HarmonyPatch(typeof(BlockEntityFirepit), "TryCook")]
    [HarmonyPostfix]
    public static void Postfix_Firepit_TryCook(BlockEntityFirepit __instance)
    {
        // If the implementation already raised via OnCookingComplete, this will be a no-op
        // We avoid duplications by not deduping here; most versions call only one or the other.
        // Intentionally left minimal.
    }

    // --- Crock owner tracking & meal creation -----------------------------------
    private static readonly ConditionalWeakTable<BlockEntityCrock, string> _crockOwners = new();

    [HarmonyPatch(typeof(BlockEntityCrock), "TrySeal")]
    [HarmonyPostfix]
    public static void Postfix_Crock_TrySeal(BlockEntityCrock __instance, IPlayer byPlayer)
    {
        if (byPlayer is null) return;
        _crockOwners.AddOrUpdate(__instance, byPlayer.PlayerUID);
    }

    [HarmonyPatch(typeof(BlockEntityCrock), "ToTreeAttributes")]
    [HarmonyPostfix]
    public static void Postfix_Crock_ToTreeAttributes(BlockEntityCrock __instance, ITreeAttribute tree)
    {
        if (_crockOwners.TryGetValue(__instance, out var uid))
        {
            tree.SetString("pw_ownerUid", uid);
        }
    }

    [HarmonyPatch(typeof(BlockEntityCrock), "FromTreeAttributes")]
    [HarmonyPostfix]
    public static void Postfix_Crock_FromTreeAttributes(BlockEntityCrock __instance, ITreeAttribute tree)
    {
        string uid = tree.GetString("pw_ownerUid");
        if (!string.IsNullOrEmpty(uid))
        {
            _crockOwners.AddOrUpdate(__instance, uid);
        }
    }

    // Detect meal creation or change when the crock is sealed (meal formation moment)
    [HarmonyPatch(typeof(BlockEntityCrock), "TrySeal")]
    [HarmonyPrefix]
    public static void Prefix_Crock_TrySeal(BlockEntityCrock __instance, out List<ItemStack?> __state)
    {
        __state = new List<ItemStack?>();
        if (__instance?.Api == null || __instance.Api.Side != EnumAppSide.Server) return;
        var inv = __instance.Inventory;
        if (inv == null) return;
        int count = inv.Count;
        for (int i = 0; i < count; i++)
        {
            var slot = inv[i];
            __state.Add(slot?.Empty == false ? slot.Itemstack.Clone() : null);
        }
    }

    [HarmonyPatch(typeof(BlockEntityCrock), "TrySeal")]
    [HarmonyPostfix]
    public static void Postfix_Crock_TrySeal(BlockEntityCrock __instance, List<ItemStack?> __state)
    {
        if (__instance?.Api == null || __instance.Api.Side != EnumAppSide.Server) return;
        if (!_crockOwners.TryGetValue(__instance, out var uid) || string.IsNullOrEmpty(uid)) return;

        var inv = __instance.Inventory;
        if (inv == null) return;

        int count = Math.Min(__state?.Count ?? 0, inv.Count);
        for (int i = 0; i < count; i++)
        {
            var before = __state[i];
            var afterSlot = inv[i];
            if (afterSlot == null || afterSlot.Empty) continue;
            var after = afterSlot.Itemstack;

            if (before == null || before.Collectible?.Code != after.Collectible?.Code)
            {
                RaiseMealCooked(uid, after.Clone(), __instance.Pos);
            }
        }
    }
}
