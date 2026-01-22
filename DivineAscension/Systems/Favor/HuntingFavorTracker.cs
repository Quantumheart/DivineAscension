using System;
using System.Collections.Generic;
using DivineAscension.API.Interfaces;
using DivineAscension.Models.Enum;
using DivineAscension.Services;
using DivineAscension.Systems.Interfaces;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Server;

namespace DivineAscension.Systems.Favor;

public class HuntingFavorTracker(
    IPlayerProgressionDataManager playerProgressionDataManager,
    ILoggerWrapper logger,
    IEventService eventService,
    IWorldService worldService,
    IFavorSystem favorSystem) : IFavorTracker, IDisposable
{
    private readonly IEventService
        _eventService = eventService ?? throw new ArgumentNullException(nameof(eventService));

    private readonly IFavorSystem _favorSystem = favorSystem ?? throw new ArgumentNullException(nameof(favorSystem));

    private readonly ILoggerWrapper _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    private readonly IPlayerProgressionDataManager _playerProgressionDataManager =
        playerProgressionDataManager ?? throw new ArgumentNullException(nameof(playerProgressionDataManager));

    private readonly HashSet<string> _wildFollowers = new();

    private readonly IWorldService
        _worldService = worldService ?? throw new ArgumentNullException(nameof(worldService));

    public void Dispose()
    {
        _eventService.UnsubscribeEntityDeath(OnEntityDeath);
        _playerProgressionDataManager.OnPlayerDataChanged -= OnPlayerDataChanged;
        _playerProgressionDataManager.OnPlayerLeavesReligion -= OnPlayerLeavesProgression;
        _wildFollowers.Clear();
    }

    public DeityDomain DeityDomain { get; } = DeityDomain.Wild;


    public void Initialize()
    {
        _eventService.OnEntityDeath(OnEntityDeath);

        // Cache followers
        RefreshFollowerCache();

        _playerProgressionDataManager.OnPlayerDataChanged += OnPlayerDataChanged;
        _playerProgressionDataManager.OnPlayerLeavesReligion += OnPlayerLeavesProgression;
    }

    private void RefreshFollowerCache()
    {
        var onlinePlayers = _worldService.GetAllOnlinePlayers();
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

    private void OnEntityDeath(Entity? entity, DamageSource? damageSource)
    {
        if (entity == null || damageSource == null) return;

        // Check if the killer is a player
        var killer = damageSource.GetCauseEntity();
        if (killer is EntityPlayer { Player: IServerPlayer player })
        {
            if (!_wildFollowers.Contains(player.PlayerUID)) return;
            if (entity is not EntityAgent || entity is EntityPlayer) return;

            if (!IsHuntable(entity)) return;
            var favor = GetFavorForEntity(entity);
            if (favor > 0) _favorSystem.AwardFavorForAction(player, "hunting " + entity.Code.Path, favor);
        }
    }

    private int GetFavorForEntity(Entity entity)
    {
        if (entity is not EntityAgent || entity is EntityPlayer) return 0;

        // Get entity weight (defaults to 0 if not defined)
        float weight = entity.Properties?.Weight ?? 0f;


        return CalculateFavorByWeight(weight);
    }

    /// <summary>
    /// Checks if an entity is marked as huntable via its tags.
    /// </summary>
    private bool IsHuntable(Entity entity)
    {
        if (entity.HasTags("huntable", "animal"))
            return true;

        return false;
    }

    /// <summary>
    /// Calculates favor tier based on entity weight in kilograms.
    /// Weight thresholds calibrated against vanilla and FotSA-Capreolinae animals.
    /// </summary>
    internal int CalculateFavorByWeight(float weight)
    {
        return weight switch
        {
            >= 300 => 15, // Apex predators, massive herbivores (bears, elephants)
            >= 150 => 12, // Large herbivores (moose, bison, wolves)
            >= 75 => 10, // Large deer, scavengers (caribou, hyenas)
            >= 35 => 8, // Medium prey (deer, foxes, boar)
            >= 10 => 5, // Small animals (sheep, goats, raccoons)
            _ => 3 // Tiny animals (chickens, rabbits, mice)
        };
    }
}