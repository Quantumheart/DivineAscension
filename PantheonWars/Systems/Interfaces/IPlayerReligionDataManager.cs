using System;
using PantheonWars.Data;

namespace PantheonWars.Systems.Interfaces;

/// <summary>
///     Interface for managing player-religion relationships.
///     In the religion-only system, blessings come from the religion, not individual players.
/// </summary>
public interface IPlayerReligionDataManager
{
    event PlayerReligionDataManager.PlayerReligionDataChangedDelegate OnPlayerLeavesReligion;

    event PlayerReligionDataManager.PlayerDataChangedDelegate OnPlayerDataChanged;

    /// <summary>
    ///     Initializes the player religion data manager
    /// </summary>
    void Initialize();

    /// <summary>
    ///     Gets or creates player data
    /// </summary>
    PlayerReligionData GetOrCreatePlayerData(string playerUID);

    /// <summary>
    ///     Joins a player to a religion
    /// </summary>
    void JoinReligion(string playerUID, string religionUID);

    /// <summary>
    ///     Removes a player from their current religion
    /// </summary>
    void LeaveReligion(string playerUID);

    /// <summary>
    ///     Checks if a player can switch religions (cooldown check)
    /// </summary>
    bool CanSwitchReligion(string playerUID);

    /// <summary>
    ///     Gets remaining cooldown time for religion switching
    /// </summary>
    TimeSpan? GetSwitchCooldownRemaining(string playerUID);
}
