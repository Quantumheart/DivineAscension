using System;
using PantheonWars.Data;
using PantheonWars.Models.Enum;
using PantheonWars.Systems.Favor;
using PantheonWars.Systems.Interfaces;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;
using IPlayerDataManager = PantheonWars.Systems.Interfaces.IPlayerDataManager;

namespace PantheonWars.Systems;

/// <summary>
///     Manages divine favor rewards and penalties
/// </summary>
public class FavorSystem : IFavorSystem, IDisposable
{
    private const int BASE_KILL_FAVOR = 10;
    private const int DEATH_PENALTY_FAVOR = 5;
    private const float BASE_FAVOR_PER_HOUR = 0.5f; // Passive favor generation rate
    private const int PASSIVE_TICK_INTERVAL_MS = 1000; // 1 second ticks

    private readonly IDeityRegistry _deityRegistry;
    private readonly IPlayerDataManager _playerDataManager;
    private readonly IPlayerReligionDataManager _playerReligionDataManager;
    private readonly IReligionManager _religionManager;
    private readonly IReligionPrestigeManager _prestigeManager;
    private MiningFavorTracker _miningFavorTracker;
    private AnvilFavorTracker _anvilFavorTracker;
    private SmeltingFavorTracker _smeltingFavorTracker;
    private HuntingFavorTracker _huntingFavorTracker;
    private ForagingFavorTracker _foragingFavorTracker;
    private AethraFavorTracker _aethraFavorTracker;
    private GaiaFavorTracker _gaiaFavorTracker;
    private readonly ICoreServerAPI _sapi;

    public FavorSystem(ICoreServerAPI sapi, IPlayerDataManager playerDataManager,
        IPlayerReligionDataManager playerReligionDataManager, IDeityRegistry deityRegistry,
        IReligionManager religionManager, IReligionPrestigeManager prestigeManager)
    {
        _sapi = sapi;
        _playerDataManager = playerDataManager;
        _playerReligionDataManager = playerReligionDataManager;
        _deityRegistry = deityRegistry;
        _religionManager = religionManager;
        _prestigeManager = prestigeManager;
    }

    /// <summary>
    ///     Initializes the favor system and hooks into game events
    /// </summary>
    public void Initialize()
    {
        _sapi.Logger.Notification("[PantheonWars] Initializing Favor System...");

        // Hook into player death event for PvP favor rewards
        _sapi.Event.PlayerDeath += OnPlayerDeath;

        // Register passive favor generation tick (once per second)
        _sapi.Event.RegisterGameTickListener(OnGameTick, PASSIVE_TICK_INTERVAL_MS);

        _sapi.Logger.Notification("[PantheonWars] Favor System initialized with passive favor generation");

        _miningFavorTracker = new MiningFavorTracker(_playerReligionDataManager, _sapi, this);
        _miningFavorTracker.Initialize();

        _anvilFavorTracker = new AnvilFavorTracker(_playerReligionDataManager, _sapi, this);
        _anvilFavorTracker.Initialize();

        _huntingFavorTracker = new HuntingFavorTracker(_playerReligionDataManager, _sapi, this);
        _huntingFavorTracker.Initialize();

        _foragingFavorTracker = new ForagingFavorTracker(_playerReligionDataManager, _sapi, this);
        _foragingFavorTracker.Initialize();
        
        _aethraFavorTracker = new AethraFavorTracker(_playerReligionDataManager, _sapi, this);
        _aethraFavorTracker.Initialize();

        _gaiaFavorTracker = new GaiaFavorTracker(_playerReligionDataManager, _sapi, this);
        _gaiaFavorTracker.Initialize();

        _smeltingFavorTracker = new SmeltingFavorTracker(_playerReligionDataManager, _sapi, this);
        _smeltingFavorTracker.Initialize();
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
        var attackerReligionData = _playerReligionDataManager.GetOrCreatePlayerData(attacker.PlayerUID);
        var victimReligionData = _playerReligionDataManager.GetOrCreatePlayerData(victim.PlayerUID);

        // Check if attacker has a deity through religion
        if (attackerReligionData.ActiveDeity == DeityType.None) return;

        // Calculate favor reward
        var favorReward = CalculateFavorReward(attackerReligionData.ActiveDeity, victimReligionData.ActiveDeity);

        // Award favor
        _playerReligionDataManager.AddFavor(attacker.PlayerUID, favorReward, $"PvP kill against {victim.PlayerName}");
        attackerReligionData.KillCount++;

        // Get deity for display
        var deity = _deityRegistry.GetDeity(attackerReligionData.ActiveDeity);
        var deityName = deity?.Name ?? attackerReligionData.ActiveDeity.ToString();

        // Notify attacker
        attacker.SendMessage(
            GlobalConstants.GeneralChatGroup,
            $"[Divine Favor] {deityName} rewards you with {favorReward} favor for your victory!",
            EnumChatType.Notification
        );

        // Notify victim
        if (victimReligionData.ActiveDeity != DeityType.None)
        {
            var victimDeity = _deityRegistry.GetDeity(victimReligionData.ActiveDeity);
            var victimDeityName = victimDeity?.Name ?? victimReligionData.ActiveDeity.ToString();
            victim.SendMessage(
                GlobalConstants.GeneralChatGroup,
                $"[Divine Favor] {victimDeityName} is displeased by your defeat.",
                EnumChatType.Notification
            );
        }

        _sapi.Logger.Debug(
            $"[PantheonWars] {attacker.PlayerName} earned {favorReward} favor for killing {victim.PlayerName}");
    }

    /// <summary>
    ///     Applies death penalty to the player
    /// </summary>
    internal void ProcessDeathPenalty(IServerPlayer player)
    {
        var religionData = _playerReligionDataManager.GetOrCreatePlayerData(player.PlayerUID);

        if (religionData.ActiveDeity == DeityType.None) return;

        // Remove favor as penalty (minimum 0)
        var penalty = Math.Min(DEATH_PENALTY_FAVOR, religionData.Favor);
        if (penalty > 0)
        {
            _playerReligionDataManager.RemoveFavor(player.PlayerUID, penalty, "Death penalty");

            player.SendMessage(
                GlobalConstants.GeneralChatGroup,
                $"[Divine Favor] You lost {penalty} favor upon death.",
                EnumChatType.Notification
            );
        }
    }

    /// <summary>
    ///     Calculates favor reward based on deity relationships
    /// </summary>
    internal int CalculateFavorReward(DeityType attackerDeity, DeityType victimDeity)
    {
        var baseFavor = BASE_KILL_FAVOR;

        // No victim deity = standard reward
        if (victimDeity == DeityType.None) return baseFavor;

        // Same deity = reduced favor (discourages infighting)
        if (attackerDeity == victimDeity) return baseFavor / 2;

        // Apply relationship multiplier
        var multiplier = _deityRegistry.GetFavorMultiplier(attackerDeity, victimDeity);
        return (int)(baseFavor * multiplier);
    }

    /// <summary>
    ///     Determines if an activity is deity-themed and should grant prestige
    /// </summary>
    private bool ShouldAwardPrestigeForActivity(DeityType deity, string actionType)
    {
        string actionLower = actionType.ToLowerInvariant();

        // Exclude PvP - already handled by PvPManager
        if (actionLower.Contains("pvp") || actionLower.Contains("kill"))
            return false;

        // Exclude passive favor - not deity-themed activity
        if (actionLower.Contains("passive") || actionLower.Contains("devotion"))
            return false;

        return deity switch
        {
            DeityType.Khoras =>
                actionLower.Contains("mining") ||
                actionLower.Contains("smithing") ||
                actionLower.Contains("smelting") ||
                actionLower.Contains("anvil"),

            DeityType.Lysa =>
                actionLower.Contains("hunting") ||
                actionLower.Contains("foraging") ||
                actionLower.Contains("exploration"),

            DeityType.Aethra =>
                actionLower.Contains("harvest") ||
                actionLower.Contains("planting") ||
                actionLower.Contains("cooking"),

            DeityType.Gaia =>
                actionLower.Contains("pottery") ||
                actionLower.Contains("brick") ||
                actionLower.Contains("clay"),

            _ => false
        };
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
            var religionData = _playerReligionDataManager.GetOrCreatePlayerData(player.PlayerUID);
            if (religionData.ActiveDeity != DeityType.None)
            {
                AwardFavorMessage(player, actionType, amount, religionData);
            }
        }
    }

    /// <summary>
    ///     Awards favor for deity-aligned actions (extensible for future features) by UID
    /// </summary>
    public void AwardFavorForAction(string playerUid, string actionType, int amount)
    {
        var religionData = _playerReligionDataManager.GetOrCreatePlayerData(playerUid);

        if (religionData.ActiveDeity == DeityType.None) return;

        // Award favor (existing logic)
        _playerReligionDataManager.AddFavor(playerUid, amount, actionType);

        // Award prestige if deity-themed activity and player is in a religion
        if (!string.IsNullOrEmpty(religionData.ReligionUID) &&
            ShouldAwardPrestigeForActivity(religionData.ActiveDeity, actionType))
        {
            int prestigeAmount = amount / 3; // 2:1 conversion
            if (prestigeAmount > 0)
            {
                try
                {
                    var playerForName = _sapi.World.PlayerByUid(playerUid);
                    string playerName = playerForName?.PlayerName ?? playerUid;
                    _prestigeManager.AddPrestige(
                        religionData.ReligionUID,
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
        }

        // Try to notify player if server context is available
        var player = _sapi?.World?.PlayerByUid(playerUid) as IServerPlayer;
        if (player != null)
        {
            AwardFavorMessage(player, actionType, amount, religionData);
        }
    }

    private static void AwardFavorMessage(IServerPlayer player, string actionType, int amount,
        PlayerReligionData religionData)
    {
        string deityName = nameof(DeityType.None);
        switch (religionData.ActiveDeity)
        {
            case DeityType.None:
                break;
            case DeityType.Khoras:
                deityName = nameof(DeityType.Khoras);
                break;
            case DeityType.Lysa:
                deityName = nameof(DeityType.Lysa);
                break;
            case DeityType.Aethra:
                deityName = nameof(DeityType.Aethra);
                break;
            case DeityType.Gaia:
                deityName = nameof(DeityType.Gaia);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        player.SendMessage(
            GlobalConstants.GeneralChatGroup,
            $"[Divine Favor] {deityName}: You gained {amount} favor for {actionType}",
            EnumChatType.Notification,
            null
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
        {
            if (player is IServerPlayer serverPlayer)
            {
                AwardPassiveFavor(serverPlayer, dt);
            }
        }
    }

    /// <summary>
    ///     Awards passive favor to a player based on their devotion and time played
    /// </summary>
    internal void AwardPassiveFavor(IServerPlayer player, float dt)
    {
        var religionData = _playerReligionDataManager.GetOrCreatePlayerData(player.PlayerUID);

        if (religionData.ActiveDeity == DeityType.None) return;

        // Calculate in-game hours elapsed this tick
        // dt is in real-time seconds, convert to in-game hours
        float inGameHoursElapsed = dt / _sapi.World.Calendar.HoursPerDay;

        // Calculate base favor for this tick
        float baseFavor = BASE_FAVOR_PER_HOUR * inGameHoursElapsed;

        // Apply multipliers
        float finalFavor = baseFavor * CalculatePassiveFavorMultiplier(player, religionData);

        // Award favor using fractional accumulation
        if (finalFavor >= 0.01f) // Only award when we have at least 0.01 favor
        {
            _playerReligionDataManager.AddFractionalFavor(player.PlayerUID, finalFavor, "Passive devotion");
        }
    }

    /// <summary>
    ///     Calculates the total multiplier for passive favor generation
    /// </summary>
    internal float CalculatePassiveFavorMultiplier(IServerPlayer player, PlayerReligionData religionData)
    {
        float multiplier = 1.0f;

        // Favor rank bonuses (higher ranks gain passive favor faster)
        multiplier *= religionData.FavorRank switch
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
        {
            multiplier *= religion.PrestigeRank switch
            {
                PrestigeRank.Fledgling => 1.0f,
                PrestigeRank.Established => 1.1f,
                PrestigeRank.Renowned => 1.2f,
                PrestigeRank.Legendary => 1.3f,
                PrestigeRank.Mythic => 1.5f,
                _ => 1.0f
            };
        }

        // TODO: Future activity bonuses (prayer, sacred territory, time-of-day, etc.)
        // These will be added in Phase 3

        return multiplier;
    }

    #endregion

    public void Dispose()
    {
        _miningFavorTracker?.Dispose();
        _anvilFavorTracker?.Dispose();
        _smeltingFavorTracker?.Dispose();
        _huntingFavorTracker?.Dispose();
        _foragingFavorTracker?.Dispose();
        _aethraFavorTracker?.Dispose();
        _gaiaFavorTracker?.Dispose();
    }

    public void AwardFavorForAction(IServerPlayer player, string actionType, float amount)
    {
        AwardFavorForAction(player.PlayerUID, actionType, amount);
        // If world lookup path couldn't notify (e.g., headless tests), fall back to direct notify
        if (_sapi?.World?.PlayerByUid(player.PlayerUID) == null)
        {
            var religionData = _playerReligionDataManager.GetOrCreatePlayerData(player.PlayerUID);
            if (religionData.ActiveDeity != DeityType.None)
            {
                AwardFavorMessage(player, actionType, amount, religionData);
            }
        }
    }

    public void AwardFavorForAction(string playerUid, string actionType, float amount)
    {
        var religionData = _playerReligionDataManager.GetOrCreatePlayerData(playerUid);

        if (religionData.ActiveDeity == DeityType.None) return;

        // Award favor (existing logic)
        _playerReligionDataManager.AddFractionalFavor(playerUid, amount, actionType);

        // Award prestige if deity-themed activity and player is in a religion
        if (!string.IsNullOrEmpty(religionData.ReligionUID) &&
            ShouldAwardPrestigeForActivity(religionData.ActiveDeity, actionType))
        {
            float prestigeAmount = amount / 10f; // 10:1 conversion
            if (prestigeAmount >= 1.0f) // Only award whole prestige points
            {
                try
                {
                    var playerForName = _sapi.World.PlayerByUid(playerUid);
                    string playerName = playerForName?.PlayerName ?? playerUid;
                    _prestigeManager.AddPrestige(
                        religionData.ReligionUID,
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
        }

        var player = _sapi?.World?.PlayerByUid(playerUid) as IServerPlayer;
        if (player != null)
        {
            AwardFavorMessage(player, actionType, amount, religionData);
        }
    }

    private void AwardFavorMessage(IServerPlayer player, string actionType, float amount,
        PlayerReligionData religionData)
    {
        string deityName = nameof(DeityType.None);
        switch (religionData.ActiveDeity)
        {
            case DeityType.None:
                break;
            case DeityType.Khoras:
                deityName = nameof(DeityType.Khoras);
                break;
            case DeityType.Lysa:
                deityName = nameof(DeityType.Lysa);
                break;
            case DeityType.Aethra:
                deityName = nameof(DeityType.Aethra);
                break;
            case DeityType.Gaia:
                deityName = nameof(DeityType.Gaia);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        player.SendMessage(
            GlobalConstants.GeneralChatGroup,
            $"[Divine Favor] {deityName}: You gained {amount} favor for {actionType}",
            EnumChatType.Notification,
            null
        );
    }
}