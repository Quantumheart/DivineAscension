namespace DivineAscension.GUI.Events.HolySite;

/// <summary>
/// Events for the holy sites browse view (table display)
/// </summary>
public abstract record BrowseEvent
{
    /// <summary>
    /// User clicked a row in the holy sites table
    /// </summary>
    public sealed record Selected(string SiteUID, float ScrollY) : BrowseEvent;

    /// <summary>
    /// User clicked the refresh button
    /// </summary>
    public sealed record RefreshClicked : BrowseEvent;

    /// <summary>
    /// Scroll position changed
    /// </summary>
    public sealed record ScrollChanged(float NewScrollY) : BrowseEvent;
}
