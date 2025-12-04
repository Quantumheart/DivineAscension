using PantheonWars.Network;

namespace PantheonWars.GUI.UI.State;

/// <summary>
///     State management for Religion Management overlay
/// </summary>
public class ReligionManagementState
{
    /// <summary>
    ///     Religion info received from server
    /// </summary>
    public PlayerReligionInfoResponsePacket? ReligionInfo { get; set; }

    /// <summary>
    ///     Current input text for inviting a player
    /// </summary>
    public string InvitePlayerName { get; set; } = "";

    /// <summary>
    ///     Current description text being edited
    /// </summary>
    public string Description { get; set; } = "";

    /// <summary>
    ///     Current scroll position in member list
    /// </summary>
    public float MemberScrollY { get; set; }

    /// <summary>
    ///     Current scroll position in banned players list
    /// </summary>
    public float BanListScrollY { get; set; }

    /// <summary>
    ///     Error message to display (if any)
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    ///     Whether the disband confirmation dialog is shown
    /// </summary>
    public bool ShowDisbandConfirm { get; set; }

    /// <summary>
    ///     Initialize/reset all state to defaults
    /// </summary>
    public void Reset()
    {
        ReligionInfo = null;
        InvitePlayerName = "";
        Description = "";
        MemberScrollY = 0f;
        BanListScrollY = 0f;
        ErrorMessage = null;
        ShowDisbandConfirm = false;
    }

    /// <summary>
    ///     Update religion info from server response
    /// </summary>
    public void UpdateReligionInfo(PlayerReligionInfoResponsePacket info)
    {
        ReligionInfo = info;
        Description = info.Description ?? "";
    }
}