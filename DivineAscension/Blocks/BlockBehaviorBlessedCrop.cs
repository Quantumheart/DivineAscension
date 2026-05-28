using System;
using DivineAscension.Constants;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace DivineAscension.Blocks;

/// <summary>
/// Applies Aethra harvest blessings to vanilla crop drops.
/// Attached to game:crop-* via JSON patch.
/// </summary>
public class BlockBehaviorBlessedCrop : BlockBehavior
{
    public BlockBehaviorBlessedCrop(Block block) : base(block) { }

    public override ItemStack[] GetDrops(
        IWorldAccessor world,
        BlockPos pos,
        IPlayer byPlayer,
        ref float dropChanceMultiplier,
        ref EnumHandling handling)
    {
        if (world.Side != EnumAppSide.Server || byPlayer?.Entity?.Stats == null)
            return base.GetDrops(world, pos, byPlayer, ref dropChanceMultiplier, ref handling);

        var yield = byPlayer.Entity.Stats.GetBlended(VintageStoryStats.CropYield);
        if (yield > 1.0f)
            dropChanceMultiplier *= yield;

        var drops = base.GetDrops(world, pos, byPlayer, ref dropChanceMultiplier, ref handling);

        var seedChance = byPlayer.Entity.Stats.GetBlended(VintageStoryStats.SeedDropChance);
        if (drops == null || seedChance <= 0f || world.Rand.NextDouble() >= seedChance)
            return drops!;

        for (var i = 0; i < drops.Length; i++)
        {
            var path = drops[i]?.Collectible?.Code?.Path;
            if (path == null || !path.StartsWith("seeds-", StringComparison.Ordinal)) continue;

            var bonus = drops[i].Clone();
            bonus.StackSize = 1;
            Array.Resize(ref drops, drops.Length + 1);
            drops[^1] = bonus;
            break;
        }

        return drops;
    }
}
