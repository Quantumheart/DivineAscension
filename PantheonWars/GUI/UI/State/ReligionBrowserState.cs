using System.Collections.Generic;
using PantheonWars.Network;

namespace PantheonWars.GUI.UI.State;

/// <summary>
///     State management for Religion Browser overlay
/// </summary>
public class ReligionBrowserState
{
    /// <summary>
    ///     Selected deity filter ("All" or specific deity name)
    /// </summary>
    public string SelectedDeityFilter { get; set; } = "All";

    /// <summary>
    ///     Currently selected religion UID (for highlighting)
    /// </summary>
    public string? SelectedReligionUID { get; set; }

    /// <summary>
    ///     Current scroll position in religion list
    /// </summary>
    public float ScrollY { get; set; }

    /// <summary>
    ///     List of religions received from server
    /// </summary>
    public List<ReligionListResponsePacket.ReligionInfo> Religions { get; set; } = new();

    /// <summary>
    ///     Whether religions are currently being loaded from server
    /// </summary>
    public bool IsLoading { get; set; } = true;

    /// <summary>
    ///     Initialize/reset all state to defaults
    /// </summary>
    public void Reset()
    {
        SelectedDeityFilter = "All";
        SelectedReligionUID = null;
        ScrollY = 0f;
        Religions.Clear();
        IsLoading = true;
    }

    /// <summary>
    ///     Update religion list from server response
    /// </summary>
    public void UpdateReligionList(List<ReligionListResponsePacket.ReligionInfo> religions)
    {
        Religions = religions;
        IsLoading = false;
    }
}