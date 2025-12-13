using System;
using System.Collections.Generic;
using System.Linq;
using PantheonWars.Data;
using PantheonWars.Models;
using PantheonWars.Network;

namespace PantheonWars.GUI.Models.Religion.Roles;

/// <summary>
///     Immutable view model for the role detail view (viewing members with a specific role).
///     Contains role data and UI state for role assignment.
/// </summary>
public readonly struct ReligionRoleDetailViewModel(
    // Which role we're viewing
    string viewingRoleUID,
    string viewingRoleName,
    // Data
    bool isLoading,
    string currentPlayerUID,
    ReligionRolesResponse? rolesData,
    // UI state - Role Assignment
    string? openAssignRoleDropdownMemberUID,
    bool showAssignRoleConfirm,
    string? assignRoleConfirmMemberUID,
    string? assignRoleConfirmMemberName,
    string? assignRoleConfirmCurrentRoleUID,
    string? assignRoleConfirmNewRoleUID,
    string? assignRoleConfirmNewRoleName,
    // Layout & scrolling
    float x,
    float y,
    float width,
    float height,
    float memberScrollY)
{
    // Which role we're viewing
    public string ViewingRoleUID { get; } = viewingRoleUID;
    public string ViewingRoleName { get; } = viewingRoleName;

    // Core data
    public bool IsLoading { get; } = isLoading;
    public string CurrentPlayerUID { get; } = currentPlayerUID;
    public ReligionRolesResponse? RolesData { get; } = rolesData;

    // UI state - Role Assignment
    public string? OpenAssignRoleDropdownMemberUID { get; } = openAssignRoleDropdownMemberUID;
    public bool ShowAssignRoleConfirm { get; } = showAssignRoleConfirm;
    public string? AssignRoleConfirmMemberUID { get; } = assignRoleConfirmMemberUID;
    public string? AssignRoleConfirmMemberName { get; } = assignRoleConfirmMemberName;
    public string? AssignRoleConfirmCurrentRoleUID { get; } = assignRoleConfirmCurrentRoleUID;
    public string? AssignRoleConfirmNewRoleUID { get; } = assignRoleConfirmNewRoleUID;
    public string? AssignRoleConfirmNewRoleName { get; } = assignRoleConfirmNewRoleName;

    // Layout & scrolling
    public float X { get; } = x;
    public float Y { get; } = y;
    public float Width { get; } = width;
    public float Height { get; } = height;
    public float MemberScrollY { get; } = memberScrollY;

    // Computed properties
    public bool HasRolesData => RolesData != null && RolesData.Success;
    public IReadOnlyList<RoleData> Roles => (IReadOnlyList<RoleData>?)RolesData?.Roles ?? Array.Empty<RoleData>();

    public IReadOnlyDictionary<string, string> MemberRoles =>
        (IReadOnlyDictionary<string, string>?)RolesData?.MemberRoles ?? new Dictionary<string, string>();

    public IReadOnlyDictionary<string, string> MemberNames =>
        (IReadOnlyDictionary<string, string>?)RolesData?.MemberNames ?? new Dictionary<string, string>();

    /// <summary>
    ///     Gets the list of member UIDs that have the role being viewed.
    /// </summary>
    public List<string> GetMembersWithRole()
    {
        if (MemberRoles == null) return new List<string>();
        var roleUID = ViewingRoleUID; // Copy to local for lambda capture
        return MemberRoles
            .Where(kvp => kvp.Value == roleUID)
            .Select(kvp => kvp.Key)
            .ToList();
    }

    /// <summary>
    ///     Checks if the current player has permission to manage roles.
    /// </summary>
    public bool CanManageRoles()
    {
        if (!HasRolesData || string.IsNullOrEmpty(CurrentPlayerUID)) return false;

        var playerRoleUID = MemberRoles.TryGetValue(CurrentPlayerUID, out var roleUID) ? roleUID : null;
        if (playerRoleUID == null) return false;

        var playerRole = Roles.FirstOrDefault(r => r.RoleUID == playerRoleUID);
        return playerRole?.HasPermission(RolePermissions.MANAGE_ROLES) ?? false;
    }

    /// <summary>
    ///     Checks if the current player can assign a role to a specific member.
    /// </summary>
    public bool CanAssignRoleToMember(string targetMemberUID)
    {
        if (!CanManageRoles()) return false;
        if (targetMemberUID == CurrentPlayerUID) return false; // Can't change own role

        // Can't change Founder's role
        var targetRoleUID = MemberRoles.TryGetValue(targetMemberUID, out var roleUID) ? roleUID : null;
        if (targetRoleUID == RoleDefaults.FOUNDER_ROLE_ID) return false;

        return true;
    }

    /// <summary>
    ///     Gets the list of roles that can be assigned (excludes Founder role).
    /// </summary>
    public IReadOnlyList<RoleData> GetAssignableRoles()
    {
        return Roles
            .Where(r => r.RoleUID != RoleDefaults.FOUNDER_ROLE_ID)
            .OrderBy(r => r.DisplayOrder)
            .ThenBy(r => r.RoleName)
            .ToList();
    }
}