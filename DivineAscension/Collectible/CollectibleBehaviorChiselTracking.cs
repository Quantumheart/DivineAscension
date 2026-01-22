using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace DivineAscension.Collectible;

/// <summary>
/// CollectibleBehavior for tracking chiseling activity and awarding favor to Stone domain followers.
/// Attaches to chisel items via JSON patch to monitor voxel changes during carving/adding operations.
/// </summary>
public class CollectibleBehaviorChiselTracking : CollectibleBehavior
{
    /// <summary>
    /// Raised when a player modifies voxels with a chisel.
    /// Static event shared across all chisel instances.
    /// Args: IPlayer, BlockPos, int voxelsDelta
    /// </summary>
    public static event Action<IPlayer, BlockPos, int>? OnVoxelsChanged;

    /// <summary>
    /// Tracks voxel count at the start of each interaction (before chiseling).
    /// Key: BlockPos, Value: voxel count before interaction
    /// </summary>
    private static readonly Dictionary<BlockPos, int> _lastVoxelCounts = new();

    public CollectibleBehaviorChiselTracking(CollectibleObject collObj) : base(collObj)
    {
    }

    public override void OnHeldAttackStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel,
        EntitySelection entitySel, ref EnumHandHandling bhHandHandling, ref EnumHandling handling)
    {
        // Store voxel count before chisel attack (breaking/carving)
        if (blockSel?.Position != null && byEntity.World.Side == EnumAppSide.Server)
        {
            if (byEntity.World.BlockAccessor.GetBlockEntity(blockSel.Position) is BlockEntityChisel chiselEntity)
            {
                int currentVoxels = GetTotalVoxelCount(chiselEntity);
                _lastVoxelCounts[blockSel.Position] = currentVoxels;
            }
        }
    }

    public override void OnHeldAttackStop(float secondsUsed, ItemSlot slot, EntityAgent byEntity,
        BlockSelection blockSel, EntitySelection entitySel, ref EnumHandling handling)
    {
        // Detect voxel changes after chisel attack completes
        if (blockSel?.Position != null && byEntity.World.Side == EnumAppSide.Server)
        {
            CheckAndEmitVoxelChange(byEntity, blockSel.Position);
        }
    }

    public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel,
        EntitySelection entitySel, bool firstEvent, ref EnumHandHandling bhHandHandling, ref EnumHandling handling)
    {
        // Store voxel count before chisel interact (adding voxels)
        if (blockSel?.Position != null && byEntity.World.Side == EnumAppSide.Server)
        {
            if (byEntity.World.BlockAccessor.GetBlockEntity(blockSel.Position) is BlockEntityChisel chiselEntity)
            {
                int currentVoxels = GetTotalVoxelCount(chiselEntity);
                _lastVoxelCounts[blockSel.Position] = currentVoxels;
            }
        }
    }

    public override void OnHeldInteractStop(float secondsUsed, ItemSlot slot, EntityAgent byEntity,
        BlockSelection blockSel, EntitySelection entitySel, ref EnumHandling handling)
    {
        // Detect voxel changes after chisel interact completes
        if (blockSel?.Position != null && byEntity.World.Side == EnumAppSide.Server)
        {
            CheckAndEmitVoxelChange(byEntity, blockSel.Position);
        }
    }

    /// <summary>
    /// Checks for voxel changes at a block position and emits the OnVoxelsChanged event if changes detected.
    /// Compares current voxel count with previously stored count and calculates the delta.
    /// </summary>
    private void CheckAndEmitVoxelChange(EntityAgent byEntity, BlockPos pos)
    {
        if (byEntity.World.BlockAccessor.GetBlockEntity(pos) is BlockEntityChisel chiselEntity)
        {
            int currentVoxels = GetTotalVoxelCount(chiselEntity);
            int previousVoxels = _lastVoxelCounts.GetValueOrDefault(pos, currentVoxels);
            int voxelsDelta = Math.Abs(currentVoxels - previousVoxels);

            if (voxelsDelta > 0)
            {
                var player = (byEntity as EntityPlayer)?.Player;
                if (player != null)
                {
                    OnVoxelsChanged?.Invoke(player, pos, voxelsDelta);
                }
            }

            // Update tracking for next interaction
            _lastVoxelCounts[pos] = currentVoxels;
        }
    }

    /// <summary>
    /// Gets total voxel count from BlockEntityChisel.
    /// VoxelCuboids is a List&lt;uint&gt; where each uint encodes a cuboid (position + size + material).
    /// </summary>
    private int GetTotalVoxelCount(BlockEntityChisel chiselEntity)
    {
        int totalVoxels = 0;
        var tmpCuboid = new CuboidWithMaterial();

        // Directly access the public VoxelCuboids field
        foreach (uint cuboidEncoded in chiselEntity.VoxelCuboids)
        {
            BlockEntityMicroBlock.FromUint(cuboidEncoded, tmpCuboid);
            totalVoxels += tmpCuboid.SizeXYZ;
        }

        return totalVoxels;
    }

    /// <summary>
    /// Clears all subscribers and tracking data. Called during mod disposal.
    /// </summary>
    public static void ClearSubscribers()
    {
        OnVoxelsChanged = null;
        _lastVoxelCounts.Clear();
    }
}
