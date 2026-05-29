using System;
using DivineAscension.Systems.Altar;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace DivineAscension.Blocks;

/// <summary>
///     BlockBehavior on the Caravan Shrine block. Placement/destruction still flow through
///     <see cref="AltarEventEmitter"/> so the shrine sites a tier-1 holy site like an altar.
///     Right-clicking the shrine opens the player-to-player trade table (#433) rather than
///     praying — prayer is performed at the regular altar. The trade open is client-initiated:
///     the behavior runs client-side and asks the trade dialog to send an
///     <c>OpenTradeRequest</c>; the server is authoritative for seating and state.
/// </summary>
public class BlockBehaviorCaravanShrine : BlockBehavior
{
    private static AltarEventEmitter? _emitter;

    /// <summary>
    ///     Client-side hook set by the trade dialog (no DI in behaviors). Invoked with the
    ///     shrine position when a player right-clicks the block on the client.
    /// </summary>
    private static Action<BlockPos>? _onTradeInteractClient;

    public BlockBehaviorCaravanShrine(Block block) : base(block)
    {
    }

    public static void SetEventEmitter(AltarEventEmitter emitter)
    {
        _emitter = emitter;
    }

    public static void SetTradeInteractClientHandler(Action<BlockPos>? handler)
    {
        _onTradeInteractClient = handler;
    }

    public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer,
        BlockSelection blockSel, ref EnumHandling handling)
    {
        // Trade is opened from the client; the server seats the player when the
        // OpenTradeRequest packet arrives. No prayer event is raised for the shrine.
        if (world.Side == EnumAppSide.Client && blockSel?.Position != null)
        {
            _onTradeInteractClient?.Invoke(blockSel.Position);
        }

        handling = EnumHandling.PreventSubsequent;
        return true;
    }

    /// <summary>
    ///     End the interaction immediately. Without this VS keeps the player in a use-block
    ///     hold loop until right-click is released — and with the ImGui trade dialog open the
    ///     release goes to ImGui, leaving the camera pinned. Mirrors BlockBehaviorLectern.
    /// </summary>
    public override bool OnBlockInteractStep(float secondsUsed, IWorldAccessor world, IPlayer byPlayer,
        BlockSelection blockSelection, ref EnumHandling handled)
    {
        handled = EnumHandling.PreventSubsequent;
        return false;
    }

    public override void OnBlockBroken(IWorldAccessor world, BlockPos pos,
        IPlayer byPlayer, float dropQuantityMultiplier, ref EnumHandling handling)
    {
        if (world.Side == EnumAppSide.Server && byPlayer is IServerPlayer serverPlayer)
        {
            _emitter?.RaiseAltarBroken(serverPlayer, pos);
        }

        base.OnBlockBroken(world, pos, byPlayer, dropQuantityMultiplier, ref handling);
    }

    public override bool DoPlaceBlock(IWorldAccessor world, IPlayer byPlayer,
        BlockSelection blockSel, ItemStack byItemStack, ref EnumHandling handling)
    {
        if (world.Side == EnumAppSide.Server && byPlayer is IServerPlayer serverPlayer)
        {
            _emitter?.RaiseAltarPlaced(serverPlayer, 0, blockSel, byItemStack);
        }

        return base.DoPlaceBlock(world, byPlayer, blockSel, byItemStack, ref handling);
    }
}
