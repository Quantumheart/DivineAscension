using System;
using System.Collections.Generic;
using DivineAscension.Models.Enum;
using DivineAscension.Systems.Interfaces;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace DivineAscension.Systems;

/// <summary>
///     Manages player-religion relationships and player progression
/// </summary>
public class PlayerProgressionDataManager : IPlayerProgressionDataManager
{
    public delegate void PlayerDataChangedDelegate(string playerUID);

    public delegate void PlayerReligionDataChangedDelegate(IServerPlayer player, string religionUID);

    private const string DATA_KEY = "divineascension_playerprogressiondata";
    private readonly Dictionary<string, PlayerProgressionData> _playerData = new();
    private readonly IReligionManager _religionManager;

    private readonly ICoreServerAPI _sapi;

    // ReSharper disable once ConvertToPrimaryConstructor
    public PlayerProgressionDataManager(ICoreServerAPI sapi, IReligionManager religionManager)
    {
        _sapi = sapi;
        _religionManager = religionManager;
    }

    public event PlayerReligionDataChangedDelegate OnPlayerLeavesReligion = null!;
    public event PlayerDataChangedDelegate? OnPlayerDataChanged;

    /// <summary>
    ///     Initializes the player religion data manager
    /// </summary>
    public void Initialize()
    {
        _sapi.Logger.Notification("[DivineAscension] Initializing Player Religion Data Manager...");

        // Register event handlers
        _sapi.Event.PlayerJoin += OnPlayerJoin;
        _sapi.Event.PlayerDisconnect += OnPlayerDisconnect;
        _sapi.Event.SaveGameLoaded += OnSaveGameLoaded;
        _sapi.Event.GameWorldSave += OnGameWorldSave;

        _sapi.Logger.Notification("[DivineAscension] Player Religion Data Manager initialized");
    }

    public void Dispose()
    {
        _sapi.Event.PlayerJoin -= OnPlayerJoin;
        _sapi.Event.PlayerDisconnect -= OnPlayerDisconnect;
        _sapi.Event.SaveGameLoaded -= OnSaveGameLoaded;
        _sapi.Event.GameWorldSave -= OnGameWorldSave;
    }

    /// <summary>
    ///     Gets or creates player data
    /// </summary>
    public PlayerProgressionData GetOrCreatePlayerData(string playerUID)
    {
        if (!_playerData.TryGetValue(playerUID, out var data))
        {
            data = new PlayerProgressionData(playerUID);
            _playerData[playerUID] = data;
            _sapi.Logger.Debug($"[DivineAscension] Created new player progression data for {playerUID}");
        }

        return data;
    }

    /// <summary>
    ///     Adds favor to a player
    /// </summary>
    public void AddFavor(string playerUID, int amount, string reason = "")
    {
        var data = GetOrCreatePlayerData(playerUID);
        var oldRank = data.FavorRank;

        data.AddFavor(amount);

        if (!string.IsNullOrEmpty(reason))
            _sapi.Logger.Debug($"[DivineAscension] Player {playerUID} gained {amount} favor: {reason}");

        // Check for rank up
        if (data.FavorRank > oldRank) SendRankUpNotification(playerUID, data.FavorRank);

        // Notify listeners that player data changed (for UI updates)
        OnPlayerDataChanged?.Invoke(playerUID);
    }

    /// <summary>
    ///     Adds fractional favor to a player (for passive favor generation)
    /// </summary>
    public void AddFractionalFavor(string playerUID, float amount, string reason = "")
    {
        var data = GetOrCreatePlayerData(playerUID);
        var oldRank = data.FavorRank;
        var oldFavor = data.Favor;

        data.AddFractionalFavor(amount);

        // Only log when favor is actually awarded (when accumulated >= 1)
        if (data.AccumulatedFractionalFavor < amount && !string.IsNullOrEmpty(reason))
            _sapi.Logger.Debug($"[DivineAscension] Player {playerUID} gained favor: {reason}");

        // Check for rank up
        if (data.FavorRank > oldRank) SendRankUpNotification(playerUID, data.FavorRank);

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
            _sapi.Logger.Debug($"[DivineAscension] Player {playerUID} spent {amount} favor: {reason}");

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
        _sapi.Logger.Notification($"[DivineAscension] Player {playerUID} unlocked blessing: {blessingId}");

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
            _sapi.Logger.Error($"[DivineAscension] Cannot set religion data for non-existent religion: {religionUID}");
            return;
        }

        _sapi.Logger.Notification(
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
            _sapi.Logger.Error($"[DivineAscension] Cannot join non-existent religion: {religionUID}");
            throw new InvalidOperationException($"Cannot join non-existent religion: {religionUID}");
        }

        _religionManager.AddMember(religionUID, playerUID);
        _sapi.Logger.Notification($"[DivineAscension] Player {playerUID} joined religion {religion.ReligionName}");
    }

    /// <summary>
    ///     Removes a player from their current religion
    /// </summary>
    public void LeaveReligion(string playerUID)
    {
        var data = GetOrCreatePlayerData(playerUID);

        if (!HasReligion(playerUID)) return;

        HandleReligionSwitch(playerUID);
        var playerReligion = _religionManager.GetPlayerReligion(playerUID);
        // Remove from religion
        _religionManager.RemoveMember(playerReligion.ReligionUID, playerUID);

        OnPlayerLeavesReligion.Invoke((_sapi.World.PlayerByUid(playerUID) as IServerPlayer)!,
            playerReligion.ReligionUID);
        // Clear player data
        data.Favor = 0;
        data.TotalFavorEarned = 0;

        _sapi.Logger.Notification($"[DivineAscension] Player {playerUID} left religion");
    }

    public bool HasReligion(string playerUid) => _religionManager.HasReligion(playerUid);


    public DeityType GetPlayerDeityType(string playerId)
    {
        return _religionManager.GetPlayerActiveDeity(playerId);
    }

    /// <summary>
    ///     Applies switching penalty when changing religions
    /// </summary>
    public void HandleReligionSwitch(string playerUID)
    {
        var data = GetOrCreatePlayerData(playerUID);

        _sapi.Logger.Notification($"[DivineAscension] Applying religion switch penalty to player {playerUID}");

        // Apply penalty (reset favor and blessings)
        data.ApplySwitchPenalty();
    }

    /// <summary>
    ///     Sends rank-up notification to player
    /// </summary>
    internal void SendRankUpNotification(string playerUID, FavorRank newRank)
    {
        var player = _sapi.World.PlayerByUid(playerUID) as IServerPlayer;
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
    ///     Loads player data from world storage
    /// </summary>
    internal void LoadPlayerData(string playerUID)
    {
        try
        {
            var data = _sapi.WorldManager.SaveGame.GetData($"{DATA_KEY}_{playerUID}");
            if (data != null)
            {
                var playerData = SerializerUtil.Deserialize<PlayerProgressionData>(data);
                if (playerData != null)
                {
                    _playerData[playerUID] = playerData;
                    _sapi.Logger.Debug($"[DivineAscension] Loaded religion data for player {playerUID}");
                }
            }
        }
        catch (Exception ex)
        {
            _sapi.Logger.Error($"[DivineAscension] Failed to load religion data for player {playerUID}: {ex.Message}");
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
                var data = SerializerUtil.Serialize(playerData);
                _sapi.WorldManager.SaveGame.StoreData($"{DATA_KEY}_{playerUID}", data);
                _sapi.Logger.Debug($"[DivineAscension] Saved religion data for player {playerUID}");
            }
        }
        catch (Exception ex)
        {
            _sapi.Logger.Error($"[DivineAscension] Failed to save religion data for player {playerUID}: {ex.Message}");
        }
    }

    /// <summary>
    ///     Loads all player data (called on server start)
    /// </summary>
    internal void LoadAllPlayerData()
    {
        _sapi.Logger.Notification("[DivineAscension] Loading all player religion data...");
        // Player data will be loaded individually as players join
        // This method is here for future batch loading if needed
    }

    /// <summary>
    ///     Saves all player data (called on server save)
    /// </summary>
    internal void SaveAllPlayerData()
    {
        _sapi.Logger.Notification("[DivineAscension] Saving all player religion data...");
        foreach (var playerUID in _playerData.Keys) SavePlayerData(playerUID);
        _sapi.Logger.Notification($"[DivineAscension] Saved religion data for {_playerData.Count} players");
    }

    #endregion
}