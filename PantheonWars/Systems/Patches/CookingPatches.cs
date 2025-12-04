using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using HarmonyLib;
using System.Reflection;
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
    private static readonly ConditionalWeakTable<BlockEntityFirepit, string> _lastInteractor = new();

    // Track last interacting player via exact decompiled method
    [HarmonyPatch(typeof(BlockEntityFirepit), nameof(BlockEntityFirepit.OnPlayerRightClick))]
    [HarmonyPostfix]
    public static void Postfix_Firepit_OnPlayerRightClick(BlockEntityFirepit __instance, IPlayer byPlayer)
    {
        if (byPlayer != null)
        {
            _lastInteractor.AddOrUpdate(__instance, byPlayer.PlayerUID);
        }
    }

    // Attribute ownership exactly when fuel is ignited in this decompiled version
    [HarmonyPatch(typeof(BlockEntityFirepit), nameof(BlockEntityFirepit.igniteFuel))]
    [HarmonyPostfix]
    public static void Postfix_Firepit_IgniteFuel(BlockEntityFirepit __instance)
    {
        if (_lastInteractor.TryGetValue(__instance, out var uid) && !string.IsNullOrEmpty(uid))
        {
            _firepitOwners.AddOrUpdate(__instance, uid);
        }
    }

    // Also handle the direct ignite path with a specific fuel stack
    [HarmonyPatch(typeof(BlockEntityFirepit), nameof(BlockEntityFirepit.igniteWithFuel))]
    [HarmonyPostfix]
    public static void Postfix_Firepit_IgniteWithFuel(BlockEntityFirepit __instance)
    {
        if (_lastInteractor.TryGetValue(__instance, out var uid) && !string.IsNullOrEmpty(uid))
        {
            _firepitOwners.AddOrUpdate(__instance, uid);
        }
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
    [HarmonyPatch(typeof(BlockEntityFirepit), nameof(BlockEntityFirepit.smeltItems))]
    [HarmonyPrefix]
    public static void Prefix_Firepit_SmeltItems(BlockEntityFirepit __instance, out List<ItemStack?> __state)
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

    [HarmonyPatch(typeof(BlockEntityFirepit), nameof(BlockEntityFirepit.smeltItems))]
    [HarmonyPostfix]
    public static void Postfix_Firepit_SmeltItems(BlockEntityFirepit __instance, List<ItemStack?> __state)
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

    // No TryCook/etc. in this decompiled version; cooking completes via smeltItems()

    // --- Crock owner tracking & meal creation -----------------------------------
    private static readonly ConditionalWeakTable<BlockEntityCrock, string> _crockOwners = new();

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

    // Detect sealing of a placed crock via the block interaction entry point
    // In this VS build, there is no BlockEntityCrock.TrySeal; sealing is handled in BlockCrock interactions.
    [HarmonyPatch(typeof(BlockCrock), nameof(BlockCrock.OnBlockInteractStart))]
    [HarmonyPrefix]
    public static void Prefix_BlockCrock_OnBlockInteractStart(
        IWorldAccessor world,
        IPlayer byPlayer,
        BlockSelection blockSel,
        out (bool captured, bool wasSealed, List<ItemStack?> beforeInv) __state)
    {
        __state = default;
        if (world == null || blockSel == null) return;
        var be = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BlockEntityCrock;
        if (be?.Api == null || be.Api.Side != EnumAppSide.Server) return;

        var before = new List<ItemStack?>();
        var inv = be.Inventory;
        int count = inv?.Count ?? 0;
        for (int i = 0; i < count; i++)
        {
            var slot = inv[i];
            before.Add(slot?.Empty == false ? slot.Itemstack.Clone() : null);
        }
        __state = (true, be.Sealed, before);
    }

    [HarmonyPatch(typeof(BlockCrock), nameof(BlockCrock.OnBlockInteractStart))]
    [HarmonyPostfix]
    public static void Postfix_BlockCrock_OnBlockInteractStart(
        IWorldAccessor world,
        IPlayer byPlayer,
        BlockSelection blockSel,
        (bool captured, bool wasSealed, List<ItemStack?> beforeInv) __state)
    {
        if (!__state.captured || world == null || blockSel == null) return;
        var be = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BlockEntityCrock;
        if (be?.Api == null || be.Api.Side != EnumAppSide.Server) return;

        // Sealing detected: transition from unsealed to sealed after interaction
        if (!__state.wasSealed && be.Sealed)
        {
            if (byPlayer != null)
            {
                _crockOwners.AddOrUpdate(be, byPlayer.PlayerUID);
            }

            // If we have an owner, check for content changes and raise event
            if (_crockOwners.TryGetValue(be, out var uid) && !string.IsNullOrEmpty(uid))
            {
                var inv = be.Inventory;
                int count = Math.Min(__state.beforeInv?.Count ?? 0, inv?.Count ?? 0);
                for (int i = 0; i < count; i++)
                {
                    var before = __state.beforeInv?[i];
                    var afterSlot = inv?[i];
                    if (afterSlot == null || afterSlot.Empty) continue;
                    var after = afterSlot.Itemstack;
                    if (before == null || before.Collectible?.Code != after.Collectible?.Code)
                    {
                        RaiseMealCooked(uid, after.Clone(), be.Pos);
                    }
                }
            }
        }
    }
}
