using System.Collections.Generic;
using System.Linq;
using DivineAscension.GUI.Interfaces;
using DivineAscension.Models.Enum;
using DivineAscension.Network.Civilization;

namespace DivineAscension.GUI.State.Civilization;

/// <summary>
///     State for the Standing of Realms leaderboard chapter. Holds every board
///     returned by the server, the currently selected board, plus view-local
///     scroll/loading. Boards are fetched in one response, so switching boards
///     is purely client-side.
/// </summary>
public class LeaderboardState : IState
{
    /// <summary>All boards keyed by metric, each holding its ordered entries + viewer position.</summary>
    public Dictionary<LeaderboardMetric, LeaderboardResponsePacket.Board> Boards { get; set; } = new();

    /// <summary>The board the viewer is currently looking at.</summary>
    public LeaderboardMetric SelectedBoard { get; set; } = LeaderboardMetric.Standing;

    /// <summary>Total ranked realms, for the standing summary line (shared across boards).</summary>
    public int TotalRealms { get; set; }

    public bool IsLoading { get; set; }

    public string? ErrorMsg { get; set; }

    public float ScrollY { get; set; }

    /// <summary>Entries for the currently selected board, or empty when not yet loaded.</summary>
    public List<LeaderboardResponsePacket.LeaderboardEntry> SelectedEntries =>
        Boards.TryGetValue(SelectedBoard, out var board)
            ? board.Entries
            : new List<LeaderboardResponsePacket.LeaderboardEntry>();

    /// <summary>Viewer's own position in the currently selected board, or 0 when they have no realm.</summary>
    public int SelectedViewerPosition =>
        Boards.TryGetValue(SelectedBoard, out var board) ? board.ViewerPosition : 0;

    public void UpdateFromPacket(LeaderboardResponsePacket packet)
    {
        Boards = packet.Boards.ToDictionary(b => (LeaderboardMetric)b.Metric);
        TotalRealms = packet.TotalRealms;
        IsLoading = false;
        ErrorMsg = null;
    }

    public void Reset()
    {
        Boards.Clear();
        SelectedBoard = LeaderboardMetric.Standing;
        TotalRealms = 0;
        IsLoading = false;
        ErrorMsg = null;
        ScrollY = 0f;
    }
}
