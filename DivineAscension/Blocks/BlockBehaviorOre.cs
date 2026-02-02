using System;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace DivineAscension.Blocks;

public class BlockBehaviorOre(Block block) : BlockBehavior(block)
{
    /// <summary>
    /// Raised when a player breaks an ore block.
    /// Parameters: world, position, player, block, handling
    /// Static event shared across all ore block instances.
    /// </summary>
    public static event Action<IWorldAccessor, BlockPos, IPlayer?, Block, EnumHandling>? OnOreBlockBroken;

    public override void OnBlockBroken(
        IWorldAccessor world,
        BlockPos pos,
        IPlayer byPlayer,
        ref EnumHandling handling)
    {
        // Pass the block reference so trackers can access ore type/grade
        OnOreBlockBroken?.Invoke(world, pos, byPlayer, block, handling);
    }

    /// <summary>
    /// Clears all subscribers. Called during mod disposal.
    /// </summary>
    public static void ClearSubscribers()
    {
        OnOreBlockBroken = null;
    }

    /// <summary>
    /// Triggers the OnOreBlockBroken event. Used for testing.
    /// </summary>
    internal static void TriggerOreBlockBroken(IWorldAccessor world, BlockPos pos, IPlayer? player, Block block, EnumHandling handling)
    {
        OnOreBlockBroken?.Invoke(world, pos, player, block, handling);
    }
}
