using System.Collections.Generic;
using DivineAscension.Models.Enum;
using DivineAscension.Network.Civilization;

namespace DivineAscension.GUI.Models.Civilization.Leaderboard;

/// <summary>
///     ViewModel for the Standing of Realms leaderboard chapter. Carries the
///     selected board's ordered entries plus the board selector state (#499).
/// </summary>
public readonly struct CivilizationLeaderboardViewModel(
    IReadOnlyList<LeaderboardMetric> boards,
    LeaderboardMetric selectedBoard,
    List<LeaderboardResponsePacket.LeaderboardEntry> entries,
    int viewerPosition,
    int totalRealms,
    bool isLoading,
    string? errorMsg,
    float scrollY,
    float x,
    float y,
    float width,
    float height)
{
    /// <summary>The selectable boards, in display order.</summary>
    public IReadOnlyList<LeaderboardMetric> Boards { get; } = boards;

    /// <summary>The board currently shown.</summary>
    public LeaderboardMetric SelectedBoard { get; } = selectedBoard;

    public List<LeaderboardResponsePacket.LeaderboardEntry> Entries { get; } = entries;

    /// <summary>Viewer's own realm position (1-based) in the selected board, or 0 when they have no realm.</summary>
    public int ViewerPosition { get; } = viewerPosition;

    /// <summary>Total ranked realms, for the standing summary line.</summary>
    public int TotalRealms { get; } = totalRealms;

    public bool IsLoading { get; } = isLoading;

    public string? ErrorMsg { get; } = errorMsg;

    public float ScrollY { get; } = scrollY;

    public float X { get; } = x;

    public float Y { get; } = y;

    public float Width { get; } = width;

    public float Height { get; } = height;
}
