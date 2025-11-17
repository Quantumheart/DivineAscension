using System;
using System.Collections.Generic;
using System.Linq;
using PantheonWars.Constants;
using PantheonWars.Models;
using PantheonWars.Models.Enum;
using PantheonWars.Systems.Interfaces;
using PantheonWars.Systems.SpecialEffects;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace PantheonWars.Systems;

/// <summary>
///     Manages blessing effects and stat modifiers (Phase 3.3)
/// </summary>
public class BlessingEffectSystem(
    ICoreServerAPI sapi,
    IBlessingRegistry blessingRegistry,
    IPlayerReligionDataManager playerReligionDataManager,
    IReligionManager religionManager)
    : IBlessingEffectSystem
{
    // Track applied modifiers per player for cleanup
    private readonly Dictionary<string, HashSet<string>> _appliedModifiers = new();
    private readonly IBlessingRegistry _blessingRegistry = blessingRegistry ?? throw new ArgumentNullException(nameof(blessingRegistry));

    // Cache for stat modifiers to reduce computation
    private readonly Dictionary<string, Dictionary<string, float>> _playerModifierCache = new();
    private readonly IPlayerReligionDataManager _playerReligionDataManager = playerReligionDataManager ?? throw new ArgumentNullException(nameof(playerReligionDataManager));
    private readonly IReligionManager _religionManager = religionManager ?? throw new ArgumentNullException(nameof(religionManager));
    private readonly Dictionary<string, Dictionary<string, float>> _religionModifierCache = new();
    private readonly ICoreServerAPI _sapi = sapi ?? throw new ArgumentNullException(nameof(sapi));

    // Special effects system
    private SpecialEffectHandlerRegistry? _specialEffectRegistry;

    /// <summary>
    ///     Initializes the blessing effect system
    /// </summary>
    public void Initialize()
    {
        _sapi.Logger.Notification($"{SystemConstants.LogPrefix} {SystemConstants.InfoInitializingBlessingSystem}");

        // Initialize special effects system
        _specialEffectRegistry = new SpecialEffectHandlerRegistry(_sapi);
        _specialEffectRegistry.Initialize();

        // Register event handlers
        _sapi.Event.PlayerJoin += OnPlayerJoin;
        _playerReligionDataManager.OnPlayerLeavesReligion += OnPlayerLeavesReligion;

        _sapi.Logger.Notification($"{SystemConstants.LogPrefix} {SystemConstants.InfoBlessingSystemInitialized}");
    }

    /// <summary>
    ///     Gets stat modifiers from player's unlocked blessings
    /// </summary>
    public Dictionary<string, float> GetPlayerStatModifiers(string playerUID)
    {
        // Check cache first
        if (_playerModifierCache.TryGetValue(playerUID, out var cachedModifiers))
            return new Dictionary<string, float>(cachedModifiers);

        var modifiers = new Dictionary<string, float>();
        var playerData = _playerReligionDataManager.GetOrCreatePlayerData(playerUID);

        // Get all unlocked player blessings
        var unlockedBlessingIds = playerData.UnlockedBlessings
            .Where(kvp => kvp.Value)
            .Select(kvp => kvp.Key)
            .ToList();

        // Combine stat modifiers from all blessings
        foreach (var blessingId in unlockedBlessingIds)
        {
            var blessing = _blessingRegistry.GetBlessing(blessingId);
            if (blessing != null && blessing.Kind == BlessingKind.Player) CombineModifiers(modifiers, blessing.StatModifiers);
        }

        // Cache the result
        _playerModifierCache[playerUID] = new Dictionary<string, float>(modifiers);

        return modifiers;
    }

    /// <summary>
    ///     Gets stat modifiers from religion's unlocked blessings
    /// </summary>
    public Dictionary<string, float> GetReligionStatModifiers(string religionUID)
    {
        // Check cache first
        if (_religionModifierCache.TryGetValue(religionUID, out var cachedModifiers))
            return new Dictionary<string, float>(cachedModifiers);

        var modifiers = new Dictionary<string, float>();
        var religion = _religionManager.GetReligion(religionUID);

        if (religion == null) return modifiers;

        // Get all unlocked religion blessings
        var unlockedBlessingIds = religion.UnlockedBlessings
            .Where(kvp => kvp.Value)
            .Select(kvp => kvp.Key)
            .ToList();

        // Combine stat modifiers from all blessings
        foreach (var blessingId in unlockedBlessingIds)
        {
            var blessing = _blessingRegistry.GetBlessing(blessingId);
            if (blessing != null && blessing.Kind == BlessingKind.Religion) CombineModifiers(modifiers, blessing.StatModifiers);
        }

        // Cache the result
        _religionModifierCache[religionUID] = new Dictionary<string, float>(modifiers);

        return modifiers;
    }

    /// <summary>
    ///     Gets combined stat modifiers for a player (player blessings + religion blessings)
    /// </summary>
    public Dictionary<string, float> GetCombinedStatModifiers(string playerUID)
    {
        var combined = new Dictionary<string, float>();

        // Get player modifiers
        var playerModifiers = GetPlayerStatModifiers(playerUID);
        CombineModifiers(combined, playerModifiers);

        // Get religion modifiers
        var playerData = _playerReligionDataManager.GetOrCreatePlayerData(playerUID);
        if (playerData.ReligionUID != null)
        {
            var religionModifiers = GetReligionStatModifiers(playerData.ReligionUID);
            CombineModifiers(combined, religionModifiers);
        }

        return combined;
    }

    /// <summary>
    ///     Applies blessings to a player using Vintage Story's Stats API
    ///     Based on XSkills implementation pattern
    /// </summary>
    public void ApplyBlessingsToPlayer(IServerPlayer player)
    {
        if (player?.Entity == null)
        {
            _sapi.Logger.Warning($"{SystemConstants.LogPrefix} {SystemConstants.ErrorPlayerEntityNull}");
            return;
        }

        EntityAgent agent = player.Entity;
        if (agent?.Stats == null)
        {
            _sapi.Logger.Warning($"{SystemConstants.LogPrefix} {SystemConstants.ErrorPlayerStatsNull}");
            return;
        }

        // Get combined modifiers (player blessings + religion blessings)
        var modifiers = GetCombinedStatModifiers(player.PlayerUID);

        // Remove old modifiers first
        RemoveBlessingsFromPlayer(player);

        // Apply new modifiers
        var appliedCount = 0;
        var appliedSet = new HashSet<string>();

        foreach (var modifier in modifiers)
        {
            // Stat names now come directly from VintageStoryStats constants
            var statName = modifier.Key;

            // Use namespaced modifier ID to avoid conflicts
            var modifierId = string.Format(SystemConstants.ModifierIdFormat, player.PlayerUID);
            var value = modifier.Value;

            try
            {
                agent.Stats.Set(statName, modifierId, value);
                appliedSet.Add(statName);
                appliedCount++;

                _sapi.Logger.Debug(
                    $"{SystemConstants.LogPrefix} {string.Format(SystemConstants.DebugAppliedStatFormat, statName, modifierId, value)}");
            }
            catch (KeyNotFoundException ex)
            {
                _sapi.Logger.Warning(
                    $"{SystemConstants.LogPrefix} {string.Format(SystemConstants.ErrorStatNotFoundFormat, statName, player.PlayerName)}: {ex.Message}");
            }
            catch (Exception ex)
            {
                _sapi.Logger.Error(
                    $"{SystemConstants.LogPrefix} {string.Format(SystemConstants.ErrorApplyingStatFormat, statName, player.PlayerName)}: {ex}");
            }
        }

        // Track applied modifiers for cleanup later
        _appliedModifiers[player.PlayerUID] = appliedSet;

        // Force health recalculation after applying stats
        var healthBehavior = agent.GetBehavior<EntityBehaviorHealth>();
        if (healthBehavior != null)
        {
            var beforeHealth = healthBehavior.MaxHealth;
            healthBehavior.UpdateMaxHealth();
            var afterHealth = healthBehavior.MaxHealth;

            if (Math.Abs(beforeHealth - afterHealth) > 0.01f)
            {
                var statValue = agent.Stats.GetBlended("maxhealthExtraPoints");
                _sapi.Logger.Notification(
                    $"{SystemConstants.LogPrefix} {string.Format(SystemConstants.SuccessHealthUpdateFormat, player.PlayerName, beforeHealth, afterHealth, healthBehavior.BaseMaxHealth, statValue)}"
                );
            }
        }

        if (appliedCount > 0)
            _sapi.Logger.Notification(
                $"{SystemConstants.LogPrefix} {string.Format(SystemConstants.SuccessAppliedModifiersFormat, appliedCount, player.PlayerName)}");
    }

    /// <summary>
    ///     Refreshes all blessing effects for a player
    /// </summary>
    public void RefreshPlayerBlessings(string playerUID)
    {
        // Clear cached modifiers
        _playerModifierCache.Remove(playerUID);

        var playerData = _playerReligionDataManager.GetOrCreatePlayerData(playerUID);
        if (playerData.ReligionUID != null) _religionModifierCache.Remove(playerData.ReligionUID);

        // Recalculate and apply stat modifiers
        var player = _sapi.World.PlayerByUid(playerUID) as IServerPlayer;
        if (player != null) ApplyBlessingsToPlayer(player);

        // Load special effect handlers
        LoadSpecialEffectHandlers(playerUID);

        _sapi.Logger.Debug(
            $"{SystemConstants.LogPrefix} {string.Format(SystemConstants.SuccessRefreshedBlessingsFormat, playerUID)}");
    }

    /// <summary>
    ///     Refreshes blessing effects for all members of a religion
    /// </summary>
    public void RefreshReligionBlessings(string religionUID)
    {
        // Clear religion modifier cache
        _religionModifierCache.Remove(religionUID);

        var religion = _religionManager.GetReligion(religionUID);
        if (religion == null) return;

        // Refresh all members
        foreach (var memberUID in religion.MemberUIDs) RefreshPlayerBlessings(memberUID);

        _sapi.Logger.Debug(
            $"{SystemConstants.LogPrefix} {string.Format(SystemConstants.SuccessRefreshedReligionBlessingsFormat, religion.ReligionName, religion.MemberUIDs.Count)}");
    }

    /// <summary>
    ///     Gets a summary of active blessings for a player
    /// </summary>
    public (List<Blessing> playerBlessings, List<Blessing> religionBlessings) GetActiveBlessings(string playerUID)
    {
        var playerBlessings = new List<Blessing>();
        var religionBlessings = new List<Blessing>();

        var playerData = _playerReligionDataManager.GetOrCreatePlayerData(playerUID);

        // Get player blessings
        var playerBlessingIds = playerData.UnlockedBlessings
            .Where(kvp => kvp.Value)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var blessingId in playerBlessingIds)
        {
            var blessing = _blessingRegistry.GetBlessing(blessingId);
            if (blessing != null) playerBlessings.Add(blessing);
        }

        // Get religion blessings
        if (playerData.ReligionUID != null)
        {
            var religion = _religionManager.GetReligion(playerData.ReligionUID);
            if (religion != null)
            {
                var religionBlessingIds = religion.UnlockedBlessings
                    .Where(kvp => kvp.Value)
                    .Select(kvp => kvp.Key)
                    .ToList();

                foreach (var blessingId in religionBlessingIds)
                {
                    var blessing = _blessingRegistry.GetBlessing(blessingId);
                    if (blessing != null) religionBlessings.Add(blessing);
                }
            }
        }

        return (playerBlessings, religionBlessings);
    }

    /// <summary>
    ///     Clears all modifier caches (useful for debugging/testing)
    /// </summary>
    public void ClearAllCaches()
    {
        _playerModifierCache.Clear();
        _religionModifierCache.Clear();
        _sapi.Logger.Notification($"{SystemConstants.LogPrefix} {SystemConstants.InfoClearedCaches}");
    }

    /// <summary>
    ///     Gets a formatted string of all stat modifiers for display
    /// </summary>
    public string FormatStatModifiers(Dictionary<string, float> modifiers)
    {
        if (modifiers.Count == 0) return SystemConstants.NoActiveModifiers;

        var lines = new List<string>();
        foreach (var kvp in modifiers.OrderBy(m => m.Key))
        {
            var statName = FormatStatName(kvp.Key);
            var percentage = kvp.Value * 100f;
            var sign = percentage >= 0 ? "+" : "";
            lines.Add($"  {statName}: {sign}{percentage:F1}%");
        }

        return string.Join("\n", lines);
    }

    internal void OnPlayerLeavesReligion(IServerPlayer player, string religionUID)
    {
        RemoveBlessingsFromPlayer(player);
        RefreshBlessings(player.PlayerUID, religionUID);
    }

    internal void RefreshBlessings(string playerUid, string religionUID)
    {
        RefreshPlayerBlessings(playerUid);
        RefreshReligionBlessings(religionUID);
    }

    /// <summary>
    ///     Handles player join to apply blessings
    /// </summary>
    internal void OnPlayerJoin(IServerPlayer player)
    {
        // Attach blessing entity behavior to player for special effects
        AttachBlessingBehavior(player);

        // Apply stat modifiers and load special effect handlers
        RefreshPlayerBlessings(player.PlayerUID);
    }

    /// <summary>
    ///     Removes all blessing modifiers from a player
    /// </summary>
    internal void RemoveBlessingsFromPlayer(IServerPlayer player)
    {
        var agent = player?.Entity as EntityAgent;
        if (agent?.Stats == null) return;

        // Get previously applied modifiers
        if (!_appliedModifiers.TryGetValue(player.PlayerUID, out var appliedSet)) return; // No modifiers to remove

        var modifierId = string.Format(SystemConstants.ModifierIdFormat, player.PlayerUID);
        var removedCount = 0;

        foreach (var statName in appliedSet)
            try
            {
                agent.Stats.Remove(statName, modifierId);
                removedCount++;
            }
            catch (Exception ex)
            {
                _sapi.Logger.Debug(
                    $"{SystemConstants.LogPrefix} {string.Format(SystemConstants.ErrorRemovingModifierFormat, statName, player.PlayerName)}: {ex.Message}");
            }

        if (removedCount > 0)
            _sapi.Logger.Debug(
                $"{SystemConstants.LogPrefix} {string.Format(SystemConstants.SuccessRemovedModifiersFormat, removedCount, player.PlayerName)}");

        appliedSet.Clear();

        // Update health after removing modifiers
        var healthBehavior = agent.GetBehavior<EntityBehaviorHealth>();
        if (healthBehavior != null) healthBehavior.UpdateMaxHealth();
    }

    /// <summary>
    ///     Helper method to combine modifiers (additive)
    /// </summary>
    internal void CombineModifiers(Dictionary<string, float> target, Dictionary<string, float> source)
    {
        foreach (var kvp in source)
            if (target.ContainsKey(kvp.Key))
                target[kvp.Key] += kvp.Value;
            else
                target[kvp.Key] = kvp.Value;
    }

    /// <summary>
    ///     Formats stat names for display
    /// </summary>
    internal string FormatStatName(string statKey)
    {
        return statKey switch
        {
            VintageStoryStats.MeleeWeaponsDamage => SystemConstants.StatDisplayMeleeDamage,
            VintageStoryStats.RangedWeaponsDamage => SystemConstants.StatDisplayRangedDamage,
            VintageStoryStats.MeleeWeaponsSpeed => SystemConstants.StatDisplayAttackSpeed,
            VintageStoryStats.MeleeWeaponArmor => SystemConstants.StatDisplayArmor,
            VintageStoryStats.MaxHealthExtraPoints => SystemConstants.StatDisplayMaxHealth,
            VintageStoryStats.WalkSpeed => SystemConstants.StatDisplayWalkSpeed,
            VintageStoryStats.HealingEffectiveness => SystemConstants.StatDisplayHealthRegen,
            _ => statKey
        };
    }

    /// <summary>
    ///     Attaches BlessingEntityBehavior to player entity for special effects handling
    /// </summary>
    internal void AttachBlessingBehavior(IServerPlayer player)
    {
        if (_specialEffectRegistry == null || player?.Entity == null) return;

        // Check if behavior already attached
        var existingBehavior = player.Entity.GetBehavior<BlessingEntityBehavior>();
        if (existingBehavior != null) return; // Already attached

        // Create and attach new behavior
        var blessingBehavior = new BlessingEntityBehavior(player.Entity, _specialEffectRegistry);
        player.Entity.AddBehavior(blessingBehavior);

        _sapi.Logger.Debug($"{SystemConstants.LogPrefix} Attached BlessingEntityBehavior to player {player.PlayerName}");
    }

    /// <summary>
    ///     Loads special effect handlers for a player based on their active blessings
    /// </summary>
    internal void LoadSpecialEffectHandlers(string playerUID)
    {
        if (_specialEffectRegistry == null) return;

        var player = _sapi.World.PlayerByUid(playerUID) as IServerPlayer;
        if (player?.Entity == null) return;

        // Get blessing behavior
        var blessingBehavior = player.Entity.GetBehavior<BlessingEntityBehavior>();
        if (blessingBehavior == null)
        {
            _sapi.Logger.Warning($"{SystemConstants.LogPrefix} BlessingEntityBehavior not found for player {player.PlayerName} - cannot load special effects");
            return;
        }

        // Get all active blessings (player + religion)
        var (playerBlessings, religionBlessings) = GetActiveBlessings(playerUID);
        var allBlessings = new List<Blessing>();
        allBlessings.AddRange(playerBlessings);
        allBlessings.AddRange(religionBlessings);

        // Extract all special effect IDs
        var effectIds = new List<string>();
        foreach (var blessing in allBlessings)
        {
            if (blessing.SpecialEffects.Count > 0)
            {
                effectIds.AddRange(blessing.SpecialEffects);
            }
        }

        // Load handlers into behavior
        blessingBehavior.LoadHandlers(effectIds);

        _sapi.Logger.Debug($"{SystemConstants.LogPrefix} Loaded {effectIds.Count} special effect(s) for player {player.PlayerName}");
    }
}