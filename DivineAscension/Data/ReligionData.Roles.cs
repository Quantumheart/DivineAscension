using System;
using System.Collections.Generic;
using DivineAscension.Models;
using ProtoBuf;

namespace DivineAscension.Data;

public partial class ReligionData
{
    /// <summary>
    ///     Backing field for roles (serialized)
    /// </summary>
    [ProtoMember(14)]
    private Dictionary<string, RoleData> _roles = new();

    /// <summary>
    ///     Dictionary of roles in the religion.
    ///     Returns a thread-safe snapshot copy.
    /// </summary>
    [ProtoIgnore]
    public IReadOnlyDictionary<string, RoleData> Roles
    {
        get
        {
            lock (Lock)
            {
                return new Dictionary<string, RoleData>(_roles);
            }
        }
    }

    /// <summary>
    ///     Backing field for member roles (serialized)
    /// </summary>
    [ProtoMember(15)]
    private Dictionary<string, string> _memberRoles = new();

    /// <summary>
    ///     A dictionary of roles for the religion. Keys are player UIDs, values are the role IDs.
    ///     Returns a thread-safe snapshot copy.
    /// </summary>
    [ProtoIgnore]
    public IReadOnlyDictionary<string, string> MemberRoles
    {
        get
        {
            lock (Lock)
            {
                return new Dictionary<string, string>(_memberRoles);
            }
        }
    }

    /// <summary>
    ///     Sets or updates a role in the religion.
    ///     Thread-safe.
    /// </summary>
    public void SetRole(string roleId, RoleData role)
    {
        lock (Lock)
        {
            _roles[roleId] = role;
        }
    }

    /// <summary>
    ///     Removes a role from the religion.
    ///     Thread-safe.
    /// </summary>
    public bool RemoveRole(string roleId)
    {
        lock (Lock)
        {
            return _roles.Remove(roleId);
        }
    }

    /// <summary>
    ///     Assigns a role to a member.
    ///     Thread-safe.
    /// </summary>
    public void AssignMemberRole(string playerUID, string roleId)
    {
        lock (Lock)
        {
            _memberRoles[playerUID] = roleId;
        }
    }

    /// <summary>
    ///     Removes a member's role assignment.
    ///     Thread-safe.
    /// </summary>
    public bool RemoveMemberRole(string playerUID)
    {
        lock (Lock)
        {
            return _memberRoles.Remove(playerUID);
        }
    }

    /// <summary>
    ///     Initializes the roles dictionary (for use during construction only).
    ///     Thread-safe.
    /// </summary>
    internal void InitializeRoles(Dictionary<string, RoleData> roles)
    {
        lock (Lock)
        {
            _roles = roles ?? new Dictionary<string, RoleData>();
        }
    }

    /// <summary>
    ///     Initializes the member roles dictionary (for use during construction only).
    ///     Thread-safe.
    /// </summary>
    internal void InitializeMemberRoles(Dictionary<string, string> memberRoles)
    {
        lock (Lock)
        {
            _memberRoles = memberRoles ?? new Dictionary<string, string>();
        }
    }

    // Get player's role
    public string GetPlayerRole(string playerUID)
    {
        if (MemberRoles.TryGetValue(playerUID, out var roleUID))
            return roleUID;

        return RoleDefaults.MEMBER_ROLE_ID; // Fallback
    }

// Get role data
    public RoleData? GetRole(string roleUID)
    {
        return Roles.TryGetValue(roleUID, out var role) ? role : null;
    }

// Check if player has a specific permission
    public bool HasPermission(string playerUID, string permission)
    {
        var roleUID = GetPlayerRole(playerUID);
        var role = GetRole(roleUID);

        if (role == null)
            return false;

        return role.HasPermission(permission);
    }

// Check if player can assign a specific role
    public bool CanAssignRole(string assignerUID, string targetRoleUID)
    {
        // SYSTEM can always assign roles (for automated assignments like join/invite)
        if (assignerUID == "SYSTEM")
        {
            // Cannot assign Founder role (must use transfer)
            if (targetRoleUID == RoleDefaults.FOUNDER_ROLE_ID)
                return false;

            // Role must exist
            if (!Roles.ContainsKey(targetRoleUID))
                return false;

            return true;
        }

        // Must have MANAGE_ROLES permission
        if (!HasPermission(assignerUID, RolePermissions.MANAGE_ROLES))
            return false;

        // Cannot assign Founder role (must use transfer)
        if (targetRoleUID == RoleDefaults.FOUNDER_ROLE_ID)
            return false;

        // Role must exist
        if (!Roles.ContainsKey(targetRoleUID))
            return false;

        return true;
    }

// Get list of roles a player can assign
    public List<RoleData> GetAssignableRoles(string playerUID)
    {
        var assignable = new List<RoleData>();

        if (!HasPermission(playerUID, RolePermissions.MANAGE_ROLES))
            return assignable;

        foreach (var role in Roles.Values)
        {
            // Cannot assign Founder role
            if (role.RoleUID == RoleDefaults.FOUNDER_ROLE_ID)
                continue;

            assignable.Add(role);
        }

        return assignable;
    }

// Get role by name (case-insensitive)
    public RoleData? GetRoleByName(string roleName)
    {
        foreach (var role in Roles.Values)
            if (role.RoleName.Equals(roleName, StringComparison.OrdinalIgnoreCase))
                return role;

        return null;
    }

// Check if role name is taken
    public bool IsRoleNameTaken(string roleName, string? excludeRoleUID = null)
    {
        foreach (var role in Roles.Values)
        {
            if (role.RoleUID == excludeRoleUID)
                continue;

            if (role.RoleName.Equals(roleName, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

// Get members with a specific role
    public List<string> GetMembersWithRole(string roleUID)
    {
        var members = new List<string>();

        foreach (var kvp in MemberRoles)
            if (kvp.Value == roleUID)
                members.Add(kvp.Key);

        return members;
    }

// Count members per role
    public Dictionary<string, int> GetRoleMemberCounts()
    {
        var counts = new Dictionary<string, int>();

        foreach (var role in Roles.Keys) counts[role] = 0;

        foreach (var roleUID in MemberRoles.Values)
            if (counts.ContainsKey(roleUID))
                counts[roleUID]++;
            else
                counts[roleUID] = 1;

        return counts;
    }
}
