using System;
using System.Collections.Generic;
using DivineAscension.API.Interfaces;
using DivineAscension.Models.Enum;
using DivineAscension.Systems.Interfaces;
using DivineAscension.Systems.Patches;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace DivineAscension.Systems.Favor;

public class ForagingFavorTracker(
    ILogger logger,
    IWorldService worldService,
    IPlayerProgressionDataManager playerProgressionDataManager,
    IFavorSystem favorSystem) : IFavorTracker, IDisposable
{
    private readonly IFavorSystem _favorSystem = favorSystem ?? throw new ArgumentNullException(nameof(favorSystem));
    private readonly ILogger _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    private readonly IPlayerProgressionDataManager _playerProgressionDataManager =
        playerProgressionDataManager ?? throw new ArgumentNullException(nameof(playerProgressionDataManager));

    private readonly HashSet<string> _wildFollowers = new();

    private readonly IWorldService
        _worldService = worldService ?? throw new ArgumentNullException(nameof(worldService));

    public void Dispose()
    {
        ForagingPatches.Picked -= OnBlockUsed;
        MushroomPatches.OnMushroomHarvested -= OnMushroomHarvested;
        FlowerPatches.OnFlowerHarvested -= OnFlowerHarvested;
        _playerProgressionDataManager.OnPlayerDataChanged -= OnPlayerDataChanged;
        _playerProgressionDataManager.OnPlayerLeavesReligion -= OnPlayerLeavesProgression;
        _wildFollowers.Clear();
    }

    public DeityDomain DeityDomain { get; } = DeityDomain.Wild;

    public void Initialize()
    {
        ForagingPatches.Picked += OnBlockUsed;
        MushroomPatches.OnMushroomHarvested += OnMushroomHarvested;
        FlowerPatches.OnFlowerHarvested += OnFlowerHarvested;

        // Cache followers
        RefreshFollowerCache();

        _playerProgressionDataManager.OnPlayerDataChanged += OnPlayerDataChanged;
        _playerProgressionDataManager.OnPlayerLeavesReligion += OnPlayerLeavesProgression;
    }

    private void RefreshFollowerCache()
    {
        var onlinePlayers = _worldService.GetAllOnlinePlayers();

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

    /// <summary>
    ///     Handles mushroom harvesting via dedicated patch.
    /// </summary>
    private void OnMushroomHarvested(IServerPlayer player, Block block, string? mushroomType)
    {
        if (!_wildFollowers.Contains(player.PlayerUID)) return;

        _favorSystem.AwardFavorForAction(player, "foraging", 0.5f);
    }

    /// <summary>
    ///     Handles flower harvesting via FlowerPatches.
    ///     Uses batching for scythe harvests, immediate for manual harvests.
    /// </summary>
    private void OnFlowerHarvested(IServerPlayer player, Block block, string? flowerType, bool isScytheHarvest)
    {
        if (!_wildFollowers.Contains(player.PlayerUID)) return;

        if (isScytheHarvest)
        {
            // Use batched favor for scythe harvesting (avoid performance issues on large fields)
            _favorSystem.QueueFavorForAction(player, "foraging flowers", 0.5f, DeityDomain);
        }
        else
        {
            // Use immediate favor for manual harvesting (better player feedback)
            _favorSystem.AwardFavorForAction(player, "foraging", 0.5f);
        }
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