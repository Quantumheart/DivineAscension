using System.Collections.Generic;

namespace DivineAscension.GUI.Events.Religion;

/// <summary>
///     Events emitted from the roles browse view (role cards list).
/// </summary>
public abstract record RolesBrowseEvent
{
    // Navigation
    public record ViewRoleDetailsClicked(string RoleUID, string RoleName) : RolesBrowseEvent;

    // Scrolling
    public record ScrollChanged(float NewScrollY) : RolesBrowseEvent;

    // Role creation
    public record CreateRoleOpen : RolesBrowseEvent;

    public record CreateRoleCancel : RolesBrowseEvent;

    public record CreateRoleNameChanged(string RoleName) : RolesBrowseEvent;

    public record CreateRoleConfirm(string RoleName) : RolesBrowseEvent;

    // Role editing
    public record EditRoleOpen(string RoleUID) : RolesBrowseEvent;

    public record EditRoleCancel : RolesBrowseEvent;

    public record EditRoleNameChanged(string RoleName) : RolesBrowseEvent;

    public record EditRolePermissionToggled(string Permission, bool Enabled) : RolesBrowseEvent;

    public record EditRoleSave(string RoleUID, string RoleName, HashSet<string> Permissions) : RolesBrowseEvent;

    // Role deletion
    public record DeleteRoleOpen(string RoleUID, string RoleName) : RolesBrowseEvent;

    public record DeleteRoleConfirm(string RoleUID) : RolesBrowseEvent;

    public record DeleteRoleCancel : RolesBrowseEvent;

    // Refresh
    public record RefreshRequested : RolesBrowseEvent;
}