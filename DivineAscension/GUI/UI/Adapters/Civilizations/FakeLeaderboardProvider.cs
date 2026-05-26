using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using DivineAscension.Models.Enum;

namespace DivineAscension.GUI.UI.Adapters.Civilizations;

/// <summary>
///     Dev-only, UI-only leaderboard provider that generates synthetic realm standings
///     deterministically across all four boards. Uses the adapter pattern via
///     <see cref="ILeaderboardProvider" /> so the chapter can be styled against seeded,
///     representative data without the server/network plumbing (epic #496, slice 5).
///     The viewer's own realm is seeded to land mid-table so the pin/highlight (slice 2)
///     is exercisable.
/// </summary>
[ExcludeFromCodeCoverage]
internal sealed class FakeLeaderboardProvider : ILeaderboardProvider
{
    private static readonly string[] RealmNames =
    {
        "Northwind Pact", "The Sunken Marches", "Long-Row Covenant", "The Gilded Reach",
        "Hollowmere Concord", "Ashfell Dominion", "The Iron Tessellate", "Greyfen Union",
        "The Amber League", "Stormhold Accord", "The Verdant Assembly", "Duskwater Realm",
        "The Bronze Confederacy", "Whitethorn Order", "The Cinder Reaches", "Saltmarch Coalition",
        "The Pale Covenant", "Highreach Federation", "The Drowned Kingdom", "Emberfall Concord"
    };

    private static readonly CivilizationEthos[] EthosPool =
    {
        CivilizationEthos.Sovereign, CivilizationEthos.Mercantile, CivilizationEthos.Martial,
        CivilizationEthos.Mystic, CivilizationEthos.Ascetic
    };

    private readonly List<LeaderboardBoardVM> _boards = new();
    private int _count = 12;
    private int _seed = 20260526;

    public LeaderboardBoardVM GetLeaderboard(LeaderboardBoard board)
    {
        if (_boards.Count == 0) Regenerate();
        return _boards.First(b => b.board == board);
    }

    public IReadOnlyList<LeaderboardBoardVM> GetLeaderboards()
    {
        if (_boards.Count == 0) Regenerate();
        return _boards;
    }

    public void ConfigureDevSeed(int count, int seed)
    {
        _count = Math.Max(1, count);
        _seed = seed;
        Regenerate();
    }

    public void Refresh()
    {
        Regenerate();
    }

    private void Regenerate()
    {
        var rnd = new Random(_seed);

        // Generate the "other" realms with random metrics across all boards.
        var realms = new List<Realm>(_count);
        for (var i = 0; i < _count - 1; i++)
        {
            var name = RealmNames[i % RealmNames.Length];
            if (i >= RealmNames.Length) name = $"{name} {i / RealmNames.Length + 1}";

            realms.Add(new Realm(
                GenerateGuid(rnd).ToString("N"),
                name,
                EthosPool[rnd.Next(EthosPool.Length)],
                rnd.Next(0, 5),       // CivilizationRank value
                rnd.Next(0, 400),     // war kills
                rnd.Next(1, 2000),    // age in days
                rnd.Next(0, 13),      // milestones completed
                false));
        }

        // Seed the viewer's realm with the median of each metric so it lands
        // mid-table on every board (exercises the self-pin/highlight in slice 2).
        var viewer = new Realm(
            GenerateGuid(rnd).ToString("N"),
            "Ashfell",
            CivilizationEthos.Martial,
            Median(realms.Select(r => r.RankValue)),
            Median(realms.Select(r => r.ConquestKills)),
            Median(realms.Select(r => r.EnduranceDays)),
            Median(realms.Select(r => r.DeedsCount)),
            true);
        realms.Add(viewer);

        _boards.Clear();
        _boards.Add(BuildBoard(LeaderboardBoard.Standing, realms, r => r.RankValue));
        _boards.Add(BuildBoard(LeaderboardBoard.Conquest, realms, r => r.ConquestKills));
        _boards.Add(BuildBoard(LeaderboardBoard.Endurance, realms, r => r.EnduranceDays));
        _boards.Add(BuildBoard(LeaderboardBoard.Deeds, realms, r => r.DeedsCount));
    }

    private static LeaderboardBoardVM BuildBoard(
        LeaderboardBoard board, IReadOnlyList<Realm> realms, Func<Realm, long> score)
    {
        // Older realm wins score ties (tie-breaking is finalised in slice 4).
        var ordered = realms
            .OrderByDescending(score)
            .ThenByDescending(r => r.EnduranceDays)
            .ToList();

        var entries = new List<LeaderboardEntryVM>(ordered.Count);
        var viewerPosition = 0;
        for (var i = 0; i < ordered.Count; i++)
        {
            var realm = ordered[i];
            var position = i + 1;
            if (realm.IsViewer) viewerPosition = position;

            entries.Add(new LeaderboardEntryVM(
                position,
                realm.CivId,
                realm.Name,
                realm.Ethos,
                ((CivilizationRank)realm.RankValue).ToString(),
                score(realm),
                realm.IsViewer));
        }

        return new LeaderboardBoardVM(board, entries, viewerPosition, ordered.Count);
    }

    private static int Median(IEnumerable<int> values)
    {
        var sorted = values.OrderBy(v => v).ToArray();
        return sorted.Length == 0 ? 0 : sorted[sorted.Length / 2];
    }

    private static Guid GenerateGuid(Random rnd)
    {
        var bytes = new byte[16];
        rnd.NextBytes(bytes);
        return new Guid(bytes);
    }

    private sealed record Realm(
        string CivId,
        string Name,
        CivilizationEthos Ethos,
        int RankValue,
        int ConquestKills,
        int EnduranceDays,
        int DeedsCount,
        bool IsViewer);
}
