using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace DivineAscension.Models.Dto;

/// <summary>
///     DTO for deserializing the root structure of blessing JSON files.
///     Each file contains blessings for a single deity domain.
/// </summary>
public class BlessingFileDto
{
    /// <summary>
    ///     The deity domain for all blessings in this file.
    ///     Must be one of: "Craft", "Wild", "Conquest", "Harvest", "Stone"
    /// </summary>
    [JsonPropertyName("domain")]
    public string Domain { get; set; } = string.Empty;

    /// <summary>
    ///     Schema version for future compatibility.
    ///     Current version: 1
    /// </summary>
    [JsonPropertyName("version")]
    public int Version { get; set; } = 1;

    /// <summary>
    ///     List of blessing definitions for this domain.
    /// </summary>
    [JsonPropertyName("blessings")]
    public List<BlessingJsonDto> Blessings { get; set; } = new();
}
