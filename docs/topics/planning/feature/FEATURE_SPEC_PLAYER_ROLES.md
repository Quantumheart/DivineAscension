# Feature Specification: Player Roles & Permissions System

## Overview
This document outlines the design and implementation plan for a flexible role-based permission system for religions in PantheonWars. The system will replace the current binary Founder/Member model with a granular, customizable role hierarchy.

---

## 1. Feature Goals

### Primary Goals
- **Granular Permissions**: Enable fine-grained control over religion management capabilities
- **Flexibility**: Allow religion founders to create custom roles beyond defaults
- **Delegation**: Enable founders to delegate specific responsibilities without full founder access
- **User Experience**: Provide intuitive role management through both commands and GUI
- **Backwards Compatibility**: Seamlessly migrate existing religions to the new system

### Non-Goals
- Player-level permission overrides (permissions are role-based only)
- Role hierarchies or inheritance (roles are flat, permissions are boolean)
- Cross-religion role templates or sharing

---

## 2. Role System Design

### 2.1 Default Roles

Three pre-defined roles will be created automatically for every religion:

#### **Founder Role**
- **Automatically Assigned**: Religion creator
- **Transferable**: Can be transferred to another member
- **Unique**: Only one Founder per religion
- **Cannot Be Deleted**: System-protected role
- **Default Permissions**: All permissions enabled

#### **Officer Role**
- **Purpose**: Trusted members with moderation capabilities
- **Assignable**: Founder can assign this role to members
- **Multiple Allowed**: Multiple officers per religion
- **Can Be Deleted**: Yes (if no members have this role)
- **Default Permissions**:
  - ✅ Invite players
  - ✅ Kick members (except Founder and Officers)
  - ✅ Manage invitations
  - ✅ View member list
  - ✅ Edit religion description
  - ❌ Ban/unban players
  - ❌ Manage roles
  - ❌ Disband religion
  - ❌ Transfer founder status

#### **Member Role**
- **Purpose**: Standard members with basic access
- **Default Assignment**: All new members receive this role
- **Cannot Be Deleted**: System-protected role (fallback role)
- **Default Permissions**:
  - ✅ View member list
  - ✅ View religion info
  - ✅ Leave religion
  - ❌ Invite players
  - ❌ Kick members
  - ❌ Manage bans
  - ❌ Edit description
  - ❌ Manage roles
  - ❌ Disband religion

### 2.2 Permission Types

The following permissions will be implemented:

| Permission ID | Name | Description |
|--------------|------|-------------|
| `INVITE_PLAYERS` | Invite Players | Can send religion invitations |
| `MANAGE_INVITATIONS` | Manage Invitations | Can cancel pending invitations |
| `KICK_MEMBERS` | Kick Members | Can remove members from religion |
| `BAN_PLAYERS` | Ban Players | Can ban/unban players |
| `EDIT_DESCRIPTION` | Edit Description | Can modify religion description |
| `MANAGE_ROLES` | Manage Roles | Can create/edit/delete custom roles and assign roles to members |
| `TRANSFER_FOUNDER` | Transfer Founder | Can transfer founder status to another member |
| `DISBAND_RELIGION` | Disband Religion | Can permanently disband the religion |
| `CHANGE_PRIVACY` | Change Privacy | Can toggle religion between public/private |
| `VIEW_MEMBERS` | View Members | Can see the member list |
| `VIEW_BAN_LIST` | View Ban List | Can see banned players list |

### 2.3 Role Constraints

1. **Founder Role**:
   - Always has all permissions (cannot be modified)
   - Cannot be deleted
   - Must exist exactly once per religion
   - Can only be transferred, not reassigned

2. **Member Role**:
   - Cannot be deleted (fallback role)
   - Can have permissions customized
   - Automatically assigned to new joiners

3. **Custom Roles**:
   - Maximum 10 custom roles per religion
   - Role names must be unique within a religion
   - Role names: 3-30 characters, alphanumeric + spaces
   - Cannot use reserved names: "Founder", "Officer", "Member"
   - Can be deleted only if no members have that role

4. **Role Assignment**:
   - Each member has exactly one role
   - Only players with `MANAGE_ROLES` permission can assign roles
   - Cannot demote the Founder to another role
   - Cannot assign the Founder role (must use transfer)

---

## 3. Data Model

### 3.1 New Classes

#### **RoleData.cs**
```csharp
[ProtoContract]
public class RoleData
{
    [ProtoMember(1)]
    public string RoleUID { get; set; } // GUID for custom roles, fixed IDs for defaults

    [ProtoMember(2)]
    public string RoleName { get; set; } // Display name

    [ProtoMember(3)]
    public bool IsDefault { get; set; } // True for Founder/Officer/Member

    [ProtoMember(4)]
    public bool IsProtected { get; set; } // True for Founder and Member (cannot delete)

    [ProtoMember(5)]
    public HashSet<string> Permissions { get; set; } // Set of permission IDs

    [ProtoMember(6)]
    public int DisplayOrder { get; set; } // For sorting in UI (0 = highest)

    [ProtoMember(7)]
    public DateTime CreatedDate { get; set; }
}
```

#### **RolePermissions.cs** (Static Constants)
```csharp
public static class RolePermissions
{
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

    public static readonly HashSet<string> AllPermissions = new()
    {
        INVITE_PLAYERS, MANAGE_INVITATIONS, KICK_MEMBERS, BAN_PLAYERS,
        EDIT_DESCRIPTION, MANAGE_ROLES, TRANSFER_FOUNDER, DISBAND_RELIGION,
        CHANGE_PRIVACY, VIEW_MEMBERS, VIEW_BAN_LIST
    };
}
```

#### **RoleDefaults.cs** (Static Factory)
```csharp
public static class RoleDefaults
{
    public const string FOUNDER_ROLE_ID = "role_founder";
    public const string OFFICER_ROLE_ID = "role_officer";
    public const string MEMBER_ROLE_ID = "role_member";

    public static RoleData CreateFounderRole() { ... }
    public static RoleData CreateOfficerRole() { ... }
    public static RoleData CreateMemberRole() { ... }
}
```

### 3.2 Modified Classes

#### **ReligionData.cs Changes**
```csharp
[ProtoContract]
public class ReligionData
{
    // ... existing fields ...

    // NEW FIELDS
    [ProtoMember(15)] // Roles defined in this religion
    public Dictionary<string, RoleData> Roles { get; set; } = new();

    [ProtoMember(16)] // Maps PlayerUID → RoleUID
    public Dictionary<string, string> MemberRoles { get; set; } = new();

    // DEPRECATED (keep for migration)
    [ProtoMember(2)]
    public string FounderUID { get; set; } // Will migrate to MemberRoles

    // NEW METHODS
    public string GetPlayerRole(string playerUID);
    public bool HasPermission(string playerUID, string permission);
    public RoleData GetRole(string roleUID);
    public bool CanAssignRole(string assignerUID, string targetRoleUID);
    public List<RoleData> GetAssignableRoles(string playerUID);
}
```

---

## 4. API Design

### 4.1 Command Interface

#### **Role Management Commands**

```bash
# View all roles in your religion
/religion roles

# View members with a specific role
/religion role members <rolename>

# Create a custom role
/religion role create <name>

# Delete a custom role (if unused)
/religion role delete <name>

# Rename a role
/religion role rename <oldname> <newname>

# Assign a role to a member
/religion role assign <playername> <rolename>

# Grant a permission to a role
/religion role grant <rolename> <permission>

# Revoke a permission from a role
/religion role revoke <rolename> <permission>

# View permissions for a role
/religion role permissions <rolename>

# Transfer founder status
/religion transfer <playername>
```

#### **Updated Existing Commands**
- `/religion members` - Now shows role badges next to names
- `/religion info` - Shows role distribution statistics
- `/religion kick <player>` - Now checks `KICK_MEMBERS` permission

### 4.2 Network Packets

#### **New Packets**

```csharp
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
    [ProtoMember(2)] public List<RoleData> Roles;
    [ProtoMember(3)] public Dictionary<string, string> MemberRoles; // UID → RoleUID
    [ProtoMember(4)] public string ErrorMessage;
}

// Create custom role
[ProtoContract]
public class CreateRoleRequest
{
    [ProtoMember(1)] public string ReligionUID;
    [ProtoMember(2)] public string RoleName;
}

[ProtoContract]
public class CreateRoleResponse
{
    [ProtoMember(1)] public bool Success;
    [ProtoMember(2)] public RoleData CreatedRole;
    [ProtoMember(3)] public string ErrorMessage;
}

// Modify role permissions
[ProtoContract]
public class ModifyRolePermissionsRequest
{
    [ProtoMember(1)] public string ReligionUID;
    [ProtoMember(2)] public string RoleUID;
    [ProtoMember(3)] public HashSet<string> Permissions;
}

[ProtoContract]
public class ModifyRolePermissionsResponse
{
    [ProtoMember(1)] public bool Success;
    [ProtoMember(2)] public RoleData UpdatedRole;
    [ProtoMember(3)] public string ErrorMessage;
}

// Assign role to member
[ProtoContract]
public class AssignRoleRequest
{
    [ProtoMember(1)] public string ReligionUID;
    [ProtoMember(2)] public string TargetPlayerUID;
    [ProtoMember(3)] public string RoleUID;
}

[ProtoContract]
public class AssignRoleResponse
{
    [ProtoMember(1)] public bool Success;
    [ProtoMember(2)] public string ErrorMessage;
}

// Delete custom role
[ProtoContract]
public class DeleteRoleRequest
{
    [ProtoMember(1)] public string ReligionUID;
    [ProtoMember(2)] public string RoleUID;
}

[ProtoContract]
public class DeleteRoleResponse
{
    [ProtoMember(1)] public bool Success;
    [ProtoMember(2)] public string ErrorMessage;
}

// Transfer founder
[ProtoContract]
public class TransferFounderRequest
{
    [ProtoMember(1)] public string ReligionUID;
    [ProtoMember(2)] public string NewFounderUID;
}

[ProtoContract]
public class TransferFounderResponse
{
    [ProtoMember(1)] public bool Success;
    [ProtoMember(2)] public string ErrorMessage;
}
```

---

## 5. GUI Design

### 5.1 New "Roles" Tab

Add a new tab to `ReligionDialog` between "Info" and "Activity":

**Layout Structure**:
```
┌─────────────────────────────────────────────────┐
│ [Roles]                                         │
├─────────────────────────────────────────────────┤
│ ┌─ Founder ─────────────────┐ [3 members]      │
│ │ All permissions            │                  │
│ │ [View Members]             │                  │
│ └────────────────────────────┘                  │
│                                                 │
│ ┌─ Officer ─────────────────┐ [5 members]      │
│ │ ✓ Invite Players           │                  │
│ │ ✓ Kick Members             │                  │
│ │ ✓ Edit Description         │                  │
│ │ [Edit Permissions]         │                  │
│ │ [View Members]             │                  │
│ └────────────────────────────┘                  │
│                                                 │
│ ┌─ Member ──────────────────┐ [12 members]     │
│ │ ✓ View Members             │                  │
│ │ [Edit Permissions]         │                  │
│ │ [View Members]             │                  │
│ └────────────────────────────┘                  │
│                                                 │
│ [+ Create Custom Role]                          │
│                                                 │
│ ┌─ Elite Guard ─────────────┐ [2 members]      │
│ │ ✓ Ban Players              │                  │
│ │ ✓ View Ban List            │                  │
│ │ [Edit] [Delete] [Members]  │                  │
│ └────────────────────────────┘                  │
└─────────────────────────────────────────────────┘
```

### 5.2 Role Permission Editor Dialog

When clicking "Edit Permissions" on a role:

```
┌───────────────────────────────────────┐
│ Edit Role: Officer                    │
├───────────────────────────────────────┤
│ Role Name: [Officer________]          │
│                                       │
│ Permissions:                          │
│ ☑ Invite Players                      │
│ ☑ Manage Invitations                  │
│ ☑ Kick Members                        │
│ ☐ Ban Players                         │
│ ☑ Edit Description                    │
│ ☐ Manage Roles                        │
│ ☐ Transfer Founder                    │
│ ☐ Disband Religion                    │
│ ☑ Change Privacy                      │
│ ☑ View Members                        │
│ ☐ View Ban List                       │
│                                       │
│         [Cancel]  [Save Changes]      │
└───────────────────────────────────────┘
```

### 5.3 Member List Enhancement

Update "Info" tab member list to show role badges:

```
Members (20):
┌────────────────────────────────────┐
│ 👑 PlayerName1      [Founder]      │
│ ⭐ PlayerName2      [Officer]      │
│ ⭐ PlayerName3      [Officer]      │
│ 🛡️  PlayerName4      [Elite Guard] │
│    PlayerName5      [Member]       │
│    PlayerName6      [Member]       │
│ [Right-click for options]          │
└────────────────────────────────────┘
```

**Right-click context menu** (if player has `MANAGE_ROLES`):
- Assign Role →
  - Founder (if has TRANSFER_FOUNDER)
  - Officer
  - Member
  - Elite Guard
  - ...

---

## 6. Migration Strategy

### 6.1 Data Migration

**Migration Steps** (executed in `ReligionManager.Initialize()`):

1. **Detect Legacy Data**:
   - Check if `ReligionData.Roles` is null or empty
   - Check if `ReligionData.MemberRoles` is null or empty

2. **Initialize Default Roles**:
   ```csharp
   if (religion.Roles == null || religion.Roles.Count == 0)
   {
       religion.Roles = new Dictionary<string, RoleData>
       {
           [RoleDefaults.FOUNDER_ROLE_ID] = RoleDefaults.CreateFounderRole(),
           [RoleDefaults.OFFICER_ROLE_ID] = RoleDefaults.CreateOfficerRole(),
           [RoleDefaults.MEMBER_ROLE_ID] = RoleDefaults.CreateMemberRole()
       };
   }
   ```

3. **Migrate Founder**:
   ```csharp
   if (religion.MemberRoles == null || religion.MemberRoles.Count == 0)
   {
       religion.MemberRoles = new Dictionary<string, string>();

       // Assign Founder role to founder
       if (!string.IsNullOrEmpty(religion.FounderUID))
       {
           religion.MemberRoles[religion.FounderUID] = RoleDefaults.FOUNDER_ROLE_ID;
       }

       // Assign Member role to all other members
       foreach (string memberUID in religion.MemberUIDs)
       {
           if (memberUID != religion.FounderUID)
           {
               religion.MemberRoles[memberUID] = RoleDefaults.MEMBER_ROLE_ID;
           }
       }
   }
   ```

4. **Save Migrated Data**:
   - Immediately persist updated `ReligionData` to disk
   - Log migration for debugging: `ModLog.Log($"Migrated religion {religion.ReligionName} to role system")`

5. **Keep Legacy Fields**:
   - Keep `FounderUID` field for backwards compatibility
   - Update it when founder transfers occur
   - Use as fallback if `MemberRoles` check fails

### 6.2 Code Migration

**Phase 1: Permission Infrastructure**
- Add new data classes
- Implement migration logic
- Add helper methods to `ReligionData`

**Phase 2: Manager Updates**
- Update `ReligionManager` to use permission checks
- Add role management methods
- Replace all `IsFounder()` checks with `HasPermission()`

**Phase 3: Command Updates**
- Update existing commands to use new permission system
- Add new role management commands
- Update help text

**Phase 4: Network Layer**
- Add new packet types
- Add network handlers
- Update existing packets to include role data

**Phase 5: GUI Updates**
- Add Roles tab to ReligionDialog
- Add role editor dialog
- Update member list with role badges
- Add role assignment context menus

### 6.3 Testing Strategy

1. **Unit Tests**:
   - Role creation and validation
   - Permission checking logic
   - Migration logic
   - Role assignment constraints

2. **Integration Tests**:
   - Full role lifecycle (create → assign → modify → delete)
   - Permission enforcement across all commands
   - Network packet serialization
   - Multi-player scenarios

3. **Migration Tests**:
   - Legacy data conversion
   - Founder preservation
   - Member role assignment
   - Empty religion handling

4. **Manual Testing Checklist**:
   - [ ] Create new religion (verify default roles)
   - [ ] Migrate existing religion (verify founder + members)
   - [ ] Create custom role
   - [ ] Assign custom role to member
   - [ ] Test each permission individually
   - [ ] Delete unused custom role
   - [ ] Attempt to delete role with members (should fail)
   - [ ] Transfer founder status
   - [ ] Test permission checks in commands
   - [ ] Verify GUI displays roles correctly
   - [ ] Test role editor dialog
   - [ ] Test member right-click menu

---

## 7. Implementation Phases

### Phase 1: Foundation (Week 1)
**Goal**: Core data structures and migration

**Tasks**:
1. Create `RoleData.cs`
2. Create `RolePermissions.cs` constants
3. Create `RoleDefaults.cs` factory
4. Modify `ReligionData.cs`:
   - Add `Roles` dictionary
   - Add `MemberRoles` dictionary
   - Add helper methods
5. Implement migration logic in `ReligionManager`
6. Write unit tests for data model
7. Test migration with existing save data

**Deliverables**:
- All new data classes
- Migration working in dev environment
- Unit tests passing

---

### Phase 2: Permission System (Week 2)
**Goal**: Replace permission checks throughout codebase

**Tasks**:
1. Add permission check methods to `ReligionData`:
   - `HasPermission(playerUID, permission)`
   - `GetPlayerRole(playerUID)`
   - `CanAssignRole(assignerUID, targetRoleUID)`
2. Update `ReligionManager` methods:
   - Add role CRUD operations
   - Replace `IsFounder()` checks with `HasPermission()`
3. Create `RoleManager.cs` system
4. Write integration tests
5. Update existing commands to use new permissions

**Deliverables**:
- Permission enforcement working
- All existing functionality preserved
- Integration tests passing

---

### Phase 3: Role Management (Week 3)
**Goal**: Add role creation and assignment

**Tasks**:
1. Implement role management in `RoleManager`:
   - `CreateCustomRole()`
   - `DeleteRole()`
   - `ModifyRolePermissions()`
   - `AssignRole()`
   - `TransferFounder()`
2. Add role management commands:
   - `/religion roles`
   - `/religion role create`
   - `/religion role delete`
   - `/religion role assign`
   - `/religion role grant/revoke`
   - `/religion transfer`
3. Add validation and error handling
4. Write command tests

**Deliverables**:
- Full role management via commands
- Validation working correctly
- Command tests passing

---

### Phase 4: Network Layer (Week 4)
**Goal**: Enable client-server role communication

**Tasks**:
1. Create new network packets (see section 4.2)
2. Register network handlers
3. Implement packet handlers in `ReligionManager`
4. Update existing packets to include role data:
   - `ReligionListResponsePacket` - add role counts
   - `PlayerReligionInfoResponsePacket` - add member roles
5. Test packet serialization
6. Test network sync

**Deliverables**:
- All packets working
- Client-server sync functional
- Serialization tests passing

---

### Phase 5: GUI Implementation (Week 5-6)
**Goal**: Visual role management interface

**Tasks**:
1. Create Roles tab:
   - `ReligionRolesViewModel.cs`
   - `ReligionRolesRenderResult.cs`
   - `ReligionRolesRenderer.cs`
   - `ReligionRolesTabState.cs`
2. Create role editor dialog:
   - `RoleEditorViewModel.cs`
   - `RoleEditorRenderer.cs`
3. Update member list renderer:
   - Add role badges
   - Add right-click context menu
4. Implement event handlers in `ReligionStateManager`
5. Create UI models for role data
6. Manual UI testing

**Deliverables**:
- Roles tab functional
- Role editor working
- Member list showing roles
- All UI interactions working

---

### Phase 6: Polish & Documentation (Week 7)
**Goal**: Finalize feature for release

**Tasks**:
1. Write user documentation
2. Add tooltips and help text
3. Performance optimization
4. Code review and refactoring
5. Final integration testing
6. Update changelog
7. Create tutorial/guide

**Deliverables**:
- Feature complete and polished
- Documentation ready
- All tests passing
- Ready for release

---

## 8. Success Criteria

### Functional Requirements
- ✅ Three default roles (Founder, Officer, Member) exist in all religions
- ✅ Founders can create up to 10 custom roles
- ✅ Founders can modify permissions for any role except Founder
- ✅ Founders can assign roles to members
- ✅ Founders can transfer founder status to another member
- ✅ All existing religion functionality preserved
- ✅ Legacy religions migrated automatically
- ✅ Permission checks prevent unauthorized actions

### Technical Requirements
- ✅ Data serializes/deserializes correctly with ProtoBuf
- ✅ No performance degradation (permission checks < 1ms)
- ✅ Network packets under 10KB per request
- ✅ Migration completes in < 100ms per religion
- ✅ All unit tests pass (90%+ code coverage)
- ✅ All integration tests pass
- ✅ No memory leaks in role management

### User Experience Requirements
- ✅ Role management accessible via both commands and GUI
- ✅ Clear error messages for permission denied
- ✅ Role badges visible in member lists
- ✅ Role editor intuitive and responsive
- ✅ No breaking changes to existing workflows
- ✅ Help documentation complete

---

## 9. Future Enhancements

**Not included in initial release, but possible future additions**:

1. **Role Templates**:
   - Pre-defined role templates (e.g., "Moderator", "Recruiter")
   - Share role configurations between religions

2. **Role Colors**:
   - Custom colors for role badges in UI
   - Color-coded member lists

3. **Role Limits**:
   - Maximum members per role
   - Minimum officers required

4. **Permission Conditions**:
   - Time-based permissions (e.g., officer for 7 days)
   - Prestige-based permissions (e.g., only Renowned+ can ban)

5. **Audit Logs**:
   - Track role changes
   - Track permission usage
   - Display in Activity tab

6. **Role Requests**:
   - Members can request role promotions
   - Officers can approve/deny requests

7. **Role Progression**:
   - Auto-promote based on favor/prestige
   - Achievement-based role unlocks

---

## 10. Open Questions

1. **Should officers be able to kick other officers?**
   - Recommendation: No, to prevent power struggles
   - Implementation: Add rank-based kick prevention

2. **Should Member role permissions be editable?**
   - Recommendation: Yes, for maximum flexibility
   - Implementation: Allow editing but warn about security implications

3. **What happens to custom roles when founder leaves?**
   - Recommendation: Transfer to new founder, preserve custom roles
   - Implementation: Keep all roles intact during founder transfer

4. **Should there be a default officer assignment?**
   - Recommendation: No, manual assignment only
   - Implementation: Founder explicitly assigns Officer role

5. **Can multiple people have TRANSFER_FOUNDER permission?**
   - Recommendation: Yes, but only founder should have it by default
   - Implementation: Allow granting to officers if desired

6. **Should role names be case-sensitive?**
   - Recommendation: Case-insensitive matching, preserve display case
   - Implementation: Use `StringComparison.OrdinalIgnoreCase`

---

## 11. Appendix

### A. Example Role Configurations

**PvP-Focused Religion**:
- Founder: All permissions
- Warlord: Ban, Kick, View Ban List
- Soldier: Invite Players
- Recruit: View Members only

**Social Religion**:
- Founder: All permissions
- Moderator: Kick, Edit Description, Manage Invitations
- Recruiter: Invite Players, View Members
- Member: View Members

**Strict Hierarchy**:
- Founder: All permissions
- Member: View Members only (no invites)

### B. Permission Matrix

| Permission | Founder | Officer (Default) | Member (Default) |
|-----------|---------|-------------------|------------------|
| Invite Players | ✅ | ✅ | ❌ |
| Manage Invitations | ✅ | ✅ | ❌ |
| Kick Members | ✅ | ✅ | ❌ |
| Ban Players | ✅ | ❌ | ❌ |
| Edit Description | ✅ | ✅ | ❌ |
| Manage Roles | ✅ | ❌ | ❌ |
| Transfer Founder | ✅ | ❌ | ❌ |
| Disband Religion | ✅ | ❌ | ❌ |
| Change Privacy | ✅ | ❌ | ❌ |
| View Members | ✅ | ✅ | ✅ |
| View Ban List | ✅ | ❌ | ❌ |

### C. Error Messages

```csharp
public static class RoleErrorMessages
{
    public const string PERMISSION_DENIED = "You don't have permission to perform this action.";
    public const string INVALID_ROLE_NAME = "Role name must be 3-30 characters and alphanumeric.";
    public const string ROLE_NAME_TAKEN = "A role with this name already exists.";
    public const string MAX_ROLES_REACHED = "Maximum of 10 custom roles allowed.";
    public const string CANNOT_DELETE_PROTECTED = "Cannot delete system roles (Founder, Member).";
    public const string ROLE_IN_USE = "Cannot delete role that has members assigned.";
    public const string INVALID_PERMISSION = "Invalid permission ID.";
    public const string CANNOT_MODIFY_FOUNDER = "Cannot modify Founder role permissions.";
    public const string CANNOT_DEMOTE_FOUNDER = "Cannot change Founder's role. Use /religion transfer instead.";
    public const string NOT_A_MEMBER = "Target player is not a member of this religion.";
    public const string ROLE_NOT_FOUND = "Role not found.";
}
```

---

## Document Version
- **Version**: 1.0
- **Date**: 2025-12-11
- **Author**: Claude (AI Assistant)
- **Status**: Draft - Pending Review
