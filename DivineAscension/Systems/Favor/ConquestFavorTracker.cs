using System;
using System.Collections.Generic;
using DivineAscension.Models.Enum;
using DivineAscension.Systems.Interfaces;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace DivineAscension.Systems.Favor;

/// <summary>
///     Tracks combat kills and awards favor to Conquest domain followers.
///     Awards favor for killing hostile creatures and monsters.
/// </summary>
public class ConquestFavorTracker(
    IPlayerProgressionDataManager playerProgressionDataManager,
    ICoreServerAPI sapi,
    IFavorSystem favorSystem) : IFavorTracker, IDisposable
{
    private readonly HashSet<string> _conquestFollowers = new();
    private readonly IFavorSystem _favorSystem = favorSystem ?? throw new ArgumentNullException(nameof(favorSystem));

    private readonly IPlayerProgressionDataManager _playerProgressionDataManager =
        playerProgressionDataManager ?? throw new ArgumentNullException(nameof(playerProgressionDataManager));

    private readonly ICoreServerAPI _sapi = sapi ?? throw new ArgumentNullException(nameof(sapi));

    public void Dispose()
    {
        _sapi.Event.OnEntityDeath -= OnEntityDeath;
        _playerProgressionDataManager.OnPlayerDataChanged -= OnPlayerDataChanged;
        _playerProgressionDataManager.OnPlayerLeavesReligion -= OnPlayerLeavesProgression;
        _conquestFollowers.Clear();
    }

    public DeityDomain DeityDomain { get; } = DeityDomain.Conquest;

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
            _conquestFollowers.Add(playerId);
        else
            _conquestFollowers.Remove(playerId);
    }

    private void OnPlayerLeavesProgression(IServerPlayer player, string religionId)
    {
        _conquestFollowers.Remove(player.PlayerUID);
    }

    private void OnEntityDeath(Entity? entity, DamageSource? damageSource)
    {
        if (entity == null || damageSource == null) return;

        // Check if the killer is a player
        var killer = damageSource.GetCauseEntity();
        if (killer is EntityPlayer { Player: IServerPlayer player })
        {
            if (!_conquestFollowers.Contains(player.PlayerUID)) return;

            // Skip if target is a player (PvP is handled separately)
            if (entity is EntityPlayer) return;

            // Only award favor for combat-worthy entities
            if (!IsCombatWorthy(entity)) return;

            var favor = GetFavorForEntity(entity);
            if (favor > 0) _favorSystem.AwardFavorForAction(player, "combat kill " + entity.Code.Path, favor);
        }
    }

    private int GetFavorForEntity(Entity entity)
    {
        if (entity is not EntityAgent || entity is EntityPlayer) return 0;

        // Get entity weight and health for favor calculation
        float weight = entity.Properties?.Weight ?? 0f;
        float maxHealth = (entity as EntityAgent)?.GetBehavior<EntityBehaviorHealth>()?.MaxHealth ?? 10f;

        return CalculateFavorByCombat(weight, maxHealth);
    }

    /// <summary>
    ///     Checks if an entity is worth fighting for favor.
    ///     Only includes hostile creatures and monsters - excludes domesticated animals and wildlife.
    /// </summary>
    private bool IsCombatWorthy(Entity entity)
    {
        if (entity is not EntityAgent) return false;

        // ONLY reward killing hostile creatures and monsters
        // Excludes: domesticated animals, wildlife, predators (those belong to Wild domain)
        if (entity.HasTags("hostile", "monster", "drifter", "locust"))
            return true;

        return false;
    }

    /// <summary>
    ///     Calculates favor based on entity combat difficulty.
    ///     Uses max health as proxy for difficulty (3-15 favor range).
    /// </summary>
    internal int CalculateFavorByCombat(float weight, float maxHealth)
    {
        // Base favor on health (reduced from previous 4-25 range to 3-15)
        var healthTier = maxHealth switch
        {
            >= 100 => 15, // Boss-tier entities (drifter kings, etc.)
            >= 50 => 10, // Strong monsters
            >= 25 => 7, // Medium threats
            >= 10 => 5, // Minor threats
            _ => 3 // Weak creatures
        };

        return healthTier;
    }
}