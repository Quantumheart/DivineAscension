# Redesign Religion Roles Assignment UI

## Objective

Redesign the religion roles assignment functionality from a modal dialog pattern to a conditional view replacement
pattern, matching the civilization "View Details" implementation.

## Current Pattern vs. Target Pattern

**Current (Modal Dialog):**

- "View Members" button → sets `ShowRoleMembersDialog = true`
- `DrawRoleMembersDialog()` overlays modal (500x600px) on top of role cards
- Close button → sets `ShowRoleMembersDialog = false`

**Target (Conditional View Replacement):**

- "View Details" button → sets `ViewingRoleUID = roleUID`
- Conditional check: if `ViewingRoleUID != null`, replace entire view with detail renderer
- "Back to Roles" button → clears `ViewingRoleUID = null`

## Implementation Approach

### Phase 1: Create New State Classes

#### 1.1 Create `RoleDetailState.cs`

**Path:** `PantheonWars/GUI/State/Religion/RoleDetailState.cs`

```csharp
public class RoleDetailState : IState
{
    // Which role we're viewing (null = browse mode, not-null = detail mode)
    public string? ViewingRoleUID { get; set; }
    public string? ViewingRoleName { get; set; }

    // Scroll position for member list
    public float MemberScrollY { get; set; }

    // Role assignment dropdown state
    public string? OpenAssignRoleDropdownMemberUID { get; set; }

    // Role assignment confirmation modal state (kept as overlay)
    public bool ShowAssignRoleConfirm { get; set; }
    public string? AssignRoleConfirmMemberUID { get; set; }
    public string? AssignRoleConfirmMemberName { get; set; }
    public string? AssignRoleConfirmCurrentRoleUID { get; set; }
    public string? AssignRoleConfirmNewRoleUID { get; set; }
    public string? AssignRoleConfirmNewRoleName { get; set; }

    public void Reset() { /* clear all properties */ }
}
```

#### 1.2 Create `RolesBrowseState.cs`

**Path:** `PantheonWars/GUI/State/Religion/RolesBrowseState.cs`

```csharp
public class RolesBrowseState : IState
{
    // Scroll position for role cards list
    public float ScrollY { get; set; }

    // Role editor state (overlays browse view)
    public bool ShowRoleEditor { get; set; }
    public string? EditingRoleUID { get; set; }
    public string EditingRoleName { get; set; } = string.Empty;
    public HashSet<string> EditingPermissions { get; set; } = new();

    // Create custom role dialog (overlays browse view)
    public bool ShowCreateRoleDialog { get; set; }
    public string NewRoleName { get; set; } = string.Empty;

    // Delete role confirmation (overlays browse view)
    public bool ShowDeleteConfirm { get; set; }
    public string? DeleteRoleUID { get; set; }
    public string? DeleteRoleName { get; set; }

    public void Reset() { /* clear all properties */ }
}
```

#### 1.3 Refactor `RolesState.cs`

**Path:** `PantheonWars/GUI/State/Religion/RolesState.cs`

**Changes:**

- Remove all properties that moved to sub-states
- Add nested `BrowseState` and `DetailState` objects
- Keep only shared data (`RolesData`, `Loading`)

```csharp
public class RolesState
{
    // Shared data
    public ReligionRolesResponse? RolesData { get; set; }
    public bool Loading { get; set; }

    // Sub-states
    public RolesBrowseState BrowseState { get; } = new();
    public RoleDetailState DetailState { get; } = new();

    public void Reset()
    {
        RolesData = null;
        Loading = false;
        BrowseState.Reset();
        DetailState.Reset();
    }
}
```

### Phase 2: Create New Event Classes

#### 2.1 Create `RolesBrowseEvent.cs`

**Path:** `PantheonWars/GUI/Events/Religion/RolesBrowseEvent.cs`

Key events:

- `ViewRoleDetailsClicked(string RoleUID, string RoleName)` - Navigate to detail
- `ScrollChanged(float NewScrollY)` - Scroll tracking
- `CreateRoleOpen/Cancel/Confirm` - Role creation
- `EditRoleOpen/Cancel/Save` - Role editing
- `DeleteRoleOpen/Confirm/Cancel` - Role deletion
- `RefreshRequested` - Refresh data

#### 2.2 Create `RoleDetailEvent.cs`

**Path:** `PantheonWars/GUI/Events/Religion/RoleDetailEvent.cs`

Key events:

- `BackToRolesClicked` - Navigate back to browse
- `MemberScrollChanged(float NewScrollY)` - Scroll tracking
- `AssignRoleDropdownToggled` - Dropdown interaction
- `AssignRoleConfirmOpen/Confirm/Cancel` - Role assignment confirmation

### Phase 3: Create New ViewModels

#### 3.1 Create `ReligionRolesBrowseViewModel.cs`

**Path:** `PantheonWars/GUI/Models/Religion/Roles/ReligionRolesBrowseViewModel.cs`

Contains:

- Data properties (roles, current player, religion UID)
- Browse-specific UI state (editor, create, delete dialogs)
- Layout properties (x, y, width, height, scroll)
- Helper methods for permissions and member counts

#### 3.2 Create `ReligionRoleDetailViewModel.cs`

**Path:** `PantheonWars/GUI/Models/Religion/Roles/ReligionRoleDetailViewModel.cs`

Contains:

- Which role being viewed (`ViewingRoleUID`, `ViewingRoleName`)
- Data properties (roles data, member info)
- Detail-specific UI state (assignment dropdown, confirmation)
- Layout properties (x, y, width, height, member scroll)
- Helper methods for getting members with role, assignable roles

### Phase 4: Create New Renderers

#### 4.1 Create `ReligionRolesBrowseRenderer.cs`

**Path:** `PantheonWars/GUI/UI/Renderers/Religion/ReligionRolesBrowseRenderer.cs`

**Responsibilities:**

- Render scrollable role cards list
- Render "Create Custom Role" button
- Render create/edit/delete role dialogs (overlays)
- Change "View Members" button to emit `ViewRoleDetailsClicked`
- Handle scroll events

**Method signature:**

```csharp
public static ReligionRolesBrowseRenderResult Draw(
    ReligionRolesBrowseViewModel viewModel,
    ImDrawListPtr drawList)
```

#### 4.2 Create `ReligionRoleDetailRenderer.cs`

**Path:** `PantheonWars/GUI/UI/Renderers/Religion/ReligionRoleDetailRenderer.cs`

**Responsibilities:**

- Render "Back to Roles" button (top-left, like civilization detail)
- Render role detail header showing which role's members are displayed
- Render scrollable members list with role assignment dropdowns
- Render assign role confirmation modal overlay
- Handle member list scrolling and dropdown interactions

**Layout:**

```
[<< Back to Roles] button

Members with '[RoleName]' role

• Member Name 1        [Current Role ▼]
• Member Name 2        [Current Role ▼]
...
```

**Method signature:**

```csharp
public static ReligionRoleDetailRenderResult Draw(
    ReligionRoleDetailViewModel viewModel,
    ImDrawListPtr drawList)
```

### Phase 5: Update State Manager

#### 5.1 Refactor `DrawReligionRoles()` in `ReligionStateManager.cs`

**Path:** `PantheonWars/GUI/Managers/ReligionStateManager.cs` (line 826)

**Current implementation:** Single view with modal overlay

**New implementation:** Conditional routing based on viewing state

```csharp
public void DrawReligionRoles(float x, float y, float width, float height)
{
    // Check if viewing role details (conditional rendering pattern)
    if (!string.IsNullOrEmpty(State.RolesState.DetailState.ViewingRoleUID))
    {
        DrawRoleDetail(x, y, width, height);
        return;
    }

    // Otherwise, draw browse view
    DrawRolesBrowse(x, y, width, height);
}
```

#### 5.2 Add `DrawRolesBrowse()` method

Builds `ReligionRolesBrowseViewModel` from `BrowseState`, calls `ReligionRolesBrowseRenderer.Draw()`, processes browse
events.

#### 5.3 Add `DrawRoleDetail()` method

Builds `ReligionRoleDetailViewModel` from `DetailState`, calls `ReligionRoleDetailRenderer.Draw()`, processes detail
events.

#### 5.4 Split `ProcessRolesEvents()` into two methods

**`ProcessRolesBrowseEvents()`:**

- Handle `ViewRoleDetailsClicked` → Set `DetailState.ViewingRoleUID` and reset scroll
- Handle browse-specific events (create, edit, delete role)
- Handle scroll events for browse

**`ProcessRoleDetailEvents()`:**

- Handle `BackToRolesClicked` → Clear `DetailState.ViewingRoleUID` and reset detail state
- Handle role assignment events (dropdown toggle, confirm open/cancel/confirm)
- Handle scroll events for detail view

### Phase 6: Cleanup

**Files to delete after migration:**

- `PantheonWars/GUI/UI/Renderers/Religion/ReligionRolesRenderer.cs` (replaced)
- `PantheonWars/GUI/Models/Religion/Roles/ReligionRolesViewModel.cs` (replaced)
- `PantheonWars/GUI/Events/Religion/RolesEvent.cs` (replaced)

## Implementation Order

1. Create state classes (RoleDetailState, RolesBrowseState, refactor RolesState)
2. Create event classes (RolesBrowseEvent, RoleDetailEvent)
3. Create ViewModels (ReligionRolesBrowseViewModel, ReligionRoleDetailViewModel)
4. Create renderers (ReligionRolesBrowseRenderer, ReligionRoleDetailRenderer)
5. Update state manager (add DrawRolesBrowse, DrawRoleDetail, refactor DrawReligionRoles, split event processing)
6. Test thoroughly
7. Delete old files

## Testing Checklist

**Navigation:**

- [ ] "View Details" button navigates to detail view
- [ ] "Back to Roles" button returns to browse view
- [ ] Browse scroll position preserved
- [ ] Detail scroll position reset when navigating to new role

**Browse View:**

- [ ] Role cards display correctly
- [ ] Create/edit/delete role dialogs work
- [ ] Scrolling works

**Detail View:**

- [ ] Members with role display correctly
- [ ] Role assignment dropdowns work
- [ ] Confirmation modal works
- [ ] Only eligible members show dropdowns (not self, not founder)
- [ ] Scrolling works

**Edge Cases:**

- [ ] Viewing role with no members
- [ ] No permission to manage roles
- [ ] Network errors handled gracefully

## Critical Files

- `PantheonWars/GUI/State/Religion/RolesState.cs` - Core state refactoring
- `PantheonWars/GUI/Managers/ReligionStateManager.cs` - Conditional rendering implementation
- `PantheonWars/GUI/UI/Renderers/Religion/ReligionRolesBrowseRenderer.cs` - Browse view renderer (new)
- `PantheonWars/GUI/UI/Renderers/Religion/ReligionRoleDetailRenderer.cs` - Detail view renderer (new)
- `PantheonWars/GUI/State/Religion/RoleDetailState.cs` - Detail state management (new)

## Key Design Decisions

1. **Keep confirmation modal as overlay** - User preference; matches existing confirmation patterns
2. **Keep dropdowns in detail view** - User preference; maintains current UX
3. **Separate browse/detail renderers** - Matches civilization pattern exactly; cleaner separation of concerns
4. **Store assignment state in DetailState** - Assignment only happens in detail view; keeps state localized
5. **Preserve browse scroll, reset detail scroll** - Better UX; matches civilization pattern
