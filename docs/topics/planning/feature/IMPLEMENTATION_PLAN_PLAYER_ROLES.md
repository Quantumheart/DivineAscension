# Implementation Plan: Player Roles & Permissions System

## Overview
This document provides a detailed, step-by-step implementation plan for the Player Roles & Permissions system. It breaks down the feature spec into concrete coding tasks organized by phase.

---

## Project Structure

### New Files to Create

```
PantheonWars/
├── Data/
│   ├── RoleData.cs                          # NEW - Role data model
│   └── BanEntry.cs                          # EXISTS (in ReligionData.cs)
│
├── Models/
│   ├── RolePermissions.cs                   # NEW - Permission constants
│   └── RoleDefaults.cs                      # NEW - Default role factory
│
├── Systems/
│   ├── RoleManager.cs                       # NEW - Role management system
│   └── Interfaces/
│       └── IRoleManager.cs                  # NEW - Role manager interface
│
├── Network/
│   ├── ReligionRolesRequestPacket.cs        # NEW
│   ├── ReligionRolesResponsePacket.cs       # NEW
│   ├── CreateRoleRequestPacket.cs           # NEW
│   ├── CreateRoleResponsePacket.cs          # NEW
│   ├── ModifyRolePermissionsRequestPacket.cs # NEW
│   ├── ModifyRolePermissionsResponsePacket.cs # NEW
│   ├── AssignRoleRequestPacket.cs           # NEW
│   ├── AssignRoleResponsePacket.cs          # NEW
│   ├── DeleteRoleRequestPacket.cs           # NEW
│   ├── DeleteRoleResponsePacket.cs          # NEW
│   ├── TransferFounderRequestPacket.cs      # NEW
│   └── TransferFounderResponsePacket.cs     # NEW
│
├── Commands/
│   └── RoleCommands.cs                      # NEW - Role management commands
│
├── GUI/
│   ├── Models/
│   │   └── Religion/
│   │       ├── ReligionRolesViewModel.cs    # NEW
│   │       ├── ReligionRolesRenderResult.cs # NEW
│   │       ├── RoleEditorViewModel.cs       # NEW
│   │       └── RoleEditorRenderResult.cs    # NEW
│   │
│   ├── State/
│   │   └── Religion/
│   │       ├── ReligionRolesTabState.cs     # NEW
│   │       └── RoleEditorState.cs           # NEW
│   │
│   ├── Events/
│   │   └── Religion/
│   │       ├── RolesTabEvents.cs            # NEW
│   │       └── RoleEditorEvents.cs          # NEW
│   │
│   └── UI/
│       └── Renderers/
│           ├── ReligionRolesRenderer.cs     # NEW
│           └── RoleEditorRenderer.cs        # NEW
│
└── Tests/
    ├── RoleDataTests.cs                     # NEW
    ├── RoleManagerTests.cs                  # NEW
    ├── RolePermissionTests.cs               # NEW
    └── RoleMigrationTests.cs                # NEW
```

### Files to Modify

```
PantheonWars/
├── Data/
│   └── ReligionData.cs                      # MODIFY - Add Roles, MemberRoles
│
├── Systems/
│   ├── ReligionManager.cs                   # MODIFY - Add migration, permission checks
│   └── Interfaces/
│       └── IReligionManager.cs              # MODIFY - Add role methods
│
├── Commands/
│   └── ReligionCommands.cs                  # MODIFY - Update permission checks
│
├── Network/
│   ├── PlayerReligionInfoResponsePacket.cs  # MODIFY - Add role data
│   └── ReligionListResponsePacket.cs        # MODIFY - Add role counts
│
├── GUI/
│   ├── Models/
│   │   └── Religion/
│   │       └── ReligionInfoViewModel.cs     # MODIFY - Add role info
│   │
│   ├── State/
│   │   └── Religion/
│   │       └── ReligionDialogState.cs       # MODIFY - Add Roles tab
│   │
│   └── UI/
│       └── Renderers/
│           ├── ReligionDialogRenderer.cs    # MODIFY - Add Roles tab
│           └── ReligionInfoRenderer.cs      # MODIFY - Add role badges
│
└── PantheonWarsSystem.cs                    # MODIFY - Register RoleManager
```

---

## Phase 1: Foundation & Data Model (Week 1)

### Task 1.1: Create RoleData Model
**File**: `/PantheonWars/Data/RoleData.cs`
**Estimated Time**: 2 hours

```csharp
using ProtoBuf;
using System;
using System.Collections.Generic;

namespace PantheonWars.Data
{
    [ProtoContract]
    public class RoleData
    {
        [ProtoMember(1)]
        public string RoleUID { get; set; } = Guid.NewGuid().ToString();

        [ProtoMember(2)]
        public string RoleName { get; set; } = string.Empty;

        [ProtoMember(3)]
        public bool IsDefault { get; set; } = false;

        [ProtoMember(4)]
        public bool IsProtected { get; set; } = false;

        [ProtoMember(5)]
        public HashSet<string> Permissions { get; set; } = new HashSet<string>();

        [ProtoMember(6)]
        public int DisplayOrder { get; set; } = 999;

        [ProtoMember(7)]
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        // Parameterless constructor for ProtoBuf
        public RoleData() { }

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
                RoleUID = this.RoleUID,
                RoleName = this.RoleName,
                IsDefault = this.IsDefault,
                IsProtected = this.IsProtected,
                Permissions = new HashSet<string>(this.Permissions),
                DisplayOrder = this.DisplayOrder,
                CreatedDate = this.CreatedDate
            };
        }
    }
}
```

**Testing**:
- Verify ProtoBuf serialization/deserialization
- Test permission add/remove
- Test clone method

---

### Task 1.2: Create RolePermissions Constants
**File**: `/PantheonWars/Models/RolePermissions.cs`
**Estimated Time**: 1 hour

```csharp
using System.Collections.Generic;

namespace PantheonWars.Models
{
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
        public static readonly HashSet<string> AllPermissions = new HashSet<string>
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
        public static readonly Dictionary<string, string> PermissionDisplayNames = new Dictionary<string, string>
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
        public static readonly Dictionary<string, string> PermissionDescriptions = new Dictionary<string, string>
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
}
```

---

### Task 1.3: Create RoleDefaults Factory
**File**: `/PantheonWars/Models/RoleDefaults.cs`
**Estimated Time**: 2 hours

```csharp
using PantheonWars.Data;
using System;
using System.Collections.Generic;

namespace PantheonWars.Models
{
    public static class RoleDefaults
    {
        // Fixed role IDs for default roles
        public const string FOUNDER_ROLE_ID = "role_founder";
        public const string OFFICER_ROLE_ID = "role_officer";
        public const string MEMBER_ROLE_ID = "role_member";

        // Reserved role names (case-insensitive)
        public static readonly HashSet<string> ReservedRoleNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Founder",
            "Officer",
            "Member"
        };

        public static RoleData CreateFounderRole()
        {
            var role = new RoleData(
                roleUID: FOUNDER_ROLE_ID,
                roleName: "Founder",
                isDefault: true,
                isProtected: true,
                displayOrder: 0
            );

            // Founder has ALL permissions
            foreach (var permission in RolePermissions.AllPermissions)
            {
                role.AddPermission(permission);
            }

            return role;
        }

        public static RoleData CreateOfficerRole()
        {
            var role = new RoleData(
                roleUID: OFFICER_ROLE_ID,
                roleName: "Officer",
                isDefault: true,
                isProtected: false,
                displayOrder: 1
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
                roleUID: MEMBER_ROLE_ID,
                roleName: "Member",
                isDefault: true,
                isProtected: true,
                displayOrder: 2
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
            foreach (char c in roleName)
            {
                if (!char.IsLetterOrDigit(c) && c != ' ')
                    return false;
            }

            return true;
        }
    }
}
```

---

### Task 1.4: Modify ReligionData
**File**: `/PantheonWars/Data/ReligionData.cs`
**Estimated Time**: 3 hours

**Add new fields**:
```csharp
[ProtoMember(15)]
public Dictionary<string, RoleData> Roles { get; set; } = new Dictionary<string, RoleData>();

[ProtoMember(16)]
public Dictionary<string, string> MemberRoles { get; set; } = new Dictionary<string, string>();
```

**Add new methods**:
```csharp
using PantheonWars.Models;

// Get player's role
public string GetPlayerRole(string playerUID)
{
    if (MemberRoles.TryGetValue(playerUID, out string roleUID))
        return roleUID;

    return RoleDefaults.MEMBER_ROLE_ID; // Fallback
}

// Get role data
public RoleData? GetRole(string roleUID)
{
    return Roles.TryGetValue(roleUID, out RoleData? role) ? role : null;
}

// Check if player has a specific permission
public bool HasPermission(string playerUID, string permission)
{
    string roleUID = GetPlayerRole(playerUID);
    RoleData? role = GetRole(roleUID);

    if (role == null)
        return false;

    return role.HasPermission(permission);
}

// Check if player can assign a specific role
public bool CanAssignRole(string assignerUID, string targetRoleUID)
{
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
    {
        if (role.RoleName.Equals(roleName, StringComparison.OrdinalIgnoreCase))
            return role;
    }
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
    {
        if (kvp.Value == roleUID)
            members.Add(kvp.Key);
    }

    return members;
}

// Count members per role
public Dictionary<string, int> GetRoleMemberCounts()
{
    var counts = new Dictionary<string, int>();

    foreach (var role in Roles.Keys)
    {
        counts[role] = 0;
    }

    foreach (var roleUID in MemberRoles.Values)
    {
        if (counts.ContainsKey(roleUID))
            counts[roleUID]++;
        else
            counts[roleUID] = 1;
    }

    return counts;
}
```

**Keep backwards compatibility**:
- Keep `FounderUID` field (ProtoMember(2))
- Update it when founder transfers
- Use as fallback in `IsFounder()` method

---

### Task 1.5: Implement Migration Logic
**File**: `/PantheonWars/Systems/ReligionManager.cs`
**Estimated Time**: 4 hours

Add migration method:
```csharp
private void MigrateReligionToRoleSystem(ReligionData religion)
{
    bool needsMigration = false;

    // Check if roles need initialization
    if (religion.Roles == null || religion.Roles.Count == 0)
    {
        ModLog.Log($"[ReligionManager] Migrating religion '{religion.ReligionName}' ({religion.ReligionUID}) to role system");

        // Initialize default roles
        religion.Roles = RoleDefaults.CreateDefaultRoles();
        needsMigration = true;
    }

    // Check if member roles need initialization
    if (religion.MemberRoles == null || religion.MemberRoles.Count == 0)
    {
        religion.MemberRoles = new Dictionary<string, string>();

        // Assign Founder role to founder
        if (!string.IsNullOrEmpty(religion.FounderUID))
        {
            religion.MemberRoles[religion.FounderUID] = RoleDefaults.FOUNDER_ROLE_ID;
            ModLog.Log($"[ReligionManager] Assigned Founder role to {religion.FounderUID}");
        }

        // Assign Member role to all other members
        foreach (string memberUID in religion.MemberUIDs)
        {
            if (memberUID != religion.FounderUID && !religion.MemberRoles.ContainsKey(memberUID))
            {
                religion.MemberRoles[memberUID] = RoleDefaults.MEMBER_ROLE_ID;
            }
        }

        ModLog.Log($"[ReligionManager] Assigned Member role to {religion.MemberUIDs.Count - 1} members");
        needsMigration = true;
    }

    // Save if we made changes
    if (needsMigration)
    {
        SaveReligion(religion);
        ModLog.Log($"[ReligionManager] Migration completed for '{religion.ReligionName}'");
    }
}

private void MigrateAllReligions()
{
    foreach (var religion in _religions.Values)
    {
        MigrateReligionToRoleSystem(religion);
    }
}
```

Call migration in `Initialize()` method:
```csharp
public void Initialize(ICoreAPI api)
{
    // ... existing initialization ...

    // Load all religions
    LoadAllReligions();

    // Migrate existing religions to role system
    MigrateAllReligions();
}
```

---

### Task 1.6: Write Unit Tests
**File**: `/PantheonWars.Tests/RoleDataTests.cs`
**Estimated Time**: 3 hours

```csharp
using Xunit;
using PantheonWars.Data;
using PantheonWars.Models;
using System;

namespace PantheonWars.Tests
{
    public class RoleDataTests
    {
        [Fact]
        public void RoleData_Constructor_SetsPropertiesCorrectly()
        {
            var role = new RoleData("test-uid", "TestRole", true, false, 5);

            Assert.Equal("test-uid", role.RoleUID);
            Assert.Equal("TestRole", role.RoleName);
            Assert.True(role.IsDefault);
            Assert.False(role.IsProtected);
            Assert.Equal(5, role.DisplayOrder);
            Assert.NotNull(role.Permissions);
            Assert.Empty(role.Permissions);
        }

        [Fact]
        public void AddPermission_AddsPermissionToSet()
        {
            var role = new RoleData("test", "Test", false, false, 0);
            role.AddPermission(RolePermissions.INVITE_PLAYERS);

            Assert.True(role.HasPermission(RolePermissions.INVITE_PLAYERS));
            Assert.Single(role.Permissions);
        }

        [Fact]
        public void RemovePermission_RemovesPermissionFromSet()
        {
            var role = new RoleData("test", "Test", false, false, 0);
            role.AddPermission(RolePermissions.INVITE_PLAYERS);
            role.RemovePermission(RolePermissions.INVITE_PLAYERS);

            Assert.False(role.HasPermission(RolePermissions.INVITE_PLAYERS));
            Assert.Empty(role.Permissions);
        }

        [Fact]
        public void Clone_CreatesDeepCopy()
        {
            var original = new RoleData("test", "Test", true, true, 1);
            original.AddPermission(RolePermissions.KICK_MEMBERS);

            var clone = original.Clone();

            Assert.Equal(original.RoleUID, clone.RoleUID);
            Assert.Equal(original.RoleName, clone.RoleName);
            Assert.Equal(original.IsDefault, clone.IsDefault);
            Assert.Equal(original.IsProtected, clone.IsProtected);
            Assert.Equal(original.DisplayOrder, clone.DisplayOrder);
            Assert.True(clone.HasPermission(RolePermissions.KICK_MEMBERS));

            // Verify it's a deep copy
            clone.AddPermission(RolePermissions.BAN_PLAYERS);
            Assert.False(original.HasPermission(RolePermissions.BAN_PLAYERS));
        }

        [Fact]
        public void RoleDefaults_CreateFounderRole_HasAllPermissions()
        {
            var founder = RoleDefaults.CreateFounderRole();

            Assert.Equal("Founder", founder.RoleName);
            Assert.True(founder.IsDefault);
            Assert.True(founder.IsProtected);
            Assert.Equal(0, founder.DisplayOrder);

            foreach (var permission in RolePermissions.AllPermissions)
            {
                Assert.True(founder.HasPermission(permission));
            }
        }

        [Fact]
        public void RoleDefaults_CreateOfficerRole_HasCorrectPermissions()
        {
            var officer = RoleDefaults.CreateOfficerRole();

            Assert.Equal("Officer", officer.RoleName);
            Assert.True(officer.IsDefault);
            Assert.False(officer.IsProtected);

            Assert.True(officer.HasPermission(RolePermissions.INVITE_PLAYERS));
            Assert.True(officer.HasPermission(RolePermissions.KICK_MEMBERS));
            Assert.False(officer.HasPermission(RolePermissions.BAN_PLAYERS));
            Assert.False(officer.HasPermission(RolePermissions.MANAGE_ROLES));
        }

        [Fact]
        public void RoleDefaults_CreateMemberRole_HasViewOnlyPermissions()
        {
            var member = RoleDefaults.CreateMemberRole();

            Assert.Equal("Member", member.RoleName);
            Assert.True(member.IsDefault);
            Assert.True(member.IsProtected);

            Assert.True(member.HasPermission(RolePermissions.VIEW_MEMBERS));
            Assert.False(member.HasPermission(RolePermissions.INVITE_PLAYERS));
        }

        [Theory]
        [InlineData("ValidRole", true)]
        [InlineData("Role 123", true)]
        [InlineData("ABC", true)]
        [InlineData("AB", false)] // Too short
        [InlineData("", false)] // Empty
        [InlineData("A", false)] // Too short
        [InlineData("ThisRoleNameIsWayTooLongAndExceedsTheMaximum", false)] // Too long
        [InlineData("Role@#$", false)] // Special chars
        public void RoleDefaults_IsValidRoleName_ValidatesCorrectly(string name, bool expected)
        {
            Assert.Equal(expected, RoleDefaults.IsValidRoleName(name));
        }

        [Theory]
        [InlineData("Founder", true)]
        [InlineData("founder", true)] // Case insensitive
        [InlineData("OFFICER", true)]
        [InlineData("Member", true)]
        [InlineData("CustomRole", false)]
        public void RoleDefaults_IsReservedName_IdentifiesReservedNames(string name, bool expected)
        {
            Assert.Equal(expected, RoleDefaults.IsReservedName(name));
        }
    }
}
```

**File**: `/PantheonWars.Tests/RoleMigrationTests.cs`
```csharp
using Xunit;
using PantheonWars.Data;
using PantheonWars.Models;
using System.Collections.Generic;

namespace PantheonWars.Tests
{
    public class RoleMigrationTests
    {
        [Fact]
        public void HasPermission_WithoutMigration_ReturnsFalse()
        {
            var religion = new ReligionData
            {
                ReligionUID = "test-religion",
                FounderUID = "founder-123",
                MemberUIDs = new List<string> { "founder-123", "member-456" }
                // Roles and MemberRoles not initialized
            };

            // Should return false since Roles is empty
            Assert.False(religion.HasPermission("founder-123", RolePermissions.INVITE_PLAYERS));
        }

        [Fact]
        public void GetPlayerRole_AfterMigration_ReturnsCorrectRole()
        {
            var religion = CreateMigratedReligion();

            Assert.Equal(RoleDefaults.FOUNDER_ROLE_ID, religion.GetPlayerRole("founder-123"));
            Assert.Equal(RoleDefaults.MEMBER_ROLE_ID, religion.GetPlayerRole("member-456"));
        }

        [Fact]
        public void HasPermission_AfterMigration_WorksCorrectly()
        {
            var religion = CreateMigratedReligion();

            // Founder should have all permissions
            Assert.True(religion.HasPermission("founder-123", RolePermissions.BAN_PLAYERS));
            Assert.True(religion.HasPermission("founder-123", RolePermissions.MANAGE_ROLES));

            // Member should only have view permission
            Assert.True(religion.HasPermission("member-456", RolePermissions.VIEW_MEMBERS));
            Assert.False(religion.HasPermission("member-456", RolePermissions.INVITE_PLAYERS));
        }

        private ReligionData CreateMigratedReligion()
        {
            var religion = new ReligionData
            {
                ReligionUID = "test-religion",
                FounderUID = "founder-123",
                MemberUIDs = new List<string> { "founder-123", "member-456" },
                Roles = RoleDefaults.CreateDefaultRoles(),
                MemberRoles = new Dictionary<string, string>
                {
                    ["founder-123"] = RoleDefaults.FOUNDER_ROLE_ID,
                    ["member-456"] = RoleDefaults.MEMBER_ROLE_ID
                }
            };

            return religion;
        }
    }
}
```

---

## Phase 2: Permission System (Week 2)

### Task 2.1: Create RoleManager System
**File**: `/PantheonWars/Systems/Interfaces/IRoleManager.cs`
**Estimated Time**: 1 hour

```csharp
using PantheonWars.Data;
using System.Collections.Generic;

namespace PantheonWars.Systems.Interfaces
{
    public interface IRoleManager
    {
        // Role CRUD
        (bool success, RoleData? role, string error) CreateCustomRole(string religionUID, string playerUID, string roleName);
        (bool success, string error) DeleteRole(string religionUID, string playerUID, string roleUID);
        (bool success, RoleData? role, string error) RenameRole(string religionUID, string playerUID, string roleUID, string newName);
        (bool success, RoleData? role, string error) ModifyRolePermissions(string religionUID, string playerUID, string roleUID, HashSet<string> permissions);

        // Role assignment
        (bool success, string error) AssignRole(string religionUID, string assignerUID, string targetPlayerUID, string roleUID);
        (bool success, string error) TransferFounder(string religionUID, string currentFounderUID, string newFounderUID);

        // Queries
        List<RoleData> GetReligionRoles(string religionUID);
        RoleData? GetPlayerRole(string religionUID, string playerUID);
        Dictionary<string, int> GetRoleMemberCounts(string religionUID);
        List<string> GetPlayersWithRole(string religionUID, string roleUID);
    }
}
```

**File**: `/PantheonWars/Systems/RoleManager.cs`
**Estimated Time**: 6 hours

```csharp
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using PantheonWars.Data;
using PantheonWars.Models;
using PantheonWars.Systems.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PantheonWars.Systems
{
    public class RoleManager : ModSystem, IRoleManager
    {
        private const int MAX_CUSTOM_ROLES = 10;
        private IReligionManager? _religionManager;

        public override void StartServerSide(ICoreServerAPI api)
        {
            base.StartServerSide(api);
            _religionManager = api.ModLoader.GetModSystem<ReligionManager>();
        }

        public (bool success, RoleData? role, string error) CreateCustomRole(string religionUID, string playerUID, string roleName)
        {
            var religion = _religionManager?.GetReligion(religionUID);
            if (religion == null)
                return (false, null, "Religion not found");

            // Check permission
            if (!religion.HasPermission(playerUID, RolePermissions.MANAGE_ROLES))
                return (false, null, "You don't have permission to manage roles");

            // Validate role name
            if (!RoleDefaults.IsValidRoleName(roleName))
                return (false, null, "Role name must be 3-30 characters and alphanumeric");

            if (RoleDefaults.IsReservedName(roleName))
                return (false, null, "Cannot use reserved role names (Founder, Officer, Member)");

            if (religion.IsRoleNameTaken(roleName))
                return (false, null, "A role with this name already exists");

            // Check role limit
            int customRoleCount = religion.Roles.Values.Count(r => !r.IsDefault);
            if (customRoleCount >= MAX_CUSTOM_ROLES)
                return (false, null, $"Maximum of {MAX_CUSTOM_ROLES} custom roles allowed");

            // Create new role
            var newRole = new RoleData(
                roleUID: Guid.NewGuid().ToString(),
                roleName: roleName,
                isDefault: false,
                isProtected: false,
                displayOrder: religion.Roles.Count
            );

            // Add default member permissions to new custom roles
            newRole.AddPermission(RolePermissions.VIEW_MEMBERS);

            religion.Roles[newRole.RoleUID] = newRole;
            _religionManager?.SaveReligion(religion);

            return (true, newRole, string.Empty);
        }

        public (bool success, string error) DeleteRole(string religionUID, string playerUID, string roleUID)
        {
            var religion = _religionManager?.GetReligion(religionUID);
            if (religion == null)
                return (false, "Religion not found");

            if (!religion.HasPermission(playerUID, RolePermissions.MANAGE_ROLES))
                return (false, "You don't have permission to manage roles");

            var role = religion.GetRole(roleUID);
            if (role == null)
                return (false, "Role not found");

            if (role.IsProtected)
                return (false, "Cannot delete system roles (Founder, Member)");

            // Check if role is in use
            var membersWithRole = religion.GetMembersWithRole(roleUID);
            if (membersWithRole.Count > 0)
                return (false, $"Cannot delete role with {membersWithRole.Count} member(s). Reassign them first.");

            religion.Roles.Remove(roleUID);
            _religionManager?.SaveReligion(religion);

            return (true, string.Empty);
        }

        public (bool success, RoleData? role, string error) RenameRole(string religionUID, string playerUID, string roleUID, string newName)
        {
            var religion = _religionManager?.GetReligion(religionUID);
            if (religion == null)
                return (false, null, "Religion not found");

            if (!religion.HasPermission(playerUID, RolePermissions.MANAGE_ROLES))
                return (false, null, "You don't have permission to manage roles");

            var role = religion.GetRole(roleUID);
            if (role == null)
                return (false, null, "Role not found");

            if (role.RoleUID == RoleDefaults.FOUNDER_ROLE_ID)
                return (false, null, "Cannot rename Founder role");

            if (!RoleDefaults.IsValidRoleName(newName))
                return (false, null, "Role name must be 3-30 characters and alphanumeric");

            if (RoleDefaults.IsReservedName(newName))
                return (false, null, "Cannot use reserved role names");

            if (religion.IsRoleNameTaken(newName, excludeRoleUID: roleUID))
                return (false, null, "A role with this name already exists");

            role.RoleName = newName;
            _religionManager?.SaveReligion(religion);

            return (true, role, string.Empty);
        }

        public (bool success, RoleData? role, string error) ModifyRolePermissions(string religionUID, string playerUID, string roleUID, HashSet<string> permissions)
        {
            var religion = _religionManager?.GetReligion(religionUID);
            if (religion == null)
                return (false, null, "Religion not found");

            if (!religion.HasPermission(playerUID, RolePermissions.MANAGE_ROLES))
                return (false, null, "You don't have permission to manage roles");

            var role = religion.GetRole(roleUID);
            if (role == null)
                return (false, null, "Role not found");

            if (role.RoleUID == RoleDefaults.FOUNDER_ROLE_ID)
                return (false, null, "Cannot modify Founder role permissions");

            // Validate all permissions
            foreach (var perm in permissions)
            {
                if (!RolePermissions.IsValidPermission(perm))
                    return (false, null, $"Invalid permission: {perm}");
            }

            role.Permissions = new HashSet<string>(permissions);
            _religionManager?.SaveReligion(religion);

            return (true, role, string.Empty);
        }

        public (bool success, string error) AssignRole(string religionUID, string assignerUID, string targetPlayerUID, string roleUID)
        {
            var religion = _religionManager?.GetReligion(religionUID);
            if (religion == null)
                return (false, "Religion not found");

            if (!religion.IsMember(targetPlayerUID))
                return (false, "Target player is not a member of this religion");

            // Check if assigner can assign roles
            if (!religion.CanAssignRole(assignerUID, roleUID))
                return (false, "You don't have permission to assign this role");

            // Cannot demote the founder
            if (religion.GetPlayerRole(targetPlayerUID) == RoleDefaults.FOUNDER_ROLE_ID)
                return (false, "Cannot change Founder's role. Use /religion transfer instead");

            // Cannot assign Founder role
            if (roleUID == RoleDefaults.FOUNDER_ROLE_ID)
                return (false, "Cannot assign Founder role. Use /religion transfer instead");

            var role = religion.GetRole(roleUID);
            if (role == null)
                return (false, "Role not found");

            religion.MemberRoles[targetPlayerUID] = roleUID;
            _religionManager?.SaveReligion(religion);

            return (true, string.Empty);
        }

        public (bool success, string error) TransferFounder(string religionUID, string currentFounderUID, string newFounderUID)
        {
            var religion = _religionManager?.GetReligion(religionUID);
            if (religion == null)
                return (false, "Religion not found");

            // Only current founder can transfer
            if (religion.GetPlayerRole(currentFounderUID) != RoleDefaults.FOUNDER_ROLE_ID)
                return (false, "Only the founder can transfer founder status");

            if (!religion.IsMember(newFounderUID))
                return (false, "Target player is not a member of this religion");

            if (currentFounderUID == newFounderUID)
                return (false, "You are already the founder");

            // Transfer founder role
            religion.MemberRoles[newFounderUID] = RoleDefaults.FOUNDER_ROLE_ID;
            religion.MemberRoles[currentFounderUID] = RoleDefaults.MEMBER_ROLE_ID; // Demote to member

            // Update legacy FounderUID field for backwards compatibility
            religion.FounderUID = newFounderUID;

            // Update member list order (founder should be first)
            religion.MemberUIDs.Remove(newFounderUID);
            religion.MemberUIDs.Insert(0, newFounderUID);

            _religionManager?.SaveReligion(religion);

            return (true, string.Empty);
        }

        public List<RoleData> GetReligionRoles(string religionUID)
        {
            var religion = _religionManager?.GetReligion(religionUID);
            if (religion == null)
                return new List<RoleData>();

            return religion.Roles.Values
                .OrderBy(r => r.DisplayOrder)
                .ThenBy(r => r.RoleName)
                .ToList();
        }

        public RoleData? GetPlayerRole(string religionUID, string playerUID)
        {
            var religion = _religionManager?.GetReligion(religionUID);
            if (religion == null)
                return null;

            string roleUID = religion.GetPlayerRole(playerUID);
            return religion.GetRole(roleUID);
        }

        public Dictionary<string, int> GetRoleMemberCounts(string religionUID)
        {
            var religion = _religionManager?.GetReligion(religionUID);
            if (religion == null)
                return new Dictionary<string, int>();

            return religion.GetRoleMemberCounts();
        }

        public List<string> GetPlayersWithRole(string religionUID, string roleUID)
        {
            var religion = _religionManager?.GetReligion(religionUID);
            if (religion == null)
                return new List<string>();

            return religion.GetMembersWithRole(roleUID);
        }
    }
}
```

---

### Task 2.2: Update Permission Checks in ReligionCommands
**File**: `/PantheonWars/Commands/ReligionCommands.cs`
**Estimated Time**: 3 hours

Replace all `IsFounder()` checks with `HasPermission()` checks:

**Example changes**:

```csharp
// OLD
if (!religion.IsFounder(player.PlayerUID))
    return TextCommandResult.Error("Only the founder can kick members");

// NEW
if (!religion.HasPermission(player.PlayerUID, RolePermissions.KICK_MEMBERS))
    return TextCommandResult.Error("You don't have permission to kick members");
```

**Commands to update**:
- `/religion kick` - Check `KICK_MEMBERS`
- `/religion ban` - Check `BAN_PLAYERS`
- `/religion unban` - Check `BAN_PLAYERS`
- `/religion banlist` - Check `VIEW_BAN_LIST`
- `/religion description` - Check `EDIT_DESCRIPTION`
- `/religion disband` - Check `DISBAND_RELIGION`
- `/religion invite` - Check `INVITE_PLAYERS` (currently allows all members)

---

### Task 2.3: Write RoleManager Tests
**File**: `/PantheonWars.Tests/RoleManagerTests.cs`
**Estimated Time**: 4 hours

(Tests for CreateCustomRole, DeleteRole, AssignRole, TransferFounder, permission validation, etc.)

---

## Phase 3: Role Management Commands (Week 3)

### Task 3.1: Create RoleCommands
**File**: `/PantheonWars/Commands/RoleCommands.cs`
**Estimated Time**: 8 hours

Implement all role management commands listed in section 4.1 of the feature spec.

---

## Phase 4: Network Layer (Week 4)

### Task 4.1-4.6: Create Network Packets
**Files**: See "New Files to Create" section
**Estimated Time**: 2 hours per packet pair (request/response)

Implement all network packets listed in section 4.2 of the feature spec.

### Task 4.7: Register Network Handlers
**File**: `/PantheonWars/PantheonWarsSystem.cs`
**Estimated Time**: 2 hours

---

## Phase 5: GUI Implementation (Week 5-6)

### Task 5.1: Create Roles Tab
**Estimated Time**: 12 hours

Implement full Roles tab with EDA pattern (State → ViewModel → Renderer → Events).

### Task 5.2: Create Role Editor Dialog
**Estimated Time**: 8 hours

Implement permission checkbox editor with save/cancel logic.

### Task 5.3: Update Member List
**Estimated Time**: 4 hours

Add role badges and right-click context menu.

---

## Phase 6: Polish & Documentation (Week 7)

### Task 6.1: Documentation
**Estimated Time**: 4 hours

- Update README
- Write user guide
- Add command help text

### Task 6.2: Integration Testing
**Estimated Time**: 8 hours

Full end-to-end testing of all features.

---

## Total Estimated Time

- **Phase 1**: 15 hours
- **Phase 2**: 13 hours
- **Phase 3**: 8 hours
- **Phase 4**: 14 hours
- **Phase 5**: 24 hours
- **Phase 6**: 12 hours

**Total**: ~86 hours (~2 weeks for full-time development)

---

## Critical Path

1. Phase 1 must complete before Phase 2
2. Phase 2 must complete before Phase 3
3. Phase 4 can run parallel to Phase 3
4. Phase 5 requires Phases 2, 3, and 4 complete
5. Phase 6 requires all previous phases

---

## Testing Checklist

- [ ] Unit tests for RoleData
- [ ] Unit tests for RoleDefaults
- [ ] Migration tests
- [ ] Permission check tests
- [ ] Role CRUD tests
- [ ] Network packet serialization tests
- [ ] Command integration tests
- [ ] GUI interaction tests
- [ ] Full end-to-end scenario tests

---

## Rollback Strategy

If critical bugs are found after deployment:

1. **Revert flag**: Add server config `UseRoleSystem=false`
2. **Fallback logic**: Fall back to `IsFounder()` checks
3. **Data preservation**: Keep migrated data intact
4. **Re-enable**: Fix bugs and re-enable role system

---

## Version Control Strategy

Branch naming: `feature/player-roles-permissions`

Commit structure:
- `feat(roles): Add RoleData model`
- `feat(roles): Implement migration logic`
- `feat(roles): Add RoleManager system`
- `feat(roles): Update permission checks`
- `feat(roles): Add role management commands`
- `feat(roles): Implement network layer`
- `feat(roles): Add Roles GUI tab`
- `docs(roles): Add user documentation`

---

**Document Version**: 1.0
**Last Updated**: 2025-12-11
