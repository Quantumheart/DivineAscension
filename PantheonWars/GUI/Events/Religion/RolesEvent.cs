using System.Collections.Generic;

namespace PantheonWars.GUI.Events.Religion;

/// <summary>
///     Events representing user interactions within the Roles tab renderer.
///     Pure UI intents that the state manager will handle.
/// </summary>
public abstract record RolesEvent
{
    // Scrolling
    public record ScrollChanged(float NewScrollY) : RolesEvent;

    // Role creation
    public record CreateRoleOpen : RolesEvent;

    public record CreateRoleCancel : RolesEvent;

    public record CreateRoleNameChanged(string RoleName) : RolesEvent;

    public record CreateRoleConfirm(string RoleName) : RolesEvent;

    // Role editing
    public record EditRoleOpen(string RoleUID) : RolesEvent;

    public record EditRoleCancel : RolesEvent;

    public record EditRoleNameChanged(string RoleName) : RolesEvent;

    public record EditRolePermissionToggled(string Permission, bool Enabled) : RolesEvent;

    public record EditRoleSave(string RoleUID, string RoleName, HashSet<string> Permissions) : RolesEvent;

    // Role deletion
    public record DeleteRoleOpen(string RoleUID, string RoleName) : RolesEvent;

    public record DeleteRoleConfirm(string RoleUID) : RolesEvent;

    public record DeleteRoleCancel : RolesEvent;

    // View members with role
    public record ViewRoleMembersOpen(string RoleUID, string RoleName) : RolesEvent;

    public record ViewRoleMembersClose : RolesEvent;

    // Role assignment
    public record AssignRoleDropdownToggled(string MemberUID, bool IsOpen) : RolesEvent;

    public record AssignRoleConfirmOpen(
        string MemberUID,
        string MemberName,
        string CurrentRoleUID,
        string NewRoleUID,
        string NewRoleName) : RolesEvent;

    public record AssignRoleConfirm(string MemberUID, string NewRoleUID) : RolesEvent;

    public record AssignRoleCancel : RolesEvent;

    // Refresh roles data
    public record RefreshRequested : RolesEvent;
}