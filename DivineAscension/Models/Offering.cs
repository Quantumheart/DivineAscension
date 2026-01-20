using System.Collections.Generic;

namespace DivineAscension.Models;

/// <summary>
/// Represents a domain-specific offering that players can present at altars during prayer.
/// Offerings provide bonus favor based on their tier and value.
/// </summary>
/// <param name="Name">Display name of the offering (e.g., "Copper Ingot")</param>
/// <param name="ItemCodes">List of exact item codes that match this offering (e.g., "game:ingot-copper")</param>
/// <param name="Tier">Offering tier (1-3), determines rarity and value</param>
/// <param name="Value">Favor bonus value awarded when offered</param>
/// <param name="MinHolySiteTier">Minimum holy site tier required to accept this offering (1-3)</param>
/// <param name="Description">Description of the offering for admin reference</param>
public record Offering(
    string Name,
    IReadOnlyList<string> ItemCodes,
    int Tier,
    int Value,
    int MinHolySiteTier,
    string Description
);