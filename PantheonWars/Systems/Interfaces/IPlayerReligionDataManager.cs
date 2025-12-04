using System;
using System.Collections.Generic;
using PantheonWars.Data;

namespace PantheonWars.Systems.Interfaces;

public interface IPlayerReligionDataManager : IDisposable
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
    ///     Adds favor to a player
    /// </summary>
    void AddFavor(string playerUID, int amount, string reason = "");

    /// <summary>
    ///     Updates favor rank for a player
    /// </summary>
    void UpdateFavorRank(string playerUID);

    /// <summary>
    ///     Unlocks a player blessing
    /// </summary>
    bool UnlockPlayerBlessing(string playerUID, string blessingId);

    /// <summary>
    ///     Gets active player blessings (to be expanded in Phase 3.3)
    /// </summary>
    List<string> GetActivePlayerBlessings(string playerUID);

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

    /// <summary>
    ///     Applies switching penalty when changing religions
    /// </summary>
    void HandleReligionSwitch(string playerUID);

    /// <summary>
    ///     Removes favor from the player
    /// </summary>
    /// <param name="playerUID"></param>
    /// <param name="amount"></param>
    /// <param name="reason"></param>
    /// <returns></returns>
    bool RemoveFavor(string playerUID, int amount, string reason = "");

    /// <summary>
    ///     Adds fractional favor to a player (for passive favor generation)
    /// </summary>
    /// <param name="playerUID">Player unique identifier</param>
    /// <param name="amount">Amount of favor to add</param>
    /// <param name="reason">Reason for adding favor (optional)</param>
    void AddFractionalFavor(string playerUID, float amount, string reason = "");
}