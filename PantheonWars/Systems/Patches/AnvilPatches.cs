using System;
using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace PantheonWars.Systems.Patches;

[HarmonyPatch]
public static class AnvilPatches
{
    // Fires once when an anvil workpiece transitions from present -> null during a hammer interaction
    // playerUid may be null if not resolvable; output may be null if not detectable
    public static event Action<string?, BlockPos, ItemStack?>? OnAnvilRecipeCompleted;

    public struct WorkState
    {
        public bool Valid;
        public BlockPos Pos;
        public bool HadWorkItem;
        public ItemStack? OutputPreview;
    }

    // We hook into the generic CollectibleObject.OnHeldInteractStep like for molds.
    // When the held item looks like a hammer and the targeted block entity is an anvil,
    // we snapshot the state before the step and compare after the step.
    [HarmonyPatch(typeof(CollectibleObject), "OnHeldInteractStep")]
    [HarmonyPrefix]
    public static void Prefix_OnHeldInteractStep(
        float secondsUsed,
        ItemSlot slot,
        EntityAgent byEntity,
        BlockSelection blockSel,
        EntitySelection entitySel,
        out WorkState __state)
    {
        __state = default;
        if (byEntity?.Api == null || byEntity.Api.Side != EnumAppSide.Server) return;
        if (blockSel == null) return;

        // Only proceed if the held item appears to be a hammer
        var codePath = slot?.Itemstack?.Collectible?.Code?.Path?.ToLowerInvariant();
        if (string.IsNullOrEmpty(codePath) || !codePath.Contains("hammer")) return;

        var be = byEntity.World.BlockAccessor.GetBlockEntity(blockSel.Position);
        if (be is BlockEntityAnvil anvil)
        {
            ItemStack? outputPreview = anvil.SelectedRecipe?.Output?.ResolvedItemstack?.Clone();
            __state = new WorkState
            {
                Valid = true,
                Pos = blockSel.Position.Copy(),
                HadWorkItem = anvil.WorkItemStack != null,
                OutputPreview = outputPreview
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
        WorkState __state)
    {
        if (!__state.Valid) return;
        if (byEntity?.Api == null || byEntity.Api.Side != EnumAppSide.Server) return;

        var be = byEntity.World.BlockAccessor.GetBlockEntity(__state.Pos) as BlockEntityAnvil;
        if (be == null) return;

        bool hasWorkItemNow = be.WorkItemStack != null;
        if (__state.HadWorkItem && !hasWorkItemNow)
        {
            // Completed during this interaction
            string? playerUid = (byEntity as EntityPlayer)?.Player?.PlayerUID;
            OnAnvilRecipeCompleted?.Invoke(playerUid, __state.Pos, __state.OutputPreview);
        }
    }
}
