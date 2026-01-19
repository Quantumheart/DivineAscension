using System.Collections.Generic;
using DivineAscension.GUI.Interfaces;
using DivineAscension.Network.HolySite;

namespace DivineAscension.GUI.State.Civilization;

/// <summary>
///     State for the Holy Sites sub-tab in the Civilization UI
/// </summary>
public class HolySitesState : IState
{
    /// <summary>
    ///     All sites (unfiltered from network)
    /// </summary>
    public List<HolySiteResponsePacket.HolySiteInfo> AllSites { get; set; } = new();

    /// <summary>
    ///     Sites filtered to current civilization's member religions, grouped by religion UID
    /// </summary>
    public Dictionary<string, List<HolySiteResponsePacket.HolySiteInfo>> SitesByReligion { get; set; } = new();

    /// <summary>
    ///     Whether the UI is currently loading data
    /// </summary>
    public bool IsLoading { get; set; }

    /// <summary>
    ///     Error message to display to the user
    /// </summary>
    public string? ErrorMsg { get; set; }

    /// <summary>
    ///     Scroll position for the holy sites panel
    /// </summary>
    public float ScrollY { get; set; }

    /// <summary>
    ///     Expanded religion sections (for collapsible groups)
    /// </summary>
    public HashSet<string> ExpandedReligions { get; set; } = new();

    public void Reset()
    {
        AllSites.Clear();
        SitesByReligion.Clear();
        IsLoading = false;
        ErrorMsg = null;
        ScrollY = 0f;
        ExpandedReligions.Clear();
    }
}
