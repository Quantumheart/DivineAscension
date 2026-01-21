using DivineAscension.Systems;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace DivineAscension.Blocks;

/// <summary>
/// BlockBehavior attached to vanilla altar blocks via JSON patches.
/// Emits events when altars are used or broken.
/// Uses Service Locator pattern since BlockBehavior constructors don't support DI.
/// </summary>
public class BlockBehaviorAltar : BlockBehavior
{
    private static AltarEventEmitter? _emitter;

    /// <summary>
    /// Constructor required by Vintage Story BlockBehavior system.
    /// </summary>
    public BlockBehaviorAltar(Block block) : base(block)
    {
        // Note: Block.Api is not available in constructor, logging must happen in method overrides
    }

    /// <summary>
    /// Sets the event emitter (Service Locator pattern).
    /// Called from DivineAscensionSystemInitializer during mod initialization.
    /// </summary>
    public static void SetEventEmitter(AltarEventEmitter emitter)
    {
        _emitter = emitter;
    }

    /// <summary>
    /// Intercepts altar block interactions (right-click).
    /// Server-side only - emits OnAltarUsed event.
    /// </summary>
    public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer,
        BlockSelection blockSel, ref EnumHandling handling)
    {
        world.Api?.Logger?.Notification($"[DivineAscension] OnBlockInteractStart called on {world.Side} side");

        if (world.Side == EnumAppSide.Server)
        {
            world.Api?.Logger?.Notification($"[DivineAscension] Raising AltarUsed event for {byPlayer.PlayerName}");
            _emitter?.RaiseAltarUsed(byPlayer, blockSel);
        }

        // Return true to indicate we handled the interaction
        // This ensures the client sends the interaction to the server
        handling = EnumHandling.PreventSubsequent;
        return true;
    }

    /// <summary>
    /// Intercepts altar block destruction.
    /// Server-side only - emits OnAltarBroken event.
    /// </summary>
    public override void OnBlockBroken(IWorldAccessor world, BlockPos pos,
        IPlayer byPlayer, ref EnumHandling handling)
    {
        if (world.Side == EnumAppSide.Server && byPlayer is IServerPlayer serverPlayer)
        {
            _emitter?.RaiseAltarBroken(serverPlayer, pos);
        }

        base.OnBlockBroken(world, pos, byPlayer, ref handling);
    }

    /// <summary>
    /// Intercepts altar block placement (Step 3 of placement lifecycle).
    /// Server-side only - emits OnAltarPlaced event.
    /// Called after placement is validated but before the block is actually set in the world.
    /// </summary>
    public override bool DoPlaceBlock(IWorldAccessor world, IPlayer byPlayer,
        BlockSelection blockSel, ItemStack byItemStack, ref EnumHandling handling)
    {
        if (world.Side == EnumAppSide.Server && byPlayer is IServerPlayer serverPlayer)
        {
            // oldBlockId is 0 (air) since this is a new placement - handler doesn't use it anyway
            _emitter?.RaiseAltarPlaced(serverPlayer, 0, blockSel, byItemStack);
        }

        return base.DoPlaceBlock(world, byPlayer, blockSel, byItemStack, ref handling);
    }
}
