using System;
using System.Collections.Generic;
using PantheonWars.Models.Enum;
using PantheonWars.Systems.Interfaces;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace PantheonWars.Systems.Favor;

public class MiningFavorTracker(IPlayerReligionDataManager playerReligionDataManager, ICoreServerAPI sapi, FavorSystem favorSystem) : IFavorTracker, IDisposable
{
    public DeityType DeityType { get; } = DeityType.Khoras;
    private readonly IPlayerReligionDataManager _playerReligionDataManager = playerReligionDataManager ?? throw new ArgumentNullException(nameof(playerReligionDataManager));
    private readonly ICoreServerAPI _sapi = sapi ?? throw new ArgumentNullException(nameof(sapi));
    private readonly FavorSystem _favorSystem = favorSystem ?? throw new ArgumentNullException(nameof(favorSystem));

    // Cache of active Khoras followers for fast lookup (avoids database hit on every block break)
    private readonly HashSet<string> _khorasFollowers = new();

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

        // Check all online players
        foreach (var player in _sapi.World.AllOnlinePlayers)
        {
            var religionData = _playerReligionDataManager.GetOrCreatePlayerData(player.PlayerUID);
            if (religionData?.ActiveDeity == DeityType)
            {
                _khorasFollowers.Add(player.PlayerUID);
            }
        }
    }

    /// <summary>
    ///     Update cache when player data changes (e.g., joins a religion)
    /// </summary>
    private void OnPlayerDataChanged(string playerUID)
    {
        var religionData = _playerReligionDataManager.GetOrCreatePlayerData(playerUID);
        if (religionData?.ActiveDeity == DeityType)
        {
            _khorasFollowers.Add(playerUID);
        }
        else
        {
            _khorasFollowers.Remove(playerUID);
        }
    }

    /// <summary>
    ///     Update cache when a player leaves a religion
    /// </summary>
    private void OnPlayerLeavesReligion(IServerPlayer player, string religionUID)
    {
        // Player left religion, remove from cache
        _khorasFollowers.Remove(player.PlayerUID);
    }

    private void OnBlockBroken(IServerPlayer player, BlockSelection blockSel, ref float dropQuantityMultiplier, ref EnumHandling handling)
    {
        var block = _sapi.World.BlockAccessor.GetBlock(blockSel.Position);
        if (!IsOreBlock(block)) return;
        
        if (!_khorasFollowers.Contains(player.PlayerUID)) return;

        // Award favor for mining ore
        _favorSystem.AwardFavorForAction(player, "mining ore", 2);
    }

    /// <summary>
    ///     Fast ore block detection using StartsWith
    /// </summary>
    private bool IsOreBlock(Block block)
    {
        if (block?.Code is null) return false;
        
        var path = block.Code.Path;
        return path.StartsWith("ore-", StringComparison.Ordinal);
    }

    public void Dispose()
    {
        _sapi.Event.BreakBlock -= OnBlockBroken;
        _playerReligionDataManager.OnPlayerDataChanged -= OnPlayerDataChanged;
        _playerReligionDataManager.OnPlayerLeavesReligion -= OnPlayerLeavesReligion;
        _khorasFollowers.Clear();
    }
}