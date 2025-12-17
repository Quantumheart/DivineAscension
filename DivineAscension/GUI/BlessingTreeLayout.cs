using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using PantheonWars.Models;

namespace PantheonWars.GUI;

/// <summary>
///     Calculates positions for blessing nodes in the tree layout
///     Uses tier-based vertical arrangement with horizontal spacing
/// </summary>
[ExcludeFromCodeCoverage]
public static class BlessingTreeLayout
{
    // Layout constants
    private const float NodeWidth = 64f;
    private const float NodeHeight = 64f;
    private const float VerticalSpacing = 120f; // Space between tiers
    private const float HorizontalSpacing = 100f; // Space between nodes in same tier
    private const float TopPadding = 40f;
    private const float LeftPadding = 40f;

    /// <summary>
    ///     Calculate layout for a collection of blessing node states
    ///     Arranges nodes by tier (1-4) vertically, spread horizontally within each tier
    /// </summary>
    public static void CalculateLayout(Dictionary<string, BlessingNodeState> blessingStates, float containerWidth)
    {
        if (blessingStates.Count == 0) return;

        // Determine tiers for each blessing based on prerequisites
        AssignTiers(blessingStates);

        // Group nodes by tier
        var nodesByTier = blessingStates.Values
            .GroupBy(node => node.Tier)
            .OrderBy(group => group.Key)
            .ToList();

        var currentY = TopPadding;

        foreach (var tierGroup in nodesByTier)
        {
            var nodesInTier = tierGroup.ToList();
            var nodeCount = nodesInTier.Count;

            // Calculate horizontal layout for this tier
            var totalWidth = nodeCount * NodeWidth + (nodeCount - 1) * HorizontalSpacing;
            var startX = LeftPadding + (containerWidth - totalWidth - LeftPadding * 2) / 2;

            // If total width exceeds container, use left alignment with smaller spacing
            if (totalWidth > containerWidth - LeftPadding * 2)
            {
                startX = LeftPadding;
                var availableWidth = containerWidth - LeftPadding * 2 - nodeCount * NodeWidth;
                var adjustedSpacing = nodeCount > 1 ? availableWidth / (nodeCount - 1) : 0;

                // Position nodes with adjusted spacing
                for (var i = 0; i < nodeCount; i++)
                {
                    var node = nodesInTier[i];
                    node.PositionX = startX + i * (NodeWidth + adjustedSpacing);
                    node.PositionY = currentY;
                    node.Width = NodeWidth;
                    node.Height = NodeHeight;
                }
            }
            else
            {
                // Position nodes with standard spacing
                for (var i = 0; i < nodeCount; i++)
                {
                    var node = nodesInTier[i];
                    node.PositionX = startX + i * (NodeWidth + HorizontalSpacing);
                    node.PositionY = currentY;
                    node.Width = NodeWidth;
                    node.Height = NodeHeight;
                }
            }

            currentY += NodeHeight + VerticalSpacing;
        }
    }

    /// <summary>
    ///     Assign tier levels to blessings based on their prerequisite chains
    ///     Tier 1 = no prerequisites, Tier 2+ = based on deepest prerequisite + 1
    /// </summary>
    private static void AssignTiers(Dictionary<string, BlessingNodeState> blessingStates)
    {
        // Build a lookup map
        var stateMap = new Dictionary<string, BlessingNodeState>();
        foreach (var kvp in blessingStates)
        {
            stateMap[kvp.Key] = kvp.Value;
            kvp.Value.Tier = 0; // Initialize
        }

        // Calculate tier for each node recursively
        foreach (var state in blessingStates.Values) CalculateTier(state, stateMap, new HashSet<string>());
    }

    /// <summary>
    ///     Recursively calculate tier for a blessing node
    /// </summary>
    private static int CalculateTier(BlessingNodeState node, Dictionary<string, BlessingNodeState> stateMap,
        HashSet<string> visiting)
    {
        // If already calculated, return cached value
        if (node.Tier > 0) return node.Tier;

        // Check for circular dependencies
        if (visiting.Contains(node.Blessing.BlessingId))
        {
            // Circular dependency detected, assign tier 1 to break cycle
            node.Tier = 1;
            return 1;
        }

        visiting.Add(node.Blessing.BlessingId);

        // If no prerequisites, this is tier 1
        if (node.Blessing.PrerequisiteBlessings == null || node.Blessing.PrerequisiteBlessings.Count == 0)
        {
            node.Tier = 1;
            visiting.Remove(node.Blessing.BlessingId);
            return 1;
        }

        // Find the maximum tier among prerequisites
        var maxPrereqTier = 0;
        foreach (var prereqId in node.Blessing.PrerequisiteBlessings)
            if (stateMap.TryGetValue(prereqId, out var prereqState))
            {
                var prereqTier = CalculateTier(prereqState, stateMap, visiting);
                maxPrereqTier = Math.Max(maxPrereqTier, prereqTier);
            }

        // This node's tier is one more than the highest prerequisite tier
        node.Tier = maxPrereqTier + 1;
        visiting.Remove(node.Blessing.BlessingId);
        return node.Tier;
    }

    /// <summary>
    ///     Get total height needed for the tree layout
    /// </summary>
    public static float GetTotalHeight(Dictionary<string, BlessingNodeState> blessingStates)
    {
        if (blessingStates.Count == 0) return 0;

        var maxTier = blessingStates.Values.Max(node => node.Tier);
        return TopPadding + maxTier * (NodeHeight + VerticalSpacing);
    }

    /// <summary>
    ///     Get total width needed for the tree layout
    /// </summary>
    public static float GetTotalWidth(Dictionary<string, BlessingNodeState> blessingStates)
    {
        if (blessingStates.Count == 0) return 0;

        // Find tier with most nodes
        var maxNodesInTier = blessingStates.Values
            .GroupBy(node => node.Tier)
            .Max(group => group.Count());

        return LeftPadding * 2 + maxNodesInTier * NodeWidth + (maxNodesInTier - 1) * HorizontalSpacing;
    }

    /// <summary>
    ///     Check if a point is inside a node's bounds (for click detection)
    /// </summary>
    public static bool IsPointInNode(BlessingNodeState node, float x, float y)
    {
        return x >= node.PositionX &&
               x <= node.PositionX + node.Width &&
               y >= node.PositionY &&
               y <= node.PositionY + node.Height;
    }

    /// <summary>
    ///     Find which node (if any) contains the given point
    /// </summary>
    public static BlessingNodeState? FindNodeAtPoint(Dictionary<string, BlessingNodeState> blessingStates, float x,
        float y)
    {
        foreach (var state in blessingStates.Values)
            if (IsPointInNode(state, x, y))
                return state;

        return null;
    }
}