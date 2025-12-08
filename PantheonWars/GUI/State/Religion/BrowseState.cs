using System.Collections.Generic;
using PantheonWars.Network;

namespace PantheonWars.GUI.State.Religion;

public class BrowseState
{
    public string DeityFilter { get; set; } = string.Empty;
    public List<ReligionListResponsePacket.ReligionInfo> AllReligions { get; set; } = new();
    public float BrowseScrollY { get; set; }
    public bool IsBrowseLoading { get; set; }
    public string? SelectedReligionUID { get; set; }

    public void Reset()
    {
        DeityFilter = string.Empty;
        AllReligions.Clear();
        BrowseScrollY = 0f;
        IsBrowseLoading = false;
        SelectedReligionUID = null;
    }
}