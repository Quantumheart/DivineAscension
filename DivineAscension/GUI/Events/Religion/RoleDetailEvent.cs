namespace PantheonWars.GUI.Events.Religion;

/// <summary>
///     Events emitted from the role detail view (viewing members with a specific role).
/// </summary>
public abstract record RoleDetailEvent
{
    // Navigation
    public record BackToRolesClicked : RoleDetailEvent;

    // Scrolling
    public record MemberScrollChanged(float NewScrollY) : RoleDetailEvent;

    // Role assignment
    public record AssignRoleDropdownToggled(string MemberUID, bool IsOpen) : RoleDetailEvent;

    public record AssignRoleConfirmOpen(
        string MemberUID,
        string MemberName,
        string CurrentRoleUID,
        string NewRoleUID,
        string NewRoleName) : RoleDetailEvent;

    public record AssignRoleConfirm(string MemberUID, string NewRoleUID) : RoleDetailEvent;

    public record AssignRoleCancel : RoleDetailEvent;
}