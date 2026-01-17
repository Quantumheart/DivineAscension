namespace DivineAscension.Services.Abstractions;

/// <summary>
/// Abstraction for player information lookup.
/// Decouples business logic from Vintage Story's player API.
/// </summary>
public interface IPlayerProvider
{
    /// <summary>
    /// Gets a player's name by their unique identifier.
    /// </summary>
    /// <param name="playerUid">The player's unique identifier</param>
    /// <returns>The player's name, or null if not found</returns>
    string? GetPlayerName(string playerUid);

    /// <summary>
    /// Checks if a player is currently online.
    /// </summary>
    /// <param name="playerUid">The player's unique identifier</param>
    /// <returns>True if the player is online, false otherwise</returns>
    bool IsPlayerOnline(string playerUid);
}