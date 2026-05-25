using System;
using System.Collections.Generic;
using System.Linq;
using DivineAscension.Models;

namespace DivineAscension.Systems;

/// <summary>
///     Pure resolver for the unlearn prerequisite cascade (epic #425, slice 2 — #460).
///     Given a target blessing and the player's currently-unlocked set, returns the ordered
///     "kill list": the target plus every unlocked child that would be orphaned once the set
///     is stripped, transitively.
///
///     Orphan rule mirrors the unlock prerequisite gate (<see cref="BlessingRegistry" />):
///     a <em>capstone</em> (no branch) needs ANY one prerequisite unlocked, so it only cascades
///     once <em>all</em> of its unlocked prerequisites are killed; a <em>branch</em> blessing
///     needs ALL prerequisites, so it cascades the moment any is killed. Blessings with no
///     prerequisites, or whose prerequisites don't intersect the kill set, are left intact.
///
///     Shared by the server (<see cref="BlessingUnlearnService" />, authoritative strip) and the
///     client UI (confirm-dialog kill list) so both compute an identical cascade.
/// </summary>
public static class BlessingCascadeResolver
{
    /// <summary>
    ///     Resolves the cascade for <paramref name="targetId"/>. Returns the target first, then
    ///     orphaned dependents in dependency layers (sorted by id within each layer for stable
    ///     ordering). Returns an empty list when the target is not in <paramref name="unlockedIds"/>.
    /// </summary>
    /// <param name="targetId">The blessing the player chose to unlearn.</param>
    /// <param name="unlockedIds">The player's currently-unlocked blessing ids.</param>
    /// <param name="getBlessing">Resolves a blessing definition by id (null if unknown).</param>
    public static List<string> Resolve(
        string targetId,
        ISet<string> unlockedIds,
        Func<string, Blessing?> getBlessing)
    {
        var ordered = new List<string>();
        if (string.IsNullOrEmpty(targetId) || !unlockedIds.Contains(targetId))
            return ordered;

        var killed = new HashSet<string> { targetId };
        ordered.Add(targetId);

        bool addedThisRound;
        do
        {
            addedThisRound = false;

            // Candidates: unlocked, not yet killed, deterministic order for stable layering.
            var newlyOrphaned = new List<string>();
            foreach (var id in unlockedIds.Where(i => !killed.Contains(i)).OrderBy(i => i, StringComparer.Ordinal))
            {
                var blessing = getBlessing(id);
                var prereqs = blessing?.PrerequisiteBlessings;
                if (prereqs == null || prereqs.Count == 0)
                    continue; // independent — never orphaned by a cascade

                if (IsStillSatisfied(blessing!, prereqs, unlockedIds, killed))
                    continue;

                newlyOrphaned.Add(id);
            }

            foreach (var id in newlyOrphaned)
                if (killed.Add(id))
                {
                    ordered.Add(id);
                    addedThisRound = true;
                }
        } while (addedThisRound);

        return ordered;
    }

    private static bool IsStillSatisfied(
        Blessing blessing,
        List<string> prereqs,
        ISet<string> unlockedIds,
        HashSet<string> killed)
    {
        bool Surviving(string p) => unlockedIds.Contains(p) && !killed.Contains(p);

        // Capstone (no branch): OR — satisfied while at least one prerequisite survives.
        // Branch: AND — satisfied only while every prerequisite survives.
        return string.IsNullOrEmpty(blessing.Branch)
            ? prereqs.Any(Surviving)
            : prereqs.All(Surviving);
    }
}
