using System.Collections.Generic;
using PantheonWars.Network.Civilization;

namespace PantheonWars.GUI.State;

/// <summary>
///     Holds all client-side state for the Civilization tab within the BlessingDialog.
///     Phase 1 scaffolding only — rendering will be added in later phases.
/// </summary>
public class CivilizationState
{
    // Main tab selection inside the Civilization tab (0=Browse, 1=My Civ, 2=Invites, 3=Create)
    public int CurrentSubTab { get; set; }

    // Browse tab
    public string DeityFilter { get; set; } = string.Empty;
    public string SearchText { get; set; } = string.Empty;
    public List<CivilizationListResponsePacket.CivilizationInfo> AllCivilizations { get; set; } = new();

    // My Civilization tab
    public CivilizationInfoResponsePacket.CivilizationDetails? MyCivilization { get; set; }

    // Invites tab
    public List<CivilizationInfoResponsePacket.PendingInvite> MyInvites { get; set; } = new();

    // Create tab
    public string CreateCivName { get; set; } = string.Empty;
    public string CreateDescription { get; set; } = string.Empty;

    /// <summary>
    ///     Reset the entire civilization state to defaults.
    /// </summary>
    public void Reset()
    {
        CurrentSubTab = 0;
        DeityFilter = string.Empty;
        SearchText = string.Empty;
        AllCivilizations.Clear();
        MyCivilization = null;
        MyInvites.Clear();
        CreateCivName = string.Empty;
        CreateDescription = string.Empty;
    }
}
