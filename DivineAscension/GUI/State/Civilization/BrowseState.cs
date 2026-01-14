using System.Collections.Generic;
using DivineAscension.GUI.Interfaces;
using DivineAscension.Network.Civilization;

namespace DivineAscension.GUI.State.Civilization;

public class BrowseState : IState
{
    public string DeityFilter { get; set; } = string.Empty;
    public string SearchText { get; set; } = string.Empty;
    public List<CivilizationListResponsePacket.CivilizationInfo> AllCivilizations { get; set; } = new();
    public float BrowseScrollY { get; set; }
    public bool IsDeityFilterOpen { get; set; }
    public bool IsLoading { get; set; }
    public string? ErrorMsg { get; set; }
    public string? SelectedCivId { get; set; } // Row selection state for table

    public void Reset()
    {
        DeityFilter = string.Empty;
        SearchText = string.Empty;
        AllCivilizations.Clear();
        BrowseScrollY = 0f;
        IsDeityFilterOpen = false;
    }
}