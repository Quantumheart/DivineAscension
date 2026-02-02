using System;
using System.Collections.Generic;
using DivineAscension.API.Interfaces;
using DivineAscension.Blocks;
using DivineAscension.Models.Enum;
using DivineAscension.Services;
using DivineAscension.Systems.Interfaces;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace DivineAscension.Systems.Favor;

public class MiningFavorTracker(
    ILoggerWrapper logger,
    IWorldService worldService,
    IPlayerProgressionDataManager playerProgressionDataManager,
    IFavorSystem favorSystem) : IFavorTracker, IDisposable
{
    // Base favor values per mineral tier
    private const int FavorLowTier = 1; // Native copper, malachite, cassiterite (tin), bismuthinite
    private const int FavorMidTier = 2; // Galena (lead), sphalerite (zinc), rhodochrosite (manganese)
    private const int FavorHighTier = 3; // Native silver, native gold
    private const int FavorEliteTier = 4; // Hematite, limonite, magnetite (iron), suevite (meteoric iron)
    private const int FavorSuperEliteTier = 5; // Ilmenite (titanium), pentlandite (nickel), chromite (chromium)

    // Quality multipliers for ore density
    private const float QualityPoor = 1.0f;
    private const float QualityMedium = 1.25f;
    private const float QualityRich = 1.5f;
    private const float QualityBountiful = 2.0f;

    // Cache of active Craft followers for fast lookup (avoids database hit on every block break)
    private readonly HashSet<string> _craftFollowers = new();
    private readonly IFavorSystem _favorSystem = favorSystem ?? throw new ArgumentNullException(nameof(favorSystem));

    private readonly ILoggerWrapper _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    private readonly IPlayerProgressionDataManager _playerProgressionDataManager =
        playerProgressionDataManager ?? throw new ArgumentNullException(nameof(playerProgressionDataManager));

    private readonly IWorldService
        _worldService = worldService ?? throw new ArgumentNullException(nameof(worldService));

    public void Dispose()
    {
        BlockBehaviorOre.OnOreBlockBroken -= OnOreBlockBroken;
        _playerProgressionDataManager.OnPlayerDataChanged -= OnPlayerDataChanged;
        _playerProgressionDataManager.OnPlayerLeavesReligion -= OnPlayerLeavesProgression;
        _craftFollowers.Clear();
    }

    public DeityDomain DeityDomain { get; } = DeityDomain.Craft;

    public void Initialize()
    {
        // Subscribe to ore block break events (via BlockBehavior)
        BlockBehaviorOre.OnOreBlockBroken += OnOreBlockBroken;

        // Build initial cache of Craft followers
        RefreshFollowerCache();

        // Listen for religion changes to update cache
        _playerProgressionDataManager.OnPlayerDataChanged += OnPlayerDataChanged;
        _playerProgressionDataManager.OnPlayerLeavesReligion += OnPlayerLeavesProgression;
    }

    /// <summary>
    ///     Rebuild cache of active Craft followers
    /// </summary>
    private void RefreshFollowerCache()
    {
        _craftFollowers.Clear();

        var onlinePlayers = _worldService.GetAllOnlinePlayers();

        foreach (var player in onlinePlayers)
        {
            if (_playerProgressionDataManager.GetPlayerDeityType(player.PlayerUID) == DeityDomain)
                _craftFollowers.Add(player.PlayerUID);
        }
    }

    /// <summary>
    ///     Update cache when player data changes (e.g., joins a religion)
    /// </summary>
    private void OnPlayerDataChanged(string playerUID)
    {
        if (_playerProgressionDataManager.GetPlayerDeityType(playerUID) == DeityDomain)
            _craftFollowers.Add(playerUID);
        else
            _craftFollowers.Remove(playerUID);
    }

    /// <summary>
    ///     Update cache when a player leaves a religion
    /// </summary>
    private void OnPlayerLeavesProgression(IServerPlayer player, string religionUID)
    {
        // Player left religion, remove from cache
        _craftFollowers.Remove(player.PlayerUID);
    }

    /// <summary>
    ///     Handles ore block broken events from BlockBehaviorOre.
    ///     Awards favor to Craft domain followers based on ore tier and quality.
    /// </summary>
    private void OnOreBlockBroken(IWorldAccessor world, BlockPos pos, IPlayer? player, Block block, EnumHandling handling)
    {
        // Player can be null when blocks break automatically (gravity, neighbor updates, etc.)
        if (player == null) return;
        if (!_craftFollowers.Contains(player.PlayerUID)) return;

        // Calculate favor based on mineral type and quality
        var baseFavor = GetMineralTierFavor(block);
        var qualityMultiplier = GetOreQualityMultiplier(block);
        var finalFavor = (int)(baseFavor * qualityMultiplier);

        var serverPlayer = _worldService.GetPlayerByUID(player.PlayerUID) as IServerPlayer;
        if (serverPlayer != null)
        {
            _favorSystem.AwardFavorForAction(serverPlayer, "mining ore", finalFavor);

            _logger.Debug(
                $"[MiningFavorTracker] Awarded {finalFavor} favor to {player.PlayerName} " +
                $"for mining {block.Code.Path} (base: {baseFavor}, quality: {qualityMultiplier}x)");
        }
    }

    /// <summary>
    ///     Get base favor for the mineral type
    /// </summary>
    internal int GetMineralTierFavor(Block block)
    {
        var path = block.Code.Path;

        // Extract mineral name from path (ore-{quality}-{mineral})
        // Check for most rare minerals first
        if (IsSuperEliteTierMineral(path)) return FavorSuperEliteTier;
        if (IsEliteTierMineral(path)) return FavorEliteTier;
        if (IsHighTierMineral(path)) return FavorHighTier;
        if (IsMidTierMineral(path)) return FavorMidTier;

        // Default to low tier
        return FavorLowTier;
    }

    internal bool IsMidTierMineral(string path)
    {
        // Galena (lead), sphalerite (zinc), rhodochrosite (manganese)
        return path.Contains("-galena-") || path.Contains("-sphalerite-") ||
               path.Contains("-rhodochrosite-") ||
               path.EndsWith("-galena") || path.EndsWith("-sphalerite") ||
               path.EndsWith("-rhodochrosite");
    }

    internal bool IsHighTierMineral(string path)
    {
        // Native silver, native gold
        return path.Contains("-nativesilver-") || path.Contains("-nativegold-") ||
               path.EndsWith("-nativesilver") || path.EndsWith("-nativegold");
    }

    internal bool IsEliteTierMineral(string path)
    {
        // Hematite, limonite, magnetite (iron ores), suevite (meteoric iron)
        return path.Contains("-hematite-") || path.Contains("-limonite-") ||
               path.Contains("-magnetite-") || path.Contains("-suevite-") ||
               path.EndsWith("-hematite") || path.EndsWith("-limonite") ||
               path.EndsWith("-magnetite") || path.EndsWith("-suevite");
    }

    internal bool IsSuperEliteTierMineral(string path)
    {
        // Ilmenite (titanium), pentlandite (nickel), chromite (chromium) - very rare and valuable
        return path.Contains("-ilmenite-") || path.Contains("-pentlandite-") ||
               path.Contains("-chromite-") ||
               path.EndsWith("-ilmenite") || path.EndsWith("-pentlandite") ||
               path.EndsWith("-chromite");
    }

    /// <summary>
    ///     Get quality multiplier from ore block path
    /// </summary>
    internal float GetOreQualityMultiplier(Block block)
    {
        var path = block.Code.Path;

        if (path.Contains("ore-poor-") || path.StartsWith("ore-poor"))
            return QualityPoor;

        if (path.Contains("ore-medium-") || path.StartsWith("ore-medium"))
            return QualityMedium;

        if (path.Contains("ore-rich-") || path.StartsWith("ore-rich"))
            return QualityRich;

        if (path.Contains("ore-bountiful-") || path.StartsWith("ore-bountiful"))
            return QualityBountiful;

        // Default to poor quality if pattern doesn't match
        return QualityPoor;
    }
}
