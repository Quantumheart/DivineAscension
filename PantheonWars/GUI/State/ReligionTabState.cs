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
    public float BrowseScrollY { get; set; } = 0f;
    public bool IsBrowseLoading { get; set; } = false;
    public string? SelectedReligionUID { get; set; }

    // My Religion tab state
    public PlayerReligionInfoResponsePacket? MyReligionInfo { get; set; }
    public float MyReligionScrollY { get; set; } = 0f;
    public float MemberScrollY { get; set; } = 0f;
    public float BanListScrollY { get; set; } = 0f;
    public string InvitePlayerName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsMyReligionLoading { get; set; } = false;
    public bool ShowDisbandConfirm { get; set; } = false;
    public string? KickConfirmPlayerUID { get; set; }
    public string? BanConfirmPlayerUID { get; set; }
    public string? KickConfirmPlayerName { get; set; }
    public string? BanConfirmPlayerName { get; set; }

    // Invites sub-tab state (visible when player has no religion)
    public List<PlayerReligionInfoResponsePacket.ReligionInviteInfo> MyInvites { get; set; } = new();
    public float InvitesScrollY { get; set; } = 0f;
    public bool IsInvitesLoading { get; set; } = false;
    public string? InvitesError { get; set; }

    // Create tab state
    public string CreateReligionName { get; set; } = string.Empty;
    public string CreateDeity { get; set; } = "Khoras";
    public bool CreateIsPublic { get; set; } = true;

    // Activity tab state (placeholder for now)
    public List<string> ActivityLog { get; set; } = new(); // Future: activity events
    public float ActivityScrollY { get; set; } = 0f;

    // Bonuses tab state
    public float BonusScrollY { get; set; } = 0f;

    // Error handling
    public string? LastActionError { get; set; }
    public string? BrowseError { get; set; }
    public string? MyReligionError { get; set; }
    public string? CreateError { get; set; }

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
        CreateReligionName = string.Empty;
        CreateDeity = "Khoras";
        CreateIsPublic = true;

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
public enum ReligionSubTab
{
    Browse = 0,
    MyReligion = 1,
    Activity = 2,
    Invites = 3,
    Create = 4
}
