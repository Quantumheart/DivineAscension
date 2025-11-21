using System.Diagnostics.CodeAnalysis;
using PantheonWars.Systems.Interfaces;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace PantheonWars.Systems;

/// <summary>
///     Tracks player activities and awards prestige to their religion.
///     MVP 1: Basic tracking for mining ore and harvesting crops.
/// </summary>
[ExcludeFromCodeCoverage]
public class PrestigeTracker
{
    private readonly ICoreServerAPI _sapi;
    private readonly IReligionManager _religionManager;
    private readonly IReligionPrestigeManager _prestigeManager;

    // Block code prefixes for ore detection
    private static readonly string[] OreBlockPrefixes = { "ore-", "looseores-" };

    // Block code prefixes for crop detection
    private static readonly string[] CropBlockPrefixes = { "crop-", "berrybush-", "mushroom-" };

    public PrestigeTracker(
        ICoreServerAPI sapi,
        IReligionManager religionManager,
        IReligionPrestigeManager prestigeManager)
    {
        _sapi = sapi;
        _religionManager = religionManager;
        _prestigeManager = prestigeManager;
    }

    /// <summary>
    ///     Initializes the prestige tracker and hooks into game events
    /// </summary>
    public void Initialize()
    {
        _sapi.Logger.Notification("[PantheonWars] Initializing Prestige Tracker...");

        // Hook into block break events
        _sapi.Event.DidBreakBlock += OnBlockBroken;

        _sapi.Logger.Notification("[PantheonWars] Prestige Tracker initialized - tracking mining and harvesting");
    }

    /// <summary>
    ///     Called when a player breaks a block
    /// </summary>
    private void OnBlockBroken(IServerPlayer byPlayer, int oldBlockId, BlockSelection blockSel)
    {
        if (byPlayer == null) return;

        // Check if player is in a religion
        var religion = _religionManager.GetPlayerReligion(byPlayer.PlayerUID);
        if (religion == null) return;

        // Get the block that was broken
        var block = _sapi.World.GetBlock(oldBlockId);
        if (block == null) return;

        var blockCode = block.Code?.ToString() ?? "";

        // Check for ore blocks
        if (IsOreBlock(blockCode))
        {
            _prestigeManager.AddPrestige(religion.ReligionUID, 1, $"Mining ore: {blockCode}");
            return;
        }

        // Check for crop/plant blocks
        if (IsCropBlock(blockCode))
        {
            _prestigeManager.AddPrestige(religion.ReligionUID, 1, $"Harvesting: {blockCode}");
        }
    }

    /// <summary>
    ///     Checks if a block code represents an ore block
    /// </summary>
    private static bool IsOreBlock(string blockCode)
    {
        foreach (var prefix in OreBlockPrefixes)
        {
            if (blockCode.StartsWith(prefix, System.StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    /// <summary>
    ///     Checks if a block code represents a harvestable crop
    /// </summary>
    private static bool IsCropBlock(string blockCode)
    {
        foreach (var prefix in CropBlockPrefixes)
        {
            if (blockCode.StartsWith(prefix, System.StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    /// <summary>
    ///     Manually awards prestige (for admin/testing purposes)
    /// </summary>
    public void AwardPrestige(string playerUID, int amount, string reason)
    {
        var religion = _religionManager.GetPlayerReligion(playerUID);
        if (religion == null)
        {
            _sapi.Logger.Warning($"[PantheonWars] Cannot award prestige - player {playerUID} not in a religion");
            return;
        }

        _prestigeManager.AddPrestige(religion.ReligionUID, amount, reason);
    }
}
