using System;
using System.Collections.Generic;
using System.Linq;
using DivineAscension.Constants;
using DivineAscension.Models;
using DivineAscension.Models.Enum;
using DivineAscension.Services;
using DivineAscension.Systems.BlessingEffects;
using DivineAscension.Systems.BlessingEffects.Handlers;
using DivineAscension.Systems.Interfaces;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace DivineAscension.Systems;

/// <summary>
///     Manages blessing effects and stat modifiers (Phase 3.3)
/// </summary>
public class BlessingEffectSystem : IBlessingEffectSystem
{
    // Track applied modifiers per player for cleanup
    private readonly Dictionary<string, HashSet<string>> _appliedModifiers = new();
    private readonly IBlessingRegistry _blessingRegistry;

    // Cache for stat modifiers to reduce computation
    private readonly Dictionary<string, Dictionary<string, float>> _playerModifierCache = new();
    private readonly IPlayerProgressionDataManager _playerProgressionDataManager;
    private readonly IReligionManager _religionManager;
    private readonly Dictionary<string, Dictionary<string, float>> _religionModifierCache = new();
    private readonly ICoreServerAPI _sapi;
    private readonly SpecialEffectRegistry _specialEffectRegistry;

    public BlessingEffectSystem(
        ICoreServerAPI sapi,
        IBlessingRegistry blessingRegistry,
        IPlayerProgressionDataManager playerProgressionDataManager,
        IReligionManager religionManager)
    {
        _sapi = sapi;
        _blessingRegistry = blessingRegistry;
        _playerProgressionDataManager = playerProgressionDataManager;
        _religionManager = religionManager;
        _specialEffectRegistry = new SpecialEffectRegistry(sapi);
    }

    /// <summary>
    ///     Initializes the blessing effect system
    /// </summary>
    public void Initialize()
    {
        _sapi.Logger.Notification($"{SystemConstants.LogPrefix} {SystemConstants.InfoInitializingBlessingSystem}");

        // Register special effect handlers
        RegisterSpecialEffectHandlers();

        // Initialize special effect registry
        _specialEffectRegistry.Initialize();

        // Register event handlers
        _sapi.Event.PlayerJoin += OnPlayerJoin;
        _playerProgressionDataManager.OnPlayerLeavesReligion += OnPlayerLeavesProgression;

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
        var playerData = _playerProgressionDataManager.GetOrCreatePlayerData(playerUID);

        // Get all unlocked player blessings
        var unlockedBlessingIds = playerData.UnlockedBlessings
            .ToList();

        // Combine stat modifiers from all blessings
        foreach (var blessingId in unlockedBlessingIds)
        {
            var blessing = _blessingRegistry.GetBlessing(blessingId);
            if (blessing != null && blessing.Kind == BlessingKind.Player)
                CombineModifiers(modifiers, blessing.StatModifiers);
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
            if (blessing != null && blessing.Kind == BlessingKind.Religion)
                CombineModifiers(modifiers, blessing.StatModifiers);
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
        if (_religionManager.HasReligion(playerUID))
        {
            var playerReligion = _religionManager.GetPlayerReligion(playerUID);
            if (playerReligion != null)
            {
                var religionModifiers = GetReligionStatModifiers(playerReligion.ReligionUID);
                CombineModifiers(combined, religionModifiers);
            }
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

        var playerReligion = _religionManager.GetPlayerReligion(playerUID);
        if (playerReligion != null) _religionModifierCache.Remove(playerReligion.ReligionUID);

        // Recalculate and apply
        var player = _sapi.World.PlayerByUid(playerUID) as IServerPlayer;
        if (player != null)
        {
            ApplyBlessingsToPlayer(player);
            RefreshSpecialEffects(player);
        }

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

        var playerData = _playerProgressionDataManager.GetOrCreatePlayerData(playerUID);

        // Get player blessings
        var playerBlessingIds = playerData.UnlockedBlessings
            .ToList();

        foreach (var blessingId in playerBlessingIds)
        {
            var blessing = _blessingRegistry.GetBlessing(blessingId);
            if (blessing != null) playerBlessings.Add(blessing);
        }

        // Get religion blessings
        var playerReligion = _religionManager.GetPlayerReligion(playerUID);

        if (playerReligion == null) return (playerBlessings, religionBlessings);

        var religionBlessingIds = playerReligion.UnlockedBlessings
            .Where(kvp => kvp.Value)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var blessingId in religionBlessingIds)
        {
            var blessing = _blessingRegistry.GetBlessing(blessingId);
            if (blessing != null) religionBlessings.Add(blessing);
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

    internal void OnPlayerLeavesProgression(IServerPlayer player, string religionUID)
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
        // Register custom stats first
        RegisterCustomStats(player);

        RefreshPlayerBlessings(player.PlayerUID);
    }

    /// <summary>
    ///     Registers all custom stats for a player with appropriate blend types
    /// </summary>
    private void RegisterCustomStats(IServerPlayer player)
    {
        if (player.Entity == null || player.Entity.Stats == null) return;

        var stats = player.Entity.Stats;

        // Craft (Forge & Craft) - Most are percentage modifiers
        // ToolDurability uses FlatSum (base 0) since we want the raw bonus value in our patch
        RegisterStatIfNeeded(stats, VintageStoryStats.ToolDurability, EnumStatBlendType.FlatSum);
        RegisterStatIfNeeded(stats, VintageStoryStats.OreDropRate, EnumStatBlendType.WeightedSum);
        RegisterStatIfNeeded(stats, VintageStoryStats.ColdResistance, EnumStatBlendType.WeightedSum);
        RegisterStatIfNeeded(stats, VintageStoryStats.RepairCostReduction, EnumStatBlendType.WeightedSum);
        RegisterStatIfNeeded(stats, VintageStoryStats.RepairEfficiency, EnumStatBlendType.WeightedSum);
        RegisterStatIfNeeded(stats, VintageStoryStats.SmithingCostReduction, EnumStatBlendType.WeightedSum);
        RegisterStatIfNeeded(stats, VintageStoryStats.MetalArmorBonus, EnumStatBlendType.WeightedSum);

        // Wild (Hunt & Wild)
        RegisterStatIfNeeded(stats, VintageStoryStats.DoubleHarvestChance,
            EnumStatBlendType.FlatSum); // Additive chance
        RegisterStatIfNeeded(stats, VintageStoryStats.AnimalDamage, EnumStatBlendType.WeightedSum);
        RegisterStatIfNeeded(stats, VintageStoryStats.AnimalDrops, EnumStatBlendType.WeightedSum);
        // FoodSpoilage uses FlatSum (base 0) since we want the raw bonus value in our handler
        RegisterStatIfNeeded(stats, VintageStoryStats.FoodSpoilage, EnumStatBlendType.FlatSum);
        RegisterStatIfNeeded(stats, VintageStoryStats.Satiety, EnumStatBlendType.WeightedSum);
        // TemperatureResistance uses FlatSum (base 0) since we want the raw bonus value in our handler
        RegisterStatIfNeeded(stats, VintageStoryStats.TemperatureResistance, EnumStatBlendType.FlatSum);
        RegisterStatIfNeeded(stats, VintageStoryStats.AnimalHarvestTime, EnumStatBlendType.WeightedSum);
        RegisterStatIfNeeded(stats, VintageStoryStats.ForagingYield, EnumStatBlendType.WeightedSum);

        // Harvest (Agriculture & Cooking)
        RegisterStatIfNeeded(stats, VintageStoryStats.CropYield, EnumStatBlendType.WeightedSum);
        RegisterStatIfNeeded(stats, VintageStoryStats.SeedDropChance, EnumStatBlendType.FlatSum); // Additive chance
        RegisterStatIfNeeded(stats, VintageStoryStats.CookingYield, EnumStatBlendType.WeightedSum);
        RegisterStatIfNeeded(stats, VintageStoryStats.HeatResistance, EnumStatBlendType.WeightedSum);
        RegisterStatIfNeeded(stats, VintageStoryStats.RareCropChance, EnumStatBlendType.FlatSum); // Additive chance
        RegisterStatIfNeeded(stats, VintageStoryStats.WildCropYield, EnumStatBlendType.WeightedSum);
        RegisterStatIfNeeded(stats, VintageStoryStats.CookedFoodSatiety, EnumStatBlendType.WeightedSum);

        // Stone (Earth & Stone)
        RegisterStatIfNeeded(stats, VintageStoryStats.StoneYield, EnumStatBlendType.WeightedSum);
        RegisterStatIfNeeded(stats, VintageStoryStats.ClayYield, EnumStatBlendType.WeightedSum);
        // New Stone utility stats
        RegisterStatIfNeeded(stats, VintageStoryStats.ClayFormingVoxelChance,
            EnumStatBlendType.FlatSum); // Legacy additive chance
        RegisterStatIfNeeded(stats, VintageStoryStats.PotteryBatchCompletionChance,
            EnumStatBlendType.FlatSum); // Additive chance
        RegisterStatIfNeeded(stats, VintageStoryStats.DiggingSpeed, EnumStatBlendType.WeightedSum);
        RegisterStatIfNeeded(stats, VintageStoryStats.ArmorEffectiveness, EnumStatBlendType.WeightedSum);
        RegisterStatIfNeeded(stats, VintageStoryStats.PickDurability, EnumStatBlendType.WeightedSum);
        RegisterStatIfNeeded(stats, VintageStoryStats.FallDamageReduction, EnumStatBlendType.WeightedSum);
        RegisterStatIfNeeded(stats, VintageStoryStats.RareStoneChance, EnumStatBlendType.FlatSum); // Additive chance
        RegisterStatIfNeeded(stats, VintageStoryStats.OreInStoneChance, EnumStatBlendType.FlatSum); // Additive chance
        RegisterStatIfNeeded(stats, VintageStoryStats.GravelYield, EnumStatBlendType.WeightedSum);

        _sapi.Logger.Debug($"{SystemConstants.LogPrefix} Registered custom stats for {player.PlayerName}");
    }

    /// <summary>
    ///     Helper to register a stat only if it doesn't already exist
    /// </summary>
    private void RegisterStatIfNeeded(EntityStats stats, string statName, EnumStatBlendType blendType)
    {
        try
        {
            // Check if stat already exists by attempting to get it
            // If it doesn't exist, Register it
            stats.Register(statName, blendType);
        }
        catch (Exception)
        {
            // Stat already registered, ignore
        }
    }

    /// <summary>
    ///     Removes all blessing modifiers from a player
    /// </summary>
    internal void RemoveBlessingsFromPlayer(IServerPlayer player)
    {
        if (player?.Entity == null) return;

        var agent = player.Entity as EntityAgent;
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
        string? key = statKey switch
        {
            VintageStoryStats.MeleeWeaponsDamage => LocalizationKeys.STAT_MELEE_DAMAGE,
            VintageStoryStats.RangedWeaponsDamage => LocalizationKeys.STAT_RANGED_DAMAGE,
            VintageStoryStats.MeleeWeaponsSpeed => LocalizationKeys.STAT_ATTACK_SPEED,
            VintageStoryStats.MeleeWeaponArmor => LocalizationKeys.STAT_ARMOR,
            VintageStoryStats.ArmorEffectiveness => LocalizationKeys.STAT_ARMOR_EFFECTIVENESS,
            VintageStoryStats.MaxHealthExtraPoints => LocalizationKeys.STAT_MAX_HEALTH,
            VintageStoryStats.WalkSpeed => LocalizationKeys.STAT_WALK_SPEED,
            VintageStoryStats.HealingEffectiveness => LocalizationKeys.STAT_HEALTH_REGEN,
            VintageStoryStats.ToolDurability => LocalizationKeys.STAT_TOOL_DURABILITY,
            VintageStoryStats.OreDropRate => LocalizationKeys.STAT_ORE_YIELD,
            VintageStoryStats.ColdResistance => LocalizationKeys.STAT_COLD_RESISTANCE,
            VintageStoryStats.MiningSpeed => LocalizationKeys.STAT_MINING_SPEED,
            VintageStoryStats.RepairCostReduction => LocalizationKeys.STAT_REPAIR_COST_REDUCTION,
            VintageStoryStats.RepairEfficiency => LocalizationKeys.STAT_REPAIR_EFFICIENCY,
            VintageStoryStats.SmithingCostReduction => LocalizationKeys.STAT_SMITHING_COST_REDUCTION,
            VintageStoryStats.MetalArmorBonus => LocalizationKeys.STAT_METAL_ARMOR_BONUS,
            VintageStoryStats.HungerRate => LocalizationKeys.STAT_HUNGER_RATE,
            VintageStoryStats.ArmorDurabilityLoss => LocalizationKeys.STAT_ARMOR_DURABILITY_LOSS,
            VintageStoryStats.ArmorWalkSpeedAffectedness => LocalizationKeys.STAT_ARMOR_WALK_SPEED,
            VintageStoryStats.PotteryBatchCompletionChance => LocalizationKeys.STAT_POTTERY_BATCH_COMPLETION,
            _ => null
        };

        return key != null ? LocalizationService.Instance.Get(key) : statKey;
    }

    /// <summary>
    ///     Registers all special effect handlers for deities
    /// </summary>
    private void RegisterSpecialEffectHandlers()
    {
        // Craft (Forge & Craft) handlers
        _specialEffectRegistry.RegisterHandler(new KhorasEffectHandlers.PassiveToolRepairEffect());

        // Wild (Hunt & Wild) handlers
        _specialEffectRegistry.RegisterHandler(new LysaEffectHandlers.RareForageChanceEffect());
        _specialEffectRegistry.RegisterHandler(new LysaEffectHandlers.FoodSpoilageEffect());
        _specialEffectRegistry.RegisterHandler(new LysaEffectHandlers.TemperatureResistanceEffect());

        // Harvest (Agriculture & Light) handlers
        _specialEffectRegistry.RegisterHandler(new AethraEffectHandlers.NeverMalnourishedEffect());
        _specialEffectRegistry.RegisterHandler(new AethraEffectHandlers.BlessedMealsEffect());
        _specialEffectRegistry.RegisterHandler(new AethraEffectHandlers.RareCropDiscoveryEffect());
        // Note: FoodSpoilageReduction is shared with Wild and already registered above

        // Stone (Pottery & Clay) handlers
        _specialEffectRegistry.RegisterHandler(new GaiaEffectHandlers.PotteryBatchCompletionEffect());

        _sapi.Logger.Debug($"{SystemConstants.LogPrefix} Registered all special effect handlers");
    }

    /// <summary>
    ///     Refreshes special effects for a player based on their unlocked blessings
    /// </summary>
    private void RefreshSpecialEffects(IServerPlayer player)
    {
        var (playerBlessings, religionBlessings) = GetActiveBlessings(player.PlayerUID);

        // Collect all special effect IDs from active blessings
        var activeEffectIds = new List<string>();

        foreach (var blessing in playerBlessings)
            if (blessing.SpecialEffects != null)
                activeEffectIds.AddRange(blessing.SpecialEffects);

        foreach (var blessing in religionBlessings)
            if (blessing.SpecialEffects != null)
                activeEffectIds.AddRange(blessing.SpecialEffects);

        // Refresh effects via registry
        _specialEffectRegistry.RefreshPlayerEffects(player, activeEffectIds);

        _sapi.Logger.Debug(
            $"{SystemConstants.LogPrefix} Refreshed {activeEffectIds.Count} special effects for {player.PlayerName}");
    }
}