using System;
using System.Collections.Generic;
using DivineAscension.Models.Enum;
using DivineAscension.Systems.Interfaces;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Server;

namespace DivineAscension.Systems.Favor;

/// <summary>
///     Tracks combat kills and awards favor to War domain followers.
///     Awards favor for killing hostile creatures and monsters.
/// </summary>
public class CombatFavorTracker(
    IPlayerProgressionDataManager playerProgressionDataManager,
    ICoreServerAPI sapi,
    IFavorSystem favorSystem) : IFavorTracker, IDisposable
{
    private readonly IFavorSystem _favorSystem = favorSystem ?? throw new ArgumentNullException(nameof(favorSystem));

    private readonly IPlayerProgressionDataManager _playerProgressionDataManager =
        playerProgressionDataManager ?? throw new ArgumentNullException(nameof(playerProgressionDataManager));

    private readonly ICoreServerAPI _sapi = sapi ?? throw new ArgumentNullException(nameof(sapi));

    private readonly HashSet<string> _warFollowers = new();

    public void Dispose()
    {
        _sapi.Event.OnEntityDeath -= OnEntityDeath;
        _playerProgressionDataManager.OnPlayerDataChanged -= OnPlayerDataChanged;
        _playerProgressionDataManager.OnPlayerLeavesReligion -= OnPlayerLeavesProgression;
        _warFollowers.Clear();
    }

    public DeityDomain DeityDomain { get; } = DeityDomain.War;

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
            _warFollowers.Add(playerId);
        else
            _warFollowers.Remove(playerId);
    }

    private void OnPlayerLeavesProgression(IServerPlayer player, string religionId)
    {
        _warFollowers.Remove(player.PlayerUID);
    }

    private void OnEntityDeath(Entity? entity, DamageSource? damageSource)
    {
        if (entity == null || damageSource == null) return;

        // Check if the killer is a player
        var killer = damageSource.GetCauseEntity();
        if (killer is EntityPlayer { Player: IServerPlayer player })
        {
            if (!_warFollowers.Contains(player.PlayerUID)) return;

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
    ///     Includes hostile creatures, monsters, and aggressive animals.
    /// </summary>
    private bool IsCombatWorthy(Entity entity)
    {
        if (entity is not EntityAgent) return false;

        // Check for hostile tags
        if (entity.HasTags("hostile", "monster", "drifter", "locust"))
            return true;

        // Check for aggressive creatures that attack players
        if (entity.HasTags("aggressive"))
            return true;

        // Also award favor for dangerous animals (predators)
        if (entity.HasTags("predator"))
            return true;

        return false;
    }

    /// <summary>
    ///     Calculates favor based on entity combat difficulty.
    ///     Uses weight and max health as proxies for difficulty.
    /// </summary>
    internal int CalculateFavorByCombat(float weight, float maxHealth)
    {
        // Base favor on health primarily (more dangerous = more favor)
        var healthTier = maxHealth switch
        {
            >= 100 => 20, // Boss-tier entities
            >= 50 => 15, // Strong monsters
            >= 25 => 10, // Medium threats
            >= 10 => 7, // Minor threats
            _ => 4 // Weak creatures
        };

        // Bonus for heavier creatures
        var weightBonus = weight switch
        {
            >= 200 => 5,
            >= 100 => 3,
            >= 50 => 1,
            _ => 0
        };

        return healthTier + weightBonus;
    }
}
