using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using DivineAscension.Models;
using Vintagestory.API.Common;

namespace DivineAscension.Systems;

/// <summary>
/// Matches items against ritual requirements, supporting both exact matches and glob patterns.
/// </summary>
public class RitualMatcher
{
    /// <summary>
    /// Checks if an offered item matches a ritual requirement.
    /// </summary>
    /// <param name="offering">The item stack being offered</param>
    /// <param name="requirement">The ritual requirement to match against</param>
    /// <returns>True if the item matches the requirement</returns>
    public bool DoesItemMatchRequirement(ItemStack offering, RitualRequirement requirement)
    {
        if (offering?.Collectible?.Code == null)
            return false;

        var itemCode = offering.Collectible.Code.ToString();

        return requirement.Type switch
        {
            RequirementType.Exact => MatchesExact(itemCode, requirement.ItemCodes),
            RequirementType.Category => MatchesGlobPattern(itemCode, requirement.ItemCodes),
            _ => false
        };
    }

    /// <summary>
    /// Finds the first requirement that matches the offered item.
    /// </summary>
    /// <param name="offering">The item stack being offered</param>
    /// <param name="requirements">List of requirements to check</param>
    /// <returns>The matching requirement, or null if none match</returns>
    public RitualRequirement? FindMatchingRequirement(ItemStack offering, IReadOnlyList<RitualRequirement> requirements)
    {
        return requirements.FirstOrDefault(req => DoesItemMatchRequirement(offering, req));
    }

    /// <summary>
    /// Checks if an item code exactly matches any of the provided item codes.
    /// </summary>
    private static bool MatchesExact(string itemCode, IReadOnlyList<string> itemCodes)
    {
        return itemCodes.Contains(itemCode, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Checks if an item code matches any of the provided glob patterns.
    /// Supports wildcard patterns like "game:ingot-*" matching "game:ingot-copper".
    /// </summary>
    private static bool MatchesGlobPattern(string itemCode, IReadOnlyList<string> patterns)
    {
        foreach (var pattern in patterns)
        {
            if (MatchesSingleGlobPattern(itemCode, pattern))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Checks if an item code matches a single glob pattern.
    /// Converts glob wildcards (*) to regex and performs case-insensitive matching.
    /// </summary>
    private static bool MatchesSingleGlobPattern(string itemCode, string pattern)
    {
        // Convert glob pattern to regex pattern
        // Escape special regex characters except *
        var regexPattern = Regex.Escape(pattern)
            .Replace("\\*", ".*"); // Replace escaped \* with .* (match any characters)

        // Add anchors to match the entire string
        regexPattern = $"^{regexPattern}$";

        try
        {
            return Regex.IsMatch(itemCode, regexPattern, RegexOptions.IgnoreCase);
        }
        catch (Exception)
        {
            // If regex fails, fall back to exact match
            return string.Equals(itemCode, pattern, StringComparison.OrdinalIgnoreCase);
        }
    }
}
