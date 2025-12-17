using System;
using System.Collections.Concurrent;
using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace PantheonWars.Systems.Patches;

[HarmonyPatch]
public static class MoldPourPatches
{
    private const int DEBOUNCE_MS = 500; // Fire event at most once per 500ms per position

    // Debounce tracking to avoid firing event on every tick during continuous pouring
    private static readonly ConcurrentDictionary<string, PourAccumulator> ActivePours = new();

    // Fires when molten metal is poured into a mold and the fill level increases
    // playerUid may be null if not resolvable
    public static event Action<string?, BlockPos, int, bool>? OnMoldPoured;

    private static long NowMs()
    {
        return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    }

    // Patch BlockSmeltedContainer (crucible) to catch pouring into molds
    [HarmonyPatch(typeof(BlockSmeltedContainer), "OnHeldInteractStep")]
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
            var prev = ingot.FillLevelLeft + ingot.FillLevelRight;
            __state = new PourState
            {
                Pos = blockSel.Position.Copy(),
                PrevLevel = prev,
                IsToolMold = false,
                Valid = true
            };
        }
    }

    [HarmonyPatch(typeof(BlockSmeltedContainer), "OnHeldInteractStep")]
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
        var current = __state.PrevLevel;
        if (__state.IsToolMold && be is BlockEntityToolMold tool)
            current = tool.FillLevel;
        else if (!__state.IsToolMold && be is BlockEntityIngotMold ingot)
            current = ingot.FillLevelLeft + ingot.FillLevelRight;
        else
            return;

        var delta = current - __state.PrevLevel;
        if (delta <= 0) return;

        var playerUid = (byEntity as EntityPlayer)?.Player?.PlayerUID;
        if (string.IsNullOrEmpty(playerUid)) return;

        // Use a key combining position and player to track independent pour sessions
        var key = $"{__state.Pos.X}_{__state.Pos.Y}_{__state.Pos.Z}_{playerUid}";
        var now = NowMs();

        // Accumulate units and debounce event firing
        var accumulator = ActivePours.GetOrAdd(key, _ => new PourAccumulator
        {
            PlayerUid = playerUid,
            Pos = __state.Pos.Copy(),
            IsToolMold = __state.IsToolMold,
            LastFireMs = now - DEBOUNCE_MS // Allow immediate first fire
        });

        accumulator.AccumulatedUnits += delta;

        // Only fire event if enough time has passed since last fire
        if (now - accumulator.LastFireMs >= DEBOUNCE_MS)
        {
            OnMoldPoured?.Invoke(playerUid, __state.Pos, accumulator.AccumulatedUnits, __state.IsToolMold);
            accumulator.AccumulatedUnits = 0;
            accumulator.LastFireMs = now;
        }
    }

    // Hook into OnHeldInteractStop to fire any remaining accumulated units and cleanup
    [HarmonyPatch(typeof(BlockSmeltedContainer), "OnHeldInteractStop")]
    [HarmonyPostfix]
    public static void Postfix_OnHeldInteractStop(
        float secondsUsed,
        ItemSlot slot,
        EntityAgent byEntity,
        BlockSelection blockSel,
        EntitySelection entitySel)
    {
        if (byEntity?.Api == null || byEntity.Api.Side != EnumAppSide.Server) return;
        if (blockSel == null) return;

        var playerUid = (byEntity as EntityPlayer)?.Player?.PlayerUID;
        if (string.IsNullOrEmpty(playerUid)) return;

        var key = $"{blockSel.Position.X}_{blockSel.Position.Y}_{blockSel.Position.Z}_{playerUid}";

        // Fire any remaining accumulated units before cleanup
        if (ActivePours.TryRemove(key, out var accumulator) && accumulator.AccumulatedUnits > 0)
            OnMoldPoured?.Invoke(playerUid, blockSel.Position, accumulator.AccumulatedUnits, accumulator.IsToolMold);
    }

    // Capture state before a crucible pour step
    public struct PourState
    {
        public BlockPos Pos;
        public int PrevLevel;
        public bool IsToolMold;
        public bool Valid;
    }

    // Accumulates units poured during continuous pour action to debounce event firing
    private class PourAccumulator
    {
        public string PlayerUid { get; set; } = string.Empty;
        public BlockPos Pos { get; set; } = null!;
        public bool IsToolMold { get; set; }
        public int AccumulatedUnits { get; set; }
        public long LastFireMs { get; set; }
    }
}