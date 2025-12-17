using PantheonWars.GUI.Interfaces;

namespace PantheonWars.GUI.State.Religion;

/// <summary>
///     State for the role detail view (viewing members with a specific role).
///     Used when transitioning from browse to detail view.
/// </summary>
public class RoleDetailState : IState
{
    /// <summary>
    ///     The UID of the role currently being viewed.
    ///     null = browse mode (showing role cards)
    ///     not-null = detail mode (showing members with this role)
    /// </summary>
    public string? ViewingRoleUID { get; set; }

    /// <summary>
    ///     The name of the role currently being viewed (for display).
    /// </summary>
    public string? ViewingRoleName { get; set; }

    /// <summary>
    ///     Scroll position for the member list in the detail view.
    /// </summary>
    public float MemberScrollY { get; set; }

    /// <summary>
    ///     UID of the member whose role assignment dropdown is currently open.
    ///     Only one dropdown can be open at a time (null = all closed).
    /// </summary>
    public string? OpenAssignRoleDropdownMemberUID { get; set; }

    // Role assignment confirmation modal state
    public bool ShowAssignRoleConfirm { get; set; }
    public string? AssignRoleConfirmMemberUID { get; set; }
    public string? AssignRoleConfirmMemberName { get; set; }
    public string? AssignRoleConfirmCurrentRoleUID { get; set; }
    public string? AssignRoleConfirmNewRoleUID { get; set; }
    public string? AssignRoleConfirmNewRoleName { get; set; }

    public void Reset()
    {
        ViewingRoleUID = null;
        ViewingRoleName = null;
        MemberScrollY = 0f;
        OpenAssignRoleDropdownMemberUID = null;
        ShowAssignRoleConfirm = false;
        AssignRoleConfirmMemberUID = null;
        AssignRoleConfirmMemberName = null;
        AssignRoleConfirmCurrentRoleUID = null;
        AssignRoleConfirmNewRoleUID = null;
        AssignRoleConfirmNewRoleName = null;
    }
}