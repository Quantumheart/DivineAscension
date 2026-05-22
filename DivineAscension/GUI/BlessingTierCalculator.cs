using System.Collections.Generic;
using DivineAscension.Models;

namespace DivineAscension.GUI;

/// <summary>
/// Assigns a tier level (1+) to each blessing based on the depth of its
/// prerequisite chain. Tier 1 = no prerequisites; tier N = max(prereq tiers) + 1.
/// Used by the ledger renderer to group blessings into tier sections.
/// </summary>
public static class BlessingTierCalculator
{
    public static void AssignTiers(IReadOnlyDictionary<string, BlessingNodeState> blessingStates)
    {
        foreach (var node in blessingStates.Values)
            node.Tier = 0;

        foreach (var node in blessingStates.Values)
            CalculateTier(node, blessingStates, new HashSet<string>());
    }

    private static int CalculateTier(BlessingNodeState node,
        IReadOnlyDictionary<string, BlessingNodeState> stateMap,
        HashSet<string> visiting)
    {
        if (node.Tier > 0) return node.Tier;

        if (!visiting.Add(node.Blessing.BlessingId))
        {
            node.Tier = 1;
            return 1;
        }

        var prereqs = node.Blessing.PrerequisiteBlessings;
        if (prereqs == null || prereqs.Count == 0)
        {
            node.Tier = 1;
            visiting.Remove(node.Blessing.BlessingId);
            return 1;
        }

        var maxPrereqTier = 0;
        foreach (var prereqId in prereqs)
            if (stateMap.TryGetValue(prereqId, out var prereqState))
            {
                var t = CalculateTier(prereqState, stateMap, visiting);
                if (t > maxPrereqTier) maxPrereqTier = t;
            }

        node.Tier = maxPrereqTier + 1;
        visiting.Remove(node.Blessing.BlessingId);
        return node.Tier;
    }
}
