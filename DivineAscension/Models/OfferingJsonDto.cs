using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace DivineAscension.Models;

/// <summary>
/// Data transfer object for offering JSON file structure.
/// Represents the root structure of an offering configuration file.
/// </summary>
public class OfferingFileDto
{
    /// <summary>
    /// Deity domain this file applies to (e.g., "Craft", "Wild")
    /// </summary>
    [JsonPropertyName("domain")]
    public string Domain { get; set; } = string.Empty;

    /// <summary>
    /// Configuration file version for future migrations
    /// </summary>
    [JsonPropertyName("version")]
    public int Version { get; set; }

    /// <summary>
    /// List of offerings for this domain
    /// </summary>
    [JsonPropertyName("offerings")]
    public List<OfferingJsonDto> Offerings { get; set; } = new();
}

/// <summary>
/// Data transfer object for individual offering definitions from JSON.
/// Maps to the Offering domain model after deserialization.
/// </summary>
public class OfferingJsonDto
{
    /// <summary>
    /// Display name of the offering (e.g., "Copper Ingot")
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// List of exact item codes that match this offering (e.g., ["game:ingot-copper"])
    /// Supports multiple variants (e.g., different bread types)
    /// </summary>
    [JsonPropertyName("itemCodes")]
    public List<string> ItemCodes { get; set; } = new();

    /// <summary>
    /// Offering tier (1-3), determines rarity and value
    /// </summary>
    [JsonPropertyName("tier")]
    public int Tier { get; set; }

    /// <summary>
    /// Favor bonus value awarded when offered
    /// Standard values: 2 (tier 1), 5 (tier 2), 10 (tier 3)
    /// </summary>
    [JsonPropertyName("value")]
    public int Value { get; set; }

    /// <summary>
    /// Minimum holy site tier required to accept this offering (1-3)
    /// Default: 1 (accepted at all holy sites)
    /// </summary>
    [JsonPropertyName("minHolySiteTier")]
    public int MinHolySiteTier { get; set; } = 1;

    /// <summary>
    /// Description of the offering for admin reference
    /// </summary>
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;
}