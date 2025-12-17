using System.Collections.Generic;

namespace PantheonWars.Models;

public static class RolePermissions
{
    // Permission constants
    public const string INVITE_PLAYERS = "invite_players";
    public const string MANAGE_INVITATIONS = "manage_invitations";
    public const string KICK_MEMBERS = "kick_members";
    public const string BAN_PLAYERS = "ban_players";
    public const string EDIT_DESCRIPTION = "edit_description";
    public const string MANAGE_ROLES = "manage_roles";
    public const string TRANSFER_FOUNDER = "transfer_founder";
    public const string DISBAND_RELIGION = "disband_religion";
    public const string CHANGE_PRIVACY = "change_privacy";
    public const string VIEW_MEMBERS = "view_members";
    public const string VIEW_BAN_LIST = "view_ban_list";

    // All permissions set
    public static readonly HashSet<string> AllPermissions = new()
    {
        INVITE_PLAYERS,
        MANAGE_INVITATIONS,
        KICK_MEMBERS,
        BAN_PLAYERS,
        EDIT_DESCRIPTION,
        MANAGE_ROLES,
        TRANSFER_FOUNDER,
        DISBAND_RELIGION,
        CHANGE_PRIVACY,
        VIEW_MEMBERS,
        VIEW_BAN_LIST
    };

    // Display names for UI
    public static readonly Dictionary<string, string> PermissionDisplayNames = new()
    {
        [INVITE_PLAYERS] = "Invite Players",
        [MANAGE_INVITATIONS] = "Manage Invitations",
        [KICK_MEMBERS] = "Kick Members",
        [BAN_PLAYERS] = "Ban Players",
        [EDIT_DESCRIPTION] = "Edit Description",
        [MANAGE_ROLES] = "Manage Roles",
        [TRANSFER_FOUNDER] = "Transfer Founder",
        [DISBAND_RELIGION] = "Disband Religion",
        [CHANGE_PRIVACY] = "Change Privacy",
        [VIEW_MEMBERS] = "View Members",
        [VIEW_BAN_LIST] = "View Ban List"
    };

    // Descriptions for tooltips
    public static readonly Dictionary<string, string> PermissionDescriptions = new()
    {
        [INVITE_PLAYERS] = "Can send invitations to players to join the religion",
        [MANAGE_INVITATIONS] = "Can cancel pending invitations",
        [KICK_MEMBERS] = "Can remove members from the religion",
        [BAN_PLAYERS] = "Can ban and unban players from the religion",
        [EDIT_DESCRIPTION] = "Can modify the religion's description",
        [MANAGE_ROLES] = "Can create, edit, and delete roles, and assign roles to members",
        [TRANSFER_FOUNDER] = "Can transfer founder status to another member",
        [DISBAND_RELIGION] = "Can permanently disband the religion",
        [CHANGE_PRIVACY] = "Can change religion between public and private",
        [VIEW_MEMBERS] = "Can view the member list",
        [VIEW_BAN_LIST] = "Can view the list of banned players"
    };

    public static bool IsValidPermission(string permission)
    {
        return AllPermissions.Contains(permission);
    }

    public static string GetDisplayName(string permission)
    {
        return PermissionDisplayNames.TryGetValue(permission, out var name) ? name : permission;
    }

    public static string GetDescription(string permission)
    {
        return PermissionDescriptions.TryGetValue(permission, out var desc) ? desc : "";
    }
}