using System;
using System.Collections.Generic;
using System.Linq;
using PantheonWars.Data;
using PantheonWars.Models;
using PantheonWars.Network;

namespace PantheonWars.GUI.Models.Religion.Roles;

/// <summary>
///     Immutable view model for the roles browse view (role cards list).
///     Contains role data and UI state for role management (create, edit, delete).
/// </summary>
public readonly struct ReligionRolesBrowseViewModel(
    // Data
    bool isLoading,
    bool hasReligion,
    string religionUID,
    string currentPlayerUID,
    ReligionRolesResponse? rolesData,
    // UI state - Role Editor
    bool showRoleEditor,
    string? editingRoleUID,
    string editingRoleName,
    HashSet<string> editingPermissions,
    // UI state - Create Role Dialog
    bool showCreateRoleDialog,
    string newRoleName,
    // UI state - Delete Confirmation
    bool showDeleteConfirm,
    string? deleteRoleUID,
    string? deleteRoleName,
    // Layout & scrolling
    float x,
    float y,
    float width,
    float height,
    float scrollY)
{
    // Core data
    public bool IsLoading { get; } = isLoading;
    public bool HasReligion { get; } = hasReligion;
    public string ReligionUID { get; } = religionUID;
    public string CurrentPlayerUID { get; } = currentPlayerUID;
    public ReligionRolesResponse? RolesData { get; } = rolesData;

    // UI state - Role Editor
    public bool ShowRoleEditor { get; } = showRoleEditor;
    public string? EditingRoleUID { get; } = editingRoleUID;
    public string EditingRoleName { get; } = editingRoleName;
    public HashSet<string> EditingPermissions { get; } = editingPermissions;

    // UI state - Create Role Dialog
    public bool ShowCreateRoleDialog { get; } = showCreateRoleDialog;
    public string NewRoleName { get; } = newRoleName;

    // UI state - Delete Confirmation
    public bool ShowDeleteConfirm { get; } = showDeleteConfirm;
    public string? DeleteRoleUID { get; } = deleteRoleUID;
    public string? DeleteRoleName { get; } = deleteRoleName;

    // Layout & scrolling
    public float X { get; } = x;
    public float Y { get; } = y;
    public float Width { get; } = width;
    public float Height { get; } = height;
    public float ScrollY { get; } = scrollY;

    // Computed properties
    public bool HasRolesData => RolesData != null && RolesData.Success;
    public IReadOnlyList<RoleData> Roles => (IReadOnlyList<RoleData>?)RolesData?.Roles ?? Array.Empty<RoleData>();

    public IReadOnlyDictionary<string, string> MemberRoles =>
        (IReadOnlyDictionary<string, string>?)RolesData?.MemberRoles ?? new Dictionary<string, string>();

    /// <summary>
    ///     Gets the number of members assigned to a specific role.
    /// </summary>
    public int GetMemberCount(string roleUID)
    {
        if (MemberRoles == null) return 0;
        return MemberRoles.Values.Count(r => r == roleUID);
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
    ///     Checks if a role can be edited (not Founder role).
    /// </summary>
    public bool CanEditRole(RoleData role)
    {
        return CanManageRoles() && role.RoleUID != RoleDefaults.FOUNDER_ROLE_ID;
    }

    /// <summary>
    ///     Checks if a role can be deleted (not protected and has no members).
    /// </summary>
    public bool CanDeleteRole(RoleData role)
    {
        return CanManageRoles() && !role.IsProtected && GetMemberCount(role.RoleUID) == 0;
    }
}