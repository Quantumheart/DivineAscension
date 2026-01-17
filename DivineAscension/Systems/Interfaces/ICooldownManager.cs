using System;
using DivineAscension.Models.Enum;

namespace DivineAscension.Systems.Interfaces;

/// <summary>
///     Interface for managing cooldowns to prevent griefing attacks.
/// </summary>
public interface ICooldownManager : IDisposable
{
    /// <summary>
    ///     Initializes the cooldown manager and registers cleanup callback.
    /// </summary>
    void Initialize();

    /// <summary>
    ///     Checks if an operation is allowed for a player, enforcing cooldown if necessary.
    /// </summary>
    /// <param name="playerUID">The player's unique identifier</param>
    /// <param name="cooldownType">The type of operation being performed</param>
    /// <param name="errorMessage">Error message if cooldown is active (null if allowed)</param>
    /// <returns>True if operation is allowed, false if on cooldown</returns>
    bool CanPerformOperation(string playerUID, CooldownType cooldownType, out string? errorMessage);

    /// <summary>
    ///     Records that a player has performed an operation and starts the cooldown timer.
    /// </summary>
    /// <param name="playerUID">The player's unique identifier</param>
    /// <param name="cooldownType">The type of operation performed</param>
    void RecordOperation(string playerUID, CooldownType cooldownType);

    /// <summary>
    ///     Checks if an operation is allowed and records it atomically if so.
    /// </summary>
    /// <param name="playerUID">The player's unique identifier</param>
    /// <param name="cooldownType">The type of operation being performed</param>
    /// <param name="errorMessage">Error message if cooldown is active (null if allowed)</param>
    /// <returns>True if operation was allowed and recorded, false if on cooldown</returns>
    bool TryPerformOperation(string playerUID, CooldownType cooldownType, out string? errorMessage);

    /// <summary>
    ///     Gets the remaining cooldown time in seconds for a specific operation.
    /// </summary>
    /// <param name="playerUID">The player's unique identifier</param>
    /// <param name="cooldownType">The type of operation</param>
    /// <returns>Remaining time in seconds, or 0 if no cooldown is active</returns>
    double GetRemainingCooldown(string playerUID, CooldownType cooldownType);

    /// <summary>
    ///     Clears all cooldowns for a specific player.
    ///     Useful for testing or admin commands.
    /// </summary>
    /// <param name="playerUID">The player's unique identifier</param>
    void ClearPlayerCooldowns(string playerUID);

    /// <summary>
    ///     Clears a specific cooldown for a player.
    ///     Useful for testing or admin commands.
    /// </summary>
    /// <param name="playerUID">The player's unique identifier</param>
    /// <param name="cooldownType">The type of cooldown to clear</param>
    void ClearSpecificCooldown(string playerUID, CooldownType cooldownType);
}
