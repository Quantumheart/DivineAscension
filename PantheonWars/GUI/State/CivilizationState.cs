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
    public float BrowseScrollY { get; set; } = 0f;

    // My Civilization tab
    public CivilizationInfoResponsePacket.CivilizationDetails? MyCivilization { get; set; }
    public float MemberScrollY { get; set; } = 0f;
    public string InviteReligionName { get; set; } = string.Empty;

    // Invites tab
    public List<CivilizationInfoResponsePacket.PendingInvite> MyInvites { get; set; } = new();
    public float InvitesScrollY { get; set; } = 0f;

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
        BrowseScrollY = 0f;
        MyCivilization = null;
        MemberScrollY = 0f;
        InviteReligionName = string.Empty;
        MyInvites.Clear();
        InvitesScrollY = 0f;
        CreateCivName = string.Empty;
        CreateDescription = string.Empty;
    }
}
