using System.Collections.Generic;
using PantheonWars.Data;
using ProtoBuf;

namespace PantheonWars.Network;

// Request all roles for a religion
[ProtoContract]
public class ReligionRolesRequest
{
    [ProtoMember(1)] public string ReligionUID;
}

[ProtoContract]
public class ReligionRolesResponse
{
    [ProtoMember(1)] public bool Success;
    [ProtoMember(2)] public List<RoleData>? Roles;
    [ProtoMember(3)] public Dictionary<string, string>? MemberRoles; // UID → RoleUID
    [ProtoMember(4)] public string? ErrorMessage;
}

// Create custom role
[ProtoContract]
public class CreateRoleRequest
{
    [ProtoMember(1)] public string? ReligionUID;
    [ProtoMember(2)] public string? RoleName;
}

[ProtoContract]
public class CreateRoleResponse
{
    [ProtoMember(1)] public bool Success;
    [ProtoMember(2)] public RoleData? CreatedRole;
    [ProtoMember(3)] public string? ErrorMessage;
}

// Modify role permissions
[ProtoContract]
public class ModifyRolePermissionsRequest
{
    [ProtoMember(1)] public string? ReligionUID;
    [ProtoMember(2)] public string? RoleUID;
    [ProtoMember(3)] public HashSet<string>? Permissions;
}

[ProtoContract]
public class ModifyRolePermissionsResponse
{
    [ProtoMember(1)] public bool Success;
    [ProtoMember(2)] public RoleData? UpdatedRole;
    [ProtoMember(3)] public string? ErrorMessage;
}

// Assign role to member
[ProtoContract]
public class AssignRoleRequest
{
    [ProtoMember(1)] public string? ReligionUID;
    [ProtoMember(2)] public string? TargetPlayerUID;
    [ProtoMember(3)] public string? RoleUID;
}

[ProtoContract]
public class AssignRoleResponse
{
    [ProtoMember(1)] public bool Success;
    [ProtoMember(2)] public string? ErrorMessage;
}

// Delete custom role
[ProtoContract]
public class DeleteRoleRequest
{
    [ProtoMember(1)] public string? ReligionUID;
    [ProtoMember(2)] public string? RoleUID;
}

[ProtoContract]
public class DeleteRoleResponse
{
    [ProtoMember(1)] public bool Success;
    [ProtoMember(2)] public string? ErrorMessage;
}

// Transfer founder
[ProtoContract]
public class TransferFounderRequest
{
    [ProtoMember(1)] public string? ReligionUID;
    [ProtoMember(2)] public string? NewFounderUID;
}

[ProtoContract]
public class TransferFounderResponse
{
    [ProtoMember(1)] public bool Success;
    [ProtoMember(2)] public string? ErrorMessage;
}