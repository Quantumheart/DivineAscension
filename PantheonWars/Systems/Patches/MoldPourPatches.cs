using System;
using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace PantheonWars.Systems.Patches;

[HarmonyPatch]
public static class MoldPourPatches
{
    // Fires when molten metal is poured into a mold and the fill level increases
    // playerUid may be null if not resolvable
    public static event Action<string?, BlockPos, int, bool>? OnMoldPoured;

    // Capture state before a crucible pour step
    public struct PourState
    {
        public BlockPos Pos;
        public int PrevLevel;
        public bool IsToolMold;
        public bool Valid;
    }

    // Patch the generic CollectibleObject to catch crucible pouring without depending on ItemCrucible type
    [HarmonyPatch(typeof(CollectibleObject), "OnHeldInteractStep")]
    [HarmonyPrefix]
    public static void Prefix_OnHeldInteractStep(
        float secondsUsed,
        ItemSlot slot,
        EntityAgent byEntity,
        BlockSelection blockSel,
        EntitySelection entitySel,
        out PourState __state)
    {
        __state = default;
        if (byEntity?.Api == null || byEntity.Api.Side != EnumAppSide.Server) return;
        if (blockSel == null) return;
        // Only proceed if the held item appears to be a crucible
        var codePath = slot?.Itemstack?.Collectible?.Code?.Path?.ToLowerInvariant();
        if (string.IsNullOrEmpty(codePath) || !codePath.Contains("crucible")) return;

        var be = byEntity.World.BlockAccessor.GetBlockEntity(blockSel.Position);
        if (be is BlockEntityToolMold tool)
        {
            __state = new PourState
            {
                Pos = blockSel.Position.Copy(),
                PrevLevel = tool.FillLevel,
                IsToolMold = true,
                Valid = true
            };
        }
        else if (be is BlockEntityIngotMold ingot)
        {
            int prev = (ingot.FillLevelLeft + ingot.FillLevelRight);
            __state = new PourState
            {
                Pos = blockSel.Position.Copy(),
                PrevLevel = prev,
                IsToolMold = false,
                Valid = true
            };
        }
    }

    [HarmonyPatch(typeof(CollectibleObject), "OnHeldInteractStep")]
    [HarmonyPostfix]
    public static void Postfix_OnHeldInteractStep(
        float secondsUsed,
        ItemSlot slot,
        EntityAgent byEntity,
        BlockSelection blockSel,
        EntitySelection entitySel,
        PourState __state)
    {
        if (!__state.Valid) return;
        if (byEntity?.Api == null || byEntity.Api.Side != EnumAppSide.Server) return;

        var be = byEntity.World.BlockAccessor.GetBlockEntity(__state.Pos);
        int current = __state.PrevLevel;
        if (__state.IsToolMold && be is BlockEntityToolMold tool)
        {
            current = tool.FillLevel;
        }
        else if (!__state.IsToolMold && be is BlockEntityIngotMold ingot)
        {
            current = ingot.FillLevelLeft + ingot.FillLevelRight;
        }
        else
        {
            return;
        }

        int delta = current - __state.PrevLevel;
        if (delta <= 0) return;

        string? playerUid = (byEntity as EntityPlayer)?.Player?.PlayerUID;
        OnMoldPoured?.Invoke(playerUid, __state.Pos, delta, __state.IsToolMold);
    }
}
