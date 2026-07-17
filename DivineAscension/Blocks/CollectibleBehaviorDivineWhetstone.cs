using System;
using System.Collections.Generic;
using DivineAscension.Systems.Toolsmith;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace DivineAscension.Blocks;

/// <summary>
///     CollectibleBehavior attached to Toolsmith's whetstone item via JSON patch.
///     Detects whetstone sharpening (offhand whetstone + mainhand tool, right-click)
///     and emits an event through the ToolsmithEventEmitter service locator.
///     Mirrors the BlockBehaviorOre / BlockBehaviorAltar pattern.
/// </summary>
public class CollectibleBehaviorDivineWhetstone : CollectibleBehavior
{
    private static ToolsmithEventEmitter? _emitter;

    /// <summary>
    ///     Tracks players who have already had a sharpening event fired this whetstone session.
    /// </summary>
    private static readonly HashSet<string> SharpeningFired = new();

    public CollectibleBehaviorDivineWhetstone(CollectibleObject collObj) : base(collObj) { }

    public static void SetEventEmitter(ToolsmithEventEmitter emitter)
    {
        _emitter = emitter;
    }

    public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel,
        EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handHandling,
        ref EnumHandling handling)
    {
        if (byEntity?.Api?.Side == EnumAppSide.Server && firstEvent)
        {
            var player = (byEntity as EntityPlayer)?.Player as IServerPlayer;
            if (player != null)
            {
                SharpeningFired.Remove(player.PlayerUID);
            }
        }
    }

    public override bool OnHeldInteractStep(float secondsUsed, ItemSlot slot, EntityAgent byEntity,
        BlockSelection blockSel, EntitySelection entitySel, ref EnumHandling handling)
    {
        if (byEntity?.Api?.Side != EnumAppSide.Server || _emitter == null)
            return true;

        var player = (byEntity as EntityPlayer)?.Player as IServerPlayer;
        if (player == null)
            return true;

        // The whetstone is in the offhand slot; the tool being sharpened is in the main hand.
        var mainHandSlot = byEntity.RightHandItemSlot;
        if (mainHandSlot?.Itemstack == null)
            return true;

        // Fire once per sharpening session
        if (secondsUsed > 0.1f && !SharpeningFired.Contains(player.PlayerUID))
        {
            SharpeningFired.Add(player.PlayerUID);
            _emitter.RaiseToolSharpened(player,
                new BlockPos((int)byEntity.Pos.X, (int)byEntity.Pos.Y, (int)byEntity.Pos.Z),
                mainHandSlot.Itemstack);
        }

        return true;
    }

    public override void OnHeldInteractStop(float secondsUsed, ItemSlot slot, EntityAgent byEntity,
        BlockSelection blockSel, EntitySelection entitySel, ref EnumHandling handling)
    {
        if (byEntity?.Api?.Side == EnumAppSide.Server)
        {
            var player = (byEntity as EntityPlayer)?.Player as IServerPlayer;
            if (player != null)
            {
                SharpeningFired.Remove(player.PlayerUID);
            }
        }
    }

    public static void ClearSubscribers()
    {
        _emitter = null;
        SharpeningFired.Clear();
    }
}