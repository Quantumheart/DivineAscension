using System.Linq;
using DivineAscension.Blocks;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace DivineAscension.Systems.Patches;

/// <summary>
/// Patches vanilla altar blocks to add BlockBehaviorAltar.
/// Uses code-based patching instead of JSON patches for reliability.
/// </summary>
public static class AltarBlockBehaviorPatch
{
    /// <summary>
    /// Applies BlockBehaviorAltar to all altar blocks during AssetsFinalize stage.
    /// CRITICAL: Must add to both CollectibleBehaviors AND BlockBehaviors arrays.
    /// Runs on BOTH client and server sides (behaviors must exist on both).
    /// </summary>
    public static void PatchAltarBlocks(ICoreAPI api)
    {
        int patchedCount = 0;

        // Iterate through all registered blocks
        foreach (var block in api.World.Blocks)
        {
            if (block == null || block.Code == null)
                continue;

            // Match altar blocks by code pattern: game:altar-north, game:altar-south, etc.
            if (block.Code.Path.StartsWith("altar-"))
            {
                // Create BlockBehaviorAltar instance
                var behavior = new BlockBehaviorAltar(block);

                // CRITICAL: Add to BOTH arrays (blocks require both, items only need CollectibleBehaviors)
                // CollectibleBehaviors
                var collectibleBehaviors = block.CollectibleBehaviors?.ToList() ?? new System.Collections.Generic.List<CollectibleBehavior>();
                collectibleBehaviors.Add(behavior);
                block.CollectibleBehaviors = collectibleBehaviors.ToArray();

                // BlockBehaviors
                var blockBehaviors = block.BlockBehaviors?.ToList() ?? new System.Collections.Generic.List<BlockBehavior>();
                blockBehaviors.Add(behavior);
                block.BlockBehaviors = blockBehaviors.ToArray();

                patchedCount++;
                api.Logger.Notification($"[DivineAscension] Patched block behavior onto: {block.Code}");
            }
        }

        if (patchedCount > 0)
        {
            api.Logger.Notification($"[DivineAscension] Successfully patched {patchedCount} altar block(s) with BlockBehaviorAltar");
        }
        else
        {
            api.Logger.Warning("[DivineAscension] No altar blocks found to patch - this may indicate a problem");
        }
    }
}
