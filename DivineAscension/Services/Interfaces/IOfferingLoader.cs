using System.Collections.Generic;
using DivineAscension.Models;
using DivineAscension.Models.Enum;

namespace DivineAscension.Services.Interfaces;

/// <summary>
/// Service for loading and querying offering configurations from JSON files.
/// Offerings define which items can be presented at altars during prayer for bonus favor.
/// </summary>
public interface IOfferingLoader
{
    /// <summary>
    /// Loads all offering configurations from JSON files.
    /// Should be called during mod initialization.
    /// </summary>
    void LoadOfferings();

    /// <summary>
    /// Gets all offerings configured for a specific deity domain.
    /// </summary>
    /// <param name="domain">The deity domain to query</param>
    /// <returns>Read-only list of offerings for the domain, or empty list if none configured</returns>
    IReadOnlyList<Offering> GetOfferingsForDomain(DeityDomain domain);

    /// <summary>
    /// Finds an offering by exact item code match for a specific domain.
    /// </summary>
    /// <param name="itemCode">Full item code to match (e.g., "game:ingot-copper")</param>
    /// <param name="domain">The deity domain to search within</param>
    /// <returns>Matching offering, or null if not found</returns>
    Offering? FindOfferingByItemCode(string itemCode, DeityDomain domain);
}