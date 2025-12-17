using System.Collections.Generic;
using PantheonWars.Models.Enum;

namespace PantheonWars.Models;

/// <summary>
///     Represents a deity in the Pantheon Wars system
/// </summary>
public class Deity
{
    public Deity(DeityType type, string name, string domain)
    {
        Type = type;
        Name = name;
        Domain = domain;
    }

    /// <summary>
    ///     Unique identifier for the deity
    /// </summary>
    public DeityType Type { get; set; }

    /// <summary>
    ///     Display name of the deity
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    ///     Domain/sphere of influence (e.g., "War", "Hunt", "Death")
    /// </summary>
    public string Domain { get; set; } = string.Empty;

    /// <summary>
    ///     Detailed description of the deity
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    ///     Moral alignment of the deity
    /// </summary>
    public DeityAlignment Alignment { get; set; }

    /// <summary>
    ///     Primary color associated with the deity (for visual effects)
    /// </summary>
    public string PrimaryColor { get; set; } = "#FFFFFF";

    /// <summary>
    ///     Secondary color associated with the deity
    /// </summary>
    public string SecondaryColor { get; set; } = "#CCCCCC";

    /// <summary>
    ///     Relationships with other deities
    ///     Key: DeityType, Value: RelationshipType
    /// </summary>
    public Dictionary<DeityType, DeityRelationshipType> Relationships { get; set; } = new();

    /// <summary>
    ///     Playstyle hints for the deity
    /// </summary>
    public string Playstyle { get; set; } = string.Empty;

    /// <summary>
    ///     List of ability IDs for this deity (to be implemented in Phase 1 Task 6)
    /// </summary>
    public List<string> AbilityIds { get; set; } = new();
}