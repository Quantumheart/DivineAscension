using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace DivineAscension.Models.Dto;

/// <summary>
///     DTO for deserializing individual blessing definitions from JSON.
///     Maps to the Blessing model after validation and conversion.
/// </summary>
public class BlessingJsonDto
{
    /// <summary>
    ///     Unique identifier for this blessing (e.g., "khoras_craftsmans_touch")
    /// </summary>
    [JsonPropertyName("blessingId")]
    public string BlessingId { get; set; } = string.Empty;

    /// <summary>
    ///     Display name of the blessing
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    ///     Detailed description of what the blessing does
    /// </summary>
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    ///     Type of blessing: "Player" or "Religion"
    /// </summary>
    [JsonPropertyName("kind")]
    public string Kind { get; set; } = string.Empty;

    /// <summary>
    ///     Category for organization: "Combat", "Defense", "Utility"
    /// </summary>
    [JsonPropertyName("category")]
    public string Category { get; set; } = string.Empty;

    /// <summary>
    ///     Icon name for this blessing (e.g., "hammer-drop", "paw")
    /// </summary>
    [JsonPropertyName("iconName")]
    public string IconName { get; set; } = string.Empty;

    /// <summary>
    ///     Required player favor rank to unlock (for Player blessings)
    ///     0 = Initiate, 1 = Disciple, 2 = Zealot, 3 = Champion, 4 = Avatar
    /// </summary>
    [JsonPropertyName("requiredFavorRank")]
    public int RequiredFavorRank { get; set; }

    /// <summary>
    ///     Required religion prestige rank to unlock (for Religion blessings)
    ///     0 = Fledgling, 1 = Established, 2 = Renowned, 3 = Legendary, 4 = Divine
    /// </summary>
    [JsonPropertyName("requiredPrestigeRank")]
    public int RequiredPrestigeRank { get; set; }

    /// <summary>
    ///     List of prerequisite blessing IDs that must be unlocked first
    /// </summary>
    [JsonPropertyName("prerequisiteBlessings")]
    public List<string>? PrerequisiteBlessings { get; set; }

    /// <summary>
    ///     Dictionary of stat modifiers this blessing provides.
    ///     Keys must match VintageStoryStats constant values exactly.
    /// </summary>
    [JsonPropertyName("statModifiers")]
    public Dictionary<string, float>? StatModifiers { get; set; }

    /// <summary>
    ///     List of special effect identifiers for complex blessing behaviors.
    ///     Keys must match SpecialEffects constant values.
    /// </summary>
    [JsonPropertyName("specialEffects")]
    public List<string>? SpecialEffects { get; set; }

    /// <summary>
    ///     Cost to unlock this blessing.
    ///     For Player blessings: favor cost.
    ///     For Religion blessings: prestige cost.
    /// </summary>
    [JsonPropertyName("cost")]
    public int Cost { get; set; }

    /// <summary>
    ///     The branch this blessing belongs to within its domain.
    ///     Null or empty means "Shared" (no branch restrictions).
    /// </summary>
    [JsonPropertyName("branch")]
    public string? Branch { get; set; }

    /// <summary>
    ///     List of branch names that become locked if this blessing is unlocked.
    ///     Only applies when unlocking the first blessing in a branch.
    /// </summary>
    [JsonPropertyName("exclusiveBranches")]
    public List<string>? ExclusiveBranches { get; set; }
}
