using System;
using System.Collections.Generic;
using DivineAscension.Models.Enum;
using DivineAscension.Systems.Interfaces;
using DivineAscension.Systems.Patches;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Server;

namespace DivineAscension.Systems.Favor;

public class SkinningFavorTracker(
    IPlayerProgressionDataManager playerProgressionDataManager,
    ICoreServerAPI sapi,
    IFavorSystem favorSystem) : IFavorTracker, IDisposable
{
    private static readonly TimeSpan SkinningAwardCooldown = TimeSpan.FromSeconds(5);
    private readonly IFavorSystem _favorSystem = favorSystem ?? throw new ArgumentNullException(nameof(favorSystem));

    // Per-player, per-entity throttling to prevent duplicate messages
    // Key format: "playerUID:entityID"
    private readonly Dictionary<string, DateTime> _lastSkinningAwardUtc = new();

    private readonly IPlayerProgressionDataManager _playerProgressionDataManager =
        playerProgressionDataManager ?? throw new ArgumentNullException(nameof(playerProgressionDataManager));

    private readonly ICoreServerAPI _sapi = sapi ?? throw new ArgumentNullException(nameof(sapi));

    private readonly HashSet<string> _wildFollowers = new();

    public void Dispose()
    {
        SkinningPatches.OnAnimalSkinned -= OnAnimalSkinned;
        _playerProgressionDataManager.OnPlayerDataChanged -= OnPlayerDataChanged;
        _playerProgressionDataManager.OnPlayerLeavesReligion -= OnPlayerLeavesProgression;
        _sapi.Event.PlayerDisconnect -= OnPlayerDisconnect;
        _wildFollowers.Clear();
        _lastSkinningAwardUtc.Clear();
    }

    public DeityDomain DeityDomain { get; } = DeityDomain.Wild;


    public void Initialize()
    {
        SkinningPatches.OnAnimalSkinned += OnAnimalSkinned;

        // Cache followers
        RefreshFollowerCache();

        _playerProgressionDataManager.OnPlayerDataChanged += OnPlayerDataChanged;
        _playerProgressionDataManager.OnPlayerLeavesReligion += OnPlayerLeavesProgression;

        // Clean up throttle cache on player disconnect
        _sapi.Event.PlayerDisconnect += OnPlayerDisconnect;
    }

    private void RefreshFollowerCache()
    {
        var onlinePlayers = _sapi?.World?.AllOnlinePlayers;
        if (onlinePlayers == null) return;

        foreach (var player in onlinePlayers) UpdateFollower(player.PlayerUID);
    }

    private void OnPlayerDataChanged(string playerId)
    {
        UpdateFollower(playerId);
    }

    private void UpdateFollower(string playerId)
    {
        var deityType = _playerProgressionDataManager.GetPlayerDeityType(playerId);
        if (deityType == DeityDomain)
            _wildFollowers.Add(playerId);
        else
            _wildFollowers.Remove(playerId);
    }

    private void OnPlayerLeavesProgression(IServerPlayer player, string religionId)
    {
        _wildFollowers.Remove(player.PlayerUID);
    }

    private void OnPlayerDisconnect(IServerPlayer player)
    {
        // Clean up throttle cache entries for disconnected player
        var keysToRemove = new List<string>();
        foreach (var key in _lastSkinningAwardUtc.Keys)
        {
            if (key.StartsWith(player.PlayerUID + ":"))
                keysToRemove.Add(key);
        }

        foreach (var key in keysToRemove)
            _lastSkinningAwardUtc.Remove(key);
    }

    private void OnAnimalSkinned(IServerPlayer player, Entity entity, float weight)
    {
        if (player == null || entity == null) return;

        // Only award favor to Wild domain followers
        if (!_wildFollowers.Contains(player.PlayerUID)) return;

        // Rate limit per player per entity to prevent duplicate messages
        var key = $"{player.PlayerUID}:{entity.EntityId}";
        var now = DateTime.UtcNow;
        if (_lastSkinningAwardUtc.TryGetValue(key, out var last) &&
            now - last < SkinningAwardCooldown)
            return;

        // Calculate favor based on animal weight (50% of hunting values)
        var favor = CalculateFavorByWeight(weight);
        if (favor > 0)
        {
            // Award favor with action type for prestige integration
            _favorSystem.AwardFavorForAction(player, "skinning " + entity.Code.Path, favor);
            _lastSkinningAwardUtc[key] = now;
        }
    }

    /// <summary>
    /// Calculates favor tier based on entity weight in kilograms.
    /// Returns 50% of hunting favor values to prevent double-dipping while rewarding thorough gameplay.
    /// Weight thresholds calibrated against vanilla and FotSA-Capreolinae animals.
    /// </summary>
    internal int CalculateFavorByWeight(float weight)
    {
        return weight switch
        {
            >= 300 => 8, // 50% of 15 (rounded up) - Apex predators, massive herbivores (bears, elephants)
            >= 150 => 6, // 50% of 12 - Large herbivores (moose, bison, wolves)
            >= 75 => 5, // 50% of 10 - Large deer, scavengers (caribou, hyenas)
            >= 35 => 4, // 50% of 8 - Medium prey (deer, foxes, boar)
            >= 10 => 3, // 50% of 5 (rounded up) - Small animals (sheep, goats, raccoons)
            _ => 2 // 50% of 3 (rounded up) - Tiny animals (chickens, rabbits, mice)
        };
    }
}