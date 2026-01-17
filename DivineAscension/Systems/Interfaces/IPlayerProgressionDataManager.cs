using System;
using System.Collections.Generic;
using DivineAscension.Data;
using DivineAscension.Models.Enum;

namespace DivineAscension.Systems.Interfaces;

public interface IPlayerProgressionDataManager : IDisposable
{
    event PlayerProgressionDataManager.PlayerReligionDataChangedDelegate OnPlayerLeavesReligion;

    event PlayerProgressionDataManager.PlayerDataChangedDelegate OnPlayerDataChanged;

    /// <summary>
    ///     Initializes the player religion data manager
    /// </summary>
    void Initialize();

    /// <summary>
    ///     Gets or creates player data
    /// </summary>
    PlayerProgressionData GetOrCreatePlayerData(string playerUID);

    /// <summary>
    ///     Tries to get player data without creating it if it doesn't exist.
    ///     Use this when you need to check if data exists without triggering creation.
    /// </summary>
    /// <returns>True if data exists and was retrieved, false otherwise</returns>
    bool TryGetPlayerData(string playerUID, out PlayerProgressionData? data);

    /// <summary>
    ///     Adds favor to a player
    /// </summary>
    void AddFavor(string playerUID, int amount, string reason = "");

    /// <summary>
    ///     Unlocks a player blessing
    /// </summary>
    bool UnlockPlayerBlessing(string playerUID, string blessingId);

    /// <summary>
    ///     Gets active player blessings (to be expanded in Phase 3.3)
    /// </summary>
    List<string> GetActivePlayerBlessings(string playerUID);

    /// <summary>
    ///     Sets up player religion data without adding to religion members
    ///     Used for founders who are already added via ReligionData constructor
    /// </summary>
    void SetPlayerReligionData(string playerUID, string religionUID);

    /// <summary>
    ///     Joins a player to a religion
    /// </summary>
    void JoinReligion(string playerUID, string religionUID);

    /// <summary>
    ///     Removes a player from their current religion
    /// </summary>
    void LeaveReligion(string playerUID);

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

    /// <summary>
    ///     Triggers the OnPlayerDataChanged event for the specified player.
    ///     Use this when player data has been modified externally and clients need to be notified.
    /// </summary>
    /// <param name="playerUID">Player unique identifier</param>
    void NotifyPlayerDataChanged(string playerUID);

    /// <summary>
    /// Determines whether the specified player is associated with a religion.
    /// </summary>
    /// <param name="playerId">The unique identifier of the player.</param>
    /// <returns>
    /// True if the player has a religion; otherwise, false.
    /// </returns>
    public bool HasReligion(string playerId);

    /// <summary>
    /// Retrieves the deity type associated with the specified player.
    /// </summary>
    /// <param name="playerId">
    /// The unique identifier of the player whose deity type is to be retrieved.
    /// </param>
    /// <returns>
    /// The <see cref="DeityDomain"/> representing the player's associated deity.
    /// Returns <see cref="DeityDomain.None"/> if the player has no associated deity.
    /// </returns>
    public DeityDomain GetPlayerDeityType(string playerId);

    /// <summary>
    /// Retrieves the favor rank for the specified player based on their total favor earned.
    /// </summary>
    /// <param name="playerUID">
    /// The unique identifier of the player whose favor rank is to be retrieved.
    /// </param>
    /// <returns>
    /// The <see cref="FavorRank"/> representing the player's current favor rank.
    /// </returns>
    public FavorRank GetPlayerFavorRank(string playerUID);
}