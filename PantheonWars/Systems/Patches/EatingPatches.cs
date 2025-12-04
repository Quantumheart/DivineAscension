using System;
using System.Collections.Generic;
using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace PantheonWars.Systems.Patches;

[HarmonyPatch]
public static class EatingPatches
{
    // Track original stack before eating begins so we know what was eaten
    private static readonly Dictionary<string, ItemStack> _originalEatenStacks = new();
    public static event Action<IServerPlayer, ItemStack>? OnFoodEaten;

    public static void ClearSubscribers()
    {
        OnFoodEaten = null;
        _originalEatenStacks.Clear();
    }

    [HarmonyPatch(typeof(CollectibleObject), "tryEatBegin")]
    [HarmonyPrefix]
    public static void Prefix_tryEatBegin(ItemSlot slot, EntityAgent byEntity)
    {
        if (byEntity?.Api == null || byEntity.Api.Side != EnumAppSide.Server) return;

        if (slot?.Itemstack == null) return;

        if (byEntity is EntityPlayer eplr)
        {
            var uid = eplr.PlayerUID;
            if (!string.IsNullOrEmpty(uid))
                // store a clone so later mutations won't affect us
                _originalEatenStacks[uid] = slot.Itemstack.Clone();
        }
    }

    [HarmonyPatch(typeof(CollectibleObject), "tryEatStop")]
    [HarmonyPostfix]
    public static void Postfix_tryEatStop(ItemSlot slot, EntityAgent byEntity)
    {
        if (byEntity?.Api == null || byEntity.Api.Side != EnumAppSide.Server) return;

        if (byEntity is EntityPlayer eplr)
        {
            var uid = eplr.PlayerUID;
            if (string.IsNullOrEmpty(uid)) return;

            var sapi = byEntity.Api as ICoreServerAPI;
            var sp = sapi?.World.PlayerByUid(uid) as IServerPlayer;
            if (sp == null) return;

            // Prefer original stack captured at begin, fall back to current slot content
            _originalEatenStacks.TryGetValue(uid, out var eatenStack);
            if (eatenStack == null) eatenStack = slot?.Itemstack?.Clone();

            if (eatenStack != null)
                try
                {
                    OnFoodEaten?.Invoke(sp, eatenStack);
                }
                catch
                {
                    /* ignore listener errors */
                }

            // cleanup
            _originalEatenStacks.Remove(uid);
        }
    }
}