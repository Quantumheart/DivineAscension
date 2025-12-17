using System.Collections.Generic;
using PantheonWars.Models;
using Vintagestory.API.Server;

namespace PantheonWars.Systems.Interfaces;

public interface IBlessingEffectSystem
{
    /// <summary>
    ///     Initializes the blessing effect system
    /// </summary>
    void Initialize();

    /// <summary>
    ///     Gets stat modifiers from player's unlocked blessings
    /// </summary>
    Dictionary<string, float> GetPlayerStatModifiers(string playerUID);

    /// <summary>
    ///     Gets stat modifiers from religion's unlocked blessings
    /// </summary>
    Dictionary<string, float> GetReligionStatModifiers(string religionUID);

    /// <summary>
    ///     Gets combined stat modifiers for a player (player blessings + religion blessings)
    /// </summary>
    Dictionary<string, float> GetCombinedStatModifiers(string playerUID);

    /// <summary>
    ///     Applies blessings to a player using Vintage Story's Stats API
    ///     Based on XSkills implementation pattern
    /// </summary>
    void ApplyBlessingsToPlayer(IServerPlayer player);

    /// <summary>
    ///     Refreshes all blessing effects for a player
    /// </summary>
    void RefreshPlayerBlessings(string playerUID);

    /// <summary>
    ///     Refreshes blessing effects for all members of a religion
    /// </summary>
    void RefreshReligionBlessings(string religionUID);

    /// <summary>
    ///     Gets a summary of active blessings for a player
    /// </summary>
    (List<Blessing> playerBlessings, List<Blessing> religionBlessings) GetActiveBlessings(string playerUID);

    /// <summary>
    ///     Clears all modifier caches (useful for debugging/testing)
    /// </summary>
    void ClearAllCaches();

    /// <summary>
    ///     Gets a formatted string of all stat modifiers for display
    /// </summary>
    string FormatStatModifiers(Dictionary<string, float> modifiers);
}