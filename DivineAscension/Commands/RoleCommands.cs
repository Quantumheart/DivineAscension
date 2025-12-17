using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DivineAscension.Models;
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
    IPlayerReligionDataManager playerReligionDataManager)
{
    private readonly IPlayerReligionDataManager _playerReligionDataManager =
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
            .WithDescription("View all roles in your religion")
            .HandleWith(OnListRoles)
            .EndSubCommand();

        // Add transfer command
        religionCmd.BeginSubCommand("transfer")
            .WithDescription("Transfer founder status to another member")
            .WithArgs(_sapi.ChatCommands.Parsers.Word("playername"))
            .HandleWith(OnTransferFounder)
            .EndSubCommand();

        // Add role subcommands
        religionCmd.BeginSubCommand("role")
            .WithDescription("Manage roles")
            .BeginSubCommand("members")
            .WithDescription("View members with a specific role")
            .WithArgs(_sapi.ChatCommands.Parsers.Word("rolename"))
            .HandleWith(OnListRoleMembers)
            .EndSubCommand()
            .BeginSubCommand("create")
            .WithDescription("Create a custom role")
            .WithArgs(_sapi.ChatCommands.Parsers.Word("name"))
            .HandleWith(OnCreateRole)
            .EndSubCommand()
            .BeginSubCommand("delete")
            .WithDescription("Delete a custom role")
            .WithArgs(_sapi.ChatCommands.Parsers.Word("name"))
            .HandleWith(OnDeleteRole)
            .EndSubCommand()
            .BeginSubCommand("rename")
            .WithDescription("Rename a role")
            .WithArgs(_sapi.ChatCommands.Parsers.Word("oldname"),
                _sapi.ChatCommands.Parsers.Word("newname"))
            .HandleWith(OnRenameRole)
            .EndSubCommand()
            .BeginSubCommand("assign")
            .WithDescription("Assign a role to a member")
            .WithArgs(_sapi.ChatCommands.Parsers.Word("playername"),
                _sapi.ChatCommands.Parsers.Word("rolename"))
            .HandleWith(OnAssignRole)
            .EndSubCommand()
            .BeginSubCommand("grant")
            .WithDescription("Grant a permission to a role")
            .WithArgs(_sapi.ChatCommands.Parsers.Word("rolename"),
                _sapi.ChatCommands.Parsers.Word("permission"))
            .HandleWith(OnGrantPermission)
            .EndSubCommand()
            .BeginSubCommand("revoke")
            .WithDescription("Revoke a permission from a role")
            .WithArgs(_sapi.ChatCommands.Parsers.Word("rolename"),
                _sapi.ChatCommands.Parsers.Word("permission"))
            .HandleWith(OnRevokePermission)
            .EndSubCommand()
            .BeginSubCommand("permissions")
            .WithDescription("View permissions for a role")
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
        if (player == null) return TextCommandResult.Error("Command can only be used by players");

        var playerData = _playerReligionDataManager.GetOrCreatePlayerData(player.PlayerUID);
        if (!playerData.HasReligion()) return TextCommandResult.Error("You are not in any religion");

        var religion = _religionManager.GetReligion(playerData.ReligionUID!);
        if (religion == null) return TextCommandResult.Error("Could not find your religion data");

        var roles = _roleManager.GetReligionRoles(religion.ReligionUID);
        var roleCounts = _roleManager.GetRoleMemberCounts(religion.ReligionUID);

        var sb = new StringBuilder();
        sb.AppendLine($"=== {religion.ReligionName} Roles ===");
        sb.AppendLine();

        foreach (var role in roles)
        {
            var memberCount = roleCounts.ContainsKey(role.RoleUID) ? roleCounts[role.RoleUID] : 0;
            var roleType = role.IsDefault ? " (Default)" : " (Custom)";
            var protectedTag = role.IsProtected ? " [Protected]" : "";

            sb.AppendLine($"• {role.RoleName}{roleType}{protectedTag}");
            sb.AppendLine($"  Members: {memberCount}");
            sb.AppendLine($"  Permissions: {role.Permissions.Count}");
            sb.AppendLine();
        }

        sb.AppendLine("Use '/religion role permissions <rolename>' to view a role's permissions");

        return TextCommandResult.Success(sb.ToString());
    }

    /// <summary>
    ///     Handler for /religion role members <rolename>
    /// </summary>
    internal TextCommandResult OnListRoleMembers(TextCommandCallingArgs args)
    {
        var roleName = (string)args[0];

        var player = args.Caller.Player as IServerPlayer;
        if (player == null) return TextCommandResult.Error("Command can only be used by players");

        var playerData = _playerReligionDataManager.GetOrCreatePlayerData(player.PlayerUID);
        if (!playerData.HasReligion()) return TextCommandResult.Error("You are not in any religion");

        var religion = _religionManager.GetReligion(playerData.ReligionUID!);
        if (religion == null) return TextCommandResult.Error("Could not find your religion data");

        // Check if player has permission to view members
        if (!religion.HasPermission(player.PlayerUID, RolePermissions.VIEW_MEMBERS))
            return TextCommandResult.Error("You don't have permission to view members");

        // Find role by name
        var role = religion.GetRoleByName(roleName);
        if (role == null) return TextCommandResult.Error($"Role '{roleName}' not found");

        var membersWithRole = _roleManager.GetPlayersWithRole(religion.ReligionUID, role.RoleUID);

        var sb = new StringBuilder();
        sb.AppendLine($"=== Members with role '{role.RoleName}' ({membersWithRole.Count}) ===");
        sb.AppendLine();

        if (membersWithRole.Count == 0)
            sb.AppendLine("No members have this role.");
        else
            foreach (var memberUID in membersWithRole)
            {
                var memberPlayer = _sapi.World.PlayerByUid(memberUID);
                var memberName = memberPlayer?.PlayerName ?? "Unknown";
                var memberData = _playerReligionDataManager.GetOrCreatePlayerData(memberUID);

                sb.AppendLine($"• {memberName} | Rank: {memberData.FavorRank} | Favor: {memberData.Favor}");
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
        if (player == null) return TextCommandResult.Error("Command can only be used by players");

        var playerData = _playerReligionDataManager.GetOrCreatePlayerData(player.PlayerUID);
        if (!playerData.HasReligion()) return TextCommandResult.Error("You are not in any religion");

        var religion = _religionManager.GetReligion(playerData.ReligionUID!);
        if (religion == null) return TextCommandResult.Error("Could not find your religion data");

        // Create the role
        var (success, role, error) =
            _roleManager.CreateCustomRole(religion.ReligionUID, player.PlayerUID, roleName);

        if (!success) return TextCommandResult.Error(error);

        return TextCommandResult.Success(
            $"Custom role '{role!.RoleName}' created! Use '/religion role grant {roleName} <permission>' to add permissions.");
    }

    /// <summary>
    ///     Handler for /religion role delete <name>
    /// </summary>
    internal TextCommandResult OnDeleteRole(TextCommandCallingArgs args)
    {
        var roleName = (string)args[0];

        var player = args.Caller.Player as IServerPlayer;
        if (player == null) return TextCommandResult.Error("Command can only be used by players");

        var playerData = _playerReligionDataManager.GetOrCreatePlayerData(player.PlayerUID);
        if (!playerData.HasReligion()) return TextCommandResult.Error("You are not in any religion");

        var religion = _religionManager.GetReligion(playerData.ReligionUID!);
        if (religion == null) return TextCommandResult.Error("Could not find your religion data");

        // Find role by name
        var role = religion.GetRoleByName(roleName);
        if (role == null) return TextCommandResult.Error($"Role '{roleName}' not found");

        // Delete the role
        var (success, error) = _roleManager.DeleteRole(religion.ReligionUID, player.PlayerUID, role.RoleUID);

        if (!success) return TextCommandResult.Error(error);

        return TextCommandResult.Success($"Role '{roleName}' has been deleted");
    }

    /// <summary>
    ///     Handler for /religion role rename
    ///     <oldname>
    ///         <newname>
    /// </summary>
    internal TextCommandResult OnRenameRole(TextCommandCallingArgs args)
    {
        var oldName = (string)args[0];
        var newName = (string)args[1];

        var player = args.Caller.Player as IServerPlayer;
        if (player == null) return TextCommandResult.Error("Command can only be used by players");

        var playerData = _playerReligionDataManager.GetOrCreatePlayerData(player.PlayerUID);
        if (!playerData.HasReligion()) return TextCommandResult.Error("You are not in any religion");

        var religion = _religionManager.GetReligion(playerData.ReligionUID!);
        if (religion == null) return TextCommandResult.Error("Could not find your religion data");

        // Find role by name
        var role = religion.GetRoleByName(oldName);
        if (role == null) return TextCommandResult.Error($"Role '{oldName}' not found");

        // Rename the role
        var (success, updatedRole, error) =
            _roleManager.RenameRole(religion.ReligionUID, player.PlayerUID, role.RoleUID, newName);

        if (!success) return TextCommandResult.Error(error);

        return TextCommandResult.Success($"Role renamed from '{oldName}' to '{newName}'");
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
        if (player == null) return TextCommandResult.Error("Command can only be used by players");

        var playerData = _playerReligionDataManager.GetOrCreatePlayerData(player.PlayerUID);
        if (!playerData.HasReligion()) return TextCommandResult.Error("You are not in any religion");

        var religion = _religionManager.GetReligion(playerData.ReligionUID!);
        if (religion == null) return TextCommandResult.Error("Could not find your religion data");

        // Find target player by name
        var targetPlayer = _sapi.World.AllPlayers
            .FirstOrDefault(p => p.PlayerName.Equals(targetPlayerName, StringComparison.OrdinalIgnoreCase));

        if (targetPlayer == null) return TextCommandResult.Error($"Player '{targetPlayerName}' not found");

        // Find role by name
        var role = religion.GetRoleByName(roleName);
        if (role == null) return TextCommandResult.Error($"Role '{roleName}' not found");

        // Assign the role
        var (success, error) =
            _roleManager.AssignRole(religion.ReligionUID, player.PlayerUID, targetPlayer.PlayerUID, role.RoleUID);

        if (!success) return TextCommandResult.Error(error);

        // Notify target player if online
        var targetServerPlayer = targetPlayer as IServerPlayer;
        if (targetServerPlayer != null)
            targetServerPlayer.SendMessage(
                GlobalConstants.GeneralChatGroup,
                $"You have been assigned the '{role.RoleName}' role in {religion.ReligionName}",
                EnumChatType.Notification
            );

        return TextCommandResult.Success($"{targetPlayerName} has been assigned the '{role.RoleName}' role");
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
        if (player == null) return TextCommandResult.Error("Command can only be used by players");

        var playerData = _playerReligionDataManager.GetOrCreatePlayerData(player.PlayerUID);
        if (!playerData.HasReligion()) return TextCommandResult.Error("You are not in any religion");

        var religion = _religionManager.GetReligion(playerData.ReligionUID!);
        if (religion == null) return TextCommandResult.Error("Could not find your religion data");

        // Find role by name
        var role = religion.GetRoleByName(roleName);
        if (role == null) return TextCommandResult.Error($"Role '{roleName}' not found");

        // Find permission by name (case insensitive)
        var permission = RolePermissions.AllPermissions
            .FirstOrDefault(p => p.Equals(permissionName, StringComparison.OrdinalIgnoreCase));

        if (permission == null)
        {
            var availablePerms = string.Join(", ", RolePermissions.AllPermissions);
            return TextCommandResult.Error(
                $"Invalid permission '{permissionName}'. Available permissions: {availablePerms}");
        }

        // Add permission to the role
        var updatedPermissions = new HashSet<string>(role.Permissions) { permission };

        var (success, updatedRole, error) =
            _roleManager.ModifyRolePermissions(religion.ReligionUID, player.PlayerUID, role.RoleUID,
                updatedPermissions);

        if (!success) return TextCommandResult.Error(error);

        return TextCommandResult.Success(
            $"Permission '{RolePermissions.GetDisplayName(permission)}' granted to role '{role.RoleName}'");
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
        if (player == null) return TextCommandResult.Error("Command can only be used by players");

        var playerData = _playerReligionDataManager.GetOrCreatePlayerData(player.PlayerUID);
        if (!playerData.HasReligion()) return TextCommandResult.Error("You are not in any religion");

        var religion = _religionManager.GetReligion(playerData.ReligionUID!);
        if (religion == null) return TextCommandResult.Error("Could not find your religion data");

        // Find role by name
        var role = religion.GetRoleByName(roleName);
        if (role == null) return TextCommandResult.Error($"Role '{roleName}' not found");

        // Find permission by name (case insensitive)
        var permission = RolePermissions.AllPermissions
            .FirstOrDefault(p => p.Equals(permissionName, StringComparison.OrdinalIgnoreCase));

        if (permission == null)
        {
            var availablePerms = string.Join(", ", RolePermissions.AllPermissions);
            return TextCommandResult.Error(
                $"Invalid permission '{permissionName}'. Available permissions: {availablePerms}");
        }

        // Remove permission from the role
        var updatedPermissions = new HashSet<string>(role.Permissions);
        updatedPermissions.Remove(permission);

        var (success, updatedRole, error) =
            _roleManager.ModifyRolePermissions(religion.ReligionUID, player.PlayerUID, role.RoleUID,
                updatedPermissions);

        if (!success) return TextCommandResult.Error(error);

        return TextCommandResult.Success(
            $"Permission '{RolePermissions.GetDisplayName(permission)}' revoked from role '{role.RoleName}'");
    }

    /// <summary>
    ///     Handler for /religion role permissions <rolename>
    /// </summary>
    internal TextCommandResult OnListRolePermissions(TextCommandCallingArgs args)
    {
        var roleName = (string)args[0];

        var player = args.Caller.Player as IServerPlayer;
        if (player == null) return TextCommandResult.Error("Command can only be used by players");

        var playerData = _playerReligionDataManager.GetOrCreatePlayerData(player.PlayerUID);
        if (!playerData.HasReligion()) return TextCommandResult.Error("You are not in any religion");

        var religion = _religionManager.GetReligion(playerData.ReligionUID!);
        if (religion == null) return TextCommandResult.Error("Could not find your religion data");

        // Find role by name
        var role = religion.GetRoleByName(roleName);
        if (role == null) return TextCommandResult.Error($"Role '{roleName}' not found");

        var sb = new StringBuilder();
        sb.AppendLine($"=== Permissions for '{role.RoleName}' ===");
        sb.AppendLine();

        if (role.Permissions.Count == 0)
            sb.AppendLine("This role has no permissions.");
        else
            foreach (var permission in role.Permissions.OrderBy(p => p))
            {
                var displayName = RolePermissions.GetDisplayName(permission);
                var description = RolePermissions.GetDescription(permission);
                sb.AppendLine($"✓ {displayName}");
                sb.AppendLine($"  {description}");
                sb.AppendLine();
            }

        sb.AppendLine("Available permissions:");
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
        if (player == null) return TextCommandResult.Error("Command can only be used by players");

        var playerData = _playerReligionDataManager.GetOrCreatePlayerData(player.PlayerUID);
        if (!playerData.HasReligion()) return TextCommandResult.Error("You are not in any religion");

        var religion = _religionManager.GetReligion(playerData.ReligionUID!);
        if (religion == null) return TextCommandResult.Error("Could not find your religion data");

        // Find target player by name
        var targetPlayer = _sapi.World.AllPlayers
            .FirstOrDefault(p => p.PlayerName.Equals(targetPlayerName, StringComparison.OrdinalIgnoreCase));

        if (targetPlayer == null) return TextCommandResult.Error($"Player '{targetPlayerName}' not found");

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
                    $"Founder status has been transferred from {player.PlayerName} to {targetPlayerName} in {religion.ReligionName}",
                    EnumChatType.Notification
                );
        }

        return TextCommandResult.Success($"Founder status transferred to {targetPlayerName}");
    }

    #endregion
}