using System;
using System.Collections.Generic;
using DivineAscension.Systems.Toolsmith;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace DivineAscension.Blocks;

/// <summary>
///     BlockBehavior attached to Toolsmith's workbench via JSON patch.
///     Detects tool assembly (hammer strike on crafting slots), disassembly (shift+interact on vise),
///     and reforging (hammer strike on reforge staging slot).
///     Mirrors the BlockBehaviorAltar / BlockBehaviorOre pattern.
/// </summary>
public class BlockBehaviorDivineWorkbench : BlockBehavior
{
    private static ToolsmithEventEmitter? _emitter;

    /// <summary>
    ///     Tracks players who have started a hammer-strike crafting action, keyed by player UID.
    ///     Used to detect when a craft completes (the tool appears on the bench).
    /// </summary>
    private static readonly HashSet<string> CraftingInProgress = new();

    /// <summary>
    ///     Tracks players who started a disassembly interaction (shift on vise).
    /// </summary>
    private static readonly HashSet<string> DisassemblyInProgress = new();

    /// <summary>
    ///     Tracks players who started a reforge hammer strike.
    /// </summary>
    private static readonly HashSet<string> ReforgeInProgress = new();

    // WorkbenchSlots enum values from Toolsmith's BlockWorkbench.cs
    private const int ViseSlot = 6;
    private const int ReforgeStagingSlot = 7;
    private const int CraftingSlot1 = 1;
    private const int CraftingSlot5 = 5;

    public BlockBehaviorDivineWorkbench(Block block) : base(block) { }

    public static void SetEventEmitter(ToolsmithEventEmitter emitter)
    {
        _emitter = emitter;
    }

    public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel,
        ref EnumHandling handling)
    {
        if (world.Side != EnumAppSide.Server || _emitter == null)
            return true;

        var player = byPlayer as IServerPlayer;
        if (player == null)
            return true;

        var slotIndex = blockSel.SelectionBoxIndex;
        var hasHammer = byPlayer.InventoryManager.ActiveHotbarSlot?.Itemstack?.Collectible?.Tool ==
                        EnumTool.Hammer;

        // Crafting: hammer on a crafting slot
        if (!byPlayer.Entity.Controls.ShiftKey && hasHammer == true &&
            slotIndex >= CraftingSlot1 && slotIndex <= CraftingSlot5)
        {
            CraftingInProgress.Add(player.PlayerUID);
        }

        // Disassembly: shift on vise slot
        if (byPlayer.Entity.Controls.ShiftKey && slotIndex == ViseSlot)
        {
            DisassemblyInProgress.Add(player.PlayerUID);
        }

        // Reforging: hammer on reforge staging slot
        if (!byPlayer.Entity.Controls.ShiftKey && hasHammer == true && slotIndex == ReforgeStagingSlot)
        {
            ReforgeInProgress.Add(player.PlayerUID);
        }

        return true;
    }

    public override bool OnBlockInteractStep(float secondsUsed, IWorldAccessor world, IPlayer byPlayer,
        BlockSelection blockSel, ref EnumHandling handling)
    {
        if (world.Side != EnumAppSide.Server || _emitter == null)
            return true;

        var player = byPlayer as IServerPlayer;
        if (player == null)
            return true;

        // Disassembly fires after holding long enough (Toolsmith threshold is 4.5s)
        if (DisassemblyInProgress.Contains(player.PlayerUID) &&
            byPlayer.Entity.Controls.ShiftKey && secondsUsed > 4.5f)
        {
            DisassemblyInProgress.Remove(player.PlayerUID);

            var hotbarSlot = byPlayer.InventoryManager.ActiveHotbarSlot;
            if (hotbarSlot?.Itemstack != null)
            {
                _emitter.RaiseToolDisassembled(player, blockSel.Position, hotbarSlot.Itemstack);
            }
        }

        return true;
    }

    public override void OnBlockInteractStop(float secondsUsed, IWorldAccessor world, IPlayer byPlayer,
        BlockSelection blockSel, ref EnumHandling handling)
    {
        if (world.Side != EnumAppSide.Server || _emitter == null)
            return;

        var player = byPlayer as IServerPlayer;
        if (player == null)
            return;

        // Crafting completion: check if a craft was in progress and items appeared on the bench.
        // Toolsmith's AttemptToCraft drops the crafted tool on the bench when successful.
        // We detect this by checking if the crafting slots are now empty (parts were consumed).
        if (CraftingInProgress.Remove(player.PlayerUID))
        {
            // Check the block entity for the workbench to see if craft completed.
            // The crafted tool is dropped as an item entity on the bench, not in a slot,
            // so we award favor based on the fact that a hammer-craft action completed.
            var hotbarSlot = byPlayer.InventoryManager.ActiveHotbarSlot;
            if (hotbarSlot?.Itemstack?.Collectible?.Tool == EnumTool.Hammer)
            {
                // Award favor for the assembly — we pass the hammer as context,
                // the tracker just needs to know a craft happened at this position.
                _emitter.RaiseToolAssembled(player, blockSel.Position,
                    hotbarSlot.Itemstack);
            }
        }

        // Reforge completion: if reforge was in progress, award favor.
        // Toolsmith's InitiateReforgeAttempt spawns a work item and clears the reforge slot.
        if (ReforgeInProgress.Remove(player.PlayerUID))
        {
            var hotbarSlot = byPlayer.InventoryManager.ActiveHotbarSlot;
            if (hotbarSlot?.Itemstack?.Collectible?.Tool == EnumTool.Hammer)
            {
                _emitter.RaiseToolReforged(player, blockSel.Position,
                    hotbarSlot.Itemstack);
            }
        }

        DisassemblyInProgress.Remove(player.PlayerUID);
    }

    public static void ClearSubscribers()
    {
        _emitter = null;
        CraftingInProgress.Clear();
        DisassemblyInProgress.Clear();
        ReforgeInProgress.Clear();
    }
}