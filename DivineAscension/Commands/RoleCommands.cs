using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DivineAscension.Constants;
using DivineAscension.Extensions;
using DivineAscension.Models;
using DivineAscension.Services;
using DivineAscension.Systems.Interfaces;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

namespace DivineAscension.Commands;

/// <summary>
///     Handles all role-related chat commands
/// </summary>
public class RoleCommands(
    ICoreServerAPI sapi,
    IRoleManager roleManager,
    IReligionManager religionManager,
    IPlayerProgressionDataManager playerReligionDataManager)
{
    private readonly IPlayerProgressionDataManager _playerProgressionDataManager =
        playerReligionDataManager ?? throw new ArgumentNullException(nameof(playerReligionDataManager));

    private readonly IReligionManager _religionManager =
        religionManager ?? throw new ArgumentNullException(nameof(religionManager));

    private readonly IRoleManager _roleManager = roleManager ?? throw new ArgumentNullException(nameof(roleManager));

    private readonly ICoreServerAPI _sapi = sapi ?? throw new ArgumentNullException(nameof(sapi));

    /// <summary>
    ///     Registers all role management commands
    /// </summary>
    public void RegisterCommands()
    {
        var religionCmd = _sapi.ChatCommands.Get("religion");
        if (religionCmd == null)
        {
            _sapi.Logger.Error("[DivineAscension] Religion command not found, cannot register role commands");
            return;
        }

        // Add main roles command
        religionCmd.BeginSubCommand("roles")
            .WithDescription(LocalizationService.Instance.Get(LocalizationKeys.CMD_ROLES_DESC))
            .HandleWith(OnListRoles)
            .EndSubCommand();

        // Add transfer command
        religionCmd.BeginSubCommand("transfer")
            .WithDescription(LocalizationService.Instance.Get(LocalizationKeys.CMD_TRANSFER_DESC))
            .WithArgs(_sapi.ChatCommands.Parsers.Word("playername"))
            .HandleWith(OnTransferFounder)
            .EndSubCommand();

        // Add role subcommands
        religionCmd.BeginSubCommand("role")
            .WithDescription(LocalizationService.Instance.Get(LocalizationKeys.CMD_ROLE_DESC))
            .BeginSubCommand("members")
            .WithDescription(LocalizationService.Instance.Get(LocalizationKeys.CMD_ROLE_MEMBERS_DESC))
            .WithArgs(_sapi.ChatCommands.Parsers.Word("rolename"))
            .HandleWith(OnListRoleMembers)
            .EndSubCommand()
            .BeginSubCommand("create")
            .WithDescription(LocalizationService.Instance.Get(LocalizationKeys.CMD_ROLE_CREATE_DESC))
            .WithArgs(_sapi.ChatCommands.Parsers.Word("name"))
            .HandleWith(OnCreateRole)
            .EndSubCommand()
            .BeginSubCommand("delete")
            .WithDescription(LocalizationService.Instance.Get(LocalizationKeys.CMD_ROLE_DELETE_DESC))
            .WithArgs(_sapi.ChatCommands.Parsers.Word("name"))
            .HandleWith(OnDeleteRole)
            .EndSubCommand()
            .BeginSubCommand("rename")
            .WithDescription(LocalizationService.Instance.Get(LocalizationKeys.CMD_ROLE_RENAME_DESC))
            .WithArgs(_sapi.ChatCommands.Parsers.Word("oldname"),
                _sapi.ChatCommands.Parsers.Word("newname"))
            .HandleWith(OnRenameRole)
            .EndSubCommand()
            .BeginSubCommand("assign")
            .WithDescription(LocalizationService.Instance.Get(LocalizationKeys.CMD_ROLE_ASSIGN_DESC))
            .WithArgs(_sapi.ChatCommands.Parsers.Word("playername"),
                _sapi.ChatCommands.Parsers.Word("rolename"))
            .HandleWith(OnAssignRole)
            .EndSubCommand()
            .BeginSubCommand("grant")
            .WithDescription(LocalizationService.Instance.Get(LocalizationKeys.CMD_ROLE_GRANT_DESC))
            .WithArgs(_sapi.ChatCommands.Parsers.Word("rolename"),
                _sapi.ChatCommands.Parsers.Word("permission"))
            .HandleWith(OnGrantPermission)
            .EndSubCommand()
            .BeginSubCommand("revoke")
            .WithDescription(LocalizationService.Instance.Get(LocalizationKeys.CMD_ROLE_REVOKE_DESC))
            .WithArgs(_sapi.ChatCommands.Parsers.Word("rolename"),
                _sapi.ChatCommands.Parsers.Word("permission"))
            .HandleWith(OnRevokePermission)
            .EndSubCommand()
            .BeginSubCommand("permissions")
            .WithDescription(LocalizationService.Instance.Get(LocalizationKeys.CMD_ROLE_PERMISSIONS_DESC))
            .WithArgs(_sapi.ChatCommands.Parsers.Word("rolename"))
            .HandleWith(OnListRolePermissions)
            .EndSubCommand()
            .EndSubCommand();

        _sapi.Logger.Notification("[DivineAscension] Role commands registered");
    }

    #region Command Handlers

    /// <summary>
    ///     Handler for /religion roles
    /// </summary>
    internal TextCommandResult OnListRoles(TextCommandCallingArgs args)
    {
        var player = args.Caller.Player as IServerPlayer;
        if (player == null)
            return TextCommandResult.Error(
                LocalizationService.Instance.Get(LocalizationKeys.CMD_ERROR_PLAYERS_ONLY));

        var playerId = player.PlayerUID;
        if (!_religionManager.HasReligion(playerId))
            return TextCommandResult.Error(
                LocalizationService.Instance.Get(LocalizationKeys.CMD_ERROR_NOT_IN_RELIGION));

        var religion = _religionManager.GetPlayerReligion(playerId);
        if (religion == null)
            return TextCommandResult.Error(
                LocalizationService.Instance.Get(LocalizationKeys.CMD_ERROR_RELIGION_DATA_NOT_FOUND));

        var roles = _roleManager.GetReligionRoles(religion.ReligionUID);
        var roleCounts = _roleManager.GetRoleMemberCounts(religion.ReligionUID);

        var sb = new StringBuilder();
        sb.AppendLine(LocalizationService.Instance.Get(LocalizationKeys.CMD_ROLE_HEADER_ROLES, religion.ReligionName));
        sb.AppendLine();

        foreach (var role in roles)
        {
            var memberCount = roleCounts.ContainsKey(role.RoleUID) ? roleCounts[role.RoleUID] : 0;
            var roleType = role.IsDefault
                ? LocalizationService.Instance.Get(LocalizationKeys.CMD_ROLE_LABEL_DEFAULT)
                : LocalizationService.Instance.Get(LocalizationKeys.CMD_ROLE_LABEL_CUSTOM);
            var protectedTag = role.IsProtected
                ? LocalizationService.Instance.Get(LocalizationKeys.CMD_ROLE_LABEL_PROTECTED)
                : "";

            sb.AppendLine($"• {role.RoleName}{roleType}{protectedTag}");
            sb.AppendLine(LocalizationService.Instance.Get(LocalizationKeys.CMD_ROLE_LABEL_MEMBERS, memberCount));
            sb.AppendLine(LocalizationService.Instance.Get(LocalizationKeys.CMD_ROLE_LABEL_PERMISSIONS,
                role.Permissions.Count));
            sb.AppendLine();
        }

        sb.AppendLine(LocalizationService.Instance.Get(LocalizationKeys.CMD_ROLE_FOOTER_VIEW_PERMISSIONS));

        return TextCommandResult.Success(sb.ToString());
    }

    /// <summary>
    ///     Handler for /religion role members <rolename>
    /// </summary>
    internal TextCommandResult OnListRoleMembers(TextCommandCallingArgs args)
    {
        var roleName = (string)args[0];

        var player = args.Caller.Player as IServerPlayer;
        if (player == null)
            return TextCommandResult.Error(
                LocalizationService.Instance.Get(LocalizationKeys.CMD_ERROR_PLAYERS_ONLY));

        var playerId = player.PlayerUID;
        if (!_religionManager.HasReligion(playerId))
            return TextCommandResult.Error(
                LocalizationService.Instance.Get(LocalizationKeys.CMD_ERROR_NOT_IN_RELIGION));

        var religion = _religionManager.GetPlayerReligion(playerId);
        if (religion == null)
            return TextCommandResult.Error(
                LocalizationService.Instance.Get(LocalizationKeys.CMD_ERROR_RELIGION_DATA_NOT_FOUND));

        // Check if player has permission to view members
        if (!religion.HasPermission(playerId, RolePermissions.VIEW_MEMBERS))
            return TextCommandResult.Error(
                LocalizationService.Instance.Get(LocalizationKeys.CMD_ROLE_ERROR_NO_VIEW_PERMISSION));

        // Find role by name
        var role = religion.GetRoleByName(roleName);
        if (role == null)
            return TextCommandResult.Error(
                LocalizationService.Instance.Get(LocalizationKeys.CMD_ROLE_ERROR_ROLE_NOT_FOUND, roleName));

        var membersWithRole = _roleManager.GetPlayersWithRole(religion.ReligionUID, role.RoleUID);

        var sb = new StringBuilder();
        sb.AppendLine(LocalizationService.Instance.Get(LocalizationKeys.CMD_ROLE_HEADER_MEMBERS_WITH_ROLE,
            role.RoleName, membersWithRole.Count));
        sb.AppendLine();

        if (membersWithRole.Count == 0)
            sb.AppendLine(LocalizationService.Instance.Get(LocalizationKeys.CMD_ROLE_NO_MEMBERS));
        else
            foreach (var memberUID in membersWithRole)
            {
                var memberPlayer = _sapi.World.PlayerByUid(memberUID);
                var memberName = memberPlayer?.PlayerName ??
                                 LocalizationService.Instance.Get(LocalizationKeys.UI_COMMON_UNKNOWN);
                var memberData = _playerProgressionDataManager.GetOrCreatePlayerData(memberUID);

                sb.AppendLine(LocalizationService.Instance.Get(LocalizationKeys.CMD_ROLE_FORMAT_MEMBER_INFO,
                    memberName, _playerProgressionDataManager.GetPlayerFavorRank(memberUID).ToLocalizedString(), memberData.Favor));
            }

        return TextCommandResult.Success(sb.ToString());
    }

    /// <summary>
    ///     Handler for /religion role create <name>
    /// </summary>
    internal TextCommandResult OnCreateRole(TextCommandCallingArgs args)
    {
        var roleName = (string)args[0];

        var player = args.Caller.Player as IServerPlayer;
        if (player == null)
            return TextCommandResult.Error(
                LocalizationService.Instance.Get(LocalizationKeys.CMD_ERROR_PLAYERS_ONLY));

        var playerId = player.PlayerUID;
        if (!_religionManager.HasReligion(playerId))
            return TextCommandResult.Error(
                LocalizationService.Instance.Get(LocalizationKeys.CMD_ERROR_NOT_IN_RELIGION));

        var religion = _religionManager.GetPlayerReligion(playerId);
        if (religion == null)
            return TextCommandResult.Error(
                LocalizationService.Instance.Get(LocalizationKeys.CMD_ERROR_RELIGION_DATA_NOT_FOUND));

        // Create the role
        var (success, role, error) =
            _roleManager.CreateCustomRole(religion.ReligionUID, player.PlayerUID, roleName);

        if (!success) return TextCommandResult.Error(error);

        return TextCommandResult.Success(
            LocalizationService.Instance.Get(LocalizationKeys.CMD_ROLE_SUCCESS_CREATED, role!.RoleName, roleName));
    }

    /// <summary>
    ///     Handler for /religion role delete <name>
    /// </summary>
    internal TextCommandResult OnDeleteRole(TextCommandCallingArgs args)
    {
        var roleName = (string)args[0];

        var player = args.Caller.Player as IServerPlayer;
        if (player == null)
            return TextCommandResult.Error(
                LocalizationService.Instance.Get(LocalizationKeys.CMD_ERROR_PLAYERS_ONLY));

        var playerId = player.PlayerUID;
        if (!_religionManager.HasReligion(playerId))
            return TextCommandResult.Error(
                LocalizationService.Instance.Get(LocalizationKeys.CMD_ERROR_NOT_IN_RELIGION));

        var religion = _religionManager.GetPlayerReligion(playerId);
        if (religion == null)
            return TextCommandResult.Error(
                LocalizationService.Instance.Get(LocalizationKeys.CMD_ERROR_RELIGION_DATA_NOT_FOUND));

        // Find role by name
        var role = religion.GetRoleByName(roleName);
        if (role == null)
            return TextCommandResult.Error(
                LocalizationService.Instance.Get(LocalizationKeys.CMD_ROLE_ERROR_ROLE_NOT_FOUND, roleName));

        // Delete the role
        var (success, error) = _roleManager.DeleteRole(religion.ReligionUID, player.PlayerUID, role.RoleUID);

        if (!success) return TextCommandResult.Error(error);

        return TextCommandResult.Success(
            LocalizationService.Instance.Get(LocalizationKeys.CMD_ROLE_SUCCESS_DELETED, roleName));
    }


    /// <summary>
    /// Renames a role within a player's religion.
    /// </summary>
    /// <param name="args">
    /// The command arguments, where the first argument represents the old name of the role,
    /// and the second argument represents the new name. Includes the caller's player information.
    /// </param>
    /// <returns>
    /// A <see cref="TextCommandResult"/> indicating success or an error message if the operation fails.
    /// Returns an error if the caller is not a player, if the player is not part of a religion,
    /// if the role is not found, or if the renaming operation encounters an issue.
    /// </returns>
    internal TextCommandResult OnRenameRole(TextCommandCallingArgs args)
    {
        var oldName = (string)args[0];
        var newName = (string)args[1];

        var player = args.Caller.Player as IServerPlayer;
        if (player == null)
            return TextCommandResult.Error(
                LocalizationService.Instance.Get(LocalizationKeys.CMD_ERROR_PLAYERS_ONLY));

        var playerId = player.PlayerUID;
        if (!_religionManager.HasReligion(playerId))
            return TextCommandResult.Error(
                LocalizationService.Instance.Get(LocalizationKeys.CMD_ERROR_NOT_IN_RELIGION));

        var religion = _religionManager.GetPlayerReligion(playerId);
        if (religion == null)
            return TextCommandResult.Error(
                LocalizationService.Instance.Get(LocalizationKeys.CMD_ERROR_RELIGION_DATA_NOT_FOUND));

        // Find role by name
        var role = religion.GetRoleByName(oldName);
        if (role == null)
            return TextCommandResult.Error(
                LocalizationService.Instance.Get(LocalizationKeys.CMD_ROLE_ERROR_ROLE_NOT_FOUND, oldName));

        // Rename the role
        var (success, updatedRole, error) =
            _roleManager.RenameRole(religion.ReligionUID, playerId, role.RoleUID, newName);

        if (!success) return TextCommandResult.Error(error);

        return TextCommandResult.Success(
            LocalizationService.Instance.Get(LocalizationKeys.CMD_ROLE_SUCCESS_RENAMED, oldName, newName));
    }

    /// <summary>
    ///     Handler for /religion role assign
    ///     <playername>
    ///         <rolename>
    /// </summary>
    internal TextCommandResult OnAssignRole(TextCommandCallingArgs args)
    {
        var targetPlayerName = (string)args[0];
        var roleName = (string)args[1];

        var player = args.Caller.Player as IServerPlayer;
        if (player == null)
            return TextCommandResult.Error(
                LocalizationService.Instance.Get(LocalizationKeys.CMD_ERROR_PLAYERS_ONLY));

        var playerId = player.PlayerUID;
        if (!_religionManager.HasReligion(playerId))
            return TextCommandResult.Error(
                LocalizationService.Instance.Get(LocalizationKeys.CMD_ERROR_NOT_IN_RELIGION));

        var religion = _religionManager.GetPlayerReligion(playerId);
        if (religion == null)
            return TextCommandResult.Error(
                LocalizationService.Instance.Get(LocalizationKeys.CMD_ERROR_RELIGION_DATA_NOT_FOUND));

        // Find target player by name
        var targetPlayer = _sapi.World.AllPlayers
            .FirstOrDefault(p => p.PlayerName.Equals(targetPlayerName, StringComparison.OrdinalIgnoreCase));

        if (targetPlayer == null)
            return TextCommandResult.Error(
                LocalizationService.Instance.Get(LocalizationKeys.CMD_ROLE_ERROR_PLAYER_NOT_FOUND, targetPlayerName));

        // Find role by name
        var role = religion.GetRoleByName(roleName);
        if (role == null)
            return TextCommandResult.Error(
                LocalizationService.Instance.Get(LocalizationKeys.CMD_ROLE_ERROR_ROLE_NOT_FOUND, roleName));

        // Assign the role
        var (success, error) =
            _roleManager.AssignRole(religion.ReligionUID, player.PlayerUID, targetPlayer.PlayerUID, role.RoleUID);

        if (!success) return TextCommandResult.Error(error);

        // Notify target player if online
        var targetServerPlayer = targetPlayer as IServerPlayer;
        if (targetServerPlayer != null)
            targetServerPlayer.SendMessage(
                GlobalConstants.GeneralChatGroup,
                LocalizationService.Instance.Get(LocalizationKeys.CMD_ROLE_SUCCESS_ASSIGNED_NOTIFICATION,
                    role.RoleName, religion.ReligionName),
                EnumChatType.Notification
            );

        return TextCommandResult.Success(
            LocalizationService.Instance.Get(LocalizationKeys.CMD_ROLE_SUCCESS_ASSIGNED, targetPlayerName,
                role.RoleName));
    }

    /// <summary>
    ///     Handler for /religion role grant
    ///     <rolename>
    ///         <permission>
    /// </summary>
    internal TextCommandResult OnGrantPermission(TextCommandCallingArgs args)
    {
        var roleName = (string)args[0];
        var permissionName = (string)args[1];

        var player = args.Caller.Player as IServerPlayer;
        if (player == null)
            return TextCommandResult.Error(
                LocalizationService.Instance.Get(LocalizationKeys.CMD_ERROR_PLAYERS_ONLY));

        var playerId = player.PlayerUID;
        if (!_religionManager.HasReligion(playerId))
            return TextCommandResult.Error(
                LocalizationService.Instance.Get(LocalizationKeys.CMD_ERROR_NOT_IN_RELIGION));

        var religion = _religionManager.GetPlayerReligion(playerId);
        if (religion == null)
            return TextCommandResult.Error(
                LocalizationService.Instance.Get(LocalizationKeys.CMD_ERROR_RELIGION_DATA_NOT_FOUND));

        // Find role by name
        var role = religion.GetRoleByName(roleName);
        if (role == null)
            return TextCommandResult.Error(
                LocalizationService.Instance.Get(LocalizationKeys.CMD_ROLE_ERROR_ROLE_NOT_FOUND, roleName));

        // Find permission by name (case insensitive)
        var permission = RolePermissions.AllPermissions
            .FirstOrDefault(p => p.Equals(permissionName, StringComparison.OrdinalIgnoreCase));

        if (permission == null)
        {
            var availablePerms = string.Join(", ", RolePermissions.AllPermissions);
            return TextCommandResult.Error(
                LocalizationService.Instance.Get(LocalizationKeys.CMD_ROLE_ERROR_INVALID_PERMISSION,
                    permissionName, availablePerms));
        }

        // Add permission to the role
        var updatedPermissions = new HashSet<string>(role.Permissions) { permission };

        var (success, updatedRole, error) =
            _roleManager.ModifyRolePermissions(religion.ReligionUID, player.PlayerUID, role.RoleUID,
                updatedPermissions);

        if (!success) return TextCommandResult.Error(error);

        return TextCommandResult.Success(
            LocalizationService.Instance.Get(LocalizationKeys.CMD_ROLE_SUCCESS_PERMISSION_GRANTED,
                RolePermissions.GetDisplayName(permission), role.RoleName));
    }

    /// <summary>
    ///     Handler for /religion role revoke
    ///     <rolename>
    ///         <permission>
    /// </summary>
    internal TextCommandResult OnRevokePermission(TextCommandCallingArgs args)
    {
        var roleName = (string)args[0];
        var permissionName = (string)args[1];

        var player = args.Caller.Player as IServerPlayer;
        if (player == null)
            return TextCommandResult.Error(
                LocalizationService.Instance.Get(LocalizationKeys.CMD_ERROR_PLAYERS_ONLY));

        var playerId = player.PlayerUID;
        if (!_religionManager.HasReligion(playerId))
            return TextCommandResult.Error(
                LocalizationService.Instance.Get(LocalizationKeys.CMD_ERROR_NOT_IN_RELIGION));

        var religion = _religionManager.GetPlayerReligion(playerId);
        if (religion == null)
            return TextCommandResult.Error(
                LocalizationService.Instance.Get(LocalizationKeys.CMD_ERROR_RELIGION_DATA_NOT_FOUND));

        // Find role by name
        var role = religion.GetRoleByName(roleName);
        if (role == null)
            return TextCommandResult.Error(
                LocalizationService.Instance.Get(LocalizationKeys.CMD_ROLE_ERROR_ROLE_NOT_FOUND, roleName));

        // Find permission by name (case insensitive)
        var permission = RolePermissions.AllPermissions
            .FirstOrDefault(p => p.Equals(permissionName, StringComparison.OrdinalIgnoreCase));

        if (permission == null)
        {
            var availablePerms = string.Join(", ", RolePermissions.AllPermissions);
            return TextCommandResult.Error(
                LocalizationService.Instance.Get(LocalizationKeys.CMD_ROLE_ERROR_INVALID_PERMISSION,
                    permissionName, availablePerms));
        }

        // Remove permission from the role
        var updatedPermissions = new HashSet<string>(role.Permissions);
        updatedPermissions.Remove(permission);

        var (success, updatedRole, error) =
            _roleManager.ModifyRolePermissions(religion.ReligionUID, player.PlayerUID, role.RoleUID,
                updatedPermissions);

        if (!success) return TextCommandResult.Error(error);

        return TextCommandResult.Success(
            LocalizationService.Instance.Get(LocalizationKeys.CMD_ROLE_SUCCESS_PERMISSION_REVOKED,
                RolePermissions.GetDisplayName(permission), role.RoleName));
    }

    /// <summary>
    ///     Handler for /religion role permissions <rolename>
    /// </summary>
    internal TextCommandResult OnListRolePermissions(TextCommandCallingArgs args)
    {
        var roleName = (string)args[0];

        var player = args.Caller.Player as IServerPlayer;
        if (player == null)
            return TextCommandResult.Error(
                LocalizationService.Instance.Get(LocalizationKeys.CMD_ERROR_PLAYERS_ONLY));

        var playerId = player.PlayerUID;
        if (!_religionManager.HasReligion(playerId))
            return TextCommandResult.Error(
                LocalizationService.Instance.Get(LocalizationKeys.CMD_ERROR_NOT_IN_RELIGION));

        var religion = _religionManager.GetPlayerReligion(playerId);
        if (religion == null)
            return TextCommandResult.Error(
                LocalizationService.Instance.Get(LocalizationKeys.CMD_ERROR_RELIGION_DATA_NOT_FOUND));

        // Find role by name
        var role = religion.GetRoleByName(roleName);
        if (role == null)
            return TextCommandResult.Error(
                LocalizationService.Instance.Get(LocalizationKeys.CMD_ROLE_ERROR_ROLE_NOT_FOUND, roleName));

        var sb = new StringBuilder();
        sb.AppendLine(LocalizationService.Instance.Get(LocalizationKeys.CMD_ROLE_HEADER_PERMISSIONS, role.RoleName));
        sb.AppendLine();

        if (role.Permissions.Count == 0)
            sb.AppendLine(LocalizationService.Instance.Get(LocalizationKeys.CMD_ROLE_NO_PERMISSIONS));
        else
            foreach (var permission in role.Permissions.OrderBy(p => p))
            {
                var displayName = RolePermissions.GetDisplayName(permission);
                var description = RolePermissions.GetDescription(permission);
                sb.AppendLine($"✓ {displayName}");
                sb.AppendLine($"  {description}");
                sb.AppendLine();
            }

        sb.AppendLine(LocalizationService.Instance.Get(LocalizationKeys.CMD_ROLE_LABEL_AVAILABLE_PERMISSIONS));
        foreach (var permission in RolePermissions.AllPermissions.OrderBy(p => p))
            if (!role.Permissions.Contains(permission))
            {
                var displayName = RolePermissions.GetDisplayName(permission);
                sb.AppendLine($"  • {displayName} ({permission})");
            }

        return TextCommandResult.Success(sb.ToString());
    }

    /// <summary>
    ///     Handler for /religion transfer <playername>
    /// </summary>
    internal TextCommandResult OnTransferFounder(TextCommandCallingArgs args)
    {
        var targetPlayerName = (string)args[0];

        var player = args.Caller.Player as IServerPlayer;
        if (player == null)
            return TextCommandResult.Error(
                LocalizationService.Instance.Get(LocalizationKeys.CMD_ERROR_PLAYERS_ONLY));

        var playerId = player.PlayerUID;
        if (!_religionManager.HasReligion(playerId))
            return TextCommandResult.Error(
                LocalizationService.Instance.Get(LocalizationKeys.CMD_ERROR_NOT_IN_RELIGION));

        var religion = _religionManager.GetPlayerReligion(playerId);
        if (religion == null)
            return TextCommandResult.Error(
                LocalizationService.Instance.Get(LocalizationKeys.CMD_ERROR_RELIGION_DATA_NOT_FOUND));

        // Find target player by name
        var targetPlayer = _sapi.World.AllPlayers
            .FirstOrDefault(p => p.PlayerName.Equals(targetPlayerName, StringComparison.OrdinalIgnoreCase));

        if (targetPlayer == null)
            return TextCommandResult.Error(
                LocalizationService.Instance.Get(LocalizationKeys.CMD_ROLE_ERROR_PLAYER_NOT_FOUND, targetPlayerName));

        // Transfer founder status
        var (success, error) =
            _roleManager.TransferFounder(religion.ReligionUID, player.PlayerUID, targetPlayer.PlayerUID);

        if (!success) return TextCommandResult.Error(error);

        // Notify all members
        foreach (var memberUID in religion.MemberUIDs)
        {
            var memberPlayer = _sapi.World.PlayerByUid(memberUID) as IServerPlayer;
            if (memberPlayer != null)
                memberPlayer.SendMessage(
                    GlobalConstants.GeneralChatGroup,
                    LocalizationService.Instance.Get(LocalizationKeys.CMD_ROLE_SUCCESS_FOUNDER_TRANSFERRED_NOTIFICATION,
                        player.PlayerName, targetPlayerName, religion.ReligionName),
                    EnumChatType.Notification
                );
        }

        return TextCommandResult.Success(
            LocalizationService.Instance.Get(LocalizationKeys.CMD_ROLE_SUCCESS_FOUNDER_TRANSFERRED, targetPlayerName));
    }

    #endregion
}