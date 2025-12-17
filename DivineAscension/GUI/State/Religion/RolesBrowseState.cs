using System.Collections.Generic;
using PantheonWars.GUI.Interfaces;

namespace PantheonWars.GUI.State.Religion;

/// <summary>
///     State for the roles browse view (list of role cards).
///     Includes state for dialogs that overlay the browse view (create, edit, delete).
/// </summary>
public class RolesBrowseState : IState
{
    /// <summary>
    ///     Scroll position for the role cards list.
    /// </summary>
    public float ScrollY { get; set; }

    // Role editor state (overlays browse view)
    public bool ShowRoleEditor { get; set; }
    public string? EditingRoleUID { get; set; }
    public string EditingRoleName { get; set; } = string.Empty;
    public HashSet<string> EditingPermissions { get; set; } = new();

    // Create custom role dialog (overlays browse view)
    public bool ShowCreateRoleDialog { get; set; }
    public string NewRoleName { get; set; } = string.Empty;

    // Delete role confirmation (overlays browse view)
    public bool ShowDeleteConfirm { get; set; }
    public string? DeleteRoleUID { get; set; }
    public string? DeleteRoleName { get; set; }

    public void Reset()
    {
        ScrollY = 0f;
        ShowRoleEditor = false;
        EditingRoleUID = null;
        EditingRoleName = string.Empty;
        EditingPermissions.Clear();
        ShowCreateRoleDialog = false;
        NewRoleName = string.Empty;
        ShowDeleteConfirm = false;
        DeleteRoleUID = null;
        DeleteRoleName = null;
    }
}