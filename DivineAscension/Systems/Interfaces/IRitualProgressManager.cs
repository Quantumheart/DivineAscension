using DivineAscension.Models;
using Vintagestory.API.Common;

namespace DivineAscension.Systems.Interfaces;

/// <summary>
/// Result of starting a ritual.
/// </summary>
public record RitualStartResult(
    bool Success,
    string Message,
    Ritual? Ritual = null
);

/// <summary>
/// Result of contributing to a ritual.
/// </summary>
public record RitualContributionResult(
    bool Success,
    string Message,
    string? RequirementId = null,
    int QuantityContributed = 0,
    int QuantityRequired = 0,
    bool RequirementCompleted = false,
    bool RitualCompleted = false
);

/// <summary>
/// Manages ritual progress tracking and tier upgrades for holy sites.
/// </summary>
public interface IRitualProgressManager
{
    /// <summary>
    /// Starts a ritual for upgrading a holy site tier.
    /// </summary>
    /// <param name="siteUID">The holy site UID</param>
    /// <param name="ritualId">The ritual identifier (e.g., "craft_tier2_ritual")</param>
    /// <param name="playerUID">The player starting the ritual (must be consecrator)</param>
    /// <returns>Result indicating success or failure with message</returns>
    RitualStartResult StartRitual(string siteUID, string ritualId, string playerUID);

    /// <summary>
    /// Contributes an offering to an active ritual.
    /// If the offering matches a ritual requirement, progress is tracked.
    /// </summary>
    /// <param name="siteUID">The holy site UID</param>
    /// <param name="offering">The item stack being offered</param>
    /// <param name="playerUID">The player contributing</param>
    /// <returns>Result indicating whether the contribution was accepted and progress info</returns>
    RitualContributionResult ContributeToRitual(string siteUID, ItemStack offering, string playerUID);

    /// <summary>
    /// Cancels an active ritual at a holy site.
    /// No refunds are given for contributed items.
    /// </summary>
    /// <param name="siteUID">The holy site UID</param>
    /// <param name="playerUID">The player cancelling (must be consecrator)</param>
    /// <returns>True if cancelled successfully, false otherwise</returns>
    bool CancelRitual(string siteUID, string playerUID);
}
