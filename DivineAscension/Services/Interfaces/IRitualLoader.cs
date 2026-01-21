using System.Collections.Generic;
using DivineAscension.Models;
using DivineAscension.Models.Enum;

namespace DivineAscension.Services.Interfaces;

/// <summary>
/// Service for loading and querying ritual configurations from JSON files.
/// Rituals define the upgrade paths for holy sites from one tier to the next.
/// </summary>
public interface IRitualLoader
{
    /// <summary>
    /// Loads all ritual configurations from JSON files.
    /// Should be called during mod initialization.
    /// </summary>
    void LoadRituals();

    /// <summary>
    /// Gets all rituals configured for a specific deity domain.
    /// </summary>
    /// <param name="domain">The deity domain to query</param>
    /// <returns>Read-only list of rituals for the domain, or empty list if none configured</returns>
    IReadOnlyList<Ritual> GetRitualsForDomain(DeityDomain domain);

    /// <summary>
    /// Finds a ritual by its unique identifier.
    /// </summary>
    /// <param name="ritualId">The ritual identifier (e.g., "craft_tier2_ritual")</param>
    /// <returns>Matching ritual, or null if not found</returns>
    Ritual? GetRitualById(string ritualId);

    /// <summary>
    /// Finds the ritual for upgrading from source tier to target tier for a given domain.
    /// </summary>
    /// <param name="domain">The deity domain</param>
    /// <param name="sourceTier">Current tier of the holy site (1 or 2)</param>
    /// <param name="targetTier">Desired target tier (2 or 3)</param>
    /// <returns>Matching ritual, or null if not found</returns>
    Ritual? GetRitualForTierUpgrade(DeityDomain domain, int sourceTier, int targetTier);
}
