using System.Collections.Generic;
using PantheonWars.GUI.Interfaces;
using PantheonWars.Network.Civilization;

namespace PantheonWars.GUI.State.Civilization;

public class BrowseState : IState
{
    public string DeityFilter { get; set; } = string.Empty;
    public string SearchText { get; set; } = string.Empty;
    public List<CivilizationListResponsePacket.CivilizationInfo> AllCivilizations { get; set; } = new();
    public float BrowseScrollY { get; set; }
    public bool IsDeityFilterOpen { get; set; }
    public bool IsLoading { get; set; }
    public string? ErrorMsg { get; set; }

    public void Reset()
    {
        DeityFilter = string.Empty;
        SearchText = string.Empty;
        AllCivilizations.Clear();
        BrowseScrollY = 0f;
        IsDeityFilterOpen = false;
    }
}