using System;
using System.Collections.Generic;
using PantheonWars.Data;
using PantheonWars.Systems.Interfaces;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace PantheonWars.Systems;

/// <summary>
///     Manages player-religion relationships and player progression
/// </summary>
public class PlayerReligionDataManager : IPlayerReligionDataManager
{
    public delegate void PlayerReligionDataChangedDelegate(IServerPlayer player, string religionUID);
    public delegate void PlayerDataChangedDelegate(string playerUID);

    private const string DATA_KEY = "pantheonwars_playerreligiondata";
    private const int RELIGION_SWITCH_COOLDOWN_DAYS = 7;
    private readonly Dictionary<string, PlayerReligionData> _playerData = new();
    private readonly IReligionManager _religionManager;

    private readonly ICoreServerAPI _sapi;

    // ReSharper disable once ConvertToPrimaryConstructor
    public PlayerReligionDataManager(ICoreServerAPI sapi, IReligionManager religionManager)
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
        _sapi.Logger.Notification("[PantheonWars] Initializing Player Religion Data Manager...");

        // Register event handlers
        _sapi.Event.PlayerJoin += OnPlayerJoin;
        _sapi.Event.PlayerDisconnect += OnPlayerDisconnect;
        _sapi.Event.SaveGameLoaded += OnSaveGameLoaded;
        _sapi.Event.GameWorldSave += OnGameWorldSave;

        _sapi.Logger.Notification("[PantheonWars] Player Religion Data Manager initialized");
    }

    /// <summary>
    ///     Gets or creates player data
    /// </summary>
    public PlayerReligionData GetOrCreatePlayerData(string playerUID)
    {
        if (!_playerData.TryGetValue(playerUID, out var data))
        {
            data = new PlayerReligionData(playerUID);
            _playerData[playerUID] = data;
            _sapi.Logger.Debug($"[PantheonWars] Created new player religion data for {playerUID}");
        }

        return data;
    }

    /// <summary>
    ///     Joins a player to a religion
    /// </summary>
    public void JoinReligion(string playerUID, string religionUID)
    {
        var data = GetOrCreatePlayerData(playerUID);

        // Check if player is already in a religion
        if (data.HasReligion())
            // Leave current religion first
            LeaveReligion(playerUID);

        // Get religion to set active deity
        var religion = _religionManager.GetReligion(religionUID);
        if (religion == null)
        {
            _sapi.Logger.Error($"[PantheonWars] Cannot join non-existent religion: {religionUID}");
            return;
        }

        // Set religion
        data.ReligionUID = religionUID;
        data.LastReligionSwitch = DateTime.UtcNow;
        data.TotalReligionSwitches++;

        // Add player to religion
        _religionManager.AddMember(religionUID, playerUID);

        _sapi.Logger.Notification($"[PantheonWars] Player {playerUID} joined religion {religion.ReligionName}");
    }

    /// <summary>
    ///     Removes a player from their current religion
    /// </summary>
    public void LeaveReligion(string playerUID)
    {
        var data = GetOrCreatePlayerData(playerUID);

        if (!data.HasReligion()) return;

        var religionUID = data.ReligionUID!;

        HandleReligionSwitch(playerUID);
        // Remove from religion
        _religionManager.RemoveMember(religionUID, playerUID);

        OnPlayerLeavesReligion.Invoke((_sapi.World.PlayerByUid(playerUID) as IServerPlayer)!, religionUID);
        // Clear player data
        data.ReligionUID = null;

        _sapi.Logger.Notification($"[PantheonWars] Player {playerUID} left guild");
    }

    /// <summary>
    ///     Checks if a player can switch religions (cooldown check)
    /// </summary>
    public bool CanSwitchReligion(string playerUID)
    {
        var data = GetOrCreatePlayerData(playerUID);

        // First-time joining is always allowed
        if (data.LastReligionSwitch == null) return true;

        // Check cooldown
        var cooldownEnd = data.LastReligionSwitch.Value.AddDays(RELIGION_SWITCH_COOLDOWN_DAYS);
        return DateTime.UtcNow >= cooldownEnd;
    }

    /// <summary>
    ///     Gets remaining cooldown time for religion switching
    /// </summary>
    public TimeSpan? GetSwitchCooldownRemaining(string playerUID)
    {
        var data = GetOrCreatePlayerData(playerUID);

        if (data.LastReligionSwitch == null) return null;

        var cooldownEnd = data.LastReligionSwitch.Value.AddDays(RELIGION_SWITCH_COOLDOWN_DAYS);
        var remaining = cooldownEnd - DateTime.UtcNow;

        return remaining.TotalSeconds > 0 ? remaining : TimeSpan.Zero;
    }

    /// <summary>
    ///     Records a religion switch for cooldown tracking
    /// </summary>
    public void HandleReligionSwitch(string playerUID)
    {
        var data = GetOrCreatePlayerData(playerUID);
        data.TotalReligionSwitches++;
        _sapi.Logger.Notification($"[PantheonWars] Player {playerUID} switched guilds (Total: {data.TotalReligionSwitches})");
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
                var playerData = SerializerUtil.Deserialize<PlayerReligionData>(data);
                if (playerData != null)
                {
                    _playerData[playerUID] = playerData;
                    _sapi.Logger.Debug($"[PantheonWars] Loaded religion data for player {playerUID}");
                }
            }
        }
        catch (Exception ex)
        {
            _sapi.Logger.Error($"[PantheonWars] Failed to load religion data for player {playerUID}: {ex.Message}");
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
                _sapi.Logger.Debug($"[PantheonWars] Saved religion data for player {playerUID}");
            }
        }
        catch (Exception ex)
        {
            _sapi.Logger.Error($"[PantheonWars] Failed to save religion data for player {playerUID}: {ex.Message}");
        }
    }

    /// <summary>
    ///     Loads all player data (called on server start)
    /// </summary>
    internal void LoadAllPlayerData()
    {
        _sapi.Logger.Notification("[PantheonWars] Loading all player religion data...");
        // Player data will be loaded individually as players join
        // This method is here for future batch loading if needed
    }

    /// <summary>
    ///     Saves all player data (called on server save)
    /// </summary>
    internal void SaveAllPlayerData()
    {
        _sapi.Logger.Notification("[PantheonWars] Saving all player religion data...");
        foreach (var playerUID in _playerData.Keys) SavePlayerData(playerUID);
        _sapi.Logger.Notification($"[PantheonWars] Saved religion data for {_playerData.Count} players");
    }

    #endregion
}