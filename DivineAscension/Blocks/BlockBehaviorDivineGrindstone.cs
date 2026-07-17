using System;
using System.Collections.Generic;
using DivineAscension.Systems.Toolsmith;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace DivineAscension.Blocks;

/// <summary>
///     BlockBehavior attached to Toolsmith's grindstone via JSON patch.
///     Detects tool sharpening (right-click, no shift) and disassembly (shift+right-click hold)
///     and emits events through the ToolsmithEventEmitter service locator.
///     Mirrors the BlockBehaviorOre / BlockBehaviorAltar pattern.
/// </summary>
public class BlockBehaviorDivineGrindstone : BlockBehavior
{
    private static ToolsmithEventEmitter? _emitter;

    /// <summary>
    ///     Tracks which players have already had a sharpening event fired this interaction session,
    ///     keyed by player UID. Prevents firing every tick during a hold-to-sharpen interaction.
    /// </summary>
    private static readonly HashSet<string> SharpeningFired = new();

    public BlockBehaviorDivineGrindstone(Block block) : base(block) { }

    public static void SetEventEmitter(ToolsmithEventEmitter emitter)
    {
        _emitter = emitter;
    }

    public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel,
        ref EnumHandling handling)
    {
        // Reset sharpening tracking when a new interaction begins
        if (world.Side == EnumAppSide.Server && !byPlayer.Entity.Controls.ShiftKey)
        {
            SharpeningFired.Remove(byPlayer.PlayerUID);
        }

        return true;
    }

    public override bool OnBlockInteractStep(float secondsUsed, IWorldAccessor world, IPlayer byPlayer,
        BlockSelection blockSel, ref EnumHandling handling)
    {
        if (world.Side != EnumAppSide.Server || _emitter == null)
            return true;

        var hotbarSlot = byPlayer.InventoryManager.ActiveHotbarSlot;
        if (hotbarSlot?.Itemstack == null)
            return true;

        var player = byPlayer as IServerPlayer;
        if (player == null)
            return true;

        // Disassembly: shift+interact, fires once after holding long enough
        // (Toolsmith disassembles at secondsUsed > 4.5 in BlockGrindstone.OnBlockInteractStep)
        if (byPlayer.Entity.Controls.ShiftKey && secondsUsed > 4.5f)
        {
            if (SharpeningFired.Contains(player.PlayerUID + "-disasm"))
                return true;
            SharpeningFired.Add(player.PlayerUID + "-disasm");

            _emitter.RaiseToolDisassembled(player, blockSel.Position, hotbarSlot.Itemstack);
            return true;
        }

        // Sharpening: no shift, fire once per interaction session
        if (!byPlayer.Entity.Controls.ShiftKey && secondsUsed > 0.1f)
        {
            if (SharpeningFired.Contains(player.PlayerUID))
                return true;
            SharpeningFired.Add(player.PlayerUID);

            _emitter.RaiseToolSharpened(player, blockSel.Position, hotbarSlot.Itemstack);
        }

        return true;
    }

    public override bool OnBlockInteractCancel(float secondsUsed, IWorldAccessor world, IPlayer byPlayer,
        BlockSelection blockSel, ref EnumHandling handling)
    {
        if (world.Side == EnumAppSide.Server)
        {
            SharpeningFired.Remove(byPlayer.PlayerUID);
            SharpeningFired.Remove(byPlayer.PlayerUID + "-disasm");
        }

        return true;
    }

    public override void OnBlockInteractStop(float secondsUsed, IWorldAccessor world, IPlayer byPlayer,
        BlockSelection blockSel, ref EnumHandling handling)
    {
        if (world.Side == EnumAppSide.Server)
        {
            SharpeningFired.Remove(byPlayer.PlayerUID);
            SharpeningFired.Remove(byPlayer.PlayerUID + "-disasm");
        }
    }

    public static void ClearSubscribers()
    {
        _emitter = null;
        SharpeningFired.Clear();
    }
}