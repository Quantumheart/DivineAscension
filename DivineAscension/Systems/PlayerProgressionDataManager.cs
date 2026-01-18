using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using DivineAscension.API.Interfaces;
using DivineAscension.Configuration;
using DivineAscension.Models.Enum;
using DivineAscension.Systems.Interfaces;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

namespace DivineAscension.Systems;

/// <summary>
///     Manages player-religion relationships and player progression
/// </summary>
public class PlayerProgressionDataManager : IPlayerProgressionDataManager
{
    public delegate void PlayerDataChangedDelegate(string playerUID);

    public delegate void PlayerReligionDataChangedDelegate(IServerPlayer player, string religionUID);

    private const string DATA_KEY = "divineascension_playerprogressiondata";
    private readonly GameBalanceConfig _config;
    private readonly IEventService _eventService;
    private readonly ConcurrentDictionary<string, byte> _initializedPlayers = new();

    private readonly ILogger _logger;
    private readonly IPersistenceService _persistenceService;
    private readonly ConcurrentDictionary<string, PlayerProgressionData> _playerData = new();
    private readonly IReligionManager _religionManager;
    private readonly IWorldService _worldService;

    public PlayerProgressionDataManager(
        ILogger logger,
        IEventService eventService,
        IPersistenceService persistenceService,
        IWorldService worldService,
        IReligionManager religionManager,
        GameBalanceConfig config)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _eventService = eventService ?? throw new ArgumentNullException(nameof(eventService));
        _persistenceService = persistenceService ?? throw new ArgumentNullException(nameof(persistenceService));
        _worldService = worldService ?? throw new ArgumentNullException(nameof(worldService));
        _religionManager = religionManager ?? throw new ArgumentNullException(nameof(religionManager));
        _config = config ?? throw new ArgumentNullException(nameof(config));
    }

    public event PlayerReligionDataChangedDelegate OnPlayerLeavesReligion = null!;
    public event PlayerDataChangedDelegate? OnPlayerDataChanged;

    /// <summary>
    ///     Initializes the player religion data manager
    /// </summary>
    public void Initialize()
    {
        _logger.Notification("[DivineAscension] Initializing Player Religion Data Manager...");

        // Register event handlers
        _eventService.OnPlayerJoin(OnPlayerJoin);
        _eventService.OnPlayerDisconnect(OnPlayerDisconnect);
        _eventService.OnSaveGameLoaded(OnSaveGameLoaded);
        _eventService.OnGameWorldSave(OnGameWorldSave);

        _logger.Notification("[DivineAscension] Player Religion Data Manager initialized");
    }

    public void Dispose()
    {
        _eventService.UnsubscribePlayerJoin(OnPlayerJoin);
        _eventService.UnsubscribePlayerDisconnect(OnPlayerDisconnect);
        _eventService.UnsubscribeSaveGameLoaded(OnSaveGameLoaded);
        _eventService.UnsubscribeGameWorldSave(OnGameWorldSave);
    }

    /// <summary>
    ///     Gets or creates player data.
    ///     Ensures that LoadPlayerData() is called first to load any saved data
    ///     before creating new empty data (fixes race condition on player join).
    /// </summary>
    public PlayerProgressionData GetOrCreatePlayerData(string playerUID)
    {
        if (!_playerData.TryGetValue(playerUID, out var data))
        {
            // Ensure we've attempted to load saved data before creating new data.
            // This fixes a race condition where other systems call GetOrCreatePlayerData()
            // before OnPlayerJoin fires, creating empty data that overwrites saved data.
            if (!_initializedPlayers.ContainsKey(playerUID))
            {
                LoadPlayerData(playerUID);

                // Check again after loading - saved data might now exist
                if (_playerData.TryGetValue(playerUID, out data))
                {
                    return data;
                }
            }

            // No saved data exists - create new data using GetOrAdd for thread safety
            data = _playerData.GetOrAdd(playerUID, uid =>
            {
                _logger.Debug($"[DivineAscension] Created new player progression data for {uid}");
                return new PlayerProgressionData(uid);
            });
        }

        return data;
    }

    /// <summary>
    ///     Tries to get player data without creating it if it doesn't exist.
    /// </summary>
    public bool TryGetPlayerData(string playerUID, out PlayerProgressionData? data)
    {
        return _playerData.TryGetValue(playerUID, out data);
    }

    /// <summary>
    ///     Adds favor to a player
    /// </summary>
    public void AddFavor(string playerUID, int amount, string reason = "")
    {
        var data = GetOrCreatePlayerData(playerUID);
        var oldRank = CalculateFavorRank(data.TotalFavorEarned);

        data.AddFavor(amount);

        if (!string.IsNullOrEmpty(reason))
            _logger.Debug($"[DivineAscension] Player {playerUID} gained {amount} favor: {reason}");

        // Check for rank up
        var newRank = CalculateFavorRank(data.TotalFavorEarned);
        if (newRank > oldRank) SendRankUpNotification(playerUID, newRank);

        // Notify listeners that player data changed (for UI updates)
        OnPlayerDataChanged?.Invoke(playerUID);
    }

    /// <summary>
    ///     Adds fractional favor to a player (for passive favor generation)
    /// </summary>
    public void AddFractionalFavor(string playerUID, float amount, string reason = "")
    {
        var data = GetOrCreatePlayerData(playerUID);
        var oldRank = CalculateFavorRank(data.TotalFavorEarned);
        var oldFavor = data.Favor;

        data.AddFractionalFavor(amount);

        // Only log when favor is actually awarded (when accumulated >= 1)
        if (data.AccumulatedFractionalFavor < amount && !string.IsNullOrEmpty(reason))
            _logger.Debug($"[DivineAscension] Player {playerUID} gained favor: {reason}");

        // Check for rank up
        var newRank = CalculateFavorRank(data.TotalFavorEarned);
        if (newRank > oldRank) SendRankUpNotification(playerUID, newRank);

        // Notify listeners if favor actually changed (UI updates)
        if (data.Favor != oldFavor) OnPlayerDataChanged?.Invoke(playerUID);
    }

    /// <summary>
    ///     Triggers the OnPlayerDataChanged event for the specified player.
    ///     Use this when player data has been modified externally and clients need to be notified.
    /// </summary>
    public void NotifyPlayerDataChanged(string playerUID)
    {
        OnPlayerDataChanged?.Invoke(playerUID);
    }

    /// <summary>
    ///     Removes favor from a player
    /// </summary>
    public bool RemoveFavor(string playerUID, int amount, string reason = "")
    {
        var data = GetOrCreatePlayerData(playerUID);
        var success = data.RemoveFavor(amount);

        if (success && !string.IsNullOrEmpty(reason))
            _logger.Debug($"[DivineAscension] Player {playerUID} spent {amount} favor: {reason}");

        // Notify listeners that player data changed (for UI updates)
        if (success) OnPlayerDataChanged?.Invoke(playerUID);

        return success;
    }

    /// <summary>
    ///     Unlocks a player blessing
    /// </summary>
    public bool UnlockPlayerBlessing(string playerUID, string blessingId)
    {
        var data = GetOrCreatePlayerData(playerUID);

        // Check if already unlocked
        if (data.IsBlessingUnlocked(blessingId)) return false;

        // Unlock the blessing
        data.UnlockBlessing(blessingId);
        _logger.Notification($"[DivineAscension] Player {playerUID} unlocked blessing: {blessingId}");

        return true;
    }

    /// <summary>
    ///     Gets active player blessings (to be expanded in Phase 3.3)
    /// </summary>
    public List<string> GetActivePlayerBlessings(string playerUID)
    {
        var data = GetOrCreatePlayerData(playerUID);
        var unlockedBlessings = new List<string>();

        foreach (var id in data.UnlockedBlessings)
            if (!string.IsNullOrEmpty(id)) // If unlocked
                unlockedBlessings.Add(id);

        return unlockedBlessings;
    }

    /// <summary>
    ///     Sets up player religion data without adding to religion members
    ///     Used for founders who are already added via ReligionData constructor
    /// </summary>
    public void SetPlayerReligionData(string playerUID, string religionUID)
    {
        var data = GetOrCreatePlayerData(playerUID);

        // Get religion to set active deity
        var religion = _religionManager.GetReligion(religionUID);
        if (religion == null)
        {
            _logger.Error($"[DivineAscension] Cannot set religion data for non-existent religion: {religionUID}");
            return;
        }

        _logger.Notification(
            $"[DivineAscension] Set player {playerUID} religion data for {religion.ReligionName}");
    }

    /// <summary>
    ///     Joins a player to a religion
    /// </summary>
    public void JoinReligion(string playerUID, string religionUID)
    {
        var data = GetOrCreatePlayerData(playerUID);

        // Get religion to set active deity
        var religion = _religionManager.GetReligion(religionUID);
        if (religion == null)
        {
            _logger.Error($"[DivineAscension] Cannot join non-existent religion: {religionUID}");
            throw new InvalidOperationException($"Cannot join non-existent religion: {religionUID}");
        }

        _religionManager.AddMember(religionUID, playerUID);
        _logger.Notification($"[DivineAscension] Player {playerUID} joined religion {religion.ReligionName}");
    }

    /// <summary>
    ///     Removes a player from their current religion
    /// </summary>
    public void LeaveReligion(string playerUID)
    {
        if (!HasReligion(playerUID)) return;

        HandleReligionSwitch(playerUID);
        var playerReligion = _religionManager.GetPlayerReligion(playerUID);
        if (playerReligion == null) return;
        // Remove from religion
        _religionManager.RemoveMember(playerReligion.ReligionUID, playerUID);

        OnPlayerLeavesReligion.Invoke(_worldService.GetPlayerByUID(playerUID)!,
            playerReligion.ReligionUID);

        _logger.Notification($"[DivineAscension] Player {playerUID} left religion");
    }

    public bool HasReligion(string playerUid) => _religionManager.HasReligion(playerUid);


    public DeityDomain GetPlayerDeityType(string playerId)
    {
        return _religionManager.GetPlayerActiveDeityDomain(playerId);
    }

    /// <summary>
    ///     Calculates favor rank based on total favor earned using configured thresholds
    /// </summary>
    public FavorRank GetPlayerFavorRank(string playerUID)
    {
        var data = GetOrCreatePlayerData(playerUID);
        return CalculateFavorRank(data.TotalFavorEarned);
    }

    /// <summary>
    ///     Applies switching penalty when changing religions
    /// </summary>
    public void HandleReligionSwitch(string playerUID)
    {
        var data = GetOrCreatePlayerData(playerUID);

        _logger.Notification($"[DivineAscension] Applying religion switch penalty to player {playerUID}");

        // Apply penalty (reset favor and blessings)
        data.ApplySwitchPenalty();
    }

    private FavorRank CalculateFavorRank(int totalFavor)
    {
        if (totalFavor >= _config.AvatarThreshold) return FavorRank.Avatar;
        if (totalFavor >= _config.ChampionThreshold) return FavorRank.Champion;
        if (totalFavor >= _config.ZealotThreshold) return FavorRank.Zealot;
        if (totalFavor >= _config.DiscipleThreshold) return FavorRank.Disciple;
        return FavorRank.Initiate;
    }

    /// <summary>
    ///     Sends rank-up notification to player
    /// </summary>
    internal void SendRankUpNotification(string playerUID, FavorRank newRank)
    {
        var player = _worldService.GetPlayerByUID(playerUID);
        if (player != null)
            player.SendMessage(
                GlobalConstants.GeneralChatGroup,
                $"You have ascended to {newRank} rank!",
                EnumChatType.Notification
            );
    }

    #region Event Handlers

    internal void OnPlayerJoin(IServerPlayer player)
    {
        LoadPlayerData(player.PlayerUID);
    }

    private void OnPlayerDisconnect(IServerPlayer player)
    {
        SavePlayerData(player.PlayerUID);
        // Clean up initialization tracking for memory efficiency
        // Data will be re-loaded from persistence when they rejoin
        _initializedPlayers.TryRemove(player.PlayerUID, out _);
    }

    internal void OnSaveGameLoaded()
    {
        LoadAllPlayerData();
    }

    internal void OnGameWorldSave()
    {
        SaveAllPlayerData();
    }

    #endregion

    #region Persistence

    /// <summary>
    ///     Loads player data from world storage.
    ///     Must be called before GetOrCreatePlayerData() to prevent creating empty data
    ///     that would overwrite saved data.
    /// </summary>
    internal void LoadPlayerData(string playerUID)
    {
        // Skip if already initialized (prevents double-loading)
        if (_initializedPlayers.ContainsKey(playerUID))
        {
            return;
        }

        try
        {
            var playerData = _persistenceService.Load<PlayerProgressionData>($"{DATA_KEY}_{playerUID}");
            if (playerData != null)
            {
                // Validate the data is actually v3 format by checking Id is populated
                // (v2 data will deserialize with empty Id due to different ProtoBuf member numbers)
                if (!string.IsNullOrEmpty(playerData.Id))
                {
                    _playerData[playerUID] = playerData;
                    _logger.Debug(
                        $"[DivineAscension] Loaded data for player {playerUID} (v{playerData.DataVersion})");
                }
                else
                {
                    _logger.Warning(
                        $"[DivineAscension] Player {playerUID} has incompatible data format (v2). Starting fresh.");
                }
            }

            // Mark player as initialized regardless of whether data existed
            // This prevents GetOrCreatePlayerData() from creating empty data before load completes
            _initializedPlayers.TryAdd(playerUID, 0);
        }
        catch (Exception ex)
        {
            _logger.Error($"[DivineAscension] Failed to load data for player {playerUID}: {ex.Message}");
            // Still mark as initialized so we don't block forever on repeated failures
            _initializedPlayers.TryAdd(playerUID, 0);
        }
    }

    /// <summary>
    ///     Saves player data to world storage
    /// </summary>
    internal void SavePlayerData(string playerUID)
    {
        try
        {
            if (_playerData.TryGetValue(playerUID, out var playerData))
            {
                _persistenceService.Save($"{DATA_KEY}_{playerUID}", playerData);
                _logger.Debug($"[DivineAscension] Saved religion data for player {playerUID}");
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"[DivineAscension] Failed to save religion data for player {playerUID}: {ex.Message}");
        }
    }

    /// <summary>
    ///     Loads all player data (called on server start)
    /// </summary>
    internal void LoadAllPlayerData()
    {
        _logger.Notification("[DivineAscension] Loading all player religion data...");
        // Player data will be loaded individually as players join
        // This method is here for future batch loading if needed
    }

    /// <summary>
    ///     Saves all player data (called on server save)
    /// </summary>
    internal void SaveAllPlayerData()
    {
        _logger.Notification("[DivineAscension] Saving all player religion data...");
        // Take a snapshot of keys for thread-safe iteration
        foreach (var playerUID in _playerData.Keys.ToList()) SavePlayerData(playerUID);
        _logger.Notification($"[DivineAscension] Saved religion data for {_playerData.Count} players");
    }

    #endregion
}