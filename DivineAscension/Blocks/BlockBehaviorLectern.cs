using DivineAscension.Systems.Lectern;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace DivineAscension.Blocks;

/// <summary>
/// BlockBehavior attached to vanilla clutter blocks via JSON patch.
/// Emits an interaction event only when the block is a lectern variant,
/// so the rest of the clutter family is unaffected.
/// </summary>
public class BlockBehaviorLectern : BlockBehavior
{
    private static LecternEventEmitter? _emitter;

    public BlockBehaviorLectern(Block block) : base(block)
    {
    }

    public static void SetEventEmitter(LecternEventEmitter emitter)
    {
        _emitter = emitter;
    }

    public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer,
        BlockSelection blockSel, ref EnumHandling handling)
    {
        if (world.Side != EnumAppSide.Server)
        {
            // Returning true client-side ensures VS forwards the interaction to the server.
            handling = EnumHandling.PreventSubsequent;
            return true;
        }

        if (byPlayer is not IServerPlayer serverPlayer)
            return false;

        if (!IsLecternVariant(world, blockSel))
            return false;

        _emitter?.RaiseLecternUsed(serverPlayer, blockSel);

        handling = EnumHandling.PreventSubsequent;
        return true;
    }

    private static bool IsLecternVariant(IWorldAccessor world, BlockSelection blockSel)
    {
        var block = world.BlockAccessor.GetBlock(blockSel.Position);
        var typeVariant = block?.Variant?["type"];
        return typeVariant != null && typeVariant.Contains("lectern");
    }
}
