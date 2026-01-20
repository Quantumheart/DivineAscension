using DivineAscension.GUI.Interfaces;

namespace DivineAscension.GUI.State.Civilization;

/// <summary>
/// State for the Holy Sites sub-tab in the Civilization UI.
/// Uses browse/detail pattern for navigation.
/// </summary>
public class HolySitesState : IState
{
    /// <summary>
    /// State for the browse view (table of holy sites)
    /// </summary>
    public HolySite.BrowseState Browse { get; set; } = new();

    /// <summary>
    /// State for the detail view (single holy site details)
    /// </summary>
    public HolySite.DetailState Detail { get; set; } = new();

    public void Reset()
    {
        Browse.Reset();
        Detail.Reset();
    }
}
