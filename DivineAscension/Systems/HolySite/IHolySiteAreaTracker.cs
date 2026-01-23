using System;
using DivineAscension.Data;
using Vintagestory.API.Server;

namespace DivineAscension.Systems.HolySite;

/// <summary>
/// Tracks player positions relative to holy sites and emits enter/exit events.
/// Used by patrol system and other features that need to know when players
/// enter or leave holy site areas.
/// </summary>
public interface IHolySiteAreaTracker : IDisposable
{
    /// <summary>
    /// Fired when a player enters a holy site area.
    /// </summary>
    event Action<IServerPlayer, HolySiteData>? OnPlayerEnteredHolySite;

    /// <summary>
    /// Fired when a player exits a holy site area.
    /// </summary>
    event Action<IServerPlayer, HolySiteData>? OnPlayerExitedHolySite;

    /// <summary>
    /// Initializes the tracker and starts periodic position checks.
    /// </summary>
    void Initialize();

    /// <summary>
    /// Gets the holy site the player is currently in, if any.
    /// </summary>
    /// <param name="playerUID">The player's unique identifier</param>
    /// <returns>The holy site the player is in, or null if not in any site</returns>
    HolySiteData? GetPlayerCurrentSite(string playerUID);
}
