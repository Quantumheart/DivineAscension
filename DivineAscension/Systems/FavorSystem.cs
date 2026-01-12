using System;
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
    private const int BASE_KILL_FAVOR = 10;
    private const int DEATH_PENALTY_FAVOR = 50;
    private const float BASE_FAVOR_PER_HOUR = 0.5f; // Passive favor generation rate
    private const int PASSIVE_TICK_INTERVAL_MS = 1000; // 1 second ticks

    private readonly IPlayerProgressionDataManager _playerProgressionDataManager;
    private readonly IReligionPrestigeManager _prestigeManager;
    private readonly IReligionManager _religionManager;
    private readonly ICoreServerAPI _sapi;
    private AnvilFavorTracker? _anvilFavorTracker;
    private ForagingFavorTracker? _foragingFavorTracker;
    private AethraFavorTracker? _harvestFavorTracker;
    private HuntingFavorTracker? _huntingFavorTracker;
    private MiningFavorTracker? _miningFavorTracker;
    private SmeltingFavorTracker? _smeltingFavorTracker;
    private GaiaFavorTracker? _stoneFavorTracker;

    public FavorSystem(ICoreServerAPI sapi,
        IPlayerProgressionDataManager playerProgressionDataManager,
        IReligionManager religionManager, IReligionPrestigeManager prestigeManager)
    {
        _sapi = sapi;
        _playerProgressionDataManager = playerProgressionDataManager;
        _religionManager = religionManager;
        _prestigeManager = prestigeManager;
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

        _harvestFavorTracker = new AethraFavorTracker(_playerProgressionDataManager, _sapi, this);
        _harvestFavorTracker.Initialize();

        _stoneFavorTracker = new GaiaFavorTracker(_playerProgressionDataManager, _sapi, this);
        _stoneFavorTracker.Initialize();

        _smeltingFavorTracker = new SmeltingFavorTracker(_playerProgressionDataManager, _sapi, this);
        _smeltingFavorTracker.Initialize();
    }

    /// <summary>
    ///     Awards favor for deity-aligned actions (extensible for future features)
    /// </summary>
    public void AwardFavorForAction(IServerPlayer player, string actionType, int amount)
    {
        AwardFavorForAction(player.PlayerUID, actionType, amount);
        // If world lookup path couldn't notify (e.g., headless tests), fall back to direct notify
        if (_sapi?.World?.PlayerByUid(player.PlayerUID) == null)
        {
            var deityType = _religionManager.GetPlayerActiveDeityDomain(player.PlayerUID);
            if (deityType != DeityDomain.None) AwardFavorMessage(player, actionType, amount, deityType);
        }
    }

    public void Dispose()
    {
        _miningFavorTracker?.Dispose();
        _anvilFavorTracker?.Dispose();
        _smeltingFavorTracker?.Dispose();
        _huntingFavorTracker?.Dispose();
        _foragingFavorTracker?.Dispose();
        _harvestFavorTracker?.Dispose();
        _stoneFavorTracker?.Dispose();
    }

    public void AwardFavorForAction(IServerPlayer player, string actionType, float amount)
    {
        var deityType = _religionManager.GetPlayerActiveDeityDomain(player.PlayerUID);
        AwardFavorForAction(player.PlayerUID, actionType, amount,
            deityType); // If world lookup path couldn't notify (e.g., headless tests), fall back to direct notify
        if (_sapi?.World?.PlayerByUid(player.PlayerUID) == null)
        {
            if (deityType != DeityDomain.None) AwardFavorMessage(player, actionType, amount, deityType);
        }
    }

    public void AwardFavorForAction(string playerUid, string actionType, float amount, DeityDomain deityDomain)
    {
        if (deityDomain == DeityDomain.None) return;

        // Award favor (existing logic)
        _playerProgressionDataManager.AddFractionalFavor(playerUid, amount, actionType);

        // Award prestige if deity-themed activity and player is in a religion
        var playerReligion = _religionManager.GetPlayerReligion(playerUid);
        if (!string.IsNullOrEmpty(playerReligion!.ReligionUID) &&
            ShouldAwardPrestigeForActivity(_religionManager.GetPlayerActiveDeityDomain(playerUid), actionType))
        {
            var prestigeAmount = amount / 10f; // 10:1 conversion
            if (prestigeAmount >= 1.0f) // Only award whole prestige points
                try
                {
                    var playerForName = _sapi.World.PlayerByUid(playerUid);
                    var playerName = playerForName?.PlayerName ?? playerUid;
                    _prestigeManager.AddPrestige(
                        playerReligion.ReligionUID,
                        (int)prestigeAmount,
                        $"{actionType} by {playerName}"
                    );
                }
                catch (Exception ex)
                {
                    _sapi.Logger.Error($"[FavorSystem] Failed to award prestige: {ex.Message}");
                    // Don't fail favor award if prestige fails
                }
        }

        var player = _sapi?.World?.PlayerByUid(playerUid) as IServerPlayer;
        if (player != null) AwardFavorMessage(player, actionType, amount, deityDomain);
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
        var penalty = Math.Min(DEATH_PENALTY_FAVOR, religionData.Favor);
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
        return BASE_KILL_FAVOR;
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
                actionLower.Contains("exploration"),

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
        if (_religionManager.GetPlayerActiveDeityDomain(playerUid) == DeityDomain.None) return;

        // Award favor (existing logic)
        _playerProgressionDataManager.AddFavor(playerUid, amount, actionType);

        // Award prestige if deity-themed activity and player is in a religion
        var playerReligion = _religionManager.GetPlayerReligion(playerUid);
        if (!string.IsNullOrEmpty(playerReligion!.ReligionUID) &&
            ShouldAwardPrestigeForActivity(_religionManager.GetPlayerActiveDeityDomain(playerUid), actionType))
        {
            var prestigeAmount = amount / 3; // 2:1 conversion
            if (prestigeAmount > 0)
                try
                {
                    var playerForName = _sapi.World.PlayerByUid(playerUid);
                    var playerName = playerForName?.PlayerName ?? playerUid;
                    _prestigeManager.AddPrestige(
                        playerReligion.ReligionUID,
                        prestigeAmount,
                        $"{actionType} by {playerName}"
                    );
                }
                catch (Exception ex)
                {
                    _sapi.Logger.Error($"[FavorSystem] Failed to award prestige: {ex.Message}");
                    // Don't fail favor award if prestige fails
                }
        }

        // Try to notify player if server context is available
        var player = _sapi?.World?.PlayerByUid(playerUid) as IServerPlayer;
        if (player != null)
            AwardFavorMessage(player, actionType, amount, _religionManager.GetPlayerActiveDeityDomain(playerUid));
    }

    private static void AwardFavorMessage(IServerPlayer player, string actionType, int amount,
        DeityDomain deityDomain)
    {
        var deityName = nameof(DeityDomain.None);
        switch (deityDomain)
        {
            case DeityDomain.None:
                break;
            case DeityDomain.Craft:
                deityName = nameof(DeityDomain.Craft);
                break;
            case DeityDomain.Wild:
                deityName = nameof(DeityDomain.Wild);
                break;
            case DeityDomain.Harvest:
                deityName = nameof(DeityDomain.Harvest);
                break;
            case DeityDomain.Stone:
                deityName = nameof(DeityDomain.Stone);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        player.SendMessage(
            GlobalConstants.GeneralChatGroup,
            $"[Divine Favor] {deityName}: You gained {amount} favor for {actionType}",
            EnumChatType.Notification
        );
    }

    private void AwardFavorMessage(IServerPlayer player, string actionType, float amount, DeityDomain deityDomain)
    {
        var deityName = nameof(DeityDomain.None);
        switch (deityDomain)
        {
            case DeityDomain.None:
                break;
            case DeityDomain.Craft:
                deityName = nameof(DeityDomain.Craft);
                break;
            case DeityDomain.Wild:
                deityName = nameof(DeityDomain.Wild);
                break;
            case DeityDomain.Harvest:
                deityName = nameof(DeityDomain.Harvest);
                break;
            case DeityDomain.Stone:
                deityName = nameof(DeityDomain.Stone);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        player.SendMessage(
            GlobalConstants.GeneralChatGroup,
            $"[Divine Favor] {deityName}: You gained {amount} favor for {actionType}",
            EnumChatType.Notification
        );
    }

    #region Passive Favor Generation

    /// <summary>
    ///     Game tick handler for passive favor generation
    /// </summary>
    internal void OnGameTick(float dt)
    {
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
        var baseFavor = BASE_FAVOR_PER_HOUR * inGameHoursElapsed;

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
        multiplier *= playerProgressionData.FavorRank switch
        {
            FavorRank.Initiate => 1.0f,
            FavorRank.Disciple => 1.1f,
            FavorRank.Zealot => 1.2f,
            FavorRank.Champion => 1.3f,
            FavorRank.Avatar => 1.5f,
            _ => 1.0f
        };

        // Religion prestige bonuses (active religions provide better passive gains)
        var religion = _religionManager.GetPlayerReligion(player.PlayerUID);
        if (religion != null)
            multiplier *= religion.PrestigeRank switch
            {
                PrestigeRank.Fledgling => 1.0f,
                PrestigeRank.Established => 1.1f,
                PrestigeRank.Renowned => 1.2f,
                PrestigeRank.Legendary => 1.3f,
                PrestigeRank.Mythic => 1.5f,
                _ => 1.0f
            };

        // TODO: Future activity bonuses (prayer, sacred territory, time-of-day, etc.)
        // These will be added in Phase 3

        return multiplier;
    }

    #endregion
}