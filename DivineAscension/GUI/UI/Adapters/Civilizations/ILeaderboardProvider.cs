using System.Collections.Generic;
using DivineAscension.Models.Enum;

namespace DivineAscension.GUI.UI.Adapters.Civilizations;

/// <summary>
///     The four themed boards of the Standing of Realms leaderboard (epic #496).
///     Each ranks civilizations by a different metric the civ systems already track.
/// </summary>
internal enum LeaderboardBoard
{
    Standing,  // Civilization rank (Nascent → Eternal)
    Conquest,  // War kill count
    Endurance, // Age since founding
    Deeds      // Milestones completed
}

/// <summary>
///     One ranked realm within a board.
/// </summary>
internal sealed record LeaderboardEntryVM(
    int position,        // 1-based rank within the board
    string civId,
    string name,
    CivilizationEthos ethos, // drives the per-realm glyph
    string tierLabel,    // the realm's standing, e.g. "Eternal" / "Dominant"
    long score,          // the board's metric for this realm
    bool isViewer        // the viewer's own realm (pinned/highlighted in slice 2)
);

/// <summary>
///     One board's ordered entries plus the viewer's own position and the total
///     number of ranked realms (for the "stands IV among XII" summary line).
/// </summary>
internal sealed record LeaderboardBoardVM(
    LeaderboardBoard board,
    IReadOnlyList<LeaderboardEntryVM> entries,
    int viewerPosition,  // 1-based; 0 when the viewer has no ranked realm
    int totalCount
);

/// <summary>
///     UI-only data source for the Standing of Realms leaderboard. Intended for swapping
///     between a real (network-backed) provider and a dev-only fake provider without
///     touching systems/persistence — mirrors <see cref="ICivilizationProvider" />.
/// </summary>
internal interface ILeaderboardProvider
{
    LeaderboardBoardVM GetLeaderboard(LeaderboardBoard board);
    IReadOnlyList<LeaderboardBoardVM> GetLeaderboards();
    void ConfigureDevSeed(int count, int seed);
    void Refresh();
}
