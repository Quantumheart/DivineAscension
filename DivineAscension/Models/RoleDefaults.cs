using System;
using System.Collections.Generic;
using DivineAscension.Data;

namespace DivineAscension.Models;

public static class RoleDefaults
{
    // Fixed role IDs for default roles
    public const string FOUNDER_ROLE_ID = "role_founder";
    public const string OFFICER_ROLE_ID = "role_officer";
    public const string MEMBER_ROLE_ID = "role_member";

    // Reserved role names (case-insensitive)
    public static readonly HashSet<string> ReservedRoleNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "Founder",
        "Officer",
        "Member"
    };

    public static RoleData CreateFounderRole()
    {
        var role = new RoleData(
            FOUNDER_ROLE_ID,
            "Founder",
            true,
            true,
            0
        );

        // Founder has ALL permissions
        foreach (var permission in RolePermissions.AllPermissions) role.AddPermission(permission);

        return role;
    }

    public static RoleData CreateOfficerRole()
    {
        var role = new RoleData(
            OFFICER_ROLE_ID,
            "Officer",
            true,
            false,
            1
        );

        // Officer default permissions
        role.AddPermission(RolePermissions.INVITE_PLAYERS);
        role.AddPermission(RolePermissions.MANAGE_INVITATIONS);
        role.AddPermission(RolePermissions.KICK_MEMBERS);
        role.AddPermission(RolePermissions.EDIT_DESCRIPTION);
        role.AddPermission(RolePermissions.CHANGE_PRIVACY);
        role.AddPermission(RolePermissions.VIEW_MEMBERS);

        return role;
    }

    public static RoleData CreateMemberRole()
    {
        var role = new RoleData(
            MEMBER_ROLE_ID,
            "Member",
            true,
            true,
            2
        );

        // Member default permissions
        role.AddPermission(RolePermissions.VIEW_MEMBERS);

        return role;
    }

    public static Dictionary<string, RoleData> CreateDefaultRoles()
    {
        return new Dictionary<string, RoleData>
        {
            [FOUNDER_ROLE_ID] = CreateFounderRole(),
            [OFFICER_ROLE_ID] = CreateOfficerRole(),
            [MEMBER_ROLE_ID] = CreateMemberRole()
        };
    }

    public static bool IsDefaultRole(string roleUID)
    {
        return roleUID == FOUNDER_ROLE_ID ||
               roleUID == OFFICER_ROLE_ID ||
               roleUID == MEMBER_ROLE_ID;
    }

    public static bool IsReservedName(string roleName)
    {
        return ReservedRoleNames.Contains(roleName);
    }

    public static bool IsValidRoleName(string roleName)
    {
        if (string.IsNullOrWhiteSpace(roleName))
            return false;

        if (roleName.Length < 3 || roleName.Length > 30)
            return false;

        // Allow alphanumeric and spaces
        foreach (var c in roleName)
            if (!char.IsLetterOrDigit(c) && c != ' ')
                return false;

        return true;
    }
}