using DivineAscension.Data;
using Vintagestory.API.Common;

namespace DivineAscension.Services.Interfaces;

/// <summary>
/// Result of attempting to contribute to or auto-start a ritual.
/// </summary>
/// <param name="Success">Whether the ritual operation succeeded</param>
/// <param name="RitualStarted">Whether a new ritual was auto-discovered and started</param>
/// <param name="RitualCompleted">Whether the ritual was completed by this contribution</param>
/// <param name="Message">User-facing message describing the result</param>
/// <param name="FavorAwarded">Amount of favor awarded (0 if none)</param>
/// <param name="PrestigeAwarded">Amount of prestige awarded (0 if none)</param>
/// <param name="ShouldConsumeOffering">Whether the offering item should be consumed</param>
public record RitualAttemptResult(
    bool Success,
    bool RitualStarted,
    bool RitualCompleted,
    string Message,
    int FavorAwarded = 0,
    int PrestigeAwarded = 0,
    bool ShouldConsumeOffering = false);

/// <summary>
/// Service for handling ritual auto-discovery and contribution workflow at holy sites.
/// Encapsulates the logic for determining if an offering matches a ritual requirement,
/// auto-starting rituals, and tracking contributions.
/// </summary>
public interface IRitualContributionService
{
    /// <summary>
    /// Attempts to contribute an offering to a ritual at a holy site.
    /// If no ritual is active, attempts to auto-discover and start one if the offering matches.
    /// </summary>
    /// <param name="holySite">The holy site data</param>
    /// <param name="offering">The item stack being offered</param>
    /// <param name="religion">The religion that owns the holy site</param>
    /// <param name="playerUID">The unique identifier of the contributing player</param>
    /// <param name="playerName">The display name of the contributing player</param>
    /// <returns>Result indicating success/failure and details about the ritual operation</returns>
    RitualAttemptResult TryContributeToRitual(
        HolySiteData holySite,
        ItemStack offering,
        ReligionData religion,
        string playerUID,
        string playerName);
}
