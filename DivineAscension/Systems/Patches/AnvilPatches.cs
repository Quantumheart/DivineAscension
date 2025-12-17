using System;
using System.Collections.Concurrent;
using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace DivineAscension.Systems.Patches;

[HarmonyPatch]
public static class AnvilPatches
{
    // Short-lived cache tying the last player striker to a given anvil position
    private static readonly ConcurrentDictionary<BlockPos, (string Uid, long TsMs)> LastStriker = new();

    // Debounce map to avoid duplicate completion emissions per anvil within a tiny window
    private static readonly ConcurrentDictionary<BlockPos, long> LastCompletionTs = new();

    // Fires once when an anvil workpiece transitions from present -> null during a hammer interaction
    // playerUid may be null if not resolvable (e.g., helve hammer); output may be null if not detectable
    public static event Action<string?, BlockPos, ItemStack?>? OnAnvilRecipeCompleted;

    private static long NowMs()
    {
        return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    }

    // Allows the mod bootstrap to clear any lingering subscribers/cached state on server start or reloads
    public static void ClearSubscribers()
    {
        OnAnvilRecipeCompleted = null;
        LastStriker.Clear();
    }

    [HarmonyPatch(typeof(ItemHammer), "strikeAnvil")]
    [HarmonyPostfix]
    public static void Postfix_StrikeAnvil(
        ItemHammer __instance,
        EntityAgent byEntity,
        ItemSlot slot,
        ItemStack strikingItem)
    {
        // We only attribute on server and when we can resolve player + target position
        var player = (byEntity as EntityPlayer)?.Player;
        var api = byEntity?.Api;
        if (player == null || api == null || api.Side != EnumAppSide.Server) return;

        var bsel = player.CurrentBlockSelection;
        if (bsel?.Position == null) return;

        var pos = bsel.Position.Copy();
        var uid = player.PlayerUID;
        if (!string.IsNullOrEmpty(uid)) LastStriker[pos] = (uid, NowMs());
    }

    // Detect the exact moment a hammer hit finishes processing on the anvil part
    [HarmonyPatch(typeof(BlockEntityAnvilPart), "OnHammerHitOver")]
    [HarmonyPrefix]
    public static void Prefix_OnHammerHitOver(BlockEntityAnvilPart __instance, IPlayer byPlayer, Vec3d hitPosition,
        out CompletionState __state)
    {
        __state = default;
        var api = __instance.Api;
        if (api == null || api.Side != EnumAppSide.Server) return;

        // Resolve the parent anvil at this position
        var be = api.World.BlockAccessor.GetBlockEntity(__instance.Pos) as BlockEntityAnvil;
        if (be == null) return;

        __state = new CompletionState
        {
            HadWorkItem = be.WorkItemStack != null,
            Pos = be.Pos.Copy(),
            OutputPreview = be.SelectedRecipe?.Output?.ResolvedItemstack?.Clone()
        };
    }

    [HarmonyPatch(typeof(BlockEntityAnvilPart), "OnHammerHitOver")]
    [HarmonyPostfix]
    public static void Postfix_OnHammerHitOver(BlockEntityAnvilPart __instance, IPlayer byPlayer, Vec3d hitPosition,
        CompletionState __state)
    {
        var api = __instance.Api;
        if (api == null || api.Side != EnumAppSide.Server) return;
        if (!__state.HadWorkItem) return;

        var be = api.World.BlockAccessor.GetBlockEntity(__state.Pos) as BlockEntityAnvil;
        if (be == null) return;

        var hasWorkNow = be.WorkItemStack != null;
        if (!hasWorkNow)
        {
            // Do not emit completion here to avoid duplicates with CheckIfFinished.
            // This path pertains to welding an anvil (top/base merge), not regular smithing.
        }
    }

    // Server-side authoritative completion for regular anvil smithing
    // This method clears the work item and spawns/gives the output when the voxel shape matches the recipe
    [HarmonyPatch(typeof(BlockEntityAnvil), "CheckIfFinished")]
    [HarmonyPrefix]
    public static void Prefix_CheckIfFinished(BlockEntityAnvil __instance, IPlayer byPlayer,
        out CompletionState __state)
    {
        __state = default;
        var api = __instance.Api;
        if (api == null || api.Side != EnumAppSide.Server) return;

        __state = new CompletionState
        {
            HadWorkItem = __instance.WorkItemStack != null,
            Pos = __instance.Pos.Copy(),
            OutputPreview = __instance.SelectedRecipe?.Output?.ResolvedItemstack?.Clone()
        };
    }

    [HarmonyPatch(typeof(BlockEntityAnvil), "CheckIfFinished")]
    [HarmonyPostfix]
    public static void Postfix_CheckIfFinished(BlockEntityAnvil __instance, IPlayer byPlayer, CompletionState __state)
    {
        var api = __instance.Api;
        if (api == null || api.Side != EnumAppSide.Server) return;

        // Completion occurred if we had work before and after the call there is no work item anymore
        if (!__state.HadWorkItem) return;
        var hasWorkNow = __instance.WorkItemStack != null;
        if (hasWorkNow) return;

        // Debounce: ensure single emission per completion even if called twice in rapid succession
        var now = NowMs();
        if (LastCompletionTs.TryGetValue(__state.Pos, out var lastTs))
            if (now - lastTs <= 250)
                return;

        LastCompletionTs[__state.Pos] = now;

        // Attribute to recent striker if possible, else fall back to byPlayer (may be null for helve)
        string? uid = null;
        if (LastStriker.TryGetValue(__state.Pos, out var rec) && NowMs() - rec.TsMs <= 1000)
            uid = rec.Uid;
        else if (byPlayer != null) uid = byPlayer.PlayerUID;

        OnAnvilRecipeCompleted?.Invoke(uid, __state.Pos, __state.OutputPreview);
    }

    public struct CompletionState
    {
        public bool HadWorkItem;
        public BlockPos Pos;
        public ItemStack? OutputPreview;
    }
}