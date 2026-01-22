using System;
using DivineAscension.Constants;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace DivineAscension.Blocks;

public class BlockBehaviorStone : BlockBehavior
{
    /// <summary>                                                                                         
    /// Raised when a player breaks a stone block.                                                        
    /// Static event shared across all stone block instances.                                             
    /// </summary>                                                                                        
    public static event Action<IWorldAccessor,
        BlockPos,
        IPlayer,
        EnumHandling>? OnStoneBlockBroken;
    
    /// <summary>
    /// Constructor required by Vintage Story BlockBehavior system.
    /// </summary>
    public BlockBehaviorStone(Block block) : base(block)
    {
        // Note: Block.Api is not available in constructor, logging must happen in method overrides
        // Constructor called - behavior is being instantiated
    }

    public override void OnBlockBroken(IWorldAccessor world,
        BlockPos pos,
        IPlayer byPlayer,
        ref EnumHandling handling)
    {
        OnStoneBlockBroken?.Invoke(world, pos, byPlayer, handling);
    }
    
    
    /// <summary>
    /// Intercepts GetDrops
    /// </summary>
    public override ItemStack[] GetDrops(
        IWorldAccessor world,
        BlockPos pos,
        IPlayer byPlayer,
        ref float dropChanceMultiplier,
        ref EnumHandling handling)
    {
        if (world.Side == EnumAppSide.Server)
        {
            // Apply stone yield bonus to drop multiplier
            var stoneYieldBonus = byPlayer.Entity.Stats.GetBlended(VintageStoryStats.StoneYield);
            if (stoneYieldBonus > 1.0)
            {
                dropChanceMultiplier *= stoneYieldBonus;

                world.Logger.Debug(
                    $"[DivineAscension] StoneYield bonus applied: {stoneYieldBonus:F2}x for {block.Code.Path} (player: {byPlayer.PlayerName})");
            }
        }

        return base.GetDrops(world, pos, byPlayer, ref dropChanceMultiplier, ref handling);
    }
    
    /// <summary>                                                                                         
    /// Clears all subscribers. Called during mod disposal.                                               
    /// </summary>                                                                                        
    public static void ClearSubscribers()                                                                 
    {                                                                                                     
        OnStoneBlockBroken = null;                                                                           
    }
}