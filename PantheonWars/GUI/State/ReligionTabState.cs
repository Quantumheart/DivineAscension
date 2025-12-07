using System.Collections.Generic;
using PantheonWars.Network;

namespace PantheonWars.GUI.State;


/// <summary>
///     State container for the Religion tab in BlessingDialog
///     Follows the same pattern as CivilizationState
/// </summary>
public class ReligionTabState
{
    // Tab navigation
    public ReligionSubTab CurrentSubTab { get; set; } // 0=Browse, 1=MyReligion, 2=Activity, 3=Create

    // Browse tab state
    public string DeityFilter { get; set; } = string.Empty;
    public List<ReligionListResponsePacket.ReligionInfo> AllReligions { get; set; } = new();
    public float BrowseScrollY { get; set; }
    public bool IsBrowseLoading { get; set; }
    public string? SelectedReligionUID { get; set; }

    // My Religion tab state
    public PlayerReligionInfoResponsePacket? MyReligionInfo { get; set; }
    public float MyReligionScrollY { get; set; }
    public float MemberScrollY { get; set; }
    public float BanListScrollY { get; set; }
    public string InvitePlayerName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsMyReligionLoading { get; set; }
    public bool ShowDisbandConfirm { get; set; }
    public string? KickConfirmPlayerUID { get; set; }
    public string? BanConfirmPlayerUID { get; set; }
    public string? KickConfirmPlayerName { get; set; }
    public string? BanConfirmPlayerName { get; set; }

    // Invites sub-tab state (visible when player has no religion)
    public List<PlayerReligionInfoResponsePacket.ReligionInviteInfo> MyInvites { get; set; } = new();
    public float InvitesScrollY { get; set; }
    public bool IsInvitesLoading { get; set; }
    public string? InvitesError { get; set; }
    
    // Activity tab state (placeholder for now)
    public List<string> ActivityLog { get; set; } = new(); // Future: activity events
    public float ActivityScrollY { get; set; }

    // Bonuses tab state
    public float BonusScrollY { get; set; }

    // Error handling
    public string? LastActionError { get; set; }
    public string? BrowseError { get; set; }
    public string? MyReligionError { get; set; }
    public string? CreateError { get; set; }

    public ReligionCreateState ReligionCreateState { get; } = new ();

    /// <summary>
    ///     Reset all state to default values
    /// </summary>
    public void Reset()
    {
        CurrentSubTab = ReligionSubTab.Browse;

        // Browse tab
        DeityFilter = string.Empty;
        AllReligions.Clear();
        BrowseScrollY = 0f;
        IsBrowseLoading = false;
        SelectedReligionUID = null;

        // My Religion tab
        MyReligionInfo = null;
        MyReligionScrollY = 0f;
        MemberScrollY = 0f;
        BanListScrollY = 0f;
        InvitePlayerName = string.Empty;
        Description = string.Empty;
        IsMyReligionLoading = false;
        ShowDisbandConfirm = false;
        KickConfirmPlayerUID = null;
        BanConfirmPlayerUID = null;
        KickConfirmPlayerName = null;
        BanConfirmPlayerName = null;

        // Invites tab
        MyInvites.Clear();
        InvitesScrollY = 0f;
        IsInvitesLoading = false;
        InvitesError = null;

        // Create tab
        ReligionCreateState.Name = string.Empty;
        ReligionCreateState.DeityName = "Khoras";
        ReligionCreateState.IsPublic = true;

        // Activity tab
        ActivityLog.Clear();
        ActivityScrollY = 0f;

        // Bonuses tab
        BonusScrollY = 0f;

        // Errors
        LastActionError = null;
        BrowseError = null;
        MyReligionError = null;
        CreateError = null;
    }
}

public class ReligionCreateState
{
    public string Name { get; set; } = string.Empty;
    public string DeityName { get; set; } = "Khoras";
    public bool IsPublic { get; set; } = true;
}


public enum ReligionSubTab
{
    Browse = 0,
    MyReligion = 1,
    Activity = 2,
    Invites = 3,
    Create = 4
}