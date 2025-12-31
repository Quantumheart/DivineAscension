using System;
using System.Collections.Generic;
using DivineAscension.Models.Enum;
using DivineAscension.Systems.Interfaces;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace DivineAscension.Systems.Favor;

public class MiningFavorTracker(
    IPlayerReligionDataManager playerReligionDataManager,
    ICoreServerAPI sapi,
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

    private readonly IFavorSystem _favorSystem = favorSystem ?? throw new ArgumentNullException(nameof(favorSystem));

    // Cache of active Khoras followers for fast lookup (avoids database hit on every block break)
    private readonly HashSet<string> _khorasFollowers = new();

    private readonly IPlayerReligionDataManager _playerReligionDataManager =
        playerReligionDataManager ?? throw new ArgumentNullException(nameof(playerReligionDataManager));

    private readonly ICoreServerAPI _sapi = sapi ?? throw new ArgumentNullException(nameof(sapi));

    public void Dispose()
    {
        _sapi.Event.BreakBlock -= OnBlockBroken;
        _playerReligionDataManager.OnPlayerDataChanged -= OnPlayerDataChanged;
        _playerReligionDataManager.OnPlayerLeavesReligion -= OnPlayerLeavesReligion;
        _khorasFollowers.Clear();
    }

    public DeityType DeityType { get; } = DeityType.Khoras;

    public void Initialize()
    {
        _sapi.Event.BreakBlock += OnBlockBroken;

        // Build initial cache of Khoras followers
        RefreshFollowerCache();

        // Listen for religion changes to update cache
        _playerReligionDataManager.OnPlayerDataChanged += OnPlayerDataChanged;
        _playerReligionDataManager.OnPlayerLeavesReligion += OnPlayerLeavesReligion;
    }

    /// <summary>
    ///     Rebuild cache of active Khoras followers
    /// </summary>
    private void RefreshFollowerCache()
    {
        _khorasFollowers.Clear();

        // Check all online players (guard for nulls in test/headless environments)
        var onlinePlayers = _sapi?.World?.AllOnlinePlayers;
        if (onlinePlayers == null) return;

        foreach (var player in onlinePlayers)
        {
            var religionData = _playerReligionDataManager.GetOrCreatePlayerData(player.PlayerUID);
            if (religionData?.ActiveDeity == DeityType) _khorasFollowers.Add(player.PlayerUID);
        }
    }

    /// <summary>
    ///     Update cache when player data changes (e.g., joins a religion)
    /// </summary>
    private void OnPlayerDataChanged(string playerUID)
    {
        var religionData = _playerReligionDataManager.GetOrCreatePlayerData(playerUID);
        if (religionData?.ActiveDeity == DeityType)
            _khorasFollowers.Add(playerUID);
        else
            _khorasFollowers.Remove(playerUID);
    }

    /// <summary>
    ///     Update cache when a player leaves a religion
    /// </summary>
    private void OnPlayerLeavesReligion(IServerPlayer player, string religionUID)
    {
        // Player left religion, remove from cache
        _khorasFollowers.Remove(player.PlayerUID);
    }

    internal void OnBlockBroken(IServerPlayer player, BlockSelection blockSel, ref float dropQuantityMultiplier,
        ref EnumHandling handling)
    {
        var block = _sapi.World.BlockAccessor.GetBlock(blockSel.Position);
        if (!IsOreBlock(block)) return;

        if (!_khorasFollowers.Contains(player.PlayerUID)) return;

        // Calculate favor based on mineral type and quality
        var baseFavor = GetMineralTierFavor(block);
        var qualityMultiplier = GetOreQualityMultiplier(block);
        var finalFavor = (int)(baseFavor * qualityMultiplier);

        _favorSystem.AwardFavorForAction(player, "mining ore", finalFavor);

        _sapi.Logger.Debug(
            $"[MiningFavorTracker] Awarded {finalFavor} favor to {player.PlayerName} " +
            $"for mining {block.Code.Path} (base: {baseFavor}, quality: {qualityMultiplier}x)");
    }

    /// <summary>
    ///     Fast ore block detection using StartsWith
    /// </summary>
    internal bool IsOreBlock(Block block)
    {
        if (block?.Code is null) return false;

        var path = block.Code.Path;
        return path.StartsWith("ore-", StringComparison.Ordinal);
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