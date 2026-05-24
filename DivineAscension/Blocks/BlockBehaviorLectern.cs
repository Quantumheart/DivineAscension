using DivineAscension.Systems.Lectern;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace DivineAscension.Blocks;

/// <summary>
/// BlockBehavior attached to the divineascension:lectern block.
/// Forwards right-click interactions to <see cref="LecternEventEmitter"/>
/// so the server can validate and open the menu for the player.
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
        if (world.Side == EnumAppSide.Server && byPlayer is IServerPlayer serverPlayer)
        {
            _emitter?.RaiseLecternUsed(serverPlayer, blockSel);
        }

        handling = EnumHandling.PreventSubsequent;
        return true;
    }

    /// <summary>
    /// End the interaction immediately. Without this VS keeps the player in a
    /// use-block hold loop until the right-click is released to it — and with
    /// the menu open the release goes to ImGui, leaving the camera pinned.
    /// </summary>
    public override bool OnBlockInteractStep(float secondsUsed, IWorldAccessor world, IPlayer byPlayer,
        BlockSelection blockSelection, ref EnumHandling handled)
    {
        handled = EnumHandling.PreventSubsequent;
        return false;
    }
}
