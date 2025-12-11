using System.Collections.Generic;
using PantheonWars.Network;

namespace PantheonWars.GUI.State.Religion;

/// <summary>
///     Mutable state for the Roles tab within the Religion dialog.
///     Manages role data, UI state, and editor dialogs.
/// </summary>
public class RolesState
{
    public ReligionRolesResponse? RolesData { get; set; }
    public float ScrollY { get; set; }
    public bool Loading { get; set; }

    // Role editor state
    public bool ShowRoleEditor { get; set; }
    public string? EditingRoleUID { get; set; }
    public string EditingRoleName { get; set; } = string.Empty;
    public HashSet<string> EditingPermissions { get; set; } = new();

    // Create custom role
    public bool ShowCreateRoleDialog { get; set; }
    public string NewRoleName { get; set; } = string.Empty;

    // Delete role confirmation
    public bool ShowDeleteConfirm { get; set; }
    public string? DeleteRoleUID { get; set; }
    public string? DeleteRoleName { get; set; }

    // Member list for role
    public bool ShowRoleMembersDialog { get; set; }
    public string? ViewingRoleUID { get; set; }
    public string? ViewingRoleName { get; set; }

    // Assign role
    public string? AssignRoleTargetUID { get; set; }
    public string? AssignRoleTargetName { get; set; }
    public string? SelectedRoleForAssignment { get; set; }

    public void Reset()
    {
        RolesData = null;
        ScrollY = 0f;
        Loading = false;
        ShowRoleEditor = false;
        EditingRoleUID = null;
        EditingRoleName = string.Empty;
        EditingPermissions.Clear();
        ShowCreateRoleDialog = false;
        NewRoleName = string.Empty;
        ShowDeleteConfirm = false;
        DeleteRoleUID = null;
        DeleteRoleName = null;
        ShowRoleMembersDialog = false;
        ViewingRoleUID = null;
        ViewingRoleName = null;
        AssignRoleTargetUID = null;
        AssignRoleTargetName = null;
        SelectedRoleForAssignment = null;
    }
}