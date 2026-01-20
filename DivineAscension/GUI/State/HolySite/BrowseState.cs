using System.Collections.Generic;
using DivineAscension.GUI.Interfaces;
using DivineAscension.Network.HolySite;

namespace DivineAscension.GUI.State.HolySite;

/// <summary>
/// State for the Holy Sites browse view (table display)
/// </summary>
public class BrowseState : IState
{
    /// <summary>
    /// All sites (unfiltered from network)
    /// </summary>
    public List<HolySiteResponsePacket.HolySiteInfo> AllSites { get; set; } = new();

    /// <summary>
    /// Sites filtered to current civilization's member religions, grouped by religion UID
    /// </summary>
    public Dictionary<string, List<HolySiteResponsePacket.HolySiteInfo>> SitesByReligion { get; set; } = new();

    /// <summary>
    /// UID of the selected site in the browse table (for row highlighting)
    /// </summary>
    public string? SelectedSiteUID { get; set; }

    /// <summary>
    /// Scroll position for the browse table
    /// </summary>
    public float ScrollY { get; set; }

    /// <summary>
    /// Whether the UI is currently loading data
    /// </summary>
    public bool IsLoading { get; set; }

    /// <summary>
    /// Error message to display to the user
    /// </summary>
    public string? ErrorMsg { get; set; }

    public void Reset()
    {
        AllSites.Clear();
        SitesByReligion.Clear();
        SelectedSiteUID = null;
        ScrollY = 0f;
        IsLoading = false;
        ErrorMsg = null;
    }
}
