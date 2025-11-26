using System;
using System.Collections.Generic;
using PantheonWars.Models.Enum;
using PantheonWars.Systems.Interfaces;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace PantheonWars.Systems.Favor;

public class ForagingFavorTracker(IPlayerReligionDataManager playerReligionDataManager, ICoreServerAPI sapi, FavorSystem favorSystem) : IFavorTracker, IDisposable
{
    public DeityType DeityType { get; } = DeityType.Lysa;
    private readonly IPlayerReligionDataManager _playerReligionDataManager = playerReligionDataManager ?? throw new ArgumentNullException(nameof(playerReligionDataManager));
    private readonly ICoreServerAPI _sapi = sapi ?? throw new ArgumentNullException(nameof(sapi));
    private readonly FavorSystem _favorSystem = favorSystem ?? throw new ArgumentNullException(nameof(favorSystem));
    
    private readonly HashSet<string> _lysaFollowers = new();

    public void Initialize()
    {
        _sapi.Event.BreakBlock += OnBlockBroken;
        
        // Cache followers
        RefreshFollowerCache();
        
        _playerReligionDataManager.OnPlayerDataChanged += OnPlayerDataChanged;
        _playerReligionDataManager.OnPlayerLeavesReligion += OnPlayerLeavesReligion;
    }

    private void RefreshFollowerCache()
    {
        foreach (var player in _sapi.World.AllOnlinePlayers)
        {
             UpdateFollower(player.PlayerUID);
        }
    }

    private void OnPlayerDataChanged(string playerUID) => UpdateFollower(playerUID);
    
    private void UpdateFollower(string playerUID)
    {
        var religionData = _playerReligionDataManager.GetOrCreatePlayerData(playerUID);
        if (religionData?.ActiveDeity == DeityType)
            _lysaFollowers.Add(playerUID);
        else
            _lysaFollowers.Remove(playerUID);
    }

    private void OnPlayerLeavesReligion(IServerPlayer player, string religionUID)
    {
        _lysaFollowers.Remove(player.PlayerUID);
    }

    private void OnBlockBroken(IServerPlayer player, BlockSelection blockSel, ref float dropQuantityMultiplier, ref EnumHandling handling)
    {
        if (!_lysaFollowers.Contains(player.PlayerUID)) return;
        
        var block = _sapi.World.BlockAccessor.GetBlock(blockSel.Position);
        if (IsForageBlock(block))
        {
            // Award 0.5 favor per forage
            _favorSystem.AwardFavorForAction(player, "foraging " + block.Code.Path, 0.5f);
        }
    }
    
    private bool IsForageBlock(Block block)
    {
        if (block?.Code == null) return false;
        string path = block.Code.Path;
        
        return path.StartsWith("mushroom") || 
               path.StartsWith("flower") || 
               path.StartsWith("seaweed") ||
               path.Contains("berry"); 
    }

    public void Dispose()
    {
        _sapi.Event.BreakBlock -= OnBlockBroken;
        _playerReligionDataManager.OnPlayerDataChanged -= OnPlayerDataChanged;
        _playerReligionDataManager.OnPlayerLeavesReligion -= OnPlayerLeavesReligion;
        _lysaFollowers.Clear();
    }
}
