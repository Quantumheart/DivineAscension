using DivineAscension.GUI.Interfaces;
using DivineAscension.Network.HolySite;

namespace DivineAscension.GUI.State.HolySite;

/// <summary>
/// State for the Holy Sites detail view (selected site details)
/// </summary>
public class DetailState : IState
{
    /// <summary>
    /// UID of the holy site being viewed (null = show browse table)
    /// </summary>
    public string? ViewingSiteUID { get; set; }

    /// <summary>
    /// Detailed information for the viewing site
    /// </summary>
    public HolySiteResponsePacket.HolySiteDetailInfo? ViewingSiteDetails { get; set; }

    /// <summary>
    /// Whether the UI is currently loading data
    /// </summary>
    public bool IsLoading { get; set; }

    /// <summary>
    /// Error message to display to the user
    /// </summary>
    public string? ErrorMsg { get; set; }

    // Editing state for rename
    /// <summary>
    /// Whether the user is currently editing the holy site name
    /// </summary>
    public bool IsEditingName { get; set; }

    /// <summary>
    /// The current value in the name edit field
    /// </summary>
    public string? EditingNameValue { get; set; }

    // Editing state for description
    /// <summary>
    /// Whether the user is currently editing the description
    /// </summary>
    public bool IsEditingDescription { get; set; }

    /// <summary>
    /// The current value in the description edit field
    /// </summary>
    public string? EditingDescriptionValue { get; set; }

    public void Reset()
    {
        ViewingSiteUID = null;
        ViewingSiteDetails = null;
        IsLoading = false;
        ErrorMsg = null;
        IsEditingName = false;
        EditingNameValue = null;
        IsEditingDescription = false;
        EditingDescriptionValue = null;
    }
}
