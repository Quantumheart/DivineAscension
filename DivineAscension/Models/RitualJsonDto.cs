using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace DivineAscension.Models;

/// <summary>
/// Data transfer object for ritual JSON file structure.
/// Represents the root structure of a ritual configuration file.
/// </summary>
public class RitualFileDto
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
    /// List of rituals for this domain
    /// </summary>
    [JsonPropertyName("rituals")]
    public List<RitualJsonDto> Rituals { get; set; } = new();
}

/// <summary>
/// Data transfer object for individual ritual definitions from JSON.
/// Maps to the Ritual domain model after deserialization.
/// </summary>
public class RitualJsonDto
{
    /// <summary>
    /// Unique ritual identifier (e.g., "craft_tier2_ritual")
    /// </summary>
    [JsonPropertyName("ritualId")]
    public string RitualId { get; set; } = string.Empty;

    /// <summary>
    /// Display name of the ritual (e.g., "Rite of the Master Smith")
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Source tier (starting tier required to start this ritual)
    /// </summary>
    [JsonPropertyName("sourceTier")]
    public int SourceTier { get; set; }

    /// <summary>
    /// Target tier (tier achieved upon completing this ritual)
    /// </summary>
    [JsonPropertyName("targetTier")]
    public int TargetTier { get; set; }

    /// <summary>
    /// Description of the ritual
    /// </summary>
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// List of requirements for this ritual
    /// </summary>
    [JsonPropertyName("requirements")]
    public List<RitualRequirementJsonDto> Requirements { get; set; } = new();
}

/// <summary>
/// Data transfer object for ritual requirement definitions from JSON.
/// Maps to the RitualRequirement domain model after deserialization.
/// </summary>
public class RitualRequirementJsonDto
{
    /// <summary>
    /// Unique requirement identifier (e.g., "copper_ingots")
    /// </summary>
    [JsonPropertyName("requirementId")]
    public string RequirementId { get; set; } = string.Empty;

    /// <summary>
    /// Display name of the requirement (e.g., "Copper Ingots")
    /// </summary>
    [JsonPropertyName("displayName")]
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Quantity required
    /// </summary>
    [JsonPropertyName("quantity")]
    public int Quantity { get; set; }

    /// <summary>
    /// Type of requirement matching (Exact or Category)
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// List of item codes that satisfy this requirement.
    /// For Exact type: exact item codes (e.g., ["game:ingot-copper"])
    /// For Category type: glob patterns (e.g., ["game:ingot-*"])
    /// </summary>
    [JsonPropertyName("itemCodes")]
    public List<string> ItemCodes { get; set; } = new();
}
