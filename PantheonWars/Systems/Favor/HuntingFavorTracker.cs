using System;
using System.Collections.Generic;
using PantheonWars.Models.Enum;
using PantheonWars.Systems.Interfaces;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Server;

namespace PantheonWars.Systems.Favor;

public class HuntingFavorTracker(
    IPlayerReligionDataManager playerReligionDataManager,
    ICoreServerAPI sapi,
    IFavorSystem favorSystem) : IFavorTracker, IDisposable
{
    public DeityType DeityType { get; } = DeityType.Lysa;

    private readonly IPlayerReligionDataManager _playerReligionDataManager =
        playerReligionDataManager ?? throw new ArgumentNullException(nameof(playerReligionDataManager));

    private readonly ICoreServerAPI _sapi = sapi ?? throw new ArgumentNullException(nameof(sapi));
    private readonly IFavorSystem _favorSystem = favorSystem ?? throw new ArgumentNullException(nameof(favorSystem));

    private readonly Dictionary<string, int> _animalFavorValues = new()
    {
        { "wolf", 12 },
        { "bear", 15 },
        { "deer", 8 },
        { "moose", 12 },
        { "bighorn", 8 },
        { "pig", 5 },
        { "sheep", 5 },
        { "chicken", 3 },
        { "hare", 3 },
        { "rabbit", 3 },
        { "fox", 8 },
        { "raccoon", 5 },
        { "hyena", 10 },
        { "gazelle", 8 }
    };

    private readonly HashSet<string> _lysaFollowers = new();


    public void Initialize()
    {
        _sapi.Event.OnEntityDeath += OnEntityDeath;

        // Cache followers
        RefreshFollowerCache();

        _playerReligionDataManager.OnPlayerDataChanged += OnPlayerDataChanged;
        _playerReligionDataManager.OnPlayerLeavesReligion += OnPlayerLeavesReligion;
    }

    private void RefreshFollowerCache()
    {
        var onlinePlayers = _sapi?.World?.AllOnlinePlayers;
        if (onlinePlayers == null) return;

        foreach (var player in onlinePlayers)
        {
            UpdateFollower(player.PlayerUID);
        }
    }

    private void OnPlayerDataChanged(string playerId) => UpdateFollower(playerId);

    private void UpdateFollower(string playerId)
    {
        var religionData = _playerReligionDataManager.GetOrCreatePlayerData(playerId);
        if (religionData?.ActiveDeity == DeityType)
            _lysaFollowers.Add(playerId);
        else
            _lysaFollowers.Remove(playerId);
    }

    private void OnPlayerLeavesReligion(IServerPlayer player, string religionId)
    {
        _lysaFollowers.Remove(player.PlayerUID);
    }

    private void OnEntityDeath(Entity? entity, DamageSource? damageSource)
    {
        if (entity == null || damageSource == null) return;

        // Check if the killer is a player
        Entity killer = damageSource.GetCauseEntity();
        if (killer is EntityPlayer { Player: IServerPlayer player })
        {
            if (!_lysaFollowers.Contains(player.PlayerUID)) return;

            int favor = GetFavorForEntity(entity);
            if (favor > 0)
            {
                _favorSystem.AwardFavorForAction(player, "hunting " + entity.Code.Path, favor);
            }
        }
    }

    private int GetFavorForEntity(Entity entity)
    {
        if (entity is not EntityAgent || entity is EntityPlayer) return 0;

        string code = entity.Code.Path.ToLower();

        // Monster check
        if (code.Contains("drifter") || code.Contains("locust") || code.Contains("bell")) return 0;

        // Check exact matches or contains
        foreach (var kvp in _animalFavorValues)
        {
            if (code.Contains(kvp.Key)) return kvp.Value;
        }

        // Generic fallback
        if (code.StartsWith("animal") || code.Contains("/animal/")) return 3;

        return 0;
    }

    public void Dispose()
    {
        _sapi.Event.OnEntityDeath -= OnEntityDeath;
        _playerReligionDataManager.OnPlayerDataChanged -= OnPlayerDataChanged;
        _playerReligionDataManager.OnPlayerLeavesReligion -= OnPlayerLeavesReligion;
        _lysaFollowers.Clear();
    }
}