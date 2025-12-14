# Implementation Plan: Role Assignment UI for Religion System

## Overview

Add user interface to assign roles to religion members. The backend infrastructure already exists (network packets,
server handlers, permissions), but there's no UI to trigger role assignment.

## Design Decision: Enhanced Members Dialog

Extend the existing "View Members" dialog with inline role dropdowns. Users with MANAGE_ROLES permission can click a
dropdown next to each member name to change their role.

**UX Flow:**

1. User clicks "View Members" on a role card
2. Dialog shows list of members with their current roles
3. Members with MANAGE_ROLES permission see role dropdowns (enabled for eligible members)
4. Selecting a different role shows confirmation dialog
5. Confirming triggers network request and refreshes roles data

## Critical Files to Modify

### 1. Events Definition

**File:** `/home/quantumheart/RiderProjects/PantheonWars/PantheonWars/GUI/Events/Religion/RolesEvent.cs`

Add 5 new event records after line 44:

```csharp
// Role assignment
public record AssignRoleDropdownToggled(string MemberUID, bool IsOpen) : RolesEvent;
public record AssignRoleConfirmOpen(string MemberUID, string MemberName, string CurrentRoleUID, string NewRoleUID, string NewRoleName) : RolesEvent;
public record AssignRoleConfirm(string MemberUID, string NewRoleUID) : RolesEvent;
public record AssignRoleCancel : RolesEvent;
```

### 2. State Management

**File:** `/home/quantumheart/RiderProjects/PantheonWars/PantheonWars/GUI/State/Religion/RolesState.cs`

Add properties after line 39:

```csharp
public string? OpenAssignRoleDropdownMemberUID { get; set; }
public bool ShowAssignRoleConfirm { get; set; }
public string? AssignRoleConfirmMemberUID { get; set; }
public string? AssignRoleConfirmMemberName { get; set; }
public string? AssignRoleConfirmCurrentRoleUID { get; set; }
public string? AssignRoleConfirmNewRoleUID { get; set; }
public string? AssignRoleConfirmNewRoleName { get; set; }
```

Update `Reset()` method to clear these properties.

### 3. ViewModel

**File:**
`/home/quantumheart/RiderProjects/PantheonWars/PantheonWars/GUI/Models/Religion/Roles/ReligionRolesViewModel.cs`

**Add constructor parameters** and corresponding readonly properties for the new state.

**Add helper methods:**

```csharp
public bool CanAssignRoleToMember(string targetMemberUID)
{
    if (!CanManageRoles()) return false;
    if (targetMemberUID == CurrentPlayerUID) return false; // Can't change own role
    var targetRoleUID = MemberRoles.TryGetValue(targetMemberUID, out var roleUID) ? roleUID : null;
    if (targetRoleUID == RoleDefaults.FOUNDER_ROLE_ID) return false; // Can't change Founder
    return true;
}

public IReadOnlyList<RoleData> GetAssignableRoles()
{
    return Roles
        .Where(r => r.RoleUID != RoleDefaults.FOUNDER_ROLE_ID)
        .OrderBy(r => r.DisplayOrder)
        .ThenBy(r => r.RoleName)
        .ToList();
}
```

### 4. Renderer

**File:**
`/home/quantumheart/RiderProjects/PantheonWars/PantheonWars/GUI/UI/Renderers/Religion/ReligionRolesRenderer.cs`

**Rewrite `DrawRoleMembersDialog` (lines 427-484):**

- Increase dialog dimensions (500x600 instead of 400x500)
- For each member, show:
    - Member name on left
    - Role dropdown on right (if can manage roles and can assign to this member)
    - Static role text (if no permission or cannot assign)
- Use `Dropdown.DrawButton` to show dropdown button
- When dropdown is open, use `Dropdown.DrawMenuVisual` and `DrawMenuAndHandleInteraction`
- On role selection, emit `AssignRoleConfirmOpen` event
- Close dropdown when dialog closes

**Add new method `DrawAssignRoleConfirmation`:**

- Render confirmation dialog using `ConfirmOverlay.Draw`
- Message: "Change {Name}'s role from '{OldRole}' to '{NewRole}'?"
- On confirm, emit `AssignRoleConfirm` event
- On cancel, emit `AssignRoleCancel` event

**Update main `Draw` method (line 116):**

- Add call to draw confirmation dialog if `ShowAssignRoleConfirm` is true

### 5. Event Processing

**File:** `/home/quantumheart/RiderProjects/PantheonWars/PantheonWars/GUI/Managers/ReligionStateManager.cs`

**In `ProcessRolesEvents` (after line 964), add handlers:**

```csharp
case RolesEvent.AssignRoleDropdownToggled e:
    // Close other dropdowns, toggle this one
    State.RolesState.OpenAssignRoleDropdownMemberUID = e.IsOpen ? e.MemberUID : null;
    break;

case RolesEvent.AssignRoleConfirmOpen e:
    State.RolesState.ShowAssignRoleConfirm = true;
    State.RolesState.AssignRoleConfirmMemberUID = e.MemberUID;
    State.RolesState.AssignRoleConfirmMemberName = e.MemberName;
    State.RolesState.AssignRoleConfirmCurrentRoleUID = e.CurrentRoleUID;
    State.RolesState.AssignRoleConfirmNewRoleUID = e.NewRoleUID;
    State.RolesState.AssignRoleConfirmNewRoleName = e.NewRoleName;
    State.RolesState.OpenAssignRoleDropdownMemberUID = null; // Close dropdown
    break;

case RolesEvent.AssignRoleConfirm e:
    State.RolesState.ShowAssignRoleConfirm = false;
    _uiService.RequestAssignRole(CurrentReligionUID ?? string.Empty, e.MemberUID, e.NewRoleUID);
    _soundManager.PlayClick();
    // Clear confirmation state
    State.RolesState.AssignRoleConfirmMemberUID = null;
    State.RolesState.AssignRoleConfirmMemberName = null;
    State.RolesState.AssignRoleConfirmCurrentRoleUID = null;
    State.RolesState.AssignRoleConfirmNewRoleUID = null;
    State.RolesState.AssignRoleConfirmNewRoleName = null;
    break;

case RolesEvent.AssignRoleCancel:
    State.RolesState.ShowAssignRoleConfirm = false;
    // Clear confirmation state
    break;
```

**Update ViewModel creation in `DrawReligionRoles` (line 826):**

- Add new state properties to ReligionRolesViewModel constructor call

## Implementation Order

1. **Events** (RolesEvent.cs) - No dependencies
2. **State** (RolesState.cs) - Depends on events for types
3. **ViewModel** (ReligionRolesViewModel.cs) - Add parameters and helper methods
4. **Renderer** (ReligionRolesRenderer.cs) - Rewrite members dialog, add confirmation dialog
5. **Event Processing** (ReligionStateManager.cs) - Wire up events to state and network calls

Build after each phase to catch errors early.

## Permission & Edge Cases

**Handled by `CanAssignRoleToMember()`:**

- ✅ Only users with MANAGE_ROLES permission see dropdowns
- ✅ Cannot change your own role
- ✅ Cannot change Founder's role
- ✅ Cannot assign Founder role (filtered out by `GetAssignableRoles()`)

**Server-side validation** (already exists):

- Server validates MANAGE_ROLES permission
- Prevents invalid role assignments
- Returns error if validation fails

**UI Behavior:**

- Only one dropdown open at a time
- Clicking outside closes dropdown
- Dialog close also closes any open dropdown
- Successful assignment refreshes roles list (existing handler)

## UI Specifications

**Dialog Dimensions:**

- Width: 500px (increased from 400px for dropdown space)
- Height: 600px (increased from 500px for more members)

**Member Row Layout:**

```
• Player Name           [Current Role ▼]  (if can assign)
• Player Name           Current Role (Your role)  (if cannot assign - own)
• Founder Name          Founder (Founder)  (if cannot assign - system role)
```

**Dropdown:**

- Width: 180px
- Height: 28px
- Shows all assignable roles (excludes Founder)
- Dropdown menu drawn after main content for proper z-ordering

**Confirmation Dialog:**

- Title: "Assign Role"
- Message: "Change [Name]'s role from '[OldRole]' to '[NewRole]'?"
- Buttons: "Cancel" | "Assign"

## Backend Infrastructure (Already Exists)

No changes needed to:

- ✅ Network packets: `AssignRoleRequest`/`AssignRoleResponse`
- ✅ Server handler: `OnAssignRoleRequest()` in ReligionNetworkHandler.cs:480
- ✅ Client service: `RequestAssignRole()` in UiService.cs
- ✅ Response handler: `OnRoleAssigned()` in GuiDialogHandlers.cs:250 (refreshes roles)
- ✅ Permission system: `RoleManager.AssignRole()` validates permissions

## Testing Checklist

- [ ] Users without MANAGE_ROLES don't see dropdowns
- [ ] Users with MANAGE_ROLES see dropdowns for eligible members
- [ ] Founder role never appears in dropdown options
- [ ] Cannot see dropdown for own member entry
- [ ] Only one dropdown open at a time
- [ ] Selecting role shows confirmation
- [ ] Confirming triggers network request
- [ ] Successful assignment refreshes member list
- [ ] Dialog closes properly with open dropdown

## Success Criteria

Implementation complete when:

1. Members dialog shows role dropdowns for users with MANAGE_ROLES permission
2. Dropdowns display assignable roles (excluding Founder)
3. Selecting a new role shows confirmation dialog
4. Confirming sends network request and refreshes UI
5. Permission and edge case validation working
6. No build errors or warnings