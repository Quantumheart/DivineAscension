using System;
using System.Collections.Generic;
using DivineAscension.Configuration;
using DivineAscension.Models.Enum;
using DivineAscension.Systems.Favor;
using DivineAscension.Systems.Interfaces;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

namespace DivineAscension.Systems;

/// <summary>
///     Manages divine favor rewards and penalties
/// </summary>
public class FavorSystem : IFavorSystem
{
    private const int PASSIVE_TICK_INTERVAL_MS = 1000; // 1 second ticks

    private readonly IActivityLogManager _activityLogManager;
    private readonly GameBalanceConfig _config;

    // Batching support for high-frequency favor events (e.g., scythe harvesting)
    private readonly Dictionary<string, PendingFavorData> _pendingFavor = new();
    private readonly IPlayerProgressionDataManager _playerProgressionDataManager;
    private readonly IReligionPrestigeManager _prestigeManager;
    private readonly IReligionManager _religionManager;
    private readonly ICoreServerAPI _sapi;
    private AnvilFavorTracker? _anvilFavorTracker;
    private ConquestFavorTracker? _conquestFavorTracker;
    private ForagingFavorTracker? _foragingFavorTracker;
    private HarvestFavorTracker? _harvestFavorTracker;
    private HuntingFavorTracker? _huntingFavorTracker;
    private MiningFavorTracker? _miningFavorTracker;
    private SkinningFavorTracker? _skinningFavorTracker;
    private SmeltingFavorTracker? _smeltingFavorTracker;
    private StoneFavorTracker? _stoneFavorTracker;

    public FavorSystem(ICoreServerAPI sapi,
        IPlayerProgressionDataManager playerProgressionDataManager,
        IReligionManager religionManager, IReligionPrestigeManager prestigeManager,
        IActivityLogManager activityLogManager,
        GameBalanceConfig config)
    {
        _sapi = sapi;
        _playerProgressionDataManager = playerProgressionDataManager;
        _religionManager = religionManager;
        _prestigeManager = prestigeManager;
        _activityLogManager = activityLogManager;
        _config = config;
    }

    /// <summary>
    ///     Initializes the favor system and hooks into game events
    /// </summary>
    public void Initialize()
    {
        _sapi.Logger.Notification("[DivineAscension] Initializing Favor System...");

        // Hook into player death event for PvP favor rewards
        _sapi.Event.PlayerDeath += OnPlayerDeath;

        // Register passive favor generation tick (once per second)
        _sapi.Event.RegisterGameTickListener(OnGameTick, PASSIVE_TICK_INTERVAL_MS);

        _sapi.Logger.Notification("[DivineAscension] Favor System initialized with passive favor generation");

        _miningFavorTracker = new MiningFavorTracker(_playerProgressionDataManager, _sapi, this);
        _miningFavorTracker.Initialize();

        _anvilFavorTracker = new AnvilFavorTracker(_playerProgressionDataManager, _sapi, this);
        _anvilFavorTracker.Initialize();

        _huntingFavorTracker = new HuntingFavorTracker(_playerProgressionDataManager, _sapi, this);
        _huntingFavorTracker.Initialize();

        _foragingFavorTracker = new ForagingFavorTracker(_playerProgressionDataManager, _sapi, this);
        _foragingFavorTracker.Initialize();

        _harvestFavorTracker = new HarvestFavorTracker(_playerProgressionDataManager, _sapi, this);
        _harvestFavorTracker.Initialize();

        _stoneFavorTracker = new StoneFavorTracker(_playerProgressionDataManager, _sapi, this);
        _stoneFavorTracker.Initialize();

        _smeltingFavorTracker = new SmeltingFavorTracker(_playerProgressionDataManager, _sapi, this);
        _smeltingFavorTracker.Initialize();

        _skinningFavorTracker = new SkinningFavorTracker(_playerProgressionDataManager, _sapi, this);
        _skinningFavorTracker.Initialize();

        _conquestFavorTracker = new ConquestFavorTracker(_playerProgressionDataManager, _sapi, this);
        _conquestFavorTracker.Initialize();
    }

    /// <summary>
    ///     Awards favor for deity-aligned actions (extensible for future features)
    /// </summary>
    public void AwardFavorForAction(IServerPlayer player, string actionType, int amount)
    {
        var domain = _religionManager.GetPlayerActiveDeityDomain(player.PlayerUID);
        AwardFavorCore(player.PlayerUID, actionType, amount, domain);
    }

    public void Dispose()
    {
        _miningFavorTracker?.Dispose();
        _anvilFavorTracker?.Dispose();
        _smeltingFavorTracker?.Dispose();
        _huntingFavorTracker?.Dispose();
        _foragingFavorTracker?.Dispose();
        _harvestFavorTracker?.Dispose();
        _skinningFavorTracker?.Dispose();
        _stoneFavorTracker?.Dispose();
        _conquestFavorTracker?.Dispose();
    }

    public void AwardFavorForAction(IServerPlayer player, string actionType, float amount)
    {
        var domain = _religionManager.GetPlayerActiveDeityDomain(player.PlayerUID);
        AwardFavorCore(player.PlayerUID, actionType, amount, domain);
    }

    public void AwardFavorForAction(string playerUid, string actionType, float amount, DeityDomain deityDomain)
    {
        AwardFavorCore(playerUid, actionType, amount, deityDomain);
    }

    /// <summary>
    ///     Handles player death and awards/penalizes favor
    /// </summary>
    internal void OnPlayerDeath(IServerPlayer deadPlayer, DamageSource damageSource)
    {
        // Check if death was caused by another player (PvP)
        if (damageSource?.SourceEntity is EntityPlayer attackerEntity)
            if (_sapi.World.PlayerByUid(attackerEntity.PlayerUID) is IServerPlayer attackerPlayer &&
                attackerPlayer != deadPlayer)
                ProcessPvPKill(attackerPlayer, deadPlayer);

        // Apply death penalty
        ProcessDeathPenalty(deadPlayer);
    }

    /// <summary>
    ///     Processes PvP kill and awards favor to the attacker
    /// </summary>
    internal void ProcessPvPKill(IServerPlayer attacker, IServerPlayer victim)
    {
        var attackerReligion = _religionManager.GetPlayerReligion(attacker.PlayerUID);
        var victimReligion = _religionManager.GetPlayerReligion(victim.PlayerUID);
        // Check if attacker has a deity through religion
        if (attackerReligion is null || victimReligion is null) return;

        // Calculate favor reward
        var favorReward = CalculateFavorReward(attackerReligion.Domain, victimReligion.Domain);

        // Award favor
        _playerProgressionDataManager.AddFavor(attacker.PlayerUID, favorReward,
            $"PvP kill against {victim.PlayerName}");

        // Get deity for display
        var deityName = attackerReligion.DeityName;

        // Notify attacker
        attacker.SendMessage(
            GlobalConstants.GeneralChatGroup,
            $"[Divine Favor] {deityName} rewards you with {favorReward} favor for your victory!",
            EnumChatType.Notification
        );

        // Notify victim
        if (victimReligion.Domain != DeityDomain.None)
        {
            var victimDeityName = victimReligion.DeityName;
            victim.SendMessage(
                GlobalConstants.GeneralChatGroup,
                $"[Divine Favor] {victimDeityName} is displeased by your defeat.",
                EnumChatType.Notification
            );
        }

        _sapi.Logger.Debug(
            $"[DivineAscension] {attacker.PlayerName} earned {favorReward} favor for killing {victim.PlayerName}");
    }

    /// <summary>
    ///     Applies death penalty to the player
    /// </summary>
    internal void ProcessDeathPenalty(IServerPlayer player)
    {
        var religionData = _playerProgressionDataManager.GetOrCreatePlayerData(player.PlayerUID);
        var deityType = _religionManager.GetPlayerActiveDeityDomain(player.PlayerUID);
        if (deityType == DeityDomain.None) return;

        // Remove favor as penalty (minimum 0)
        var penalty = Math.Min(_config.DeathPenalty, religionData.Favor);
        if (penalty > 0)
        {
            _playerProgressionDataManager.RemoveFavor(player.PlayerUID, penalty, "Death penalty");

            player.SendMessage(
                GlobalConstants.GeneralChatGroup,
                $"[Divine Favor] You lost {penalty} favor upon death.",
                EnumChatType.Notification
            );
        }
    }

    /// <summary>
    ///     Calculates favor reward
    /// </summary>
    internal int CalculateFavorReward(DeityDomain attackerDeity, DeityDomain victimDeity)
    {
        return _config.KillFavorReward;
    }

    /// <summary>
    ///     Determines if an activity is deity-themed and should grant prestige
    /// </summary>
    private bool ShouldAwardPrestigeForActivity(DeityDomain deity, string actionType)
    {
        var actionLower = actionType.ToLowerInvariant();

        // Exclude PvP - already handled by PvPManager
        if (actionLower.Contains("pvp") || actionLower.Contains("kill"))
            return false;

        // Exclude passive favor - not deity-themed activity
        if (actionLower.Contains("passive") || actionLower.Contains("devotion"))
            return false;

        return deity switch
        {
            DeityDomain.Craft =>
                actionLower.Contains("mining") ||
                actionLower.Contains("smithing") ||
                actionLower.Contains("smelting") ||
                actionLower.Contains("anvil"),

            DeityDomain.Wild =>
                actionLower.Contains("hunting") ||
                actionLower.Contains("foraging") ||
                actionLower.Contains("skinning") ||
                actionLower.Contains("exploration"),

            DeityDomain.Conquest =>
                actionLower.Contains("combat") ||
                actionLower.Contains("battle") ||
                actionLower.Contains("fight"),

            DeityDomain.Harvest =>
                actionLower.Contains("harvest") ||
                actionLower.Contains("planting") ||
                actionLower.Contains("cooking"),

            DeityDomain.Stone =>
                actionLower.Contains("pottery") ||
                actionLower.Contains("brick") ||
                actionLower.Contains("clay"),

            _ => false
        };
    }

    /// <summary>
    ///     Awards favor for deity-aligned actions (extensible for future features) by UID
    /// </summary>
    internal void AwardFavorForAction(string playerUid, string actionType, int amount)
    {
        var domain = _religionManager.GetPlayerActiveDeityDomain(playerUid);
        AwardFavorCore(playerUid, actionType, amount, domain);
    }

    /// <summary>
    ///     Sends favor notification to player (generic to handle int/float)
    /// </summary>
    private void AwardFavorMessage<T>(IServerPlayer player, string actionType, T amount, DeityDomain deityDomain)
        where T : struct
    {
        var deityName = deityDomain switch
        {
            DeityDomain.Craft => nameof(DeityDomain.Craft),
            DeityDomain.Wild => nameof(DeityDomain.Wild),
            DeityDomain.Conquest => nameof(DeityDomain.Conquest),
            DeityDomain.Harvest => nameof(DeityDomain.Harvest),
            DeityDomain.Stone => nameof(DeityDomain.Stone),
            _ => nameof(DeityDomain.None)
        };

        player.SendMessage(
            GlobalConstants.GeneralChatGroup,
            $"[Divine Favor] {deityName}: You gained {amount} favor for {actionType}",
            EnumChatType.Notification
        );
    }

    /// <summary>
    ///     Core favor award implementation with activity logging.
    ///     All public overloads delegate to this method.
    /// </summary>
    private void AwardFavorCore(string playerUid, string actionType, float amount, DeityDomain deityDomain)
    {
        if (deityDomain == DeityDomain.None) return;

        // 1. Award favor
        _playerProgressionDataManager.AddFractionalFavor(playerUid, amount, actionType);

        // 2. Check if player is in religion and should receive prestige
        var playerReligion = _religionManager.GetPlayerReligion(playerUid);
        if (string.IsNullOrEmpty(playerReligion?.ReligionUID))
        {
            // Not in religion - send player message and return
            NotifyPlayer(playerUid, actionType, amount, deityDomain);
            return;
        }

        // 3. Check if activity is deity-themed (determines both prestige and logging)
        var isDeityThemed = ShouldAwardPrestigeForActivity(deityDomain, actionType);
        if (!isDeityThemed)
        {
            // Not deity-themed - send player message and return (no prestige, no logging)
            NotifyPlayer(playerUid, actionType, amount, deityDomain);
            return;
        }

        try
        {
            var playerForName = _sapi.World.PlayerByUid(playerUid);
            var playerName = playerForName?.PlayerName ?? playerUid;

            // 4. Award fractional prestige (1:1 favor-to-prestige conversion)
            _prestigeManager.AddFractionalPrestige(
                playerReligion.ReligionUID,
                amount,
                $"{actionType} by {playerName}"
            );

            // 5. Log activity for ALL deity-themed activities
            // Note: prestigeAmount in log shows the fractional amount truncated for display
            _activityLogManager.LogActivity(
                playerReligion.ReligionUID,
                playerUid,
                actionType,
                (int)Math.Floor(amount),
                (int)Math.Floor(amount), // Show favor amount as prestige (1:1 ratio)
                deityDomain
            );
        }
        catch (Exception ex)
        {
            _sapi.Logger.Error($"[FavorSystem] Failed to award prestige/log activity: {ex.Message}");
        }

        // 7. Notify player
        NotifyPlayer(playerUid, actionType, amount, deityDomain);
    }

    /// <summary>
    ///     Sends favor notification to player if online
    /// </summary>
    private void NotifyPlayer(string playerUid, string actionType, float amount, DeityDomain deityDomain)
    {
        var player = _sapi?.World?.PlayerByUid(playerUid) as IServerPlayer;
        if (player != null)
        {
            AwardFavorMessage(player, actionType, amount, deityDomain);
        }
    }

    #region Passive Favor Generation

    /// <summary>
    ///     Game tick handler for passive favor generation
    /// </summary>
    internal void OnGameTick(float dt)
    {
        // Flush any pending batched favor awards (from scythe harvesting, etc.)
        FlushPendingFavor();

        // Award passive favor to all online players with deities
        foreach (var player in _sapi.World.AllOnlinePlayers)
            if (player is IServerPlayer serverPlayer)
                AwardPassiveFavor(serverPlayer, dt);
    }

    /// <summary>
    ///     Awards passive favor to a player based on their devotion and time played
    /// </summary>
    internal void AwardPassiveFavor(IServerPlayer player, float dt)
    {
        var religionData = _playerProgressionDataManager.GetOrCreatePlayerData(player.PlayerUID);

        if (_religionManager.GetPlayerActiveDeityDomain(player.PlayerUID) == DeityDomain.None) return;

        // Calculate in-game hours elapsed this tick
        // dt is in real-time seconds, convert to in-game hours
        var inGameHoursElapsed = dt / _sapi.World.Calendar.HoursPerDay;

        // Calculate base favor for this tick
        var baseFavor = _config.PassiveFavorRate * inGameHoursElapsed;

        // Apply multipliers
        var finalFavor = baseFavor * CalculatePassiveFavorMultiplier(player, religionData);

        // Award favor using fractional accumulation
        if (finalFavor >= 0.01f) // Only award when we have at least 0.01 favor
            _playerProgressionDataManager.AddFractionalFavor(player.PlayerUID, finalFavor, "Passive devotion");
    }

    /// <summary>
    ///     Calculates the total multiplier for passive favor generation
    /// </summary>
    internal float CalculatePassiveFavorMultiplier(IServerPlayer player, PlayerProgressionData playerProgressionData)
    {
        var multiplier = 1.0f;

        // Favor rank bonuses (higher ranks gain passive favor faster)
        var playerFavorRank = _playerProgressionDataManager.GetPlayerFavorRank(player.PlayerUID);
        multiplier *= playerFavorRank switch
        {
            FavorRank.Initiate => _config.InitiateMultiplier,
            FavorRank.Disciple => _config.DiscipleMultiplier,
            FavorRank.Zealot => _config.ZealotMultiplier,
            FavorRank.Champion => _config.ChampionMultiplier,
            FavorRank.Avatar => _config.AvatarMultiplier,
            _ => 1.0f
        };

        // Religion prestige bonuses (active religions provide better passive gains)
        var religion = _religionManager.GetPlayerReligion(player.PlayerUID);
        if (religion != null)
            multiplier *= religion.PrestigeRank switch
            {
                PrestigeRank.Fledgling => _config.FledglingMultiplier,
                PrestigeRank.Established => _config.EstablishedMultiplier,
                PrestigeRank.Renowned => _config.RenownedMultiplier,
                PrestigeRank.Legendary => _config.LegendaryMultiplier,
                PrestigeRank.Mythic => _config.MythicMultiplier,
                _ => 1.0f
            };

        // TODO: Future activity bonuses (prayer, sacred territory, time-of-day, etc.)
        // These will be added in Phase 3

        return multiplier;
    }

    #endregion

    #region Favor Batching

    /// <summary>
    ///     Queues favor for batched processing. Use this for high-frequency events like scythe harvesting.
    ///     Favor is accumulated and applied on the next game tick to avoid per-block overhead.
    /// </summary>
    public void QueueFavorForAction(IServerPlayer player, string actionType, float amount, DeityDomain deityDomain)
    {
        if (deityDomain == DeityDomain.None) return;

        var uid = player.PlayerUID;
        if (_pendingFavor.TryGetValue(uid, out var pending))
        {
            _pendingFavor[uid] = pending with { Amount = pending.Amount + amount };
        }
        else
        {
            _pendingFavor[uid] = new PendingFavorData(amount, actionType, deityDomain);
        }
    }

    /// <summary>
    ///     Flushes all pending favor awards. Called once per game tick.
    /// </summary>
    private void FlushPendingFavor()
    {
        if (_pendingFavor.Count == 0) return;

        foreach (var (uid, pending) in _pendingFavor)
        {
            // Use core method instead of direct calls
            AwardFavorCore(uid, pending.ActionType, pending.Amount, pending.Domain);
        }

        _pendingFavor.Clear();
    }

    /// <summary>
    ///     Holds pending favor data for batching
    /// </summary>
    private record PendingFavorData(float Amount, string ActionType, DeityDomain Domain);

    #endregion
}