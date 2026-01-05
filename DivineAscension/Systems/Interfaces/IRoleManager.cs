using System.Collections.Generic;
using DivineAscension.Data;

namespace DivineAscension.Systems.Interfaces;

public interface IRoleManager
{
    // Role CRUD
    (bool success, RoleData? role, string error) CreateCustomRole(string religionId, string playerId, string roleName);
    (bool success, string error) DeleteRole(string religionId, string playerId, string roleId);

    (bool success, RoleData? role, string error) RenameRole(string religionId, string playerId, string roleId,
        string newName);

    (bool success, RoleData? role, string error) ModifyRolePermissions(string religionId, string playerId,
        string roleId, HashSet<string> permissions);

    // Role assignment
    (bool success, string error) AssignRole(string religionId, string assignerId, string targetPlayerId, string roleId);
    (bool success, string error) TransferFounder(string religionId, string currentFounderId, string newFounderId);

    // Queries
    List<RoleData> GetReligionRoles(string? religionId);
    RoleData? GetPlayerRole(string religionId, string playerId);
    Dictionary<string, int> GetRoleMemberCounts(string religionId);
    List<string> GetPlayersWithRole(string religionId, string roleId);
}