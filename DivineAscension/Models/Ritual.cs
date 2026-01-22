using System.Collections.Generic;
using DivineAscension.Models.Enum;

namespace DivineAscension.Models;

/// <summary>
/// Represents a ritual that players complete to upgrade a holy site's tier.
/// Rituals have 3-5 steps (configurable per ritual) that must be completed in any order.
/// Each step must be discovered by offering matching items before becoming visible.
/// </summary>
public record Ritual(
    string RitualId,
    string Name,
    DeityDomain Domain,
    int SourceTier,
    int TargetTier,
    IReadOnlyList<RitualStep> Steps,
    string Description
);

/// <summary>
/// Represents one step within a ritual.
/// A step is completed when all its requirements are satisfied.
/// Steps start as undiscovered and are revealed when players offer matching items.
/// </summary>
public record RitualStep(
    string StepId,
    string StepName,
    IReadOnlyList<RitualRequirement> Requirements
);

/// <summary>
/// Represents a single requirement within a ritual.
/// Can be an exact item match or a category match using glob patterns.
/// </summary>
public record RitualRequirement(
    string RequirementId,
    string DisplayName,
    int Quantity,
    RequirementType Type,
    IReadOnlyList<string> ItemCodes
);

/// <summary>
/// Defines how items are matched against ritual requirements.
/// </summary>
public enum RequirementType
{
    /// <summary>
    /// Exact match - item must be in the ItemCodes list
    /// </summary>
    Exact,

    /// <summary>
    /// Category match - item must match a glob pattern in ItemCodes
    /// (e.g., "game:ingot-*" matches all ingots)
    /// </summary>
    Category
}
