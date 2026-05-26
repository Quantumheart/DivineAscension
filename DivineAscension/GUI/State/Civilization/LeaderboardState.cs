using System.Collections.Generic;
using DivineAscension.GUI.Interfaces;
using DivineAscension.Network.Civilization;

namespace DivineAscension.GUI.State.Civilization;

/// <summary>
///     State for the Standing of Realms leaderboard chapter. Holds the ranked
///     list of realms returned by the server plus view-local scroll/loading.
/// </summary>
public class LeaderboardState : IState
{
    /// <summary>Realms ranked by Standing, highest first.</summary>
    public List<LeaderboardResponsePacket.LeaderboardEntry> Entries { get; set; } = new();

    /// <summary>Viewer's own realm position (1-based), or 0 when they have no realm.</summary>
    public int ViewerPosition { get; set; }

    /// <summary>Total ranked realms, for the standing summary line.</summary>
    public int TotalRealms { get; set; }

    public bool IsLoading { get; set; }

    public string? ErrorMsg { get; set; }

    public float ScrollY { get; set; }

    public void UpdateFromPacket(LeaderboardResponsePacket packet)
    {
        Entries = packet.Entries;
        ViewerPosition = packet.ViewerPosition;
        TotalRealms = packet.TotalRealms;
        IsLoading = false;
        ErrorMsg = null;
    }

    public void Reset()
    {
        Entries.Clear();
        ViewerPosition = 0;
        TotalRealms = 0;
        IsLoading = false;
        ErrorMsg = null;
        ScrollY = 0f;
    }
}
