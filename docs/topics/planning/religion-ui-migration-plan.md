# Religion UI Migration Plan

> **Note**: This plan will be saved to `docs/topics/planning/religion-ui-migration-plan.md` for documentation purposes once approved.

## Executive Summary

This plan outlines the migration of religion features from old GuiDialog-based UIs and existing overlays into an integrated sub-tab structure within the BlessingDialog's "Manage Religion" tab (Tab 1). Following the successful civilization UI migration pattern, this will consolidate 7 old files (~1,297 lines) and 4 overlay files (~900 lines) into a focused sub-tab architecture (~850 lines of new renderers), resulting in a net code reduction of ~350 lines and improved user experience.

## User Decisions

Based on user input, this plan implements:
- **Architecture**: Full sub-tabs (matching civilization pattern exactly)
- **Members Tab**: Full management capabilities (kick/ban buttons in member list)
- **Activity Feed**: Placeholder implementation (v1 - "coming soon" message)
- **Bonuses Display**: All progression bonuses (prestige + civilization + any other systems)

## Current State Analysis

### Files to Migrate/Delete

**Old GuiDialog Files (7 files, ~1,297 lines):**
1. `CreateReligionDialog.cs` (175 lines) - Replaced by CreateReligionRenderer sub-tab
2. `ReligionManagementDialog.cs` (563 lines) - Split across multiple sub-tabs
3. `EditDescriptionDialog.cs` (137 lines) - Integrated into MyReligionRenderer
4. `InvitePlayerDialog.cs` (131 lines) - Integrated into MyReligionRenderer
5. `BanPlayerDialog.cs` (179 lines) - Integrated into MyReligionRenderer with confirmation
6. `FavorHudElement.cs` (112 lines) - Already deprecated, replaced by ReligionHeaderRenderer
7. **KEEP**: `DeitySelectionDialog.cs` - Still used for initial deity selection on first join

**Existing Overlay Files (4 files, ~900 lines) - TO BE DEPRECATED:**
1. `ReligionBrowserOverlay.cs` (234 lines) - Functionality moves to ReligionBrowseRenderer
2. `ReligionManagementOverlay.cs` (372 lines) - Functionality moves to MyReligionRenderer
3. `CreateReligionOverlay.cs` (~150 lines) - Functionality moves to ReligionCreateRenderer
4. `LeaveReligionConfirmOverlay.cs` (~144 lines) - Becomes inline confirmation in renderers

### Existing Components to Leverage

**Already Working (Keep):**
- `ReligionHeaderRenderer.cs` - Top banner with favor/prestige bars, action buttons
- `ReligionListRenderer.cs` - Component for rendering scrollable religion lists
- `MemberListRenderer.cs` - Component for rendering member lists (used in overlays)
- `BanListRenderer.cs` - Component for rendering banned players list
- Shared components: `ButtonRenderer`, `TextInput`, `ScrollableList`, `TabControl`, `Dropdown`, `ConfirmOverlay`

## Proposed Architecture

### Tab Structure

**Main Tab 1: "Manage Religion"**
- Sub-tab 0: **Browse** - Browse and join religions (deity filter, religion list)
- Sub-tab 1: **My Religion** - Manage your religion (members, banned, invite, description, disband)
- Sub-tab 2: **Activity** - Activity feed (placeholder: "Activity feed coming soon!")
- Sub-tab 3: **Bonuses** - Progression bonuses display (prestige + civilization + other)
- Sub-tab 4: **Create** - Create new religion form (name, deity, public/private)

### State Management

**New File: `PantheonWars/GUI/State/ReligionTabState.cs`** (~120 lines)

```csharp
public class ReligionTabState
{
    // Tab navigation
    public int CurrentSubTab { get; set; } // 0=Browse, 1=MyReligion, 2=Activity, 3=Bonuses, 4=Create

    // Browse tab state
    public string DeityFilter { get; set; } = string.Empty;
    public List<ReligionListResponsePacket.ReligionInfo> AllReligions { get; set; } = new();
    public float BrowseScrollY { get; set; } = 0f;
    public bool IsBrowseLoading { get; set; } = false;
    public string? SelectedReligionUID { get; set; }

    // My Religion tab state
    public PlayerReligionInfoResponsePacket? MyReligionInfo { get; set; }
    public float MemberScrollY { get; set; } = 0f;
    public float BanListScrollY { get; set; } = 0f;
    public string InvitePlayerName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsMyReligionLoading { get; set; } = false;
    public bool ShowDisbandConfirm { get; set; } = false;
    public string? KickConfirmPlayerUID { get; set; }
    public string? BanConfirmPlayerUID { get; set; }

    // Create tab state
    public string CreateReligionName { get; set; } = string.Empty;
    public string CreateDeity { get; set; } = "Khoras";
    public bool CreateIsPublic { get; set; } = true;

    // Activity tab state (placeholder)
    public List<string> ActivityLog { get; set; } = new(); // Future: activity events
    public float ActivityScrollY { get; set; } = 0f;

    // Bonuses tab state
    public float BonusScrollY { get; set; } = 0f;

    // Error handling
    public string? LastActionError { get; set; }
    public string? BrowseError { get; set; }
    public string? MyReligionError { get; set; }

    public void Reset() { /* Clear all state */ }
}
```

**Integration into BlessingDialogManager.cs:**

```csharp
public class BlessingDialogManager
{
    public ReligionTabState ReligionState { get; } = new();

    // Action methods
    public void RequestReligionList(string deityFilter = "") { /* ... */ }
    public void RequestPlayerReligionInfo() { /* ... */ }
    public void RequestReligionAction(string action, string religionUID = "", string targetId = "", string data = "") { /* ... */ }

    // Update methods called by event handlers
    public void UpdateReligionList(List<ReligionListResponsePacket.ReligionInfo> religions) { /* ... */ }
    public void UpdatePlayerReligionInfo(PlayerReligionInfoResponsePacket info) { /* ... */ }
}
```

### Renderer Architecture

**New File: `PantheonWars/GUI/UI/Renderers/Religion/ReligionTabRenderer.cs`** (~100 lines)
- Main coordinator for religion tab
- Draws sub-tab control (Browse, My Religion, Activity, Bonuses, Create)
- Routes to appropriate sub-renderer based on CurrentSubTab
- Pattern: Matches `CivilizationTabRenderer.cs`

**New File: `PantheonWars/GUI/UI/Renderers/Religion/ReligionBrowseRenderer.cs`** (~200 lines)
- Deity filter tabs (All, Khoras, Lysa, Morthen, Aethra, Umbros, Tharos, Gaia, Vex)
- Scrollable religion list using `ReligionListRenderer.cs`
- "Join Religion" button (enabled when religion selected)
- Conditional "Create Religion" button (only if user has no religion)
- Loading states and empty states
- Pattern: Migrates functionality from `ReligionBrowserOverlay.cs` + old dialog browse tab

**New File: `PantheonWars/GUI/UI/Renderers/Religion/ReligionMyReligionRenderer.cs`** (~280 lines)
- Religion header (name, deity, founder, stats)
- Description display and edit section (text area + save button)
- Member list section with management:
  - Scrollable member list using `MemberListRenderer.cs`
  - Inline kick/ban buttons for each member (founder only)
  - Confirmation overlays for kick/ban actions
- Banned players section:
  - Scrollable banned list using `BanListRenderer.cs`
  - Unban buttons (founder only)
- Invite section:
  - Player name input field
  - "Send Invite" button
- Action buttons:
  - "Leave Religion" (always available)
  - "Disband Religion" (founder only, with confirmation)
- "Not in a religion" empty state
- Pattern: Migrates functionality from `ReligionManagementOverlay.cs` + old dialog my religion tab

**New File: `PantheonWars/GUI/UI/Renderers/Religion/ReligionActivityRenderer.cs`** (~100 lines)
- **Phase 1 (Current Plan)**: Placeholder with "Activity feed coming soon!" message
- Styled info box with icon
- **Future Enhancement**: Display activity log (joins, leaves, invites, kicks, bans, prestige milestones)
- Pattern: Simple placeholder similar to early civilization implementation

**New File: `PantheonWars/GUI/UI/Renderers/Religion/ReligionBonusesRenderer.cs`** (~180 lines)
- Prestige rank bonuses section:
  - Current prestige rank display
  - Progress bar to next rank
  - List of active bonuses from prestige rank
- Civilization bonuses section (if in civilization):
  - List of bonuses from civilization membership
- Other progression bonuses section:
  - Any additional bonus systems (future-proof)
- Scrollable layout for comprehensive display
- Pattern: New feature, leverages `PrestigeRank` enum and bonus calculation logic

**New File: `PantheonWars/GUI/UI/Renderers/Religion/ReligionCreateRenderer.cs`** (~140 lines)
- Religion name input field (with validation feedback)
- Deity selection dropdown (all 8 deities)
- Public/Private toggle
- "Create Religion" button (validates inputs before enabling)
- Success/error feedback
- Pattern: Migrates functionality from `CreateReligionOverlay.cs` and old `CreateReligionDialog.cs`

### Event Handler Updates

**File: `PantheonWars/GUI/BlessingDialogEventHandlers.cs`** (additions)

New event handlers to add:

```csharp
private void OnReligionListReceived(ReligionListResponsePacket packet)
{
    _manager!.ReligionState.AllReligions = packet.Religions;
    _manager.ReligionState.IsBrowseLoading = false;
    _manager.ReligionState.BrowseError = null;
}

private void OnPlayerReligionInfoReceived(PlayerReligionInfoResponsePacket packet)
{
    _manager!.ReligionState.MyReligionInfo = packet;
    _manager.ReligionState.Description = packet.Description ?? "";
    _manager.ReligionState.IsMyReligionLoading = false;
    _manager.ReligionState.MyReligionError = null;
}

private void OnReligionActionCompleted(ReligionActionResponsePacket packet)
{
    _capi!.ShowChatMessage(packet.Message);

    if (packet.Success)
    {
        // Play success sound
        _capi.World.PlaySoundAt(new AssetLocation("pantheonwars:sounds/click"),
            _capi.World.Player.Entity, null, false, 8f, 0.5f);

        // Refresh data based on action
        _manager!.ReligionState.IsBrowseLoading = true;
        _pantheonWarsSystem?.RequestReligionList(_manager.ReligionState.DeityFilter);

        if (_manager.HasReligion())
        {
            _manager.ReligionState.IsMyReligionLoading = true;
            _pantheonWarsSystem?.RequestPlayerReligionInfo();
        }

        // Clear confirmations
        _manager.ReligionState.ShowDisbandConfirm = false;
        _manager.ReligionState.KickConfirmPlayerUID = null;
        _manager.ReligionState.BanConfirmPlayerUID = null;
    }
    else
    {
        // Play error sound
        _capi.World.PlaySoundAt(new AssetLocation("pantheonwars:sounds/error"),
            _capi.World.Player.Entity, null, false, 8f, 0.5f);

        _manager!.ReligionState.LastActionError = packet.Message;
    }
}
```

### Integration into BlessingDialog

**File: `PantheonWars/GUI/UI/BlessingUIRenderer.cs`** (modifications)

Replace Tab 1 placeholder:

```csharp
switch (state.CurrentMainTab)
{
    case 0: // Blessings
        DrawBlessingsTab(...);
        break;
    case 1: // Manage Religion
        // OLD: TextRenderer.DrawInfoText(drawList, "Religion management coming soon!", ...);
        // NEW:
        ReligionTabRenderer.Draw(manager, api, x, currentY, width, contentHeight);
        break;
    case 2: // Civilization
        CivilizationTabRenderer.Draw(...);
        break;
}
```

Tab switching data refresh:

```csharp
if (newMainTab != state.CurrentMainTab)
{
    state.CurrentMainTab = newMainTab;

    if (newMainTab == 1) // Religion tab
    {
        // Request both browse and my religion data
        manager.ReligionState.IsBrowseLoading = true;
        manager.RequestReligionList(manager.ReligionState.DeityFilter);

        if (manager.HasReligion())
        {
            manager.ReligionState.IsMyReligionLoading = true;
            manager.RequestPlayerReligionInfo();
        }
    }
    else if (newMainTab == 2) // Civilization tab
    {
        // ... existing civilization logic
    }
}
```

## Implementation Phases

### Phase 1: Foundation and State (2-3 hours)

**Tasks:**
1. Create `ReligionTabState.cs` with all state properties
2. Add `ReligionTabState` property to `BlessingDialogManager.cs`
3. Add request methods to `BlessingDialogManager`:
   - `RequestReligionList(string deityFilter)`
   - `RequestPlayerReligionInfo()`
   - `RequestReligionAction(string action, ...)`
4. Add update methods to `BlessingDialogManager`:
   - `UpdateReligionList(List<ReligionListResponsePacket.ReligionInfo> religions)`
   - `UpdatePlayerReligionInfo(PlayerReligionInfoResponsePacket info)`

**Testing:**
- Verify state class compiles
- Verify manager methods compile
- No runtime testing yet (no UI)

### Phase 2: Core Renderers (8-10 hours)

**Tasks:**
1. **ReligionTabRenderer.cs** (~2 hours):
   - Sub-tab control rendering
   - Sub-tab switching logic with data refresh
   - Route to appropriate sub-renderer
   - Test: Sub-tabs render and switch

2. **ReligionBrowseRenderer.cs** (~2.5 hours):
   - Deity filter tabs
   - Religion list using ReligionListRenderer
   - Join/Create buttons
   - Loading/empty states
   - Test: Can browse religions, filter by deity, select religion

3. **ReligionMyReligionRenderer.cs** (~3 hours):
   - Religion info display
   - Description edit section
   - Member list with kick/ban (inline confirmations)
   - Banned players list with unban
   - Invite section
   - Leave/Disband buttons with confirmations
   - Empty state handling
   - Test: All management actions work (invite, kick, ban, unban, edit description, leave, disband)

4. **ReligionCreateRenderer.cs** (~1.5 hours):
   - Form inputs (name, deity, public/private)
   - Validation logic
   - Create button
   - Test: Can create religion with valid inputs

5. **ReligionActivityRenderer.cs** (~0.5 hours):
   - Placeholder message
   - Test: Renders placeholder

6. **ReligionBonusesRenderer.cs** (~1.5 hours):
   - Prestige bonuses section
   - Civilization bonuses section
   - Scrollable layout
   - Test: Displays bonuses correctly

**Testing After Phase 2:**
- All sub-tabs render correctly
- Can switch between sub-tabs without errors
- Browse: Can filter, select, join religions
- My Religion: Can perform all management actions
- Create: Can create new religion
- Activity: Placeholder displays
- Bonuses: Bonuses display correctly

### Phase 3: Event Handlers and Integration (2-3 hours)

**Tasks:**
1. Add event handlers to `BlessingDialogEventHandlers.cs`:
   - `OnReligionListReceived`
   - `OnPlayerReligionInfoReceived`
   - `OnReligionActionCompleted`
2. Update `BlessingUIRenderer.cs`:
   - Replace Tab 1 placeholder with `ReligionTabRenderer.Draw(...)`
   - Add tab switching data refresh for religion tab
3. Wire up packet handlers in `BlessingDialog.cs` initialization
4. Remove overlay references from `ReligionHeaderRenderer.cs` button handlers:
   - Change button handlers to switch to appropriate sub-tab instead of opening overlays
   - "Browse Religions" → Switch to Tab 1, Sub-tab 0 (Browse)
   - "Manage Religion" → Switch to Tab 1, Sub-tab 1 (My Religion)
   - Keep "Leave Religion" as inline confirmation in header or redirect to My Religion tab

**Testing:**
- End-to-end testing:
  - Open BlessingDialog → Religion tab
  - Browse religions, filter by deity
  - Join a religion
  - Manage religion (invite, kick, ban, unban, edit description)
  - Leave religion
  - Create new religion
  - View bonuses
  - All actions should update data and refresh UI
  - All confirmations should work
  - Loading states should display correctly
  - Error messages should display correctly

### Phase 4: Mark Files Obsolete (0.5 hours)

**Tasks:**
1. Add `[Obsolete("Replaced by ReligionTabRenderer system in BlessingDialog. This dialog will be removed in a future version.")]` to:
   - `CreateReligionDialog.cs`
   - `ReligionManagementDialog.cs`
   - `EditDescriptionDialog.cs`
   - `InvitePlayerDialog.cs`
   - `BanPlayerDialog.cs`
2. Add `[Obsolete("Replaced by ReligionBrowseRenderer, ReligionMyReligionRenderer, etc. in BlessingDialog. This overlay will be removed in a future version.")]` to:
   - `ReligionBrowserOverlay.cs`
   - `ReligionManagementOverlay.cs`
   - `CreateReligionOverlay.cs`
   - `LeaveReligionConfirmOverlay.cs`
3. Verify `FavorHudElement.cs` already has `[Obsolete]` attribute (already done)

**Testing:**
- Build project, verify obsolete warnings appear
- Verify no functionality breaks (old dialogs/overlays still work for backward compatibility)

### Phase 5: Remove Old Dialog/Overlay Invocations (1 hour)

**Tasks:**
1. Search codebase for references to old dialogs/overlays
2. Remove or replace any code that opens these dialogs:
   - Remove hotkey registrations for old dialogs
   - Remove any menu items or buttons that open old dialogs
   - Ensure ReligionHeaderRenderer buttons go to correct sub-tabs
3. Update any documentation that references old dialogs

**Testing:**
- Verify no code path can open old dialogs/overlays
- Verify all religion functionality is accessible through BlessingDialog tabs
- User testing: Complete religion workflow from start to finish

### Phase 6: File Deletion and Cleanup (0.5 hours)

**Tasks:**
1. Delete old GuiDialog files:
   - `CreateReligionDialog.cs`
   - `ReligionManagementDialog.cs`
   - `EditDescriptionDialog.cs`
   - `InvitePlayerDialog.cs`
   - `BanPlayerDialog.cs`
   - `FavorHudElement.cs`
2. Delete old overlay files:
   - `ReligionBrowserOverlay.cs`
   - `ReligionManagementOverlay.cs`
   - `CreateReligionOverlay.cs`
   - `LeaveReligionConfirmOverlay.cs`
3. Remove any using statements referencing deleted files
4. Remove overlay state from `OverlayCoordinator.cs`:
   - `_showReligionBrowser`
   - `_showReligionManagement`
   - `_showCreateReligion`
   - `_showLeaveConfirmation`
5. Clean up any unused state classes:
   - `ReligionBrowserState.cs` (if fully replaced)
   - `ReligionManagementState.cs` (if fully replaced)
   - `CreateReligionState.cs` (if fully replaced)

**Testing:**
- Build project, verify no compilation errors
- Run full regression test of religion features
- Verify no missing references or broken functionality

### Phase 7: Documentation and Polish (1 hour)

**Tasks:**
1. Update plan document in `docs/topics/planning/`:
   - Mark this plan as "Completed"
   - Add completion date
   - Note any deviations from plan
2. Add TODO comments for future enhancements:
   - Activity feed full implementation
   - Additional bonus display enhancements
3. Code review pass:
   - Ensure consistent naming conventions
   - Verify all error cases are handled
   - Check for any hardcoded values that should be constants
   - Ensure loading states are properly managed

**Testing:**
- Final user acceptance testing
- Performance check (UI should be responsive)
- Memory leak check (state should not accumulate)

## Total Estimated Time: 15-19 hours

## File Summary

### New Files (6 renderers + 1 state = ~1,020 lines):
1. `PantheonWars/GUI/State/ReligionTabState.cs` (~120 lines)
2. `PantheonWars/GUI/UI/Renderers/Religion/ReligionTabRenderer.cs` (~100 lines)
3. `PantheonWars/GUI/UI/Renderers/Religion/ReligionBrowseRenderer.cs` (~200 lines)
4. `PantheonWars/GUI/UI/Renderers/Religion/ReligionMyReligionRenderer.cs` (~280 lines)
5. `PantheonWars/GUI/UI/Renderers/Religion/ReligionActivityRenderer.cs` (~100 lines)
6. `PantheonWars/GUI/UI/Renderers/Religion/ReligionBonusesRenderer.cs` (~180 lines)
7. `PantheonWars/GUI/UI/Renderers/Religion/ReligionCreateRenderer.cs` (~140 lines)

### Modified Files (3 files):
1. `PantheonWars/GUI/BlessingDialogManager.cs` (+40 lines)
2. `PantheonWars/GUI/BlessingDialogEventHandlers.cs` (+80 lines)
3. `PantheonWars/GUI/UI/BlessingUIRenderer.cs` (+20 lines)

### Deleted Files (11 files, ~2,197 lines):
1. `CreateReligionDialog.cs` (175 lines)
2. `ReligionManagementDialog.cs` (563 lines)
3. `EditDescriptionDialog.cs` (137 lines)
4. `InvitePlayerDialog.cs` (131 lines)
5. `BanPlayerDialog.cs` (179 lines)
6. `FavorHudElement.cs` (112 lines)
7. `ReligionBrowserOverlay.cs` (234 lines)
8. `ReligionManagementOverlay.cs` (372 lines)
9. `CreateReligionOverlay.cs` (~150 lines)
10. `LeaveReligionConfirmOverlay.cs` (~144 lines)
11. Overlay state classes if fully replaced

### Kept Files:
1. `DeitySelectionDialog.cs` - Still used for initial deity selection
2. `ReligionHeaderRenderer.cs` - Update button handlers to switch tabs
3. `ReligionListRenderer.cs` - Reused component
4. `MemberListRenderer.cs` - Reused component
5. `BanListRenderer.cs` - Reused component

### Net Impact:
- **Lines Added**: ~1,160 lines (new files + modifications)
- **Lines Deleted**: ~2,197 lines
- **Net Change**: **-1,037 lines** (46% reduction)

## Risk Mitigation

### Risk 1: User Workflow Disruption
**Mitigation**:
- Keep old dialogs/overlays functional through Phase 5
- Mark as obsolete but don't delete until new system is tested
- Provide clear migration path for users

### Risk 2: State Management Complexity
**Mitigation**:
- Follow proven civilization pattern exactly
- Separate loading flags for each section
- Clear error handling and messaging
- Extensive testing of state transitions

### Risk 3: Missing Functionality
**Mitigation**:
- Comprehensive feature audit comparing old vs new
- Checklist-based testing for all actions
- User acceptance testing before deletion

### Risk 4: Bonus Display Data Availability
**Mitigation**:
- Check that prestige bonus data is accessible from client
- Check that civilization bonus data is accessible
- Implement graceful degradation if data unavailable
- Can start with prestige only and add others incrementally

### Risk 5: Activity Feed Future Implementation
**Mitigation**:
- Design placeholder with clear extension points
- Document expected data structure for activity log
- Keep server-side activity tracking as separate future task

## Success Criteria

1. **Functionality**: All religion features accessible through BlessingDialog tabs
2. **Parity**: No loss of functionality compared to old system
3. **User Experience**: Smooth tab navigation, clear feedback, responsive UI
4. **Code Quality**: Follows established patterns, well-documented, maintainable
5. **Performance**: No UI lag, efficient state updates
6. **Testing**: All actions tested end-to-end, edge cases handled
7. **Cleanup**: Old files deleted, no dead code, no compilation warnings

## Future Enhancements (Post-Migration)

1. **Activity Feed Full Implementation**:
   - Server-side activity event tracking
   - Real-time activity log display
   - Activity filtering and search

2. **Enhanced Member Management**:
   - Member role system (leader, officer, member)
   - Bulk actions (mass invite, etc.)
   - Member search and filtering

3. ~~**Religion Diplomacy**~~:
   - ~~Religion alliances~~
   - ~~Inter-religion events~~
   - ~~Diplomatic actions~~
   - *Note: Diplomacy will function at the civilization level instead*

4. **Prestige Improvements**:
   - Detailed prestige breakdown
   - Prestige history graph
   - Competitive prestige leaderboard

## Appendix: Key Pattern Comparisons

### Civilization Pattern (Reference)
- 5 sub-tabs: Browse, My Civilization, Invitations, Bonuses, Create
- CivilizationState.cs with separate loading flags
- CivilizationTabRenderer.cs as coordinator
- Focused renderers for each sub-tab
- Event handlers in BlessingDialogEventHandlers.cs
- Action methods in BlessingDialogManager.cs

### Religion Pattern (This Plan)
- 5 sub-tabs: Browse, My Religion, Activity, Bonuses, Create
- ReligionTabState.cs with separate loading flags
- ReligionTabRenderer.cs as coordinator
- Focused renderers for each sub-tab
- Event handlers in BlessingDialogEventHandlers.cs
- Action methods in BlessingDialogManager.cs

**Consistency Achieved**: 100% architectural alignment with civilization pattern
