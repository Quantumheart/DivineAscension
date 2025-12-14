using System.Collections.Generic;
using PantheonWars.Data;
using ProtoBuf;

namespace PantheonWars.Network;

// Request all roles for a religion
[ProtoContract]
public class ReligionRolesRequest
{
    [ProtoMember(1)] public readonly string ReligionUID;

    public ReligionRolesRequest()
    {
    }

    public ReligionRolesRequest(string religionUid)
    {
        ReligionUID = religionUid;
    }
}

[ProtoContract]
public class ReligionRolesResponse
{
    [ProtoMember(1)] public bool Success { get; set; }
    [ProtoMember(2)] public List<RoleData> Roles { get; set; }
    [ProtoMember(3)] public Dictionary<string, string> MemberRoles { get; set; } // UID → RoleUID
    [ProtoMember(4)] public string ErrorMessage { get; set; } = string.Empty;
    [ProtoMember(5)] public Dictionary<string, string> MemberNames { get; set; } = new(); // UID → PlayerName
}

// Create custom role
[ProtoContract]
public class CreateRoleRequest
{
    [ProtoMember(1)] public string? ReligionUID;
    [ProtoMember(2)] public string? RoleName;

    public CreateRoleRequest()
    {
    }

    public CreateRoleRequest(string religionUid, string roleName)
    {
        ReligionUID = religionUid;
        RoleName = roleName;
    }
}

[ProtoContract]
public class CreateRoleResponse
{
    [ProtoMember(2)] public RoleData? CreatedRole;
    [ProtoMember(3)] public string? ErrorMessage;
    [ProtoMember(1)] public bool Success;
}

// Modify role permissions
[ProtoContract]
public class ModifyRolePermissionsRequest
{
    [ProtoMember(3)] public HashSet<string> Permissions;
    [ProtoMember(1)] public string ReligionUID;
    [ProtoMember(2)] public string RoleUID;

    public ModifyRolePermissionsRequest()
    {
    }

    public ModifyRolePermissionsRequest(string religionUid, string roleUid, HashSet<string> permissions)
    {
        ReligionUID = religionUid;
        RoleUID = roleUid;
        Permissions = permissions;
    }
}

[ProtoContract]
public class ModifyRolePermissionsResponse
{
    [ProtoMember(3)] public string? ErrorMessage;
    [ProtoMember(1)] public bool Success;
    [ProtoMember(2)] public RoleData? UpdatedRole;

    public ModifyRolePermissionsResponse()
    {
    }

    public ModifyRolePermissionsResponse(bool success, RoleData? updatedRole, string errorMessage)
    {
        Success = success;
        UpdatedRole = updatedRole;
        ErrorMessage = errorMessage;
    }
}

// Assign role to member
[ProtoContract]
public class AssignRoleRequest
{
    [ProtoMember(1)] public string ReligionUID = string.Empty;
    [ProtoMember(3)] public string RoleUID = string.Empty;
    [ProtoMember(2)] public string TargetPlayerUID = string.Empty;

    public AssignRoleRequest(string religionUid, string targetPlayerUid, string roleUid)
    {
        ReligionUID = religionUid;
        RoleUID = roleUid;
        TargetPlayerUID = targetPlayerUid;
    }

    public AssignRoleRequest()
    {
    }
}

[ProtoContract]
public class AssignRoleResponse
{
    [ProtoMember(2)] public string? ErrorMessage;
    [ProtoMember(1)] public bool Success;

    public AssignRoleResponse()
    {
    }

    public AssignRoleResponse(bool success, string? errorMessage)
    {
        Success = success;
        ErrorMessage = errorMessage;
    }
}

// Delete custom role
[ProtoContract]
public class DeleteRoleRequest
{
    [ProtoMember(1)] public string ReligionUID = string.Empty;
    [ProtoMember(2)] public string RoleUID = string.Empty;

    public DeleteRoleRequest()
    {
    }

    public DeleteRoleRequest(string id, string roleId)
    {
        ReligionUID = id;
        RoleUID = roleId;
    }
}

[ProtoContract]
public class DeleteRoleResponse
{
    [ProtoMember(2)] public string? ErrorMessage;
    [ProtoMember(1)] public bool Success;
}

// Transfer founder
[ProtoContract]
public class TransferFounderRequest
{
    [ProtoMember(2)] public string NewFounderUID = string.Empty;
    [ProtoMember(1)] public string ReligionUID = string.Empty;

    public TransferFounderRequest()
    {
    }

    public TransferFounderRequest(string religionUid, string newFounderUid)
    {
        ReligionUID = religionUid;
        NewFounderUID = newFounderUid;
    }
}

[ProtoContract]
public class TransferFounderResponse
{
    [ProtoMember(2)] public string? ErrorMessage;
    [ProtoMember(1)] public bool Success;

    public TransferFounderResponse()
    {
    }

    public TransferFounderResponse(bool success, string? errorMessage)
    {
        Success = success;
        ErrorMessage = errorMessage;
    }
}