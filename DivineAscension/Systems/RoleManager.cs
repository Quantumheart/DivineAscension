using System;
using System.Collections.Generic;
using System.Linq;
using DivineAscension.Data;
using DivineAscension.Models;
using DivineAscension.Systems.Interfaces;

namespace DivineAscension.Systems;

public class RoleManager(IReligionManager religionManager) : IRoleManager
{
    private const int MAX_CUSTOM_ROLES = 5;

    private const string ReligionNotFoundMsg = "Religion not found";
    private const string YouDonTHavePermissionToManageRolesMsg = "You don't have permission to manage roles";
    private const string RoleNameMustBeCharactersAndAlphanumeric = "Role name must be 3-30 characters and alphanumeric";

    private const string CannotUseReservedRoleNamesFounderOfficerMember =
        "Cannot use reserved role names (Founder, Officer, Member)";

    private const string ARoleWithThisNameAlreadyExists = "A role with this name already exists";
    private const string RoleNotFound = "Role not found";
    private const string CannotDeleteSystemRolesFounderMember = "Cannot delete system roles (Founder, Member)";
    private const string CannotRenameFounderRole = "Cannot rename Founder role";
    private const string CannotUseReservedRoleNames = "Cannot use reserved role names";
    private const string CannotModifyFounderRolePermissions = "Cannot modify Founder role permissions";
    private const string TargetPlayerIsNotAMemberOfThisReligion = "Target player is not a member of this religion";
    private const string YouDonTHavePermissionToAssignThisRole = "You don't have permission to assign this role";

    private const string CannotChangeFounderSRoleUseReligionTransferInstead =
        "Cannot change Founder's role. Use /religion transfer instead";

    private const string CannotAssignFounderRoleUseReligionTransferInstead =
        "Cannot assign Founder role. Use /religion transfer instead";

    private readonly IReligionManager? _religionManager =
        religionManager ?? throw new ArgumentNullException(nameof(religionManager));

    public (bool success, RoleData? role, string error) CreateCustomRole(string religionId, string playerId,
        string roleName)
    {
        var religion = _religionManager?.GetReligion(religionId);
        if (religion == null)
            return (false, null, ReligionNotFoundMsg);

        // Check permission
        if (!religion.HasPermission(playerId, RolePermissions.MANAGE_ROLES))
            return (false, null, YouDonTHavePermissionToManageRolesMsg);

        // Validate role name
        if (!RoleDefaults.IsValidRoleName(roleName))
            return (false, null, RoleNameMustBeCharactersAndAlphanumeric);

        if (RoleDefaults.IsReservedName(roleName))
            return (false, null, CannotUseReservedRoleNamesFounderOfficerMember);

        if (religion.IsRoleNameTaken(roleName))
            return (false, null, ARoleWithThisNameAlreadyExists);

        // Check role limit
        var customRoleCount = religion.Roles.Values.Count(r => !r.IsDefault);
        if (customRoleCount >= MAX_CUSTOM_ROLES)
            return (false, null, $"Maximum of {MAX_CUSTOM_ROLES} custom roles allowed");

        // Create new role
        var newRole = new RoleData(
            Guid.NewGuid().ToString(),
            roleName,
            false,
            false,
            religion.Roles.Count
        );

        // Add default member permissions to new custom roles
        newRole.AddPermission(RolePermissions.VIEW_MEMBERS);

        religion.Roles[newRole.RoleUID] = newRole;
        _religionManager?.Save(religion);

        return (true, newRole, string.Empty);
    }

    public (bool success, string error) DeleteRole(string religionId, string playerId, string roleId)
    {
        var religion = _religionManager?.GetReligion(religionId);
        if (religion == null)
            return (false, ReligionNotFoundMsg);

        if (!religion.HasPermission(playerId, RolePermissions.MANAGE_ROLES))
            return (false, YouDonTHavePermissionToManageRolesMsg);

        var role = religion.GetRole(roleId);
        if (role == null)
            return (false, RoleNotFound);

        if (role.IsProtected)
            return (false, CannotDeleteSystemRolesFounderMember);

        // Check if role is in use
        var membersWithRole = religion.GetMembersWithRole(roleId);
        if (membersWithRole.Count > 0)
            return (false, $"Cannot delete role with {membersWithRole.Count} member(s). Reassign them first.");

        religion.Roles.Remove(roleId);
        _religionManager?.Save(religion);

        return (true, string.Empty);
    }

    public (bool success, RoleData? role, string error) RenameRole(string religionId, string playerId,
        string roleId, string newName)
    {
        var religion = _religionManager?.GetReligion(religionId);
        if (religion == null)
            return (false, null, ReligionNotFoundMsg);

        if (!religion.HasPermission(playerId, RolePermissions.MANAGE_ROLES))
            return (false, null, YouDonTHavePermissionToManageRolesMsg);

        var role = religion.GetRole(roleId);
        if (role == null)
            return (false, null, RoleNotFound);

        if (role.RoleUID == RoleDefaults.FOUNDER_ROLE_ID)
            return (false, null, CannotRenameFounderRole);

        if (!RoleDefaults.IsValidRoleName(newName))
            return (false, null, RoleNameMustBeCharactersAndAlphanumeric);

        if (RoleDefaults.IsReservedName(newName))
            return (false, null, CannotUseReservedRoleNames);

        if (religion.IsRoleNameTaken(newName, roleId))
            return (false, null, ARoleWithThisNameAlreadyExists);

        role.RoleName = newName;
        _religionManager?.Save(religion);

        return (true, role, string.Empty);
    }

    public (bool success, RoleData? role, string error) ModifyRolePermissions(string religionId, string playerId,
        string roleId, HashSet<string> permissions)
    {
        var religion = _religionManager?.GetReligion(religionId);
        if (religion == null)
            return (false, null, ReligionNotFoundMsg);

        if (!religion.HasPermission(playerId, RolePermissions.MANAGE_ROLES))
            return (false, null, YouDonTHavePermissionToManageRolesMsg);

        var role = religion.GetRole(roleId);
        if (role == null)
            return (false, null, RoleNotFound);

        if (role.RoleUID == RoleDefaults.FOUNDER_ROLE_ID)
            return (false, null, CannotModifyFounderRolePermissions);

        // Validate all permissions
        foreach (var perm in permissions)
            if (!RolePermissions.IsValidPermission(perm))
                return (false, null, $"Invalid permission: {perm}");

        role.Permissions = new HashSet<string>(permissions);
        _religionManager?.Save(religion);

        return (true, role, string.Empty);
    }

    public (bool success, string error) AssignRole(string religionId, string assignerId, string targetPlayerId,
        string roleId)
    {
        var religion = _religionManager?.GetReligion(religionId);
        if (religion == null)
            return (false, ReligionNotFoundMsg);

        if (!religion.IsMember(targetPlayerId))
            return (false, TargetPlayerIsNotAMemberOfThisReligion);

        // Check if assigner can assign roles
        if (!religion.CanAssignRole(assignerId, roleId))
            return (false, YouDonTHavePermissionToAssignThisRole);

        // Cannot demote the founder
        if (religion.GetPlayerRole(targetPlayerId) == RoleDefaults.FOUNDER_ROLE_ID)
            return (false, CannotChangeFounderSRoleUseReligionTransferInstead);

        // Cannot assign Founder role
        if (roleId == RoleDefaults.FOUNDER_ROLE_ID)
            return (false, CannotAssignFounderRoleUseReligionTransferInstead);

        var role = religion.GetRole(roleId);
        if (role == null)
            return (false, RoleNotFound);

        religion.MemberRoles[targetPlayerId] = roleId;
        _religionManager?.Save(religion);

        return (true, string.Empty);
    }

    public (bool success, string error) TransferFounder(string religionId, string currentFounderId,
        string newFounderId)
    {
        var religion = _religionManager?.GetReligion(religionId);
        if (religion == null)
            return (false, ReligionNotFoundMsg);

        // Only current founder can transfer
        if (religion.GetPlayerRole(currentFounderId) != RoleDefaults.FOUNDER_ROLE_ID)
            return (false, "Only the founder can transfer founder status");

        if (!religion.IsMember(newFounderId))
            return (false, TargetPlayerIsNotAMemberOfThisReligion);

        if (currentFounderId == newFounderId)
            return (false, "You are already the founder");

        // Transfer founder role
        religion.MemberRoles[newFounderId] = RoleDefaults.FOUNDER_ROLE_ID;
        religion.MemberRoles[currentFounderId] = RoleDefaults.MEMBER_ROLE_ID; // Demote to member

        // Update legacy FounderUID field for backwards compatibility
        religion.FounderUID = newFounderId;

        // Update member list order (founder should be first)
        religion.MemberUIDs.Remove(newFounderId);
        religion.MemberUIDs.Insert(0, newFounderId);

        _religionManager?.Save(religion);

        return (true, string.Empty);
    }

    public List<RoleData> GetReligionRoles(string religionId)
    {
        var religion = _religionManager?.GetReligion(religionId);
        if (religion == null)
            return new List<RoleData>();

        return religion.Roles.Values
            .OrderBy(r => r.DisplayOrder)
            .ThenBy(r => r.RoleName)
            .ToList();
    }

    public RoleData? GetPlayerRole(string religionId, string playerId)
    {
        var religion = _religionManager?.GetReligion(religionId);
        if (religion == null)
            return null;

        var playerRole = religion.GetPlayerRole(playerId);
        return religion.GetRole(playerRole);
    }

    public Dictionary<string, int> GetRoleMemberCounts(string religionId)
    {
        var religion = _religionManager?.GetReligion(religionId);
        if (religion == null)
            return new Dictionary<string, int>();

        return religion.GetRoleMemberCounts();
    }

    public List<string> GetPlayersWithRole(string religionId, string roleId)
    {
        var religion = _religionManager?.GetReligion(religionId);
        if (religion == null)
            return new List<string>();

        return religion.GetMembersWithRole(roleId);
    }
}