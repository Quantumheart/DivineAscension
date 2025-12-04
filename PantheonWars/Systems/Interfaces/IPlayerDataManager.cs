using PantheonWars.Data;
using PantheonWars.Models.Enum;
using Vintagestory.API.Server;

namespace PantheonWars.Systems.Interfaces;

/// <summary>
///     Interface for managing player deity data persistence and runtime access
/// </summary>
public interface IPlayerDataManager
{
    /// <summary>
    ///     Initializes the player data manager
    /// </summary>
    void Initialize();

    /// <summary>
    ///     Gets player data, creating new data if it doesn't exist
    /// </summary>
    PlayerDeityData GetOrCreatePlayerData(string playerUID);

    /// <summary>
    ///     Gets player data by IServerPlayer
    /// </summary>
    PlayerDeityData GetOrCreatePlayerData(IServerPlayer player);

    /// <summary>
    ///     Checks if player has pledged to a deity
    /// </summary>
    bool HasDeity(string playerUID);

    /// <summary>
    ///     Sets player's deity (initial pledge or switch)
    /// </summary>
    void SetDeity(string playerUID, DeityType deityType);

    /// <summary>
    ///     Adds favor to a player
    /// </summary>
    void AddFavor(string playerUID, int amount, string reason = "");

    /// <summary>
    ///     Removes favor from a player
    /// </summary>
    bool RemoveFavor(string playerUID, int amount, string reason = "");

    /// <summary>
    ///     Adds fractional favor to a player (for passive favor generation)
    /// </summary>
    /// <param name="playerUID">The player's unique identifier</param>
    /// <param name="amount">The amount of fractional favor to add</param>
    /// <param name="reason">The reason for adding fractional favor (optional)</param>
    void AddFractionalFavor(string playerUID, float amount, string reason = "");
}