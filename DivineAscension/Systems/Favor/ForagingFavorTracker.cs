using System;
using System.Collections.Generic;
using DivineAscension.Models.Enum;
using DivineAscension.Systems.Interfaces;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace DivineAscension.Systems.Favor;

public class ForagingFavorTracker(
    IPlayerProgressionDataManager playerProgressionDataManager,
    ICoreServerAPI sapi,
    FavorSystem favorSystem) : IFavorTracker, IDisposable
{
    private readonly FavorSystem _favorSystem = favorSystem ?? throw new ArgumentNullException(nameof(favorSystem));

    private readonly HashSet<string> _lysaFollowers = new();

    private readonly IPlayerProgressionDataManager _playerProgressionDataManager =
        playerProgressionDataManager ?? throw new ArgumentNullException(nameof(playerProgressionDataManager));

    private readonly ICoreServerAPI _sapi = sapi ?? throw new ArgumentNullException(nameof(sapi));

    public void Dispose()
    {
        _sapi.Event.BreakBlock -= OnBlockBroken;
        _sapi.Event.DidUseBlock -= OnBlockUsed;
        _playerProgressionDataManager.OnPlayerDataChanged -= OnPlayerDataChanged;
        _playerProgressionDataManager.OnPlayerLeavesReligion -= OnPlayerLeavesProgression;
        _lysaFollowers.Clear();
    }

    public DeityType DeityType { get; } = DeityType.Lysa;

    public void Initialize()
    {
        _sapi.Event.BreakBlock += OnBlockBroken;
        _sapi.Event.DidUseBlock += OnBlockUsed;

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
        if (deityType == DeityType)
            _lysaFollowers.Add(playerUID);
        else
            _lysaFollowers.Remove(playerUID);
    }

    private void OnPlayerLeavesProgression(IServerPlayer player, string religionUID)
    {
        _lysaFollowers.Remove(player.PlayerUID);
    }

    private void OnBlockBroken(IServerPlayer player, BlockSelection blockSel, ref float dropQuantityMultiplier,
        ref EnumHandling handling)
    {
        if (!_lysaFollowers.Contains(player.PlayerUID)) return;

        var block = _sapi.World.BlockAccessor.GetBlock(blockSel.Position);
        if (IsForageBlock(block))
            // Award 0.5 favor per forage (breaking mushrooms, flowers, etc.)
            _favorSystem.AwardFavorForAction(player, "foraging " + GetForageName(block), 0.5f);
    }

    /// <summary>
    ///     Handles block usage (right-click) to detect berry harvesting
    /// </summary>
    private void OnBlockUsed(IServerPlayer player, BlockSelection blockSel)
    {
        if (!_lysaFollowers.Contains(player.PlayerUID)) return;

        var block = _sapi.World.BlockAccessor.GetBlock(blockSel.Position);
        if (IsBerryBush(block) && HasRipeBerries(block))
            // Award 0.5 favor for harvesting berries
            _favorSystem.AwardFavorForAction(player, "harvesting " + GetBerryName(block), 0.5f);
    }

    private bool IsBerryBush(Block block)
    {
        if (block?.Code == null) return false;
        var path = block.Code.Path;


        // Berry bushes: blackberry, blueberry, cranberry, redcurrant, whitecurrant
        return path.Contains("berrybush");
    }

    private bool HasRipeBerries(Block block)
    {
        if (block?.Code == null) return false;
        var path = block.Code.Path;

        // Berry bushes have growth stages: empty, flowering, ripe
        // Only ripe bushes can be harvested
        return path.Contains("ripe");
    }

    private string GetBerryName(Block block)
    {
        if (block?.Code == null) return "berries";
        var path = block.Code.Path;

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
        var path = block.Code.Path;

        // Forageable blocks that are broken (mushrooms, flowers, seaweed)
        // Note: Berry bushes are NOT broken, they're interacted with
        return path.StartsWith("mushroom") ||
               path.StartsWith("flower") ||
               path.StartsWith("seaweed");
    }

    private string GetForageName(Block block)
    {
        if (block?.Code == null) return "plants";
        var path = block.Code.Path;

        if (path.Contains("mushroom")) return "mushrooms";
        if (path.Contains("flower")) return "flowers";
        if (path.Contains("seaweed")) return "seaweed";

        return "plants";
    }
}