using System.Collections.Generic;
using PantheonWars.Models.Enum;

namespace PantheonWars.Systems.Interfaces;

/// <summary>
///     Interface for managing religion prestige progression and religion-wide blessings
/// </summary>
public interface IReligionPrestigeManager
{
    /// <summary>
    ///     Sets the blessing registry and effect system (called after they're initialized)
    /// </summary>
    void SetBlessingSystems(IBlessingRegistry blessingRegistry, IBlessingEffectSystem blessingEffectSystem);

    /// <summary>
    ///     Initializes the religion prestige manager
    /// </summary>
    void Initialize();

    /// <summary>
    ///     Adds prestige to a religion and updates rank if needed
    /// </summary>
    void AddPrestige(string religionUID, int amount, string reason = "");

    /// <summary>
    ///     Updates prestige rank based on total prestige earned
    /// </summary>
    void UpdatePrestigeRank(string religionUID);

    /// <summary>
    ///     Unlocks a religion blessing if requirements are met
    /// </summary>
    bool UnlockReligionBlessing(string religionUID, string blessingId);

    /// <summary>
    ///     Gets all active (unlocked) religion blessings
    /// </summary>
    List<string> GetActiveReligionBlessings(string religionUID);

    /// <summary>
    ///     Gets prestige progress information for display
    /// </summary>
    (int current, int nextThreshold, PrestigeRank nextRank) GetPrestigeProgress(string religionUID);
}