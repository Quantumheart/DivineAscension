using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using DivineAscension.Models;

namespace DivineAscension.GUI;

/// <summary>
///     Calculates positions for blessing nodes in the tree layout.
///     Nodes are grouped first by <see cref="Blessing.Branch"/> into vertical
///     swimlanes, then by tier within each lane. Branchless ("shared") nodes
///     occupy a centre lane so mutex branches read as parallel columns.
/// </summary>
[ExcludeFromCodeCoverage]
public static class BlessingTreeLayout
{
    private const float NodeWidth = 64f;
    private const float NodeHeight = 64f;
    private const float VerticalSpacing = 120f;   // between tiers
    private const float IntraLaneSpacing = 24f;   // siblings inside one lane/tier
    private const float LaneSpacing = 72f;        // between swimlanes
    private const float TopPadding = 40f;
    private const float LeftPadding = 40f;

    private const string SharedLaneKey = "";

    public static void CalculateLayout(Dictionary<string, BlessingNodeState> blessingStates, float containerWidth)
    {
        if (blessingStates.Count == 0) return;

        AssignTiers(blessingStates);

        var byBranch = blessingStates.Values
            .GroupBy(BranchKey)
            .ToDictionary(g => g.Key, g => g.ToList());

        var lanes = OrderLanes(byBranch.Keys);

        // Per-lane width = widest tier row in that lane.
        var laneSlots = lanes.ToDictionary(
            lane => lane,
            lane => Math.Max(1, byBranch[lane].GroupBy(n => n.Tier).Max(g => g.Count())));

        var laneStartX = new Dictionary<string, float>();
        var cursorX = LeftPadding;
        foreach (var lane in lanes)
        {
            laneStartX[lane] = cursorX;
            cursorX += LaneInnerWidth(laneSlots[lane]) + LaneSpacing;
        }

        // Centre the lane block horizontally if there's spare room.
        var totalLanesWidth = cursorX - LaneSpacing - LeftPadding;
        var available = containerWidth - LeftPadding * 2;
        var extraShift = available > totalLanesWidth ? (available - totalLanesWidth) / 2f : 0f;

        foreach (var lane in lanes)
        {
            var slots = laneSlots[lane];
            var innerWidth = LaneInnerWidth(slots);
            var startX = laneStartX[lane] + extraShift;

            foreach (var tierGroup in byBranch[lane].GroupBy(n => n.Tier))
            {
                var tierNodes = tierGroup.OrderBy(n => n.Blessing?.BlessingId, StringComparer.Ordinal).ToList();
                var count = tierNodes.Count;
                var rowWidth = count * NodeWidth + Math.Max(0, count - 1) * IntraLaneSpacing;
                var rowStartX = startX + (innerWidth - rowWidth) / 2f;
                var y = TopPadding + (tierGroup.Key - 1) * (NodeHeight + VerticalSpacing);

                for (var i = 0; i < count; i++)
                {
                    var node = tierNodes[i];
                    node.PositionX = rowStartX + i * (NodeWidth + IntraLaneSpacing);
                    node.PositionY = y;
                    node.Width = NodeWidth;
                    node.Height = NodeHeight;
                }
            }
        }
    }

    private static string BranchKey(BlessingNodeState n)
        => string.IsNullOrEmpty(n.Blessing?.Branch) ? SharedLaneKey : n.Blessing!.Branch!;

    private static float LaneInnerWidth(int slots)
        => slots * NodeWidth + Math.Max(0, slots - 1) * IntraLaneSpacing;

    /// <summary>
    /// Order lanes so real branches sit alphabetically left-to-right with the
    /// shared/branchless lane inserted at the centre.
    /// </summary>
    private static List<string> OrderLanes(IEnumerable<string> branchKeys)
    {
        var real = branchKeys
            .Where(k => k != SharedLaneKey)
            .OrderBy(k => k, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (!branchKeys.Contains(SharedLaneKey)) return real;

        var center = real.Count / 2;
        real.Insert(center, SharedLaneKey);
        return real;
    }

    private static void AssignTiers(Dictionary<string, BlessingNodeState> blessingStates)
    {
        var stateMap = new Dictionary<string, BlessingNodeState>();
        foreach (var kvp in blessingStates)
        {
            stateMap[kvp.Key] = kvp.Value;
            kvp.Value.Tier = 0;
        }

        foreach (var state in blessingStates.Values) CalculateTier(state, stateMap, new HashSet<string>());
    }

    private static int CalculateTier(BlessingNodeState node, Dictionary<string, BlessingNodeState> stateMap,
        HashSet<string> visiting)
    {
        if (node.Tier > 0) return node.Tier;

        if (visiting.Contains(node.Blessing.BlessingId))
        {
            node.Tier = 1;
            return 1;
        }

        visiting.Add(node.Blessing.BlessingId);

        if (node.Blessing.PrerequisiteBlessings == null || node.Blessing.PrerequisiteBlessings.Count == 0)
        {
            node.Tier = 1;
            visiting.Remove(node.Blessing.BlessingId);
            return 1;
        }

        var maxPrereqTier = 0;
        foreach (var prereqId in node.Blessing.PrerequisiteBlessings)
            if (stateMap.TryGetValue(prereqId, out var prereqState))
            {
                var prereqTier = CalculateTier(prereqState, stateMap, visiting);
                maxPrereqTier = Math.Max(maxPrereqTier, prereqTier);
            }

        node.Tier = maxPrereqTier + 1;
        visiting.Remove(node.Blessing.BlessingId);
        return node.Tier;
    }

    public static float GetTotalHeight(Dictionary<string, BlessingNodeState> blessingStates)
    {
        if (blessingStates.Count == 0) return 0;

        var maxTier = blessingStates.Values.Max(node => node.Tier);
        return TopPadding + maxTier * (NodeHeight + VerticalSpacing);
    }

    public static float GetTotalWidth(Dictionary<string, BlessingNodeState> blessingStates)
    {
        if (blessingStates.Count == 0) return 0;

        var byBranch = blessingStates.Values
            .GroupBy(BranchKey)
            .ToDictionary(g => g.Key, g => g.ToList());

        var laneCount = byBranch.Count;
        var total = LeftPadding * 2;
        foreach (var lane in byBranch.Keys)
        {
            var slots = Math.Max(1, byBranch[lane].GroupBy(n => n.Tier).Max(g => g.Count()));
            total += LaneInnerWidth(slots);
        }
        total += Math.Max(0, laneCount - 1) * LaneSpacing;
        return total;
    }

    public static bool IsPointInNode(BlessingNodeState node, float x, float y)
    {
        return x >= node.PositionX &&
               x <= node.PositionX + node.Width &&
               y >= node.PositionY &&
               y <= node.PositionY + node.Height;
    }

    public static BlessingNodeState? FindNodeAtPoint(Dictionary<string, BlessingNodeState> blessingStates, float x,
        float y)
    {
        foreach (var state in blessingStates.Values)
            if (IsPointInNode(state, x, y))
                return state;

        return null;
    }
}
