using System;
using System.Collections.Generic;
using DivineAscension.Models.Enum;
using DivineAscension.Systems.Interfaces;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Server;

namespace DivineAscension.Systems.Favor;

public class HuntingFavorTracker(
    IPlayerProgressionDataManager playerProgressionDataManager,
    ICoreServerAPI sapi,
    IFavorSystem favorSystem) : IFavorTracker, IDisposable
{
    private readonly IFavorSystem _favorSystem = favorSystem ?? throw new ArgumentNullException(nameof(favorSystem));

    private readonly IPlayerProgressionDataManager _playerProgressionDataManager =
        playerProgressionDataManager ?? throw new ArgumentNullException(nameof(playerProgressionDataManager));

    private readonly ICoreServerAPI _sapi = sapi ?? throw new ArgumentNullException(nameof(sapi));

    private readonly HashSet<string> _wildFollowers = new();

    public void Dispose()
    {
        _sapi.Event.OnEntityDeath -= OnEntityDeath;
        _playerProgressionDataManager.OnPlayerDataChanged -= OnPlayerDataChanged;
        _playerProgressionDataManager.OnPlayerLeavesReligion -= OnPlayerLeavesProgression;
        _wildFollowers.Clear();
    }

    public DeityDomain DeityDomain { get; } = DeityDomain.Wild;


    public void Initialize()
    {
        _sapi.Event.OnEntityDeath += OnEntityDeath;

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

            var favor = GetFavorForEntity(entity);
            if (favor > 0) _favorSystem.AwardFavorForAction(player, "hunting " + entity.Code.Path, favor);
        }
    }

    private int GetFavorForEntity(Entity entity)
    {
        if (entity is not EntityAgent || entity is EntityPlayer) return 0;

        // Check if entity is marked as huntable via attributes
        bool isHuntable = IsHuntable(entity);

        // Get entity weight (defaults to 0 if not defined)
        float weight = entity.Properties?.Weight ?? 0f;

        // If entity is huntable (via attribute), use weight-based calculation
        if (isHuntable)
        {
            // Use weight if defined, otherwise default to small animal tier
            return weight > 0 ? CalculateFavorByWeight(weight) : 3;
        }

        return CalculateFavorByWeight(weight);
    }

    /// <summary>
    /// Checks if an entity is marked as huntable via its attributes.
    /// Supports multiple attribute formats used by various mods.
    /// </summary>
    private bool IsHuntable(Entity entity)
    {
        var attributes = entity.Properties?.Attributes;
        if (attributes == null) return false;

        // Check for "huntable" attribute (used by many mods)
        if (attributes["huntable"]?.AsBool(false) == true)
            return true;

        // Check for creature diet attribute (indicates it's an animal)
        var diet = attributes["creatureDiet"]?.AsString();
        if (!string.IsNullOrEmpty(diet))
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