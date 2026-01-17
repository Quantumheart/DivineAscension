using ProtoBuf;

namespace DivineAscension.Data;

[ProtoContract]
public class RoleData
{
    // Parameterless constructor for ProtoBuf
    public RoleData()
    {
    }

    public RoleData(string roleUID, string roleName, bool isDefault, bool isProtected, int displayOrder)
    {
        RoleUID = roleUID;
        RoleName = roleName;
        IsDefault = isDefault;
        IsProtected = isProtected;
        DisplayOrder = displayOrder;
        CreatedDate = DateTime.UtcNow;
        Permissions = new HashSet<string>();
    }

    [ProtoMember(1)] public string RoleUID { get; set; } = Guid.NewGuid().ToString();

    [ProtoMember(2)] public string RoleName { get; set; } = string.Empty;

    [ProtoMember(3)] public bool IsDefault { get; set; }

    [ProtoMember(4)] public bool IsProtected { get; set; }

    [ProtoMember(5)] public HashSet<string> Permissions { get; set; } = new();

    [ProtoMember(6)] public int DisplayOrder { get; set; } = 999;

    [ProtoMember(7)] public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public bool HasPermission(string permission)
    {
        return Permissions.Contains(permission);
    }

    public void AddPermission(string permission)
    {
        Permissions.Add(permission);
    }

    public void RemovePermission(string permission)
    {
        Permissions.Remove(permission);
    }

    public RoleData Clone()
    {
        return new RoleData
        {
            RoleUID = RoleUID,
            RoleName = RoleName,
            IsDefault = IsDefault,
            IsProtected = IsProtected,
            Permissions = new HashSet<string>(Permissions),
            DisplayOrder = DisplayOrder,
            CreatedDate = CreatedDate
        };
    }
}