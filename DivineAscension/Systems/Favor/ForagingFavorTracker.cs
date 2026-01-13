using System;
using System.Collections.Generic;
using DivineAscension.Models.Enum;
using DivineAscension.Systems.Interfaces;
using DivineAscension.Systems.Patches;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace DivineAscension.Systems.Favor;

public class ForagingFavorTracker(
    IPlayerProgressionDataManager playerProgressionDataManager,
    ICoreServerAPI sapi,
    FavorSystem favorSystem) : IFavorTracker, IDisposable
{
    private readonly FavorSystem _favorSystem = favorSystem ?? throw new ArgumentNullException(nameof(favorSystem));

    private readonly IPlayerProgressionDataManager _playerProgressionDataManager =
        playerProgressionDataManager ?? throw new ArgumentNullException(nameof(playerProgressionDataManager));

    private readonly ICoreServerAPI _sapi = sapi ?? throw new ArgumentNullException(nameof(sapi));

    private readonly HashSet<string> _wildFollowers = new();

    public void Dispose()
    {
        _sapi.Event.BreakBlock -= OnBlockBroken;
        ForagingPatches.Picked -= OnBlockUsed;
        ScythePatches.OnScytheHarvest -= OnScytheHarvest;
        MushroomPatches.OnMushroomHarvested -= OnMushroomHarvested;
        FlowerPatches.OnFlowerHarvested -= OnFlowerHarvested;
        _playerProgressionDataManager.OnPlayerDataChanged -= OnPlayerDataChanged;
        _playerProgressionDataManager.OnPlayerLeavesReligion -= OnPlayerLeavesProgression;
        _wildFollowers.Clear();
    }

    public DeityDomain DeityDomain { get; } = DeityDomain.Wild;

    public void Initialize()
    {
        _sapi.Event.BreakBlock += OnBlockBroken;
        ForagingPatches.Picked += OnBlockUsed;
        ScythePatches.OnScytheHarvest += OnScytheHarvest;
        MushroomPatches.OnMushroomHarvested += OnMushroomHarvested;
        FlowerPatches.OnFlowerHarvested += OnFlowerHarvested;

        // Cache followers
        RefreshFollowerCache();

        _playerProgressionDataManager.OnPlayerDataChanged += OnPlayerDataChanged;
        _playerProgressionDataManager.OnPlayerLeavesReligion += OnPlayerLeavesProgression;
    }

    private void RefreshFollowerCache()
    {
        var onlinePlayers = _sapi?.World?.AllOnlinePlayers;
        if (onlinePlayers == null) return;

        foreach (var player in onlinePlayers) UpdateFollower(player.PlayerUID);
    }

    private void OnPlayerDataChanged(string playerUID)
    {
        UpdateFollower(playerUID);
    }

    private void UpdateFollower(string playerUID)
    {
        var deityType = _playerProgressionDataManager.GetPlayerDeityType(playerUID);
        if (deityType == DeityDomain)
            _wildFollowers.Add(playerUID);
        else
            _wildFollowers.Remove(playerUID);
    }

    private void OnPlayerLeavesProgression(IServerPlayer player, string religionUID)
    {
        _wildFollowers.Remove(player.PlayerUID);
    }

    private void OnBlockBroken(IServerPlayer player, BlockSelection blockSel, ref float dropQuantityMultiplier,
        ref EnumHandling handling)
    {
        if (!_wildFollowers.Contains(player.PlayerUID)) return;

        var block = _sapi.World.BlockAccessor.GetBlock(blockSel.Position);
        if (IsForageBlock(block))
            // Award 0.5 favor per forage (breaking mushrooms, flowers, etc.)
            _favorSystem.AwardFavorForAction(player, "foraging " + GetForageName(block), 0.5f);
    }

    /// <summary>
    ///     Handles scythe/shears harvesting to detect flower cutting.
    ///     Only awards favor for flowers, not grass.
    /// </summary>
    private void OnScytheHarvest(IServerPlayer player, Block block)
    {
        if (!_wildFollowers.Contains(player.PlayerUID)) return;
        if (block?.Code == null) return;

        // Only award for flowers (not grass, mushrooms, or seaweed)
        // Use FirstCodePart() for reliable block type detection
        if (block.FirstCodePart() != "flower") return;

        _favorSystem.AwardFavorForAction(player, "foraging flowers", 0.5f);
    }

    /// <summary>
    ///     Handles mushroom harvesting via dedicated patch.
    /// </summary>
    private void OnMushroomHarvested(IServerPlayer player, Block block, string? mushroomType)
    {
        if (!_wildFollowers.Contains(player.PlayerUID)) return;

        _favorSystem.AwardFavorForAction(player, "foraging", 0.5f);
    }

    /// <summary>
    ///     Handles flower harvesting via dedicated patch.
    /// </summary>
    private void OnFlowerHarvested(IServerPlayer player, Block block, string? flowerType)
    {
        if (!_wildFollowers.Contains(player.PlayerUID)) return;

        _favorSystem.AwardFavorForAction(player, "foraging", 0.5f);
    }

    /// <summary>
    ///     Handles block usage (right-click) to detect berry harvesting.
    ///     The block parameter is captured before harvest, so we can verify it was ripe.
    /// </summary>
    private void OnBlockUsed(IServerPlayer? player, BlockSelection? blockSel, Block? block)
    {
        if (player == null || blockSel == null || block == null) return;
        if (!_wildFollowers.Contains(player.PlayerUID)) return;

        if (IsBerryBush(block) && HasRipeBerries(block))
            _favorSystem.AwardFavorForAction(player, "foraging", 0.5f);
    }

    private bool IsBerryBush(Block block)
    {
        if (block?.Code == null) return false;

        // Berry bushes: blackberry, blueberry, cranberry, redcurrant, whitecurrant
        // Use FirstCodePart() for reliable block type detection
        var firstPart = block.FirstCodePart();
        return firstPart == "bigberrybush" || firstPart == "smallberrybush";
    }

    private bool HasRipeBerries(Block block)
    {
        if (block?.Code == null) return false;

        // Berry bushes have growth stages: empty, flowering, ripe
        // Only ripe bushes can be harvested
        // Use Variant for reliable state detection
        return block.Variant.TryGetValue("state", out var state) && state == "ripe";
    }

    private string GetBerryName(Block block)
    {
        if (block?.Code == null) return "berries";

        // Use Variant for reliable type detection
        if (block.Variant.TryGetValue("type", out var berryType))
            return berryType switch
            {
                "blackcurrant" => "blackcurrants",
                "blueberry" => "blueberries",
                "cranberry" => "cranberries",
                "redcurrant" => "redcurrants",
                "whitecurrant" => "whitecurrants",
                _ => "berries"
            };

        return "berries";
    }

    private bool IsForageBlock(Block block)
    {
        if (block?.Code == null) return false;

        // Forageable blocks that are broken (seaweed)
        // Note: Berry bushes are NOT broken, they're interacted with
        // Note: Mushrooms are handled by MushroomPatches
        // Note: Flowers are handled by FlowerPatches
        // Use FirstCodePart() for reliable block type detection
        var firstPart = block.FirstCodePart();
        return firstPart == "seaweed";
    }

    private string GetForageName(Block block)
    {
        if (block?.Code == null) return "plants";

        // Use FirstCodePart() for reliable block type detection
        // Note: Mushrooms are handled by MushroomPatches
        // Note: Flowers are handled by FlowerPatches
        var firstPart = block.FirstCodePart();
        return firstPart switch
        {
            "seaweed" => "seaweed",
            _ => "plants"
        };
    }
}