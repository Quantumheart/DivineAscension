using System.Collections.Generic;
using DivineAscension.Network.Civilization;

namespace DivineAscension.GUI.Models.Civilization.Leaderboard;

/// <summary>
///     ViewModel for the Standing of Realms leaderboard chapter (slice 1).
/// </summary>
public readonly struct CivilizationLeaderboardViewModel(
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
    public List<LeaderboardResponsePacket.LeaderboardEntry> Entries { get; } = entries;

    /// <summary>Viewer's own realm position (1-based), or 0 when they have no realm.</summary>
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
