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
        _sapi.Event.DidUseBlock += OnBlockUsed;

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
            // Award 0.5 favor per forage (breaking mushrooms, flowers, etc.)
            _favorSystem.AwardFavorForAction(player, "foraging " + GetForageName(block), 0.5f);
        }
    }

    /// <summary>
    /// Handles block usage (right-click) to detect berry harvesting
    /// </summary>
    private void OnBlockUsed(IServerPlayer player, BlockSelection blockSel)
    {
        if (!_lysaFollowers.Contains(player.PlayerUID)) return;

        var block = _sapi.World.BlockAccessor.GetBlock(blockSel.Position);
        if (IsBerryBush(block) && HasRipeBerries(block))
        {
            // Award 0.5 favor for harvesting berries
            _favorSystem.AwardFavorForAction(player, "harvesting " + GetBerryName(block), 0.5f);
        }
    }

    private bool IsBerryBush(Block block)
    {
        if (block?.Code == null) return false;
        string path = block.Code.Path;

        // Berry bushes: blackberry, blueberry, cranberry, redcurrant, whitecurrant
        return path.Contains("berrybush");
    }

    private bool HasRipeBerries(Block block)
    {
        if (block?.Code == null) return false;
        string path = block.Code.Path;

        // Berry bushes have growth stages: empty, flowering, ripe
        // Only ripe bushes can be harvested
        return path.Contains("ripe");
    }

    private string GetBerryName(Block block)
    {
        if (block?.Code == null) return "berries";
        string path = block.Code.Path;

        if (path.Contains("blackberry")) return "blackberries";
        if (path.Contains("blueberry")) return "blueberries";
        if (path.Contains("cranberry")) return "cranberries";
        if (path.Contains("redcurrant")) return "redcurrants";
        if (path.Contains("whitecurrant")) return "whitecurrants";

        return "berries";
    }
    
    private bool IsForageBlock(Block block)
    {
        if (block?.Code == null) return false;
        string path = block.Code.Path;

        // Forageable blocks that are broken (mushrooms, flowers, seaweed)
        // Note: Berry bushes are NOT broken, they're interacted with
        return path.StartsWith("mushroom") ||
               path.StartsWith("flower") ||
               path.StartsWith("seaweed");
    }

    private string GetForageName(Block block)
    {
        if (block?.Code == null) return "plants";
        string path = block.Code.Path;

        if (path.Contains("mushroom")) return "mushrooms";
        if (path.Contains("flower")) return "flowers";
        if (path.Contains("seaweed")) return "seaweed";

        return "plants";
    }

    public void Dispose()
    {
        _sapi.Event.BreakBlock -= OnBlockBroken;
        _sapi.Event.DidUseBlock -= OnBlockUsed;
        _playerReligionDataManager.OnPlayerDataChanged -= OnPlayerDataChanged;
        _playerReligionDataManager.OnPlayerLeavesReligion -= OnPlayerLeavesReligion;
        _lysaFollowers.Clear();
    }
}
