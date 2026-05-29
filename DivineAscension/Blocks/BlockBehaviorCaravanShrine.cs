using DivineAscension.Systems.Altar;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace DivineAscension.Blocks;

/// <summary>
///     BlockBehavior on the Caravan Shrine block. Reuses <see cref="AltarEventEmitter"/>
///     so the shrine emits prayer/use events that flow through the standard altar pipeline
///     (acts as a tier-1 altar — see <c>HolySiteValidationStep</c>).
///     The standard altar placement/destruction handlers filter on code path and ignore the
///     shrine; dedicated shrine handlers subscribe to the same emitter for shrine-only rules.
/// </summary>
public class BlockBehaviorCaravanShrine : BlockBehavior
{
    private static AltarEventEmitter? _emitter;

    public BlockBehaviorCaravanShrine(Block block) : base(block)
    {
    }

    public static void SetEventEmitter(AltarEventEmitter emitter)
    {
        _emitter = emitter;
    }

    public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer,
        BlockSelection blockSel, ref EnumHandling handling)
    {
        if (world.Side == EnumAppSide.Server)
        {
            _emitter?.RaiseAltarUsed(byPlayer, blockSel);
        }

        handling = EnumHandling.PreventSubsequent;
        return true;
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
