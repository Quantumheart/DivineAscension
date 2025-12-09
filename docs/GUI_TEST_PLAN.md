# GUI Test Plan - PantheonWars Mod

**Version:** 1.0
**Date:** 2025-12-09
**Author:** Test Planning Team

---

## Table of Contents

1. [Overview](#overview)
2. [Test Scope](#test-scope)
3. [Test Strategy](#test-strategy)
4. [Test Environment](#test-environment)
5. [Functional Testing](#functional-testing)
6. [Unit Testing](#unit-testing)
7. [Integration Testing](#integration-testing)
8. [UI/UX Testing](#uiux-testing)
9. [Performance Testing](#performance-testing)
10. [Edge Cases and Error Handling](#edge-cases-and-error-handling)
11. [Acceptance Criteria](#acceptance-criteria)
12. [Test Schedule](#test-schedule)

---

## 1. Overview

### Purpose
This test plan defines the testing approach for the PantheonWars mod GUI, which uses an Event-Driven Architecture (EDA) pattern with ImGui rendering.

### Architecture Summary
- **Pattern:** Event-Driven Architecture with unidirectional data flow
- **Flow:** State → ViewModel → Renderer → Events → State Manager → State
- **Main Components:**
  - GuiDialog (main entry point, Shift+G hotkey)
  - Three main tabs: Religion, Blessings, Civilization
  - Pure renderers (no side effects)
  - State managers (handle events and side effects)

### Current Test Coverage
- ✅ BlessingDialogState basic tests
- ✅ ColorPalette utility tests
- ✅ DeityHelper utility tests
- ❌ No renderer tests (marked [ExcludeFromCodeCoverage])
- ❌ No state manager tests
- ❌ No event processing tests
- ❌ No integration tests

---

## 2. Test Scope

### In Scope

#### 2.1 Main Dialog
- Dialog open/close (Shift+G hotkey)
- Window positioning and sizing
- Tab navigation (Religion, Blessings, Civilization)
- Dialog lifecycle management

#### 2.2 Blessing System
- Blessing tree rendering (player and religion views)
- Blessing node states (locked, unlocked, available, prerequisite-blocked)
- Blessing selection and info display
- Blessing unlock actions
- Prerequisite validation
- Rank requirement validation
- Independent scrolling for player/religion trees
- Tooltips and hover states
- Animated transitions

#### 2.3 Religion System
- **Browse Tab:**
  - Religion list display
  - Deity filter functionality
  - Religion selection
  - Join religion action
  - Navigation to create tab

- **Info Tab:**
  - Religion header display
  - Description editing (founder only)
  - Member list display with roles and online status
  - Invite player functionality
  - Kick member with confirmation
  - Ban/unban functionality
  - Leave religion action
  - Disband religion with confirmation (founder only)

- **Invites Tab:**
  - Pending invitations list
  - Accept invitation action
  - Decline invitation action

- **Create Tab:**
  - Religion name input and validation
  - Deity selection dropdown
  - Public/private toggle
  - Religion creation submission

- **Activity Tab:**
  - Activity log display (future feature)

#### 2.4 Civilization System
- Browse civilizations
- Create civilization
- Manage civilization membership
- View civilization details
- Invitation system

#### 2.5 UI Components
- TabControl (main tabs and sub-tabs)
- ButtonRenderer (all button variants)
- TextInput (single-line and multiline)
- Checkbox
- Dropdown
- ScrollableList
- Scrollbar
- ConfirmOverlay (modal confirmations)
- ErrorBannerRenderer
- Various list renderers (Religion, Member, Ban)
- ProgressBarRenderer

#### 2.6 State Management
- BlessingStateManager
- ReligionStateManager
- GuiDialogManager
- State transitions and resets
- Event processing logic

#### 2.7 Error Handling
- Network error handling
- Validation error display
- Retry functionality
- Context-aware error banners
- Error dismissal

### Out of Scope
- Backend server logic
- Network protocol testing (covered by backend tests)
- Asset loading (deity icons)
- ImGui framework testing
- Sound system testing

---

## 3. Test Strategy

### 3.1 Testing Levels

#### Unit Testing
- **Target:** Individual classes and methods in isolation
- **Tools:** NUnit/xUnit, Moq for mocking
- **Coverage Goal:** 80% for business logic, 60% overall
- **Priority Areas:**
  - State managers
  - Event processing
  - Validation logic
  - Utilities
  - ViewModels

#### Integration Testing
- **Target:** Component interaction and data flow
- **Focus:**
  - State manager + Event flow
  - ViewModel transformation
  - Component composition

#### UI/UX Testing
- **Target:** User experience and interface behavior
- **Method:** Manual testing with test scenarios
- **Focus:**
  - Usability
  - Visual appearance
  - Responsiveness
  - Accessibility

#### Performance Testing
- **Target:** GUI responsiveness and resource usage
- **Metrics:**
  - Frame rate impact
  - Memory usage
  - Large list rendering

#### Regression Testing
- **Target:** Ensure existing functionality remains intact
- **Method:** Automated test suite + manual smoke tests

### 3.2 Testing Approach

1. **Bottom-Up:** Start with unit tests for utilities and state classes
2. **Component Testing:** Test individual UI components
3. **Integration Testing:** Test component interactions
4. **End-to-End:** Test complete user workflows
5. **Exploratory Testing:** Ad-hoc testing to discover edge cases

---

## 4. Test Environment

### 4.1 Development Environment
- **IDE:** Visual Studio / Rider
- **Framework:** .NET
- **Test Framework:** NUnit or xUnit
- **Mocking:** Moq
- **Coverage:** Coverlet or dotCover

### 4.2 Game Environment
- **Game:** Vintage Story
- **Mod System:** VSImGui integration
- **Test Mode:** DEBUG mode with fake data providers
- **Screen Resolutions:** 1920x1080, 2560x1440, 3840x2160
- **Multiplayer:** Single-player and multiplayer scenarios

### 4.3 Test Data
- **Fake Providers:** Use DEBUG mode fake data providers
- **Test Religions:** Multiple religions with various member counts
- **Test Blessings:** Complete blessing trees for all deities
- **Test Players:** Various player states (new, established, max rank)

---

## 5. Functional Testing

### 5.1 Main Dialog Tests

#### Test Case: GUI-001 - Open Dialog
**Objective:** Verify dialog opens with Shift+G
**Preconditions:** Mod loaded, in-game
**Steps:**
1. Press Shift+G
2. Observe dialog appears

**Expected Results:**
- Dialog opens centered on screen
- Window size is 1400x900 or responsive to screen size
- Religion tab is selected by default
- Close button is visible

**Priority:** High
**Type:** Functional, Manual

---

#### Test Case: GUI-002 - Close Dialog
**Objective:** Verify dialog closes properly
**Preconditions:** Dialog is open
**Steps:**
1. Click close button (X)
2. Alternatively, press Shift+G again
3. Alternatively, press Escape

**Expected Results:**
- Dialog closes completely
- State is preserved for next opening
- No memory leaks

**Priority:** High
**Type:** Functional, Manual

---

#### Test Case: GUI-003 - Tab Navigation
**Objective:** Verify main tab switching works
**Preconditions:** Dialog is open
**Steps:**
1. Click "Religion" tab
2. Click "Blessings" tab
3. Click "Civilization" tab
4. Return to "Religion" tab

**Expected Results:**
- Tab content switches correctly
- Selected tab is highlighted
- Previous tab state is preserved when returning
- No visual glitches during transitions

**Priority:** High
**Type:** Functional, Manual

---

### 5.2 Blessing System Tests

#### Test Case: BLESS-001 - View Blessing Tree
**Objective:** Verify blessing tree renders correctly
**Preconditions:** Dialog open, Blessings tab selected
**Steps:**
1. Navigate to Blessings tab
2. Observe left panel (Player blessings)
3. Observe right panel (Religion blessings)

**Expected Results:**
- Both trees render without errors
- Node connections are visible
- Nodes show correct states (locked/unlocked/available)
- Trees are scrollable independently
- No overlapping elements

**Priority:** High
**Type:** Functional, Manual

---

#### Test Case: BLESS-002 - Select Blessing
**Objective:** Verify blessing selection displays info
**Preconditions:** Blessing tree is visible
**Steps:**
1. Click on a blessing node
2. Observe info panel

**Expected Results:**
- Node is highlighted as selected
- Info panel displays blessing details:
  - Name
  - Description
  - Requirements (rank, prerequisites)
  - Effects/stats
- Action button appears (if unlockable)

**Priority:** High
**Type:** Functional, Manual

---

#### Test Case: BLESS-003 - Unlock Player Blessing (Valid)
**Objective:** Verify player can unlock blessing when requirements met
**Preconditions:** Player has sufficient favor rank, prerequisites met
**Steps:**
1. Select an available blessing node
2. Verify button is enabled
3. Click "Unlock" button
4. Wait for server response

**Expected Results:**
- Button is clickable
- Loading state appears briefly
- On success: Blessing becomes unlocked
- On success: Success sound plays
- On success: UI updates to show new blessing state
- Favor points are deducted

**Priority:** Critical
**Type:** Functional, Manual

---

#### Test Case: BLESS-004 - Unlock Blessing (Invalid - Insufficient Rank)
**Objective:** Verify unlock fails gracefully when rank insufficient
**Preconditions:** Player lacks sufficient favor rank
**Steps:**
1. Select a blessing requiring higher rank
2. Observe unlock button

**Expected Results:**
- Button is disabled or shows as unavailable
- Tooltip explains rank requirement
- Clicking has no effect or shows error message

**Priority:** High
**Type:** Functional, Manual

---

#### Test Case: BLESS-005 - Unlock Blessing (Invalid - Prerequisites Not Met)
**Objective:** Verify unlock fails when prerequisites missing
**Preconditions:** Blessing has unmet prerequisites
**Steps:**
1. Select a blessing with locked prerequisites
2. Observe unlock button

**Expected Results:**
- Button is disabled
- Info panel shows missing prerequisites
- Visual indication of blocked state
- Tooltip explains what's needed

**Priority:** High
**Type:** Functional, Manual

---

#### Test Case: BLESS-006 - Blessing Hover Tooltip
**Objective:** Verify hover tooltips work
**Preconditions:** Blessing tree is visible
**Steps:**
1. Hover mouse over various blessing nodes
2. Wait for tooltip to appear

**Expected Results:**
- Tooltip appears after brief delay
- Shows blessing name and basic info
- Tooltip follows mouse or anchors to node
- Tooltip disappears when mouse leaves

**Priority:** Medium
**Type:** Functional, Manual

---

#### Test Case: BLESS-007 - Independent Tree Scrolling
**Objective:** Verify player and religion trees scroll independently
**Preconditions:** Blessing trees are larger than viewport
**Steps:**
1. Scroll player tree (left panel) down
2. Observe religion tree (right panel)
3. Scroll religion tree up
4. Observe player tree

**Expected Results:**
- Each tree scrolls independently
- Scrollbars are visible and functional
- Scroll position is maintained when switching tabs
- No cross-contamination of scroll positions

**Priority:** Medium
**Type:** Functional, Manual

---

#### Test Case: BLESS-008 - Blessing Animations
**Objective:** Verify animated transitions work
**Preconditions:** Can unlock a blessing
**Steps:**
1. Unlock a blessing
2. Observe visual transition

**Expected Results:**
- Smooth animation from locked to unlocked state
- No flickering or visual artifacts
- Animation completes within reasonable time
- Related nodes update visual states

**Priority:** Low
**Type:** Functional, Manual

---

### 5.3 Religion System Tests

#### Test Case: REL-001 - Browse Religions
**Objective:** Verify religion list displays correctly
**Preconditions:** Dialog open, Religion tab, Browse sub-tab
**Steps:**
1. Navigate to Religion > Browse
2. Observe religion list

**Expected Results:**
- List shows all public religions
- Each entry shows: name, deity, member count, prestige
- List is scrollable if many religions exist
- Empty state message if no religions exist

**Priority:** High
**Type:** Functional, Manual

---

#### Test Case: REL-002 - Filter Religions by Deity
**Objective:** Verify deity filter works
**Preconditions:** Multiple religions with different deities exist
**Steps:**
1. Navigate to Religion > Browse
2. Select "Khoras" from deity dropdown
3. Select "All Deities"
4. Select "Lysa"

**Expected Results:**
- List updates to show only matching religions
- Filter preserves when switching tabs and returning
- "All Deities" shows unfiltered list
- Count updates correctly

**Priority:** High
**Type:** Functional, Manual

---

#### Test Case: REL-003 - Join Religion
**Objective:** Verify player can join a public religion
**Preconditions:** Player not in a religion, public religion exists
**Steps:**
1. Select a religion from browse list
2. Click "Join Religion" button
3. Wait for server response

**Expected Results:**
- Button is enabled
- Loading state appears
- On success: Player is member of religion
- On success: UI switches to Info tab
- On success: Success sound plays
- On failure: Error banner appears with reason

**Priority:** Critical
**Type:** Functional, Manual

---

#### Test Case: REL-004 - Cannot Join Private Religion
**Objective:** Verify private religions require invitation
**Preconditions:** Private religion exists, player not invited
**Steps:**
1. Browse religions (should not see private ones)
2. OR if invited, try to join without invitation

**Expected Results:**
- Private religions don't appear in browse list (unless invited)
- Cannot join without invitation
- Error message explains privacy setting

**Priority:** High
**Type:** Functional, Manual

---

#### Test Case: REL-005 - View My Religion Info
**Objective:** Verify religion info displays correctly
**Preconditions:** Player is member of a religion
**Steps:**
1. Navigate to Religion > Info tab
2. Observe all sections

**Expected Results:**
- Religion header shows: name, deity, prestige, member count
- Description is displayed (editable if founder)
- Member list shows all members with:
  - Player name
  - Online status indicator
  - Role (Founder/Member)
  - Action buttons (if founder)
- Action buttons visible (Leave/Invite/Disband)

**Priority:** High
**Type:** Functional, Manual

---

#### Test Case: REL-006 - Edit Religion Description (Founder)
**Objective:** Verify founder can edit description
**Preconditions:** Player is founder of a religion
**Steps:**
1. Navigate to Religion > Info
2. Click description field
3. Type new description
4. Click "Save" button
5. Wait for server response

**Expected Results:**
- Description field is editable
- Character limit is enforced
- Save button becomes enabled when changed
- On success: Description updates
- On success: "Saved" confirmation appears
- On failure: Error banner with reason

**Priority:** High
**Type:** Functional, Manual

---

#### Test Case: REL-007 - Cannot Edit Description (Non-Founder)
**Objective:** Verify members cannot edit description
**Preconditions:** Player is member but not founder
**Steps:**
1. Navigate to Religion > Info
2. Observe description field

**Expected Results:**
- Description field is read-only
- No edit cursor appears
- No save button visible

**Priority:** Medium
**Type:** Functional, Manual

---

#### Test Case: REL-008 - Invite Player to Religion
**Objective:** Verify founder can invite players
**Preconditions:** Player is founder, target player not in religion
**Steps:**
1. Navigate to Religion > Info
2. Find invite section
3. Enter target player name
4. Click "Invite" button
5. Wait for server response

**Expected Results:**
- Text input accepts player name
- Button is enabled when name entered
- On success: Confirmation message
- On success: Invitation sent to target player
- On failure: Error message (player not found, already member, etc.)

**Priority:** High
**Type:** Functional, Manual

---

#### Test Case: REL-009 - Kick Member (Founder)
**Objective:** Verify founder can kick members
**Preconditions:** Player is founder, other members exist
**Steps:**
1. Navigate to Religion > Info
2. Find a member in member list
3. Click "Kick" button next to member
4. Observe confirmation overlay
5. Click "Confirm"
6. Wait for server response

**Expected Results:**
- Confirmation overlay appears with backdrop
- Overlay shows member name and warning
- On confirm: Member is removed from religion
- On confirm: Member list updates
- On cancel: No action taken
- On failure: Error banner explains reason

**Priority:** High
**Type:** Functional, Manual

---

#### Test Case: REL-010 - Ban Member (Founder)
**Objective:** Verify founder can ban members
**Preconditions:** Player is founder, member to ban exists
**Steps:**
1. Navigate to Religion > Info
2. Find a member in member list
3. Click "Ban" button
4. Confirm in overlay
5. Wait for server response

**Expected Results:**
- Confirmation overlay appears
- On confirm: Member is removed and added to ban list
- On confirm: Member list and ban list update
- Member cannot rejoin
- On failure: Error message

**Priority:** High
**Type:** Functional, Manual

---

#### Test Case: REL-011 - Unban Player (Founder)
**Objective:** Verify founder can unban players
**Preconditions:** Player is founder, banned players exist
**Steps:**
1. Navigate to Religion > Info
2. Scroll to ban list section
3. Click "Unban" next to banned player
4. Wait for server response

**Expected Results:**
- Ban list shows all banned players
- On success: Player removed from ban list
- Player can now rejoin if invited/public
- On failure: Error banner

**Priority:** Medium
**Type:** Functional, Manual

---

#### Test Case: REL-012 - Leave Religion (Member)
**Objective:** Verify member can leave religion
**Preconditions:** Player is member (not founder)
**Steps:**
1. Navigate to Religion > Info
2. Click "Leave Religion" button
3. Confirm in overlay
4. Wait for server response

**Expected Results:**
- Confirmation overlay appears with warning
- On confirm: Player leaves religion
- On confirm: UI switches to Browse tab
- On cancel: No action taken
- On failure: Error message

**Priority:** High
**Type:** Functional, Manual

---

#### Test Case: REL-013 - Disband Religion (Founder)
**Objective:** Verify founder can disband religion
**Preconditions:** Player is founder
**Steps:**
1. Navigate to Religion > Info
2. Click "Disband Religion" button
3. Observe confirmation overlay with strong warning
4. Click "Confirm Disband"
5. Wait for server response

**Expected Results:**
- Confirmation overlay with clear warning about permanent action
- Must confirm understanding of consequences
- On confirm: Religion is deleted
- On confirm: All members are removed
- On confirm: UI switches to Browse tab
- On cancel: No action taken
- On failure: Error message

**Priority:** Critical
**Type:** Functional, Manual

---

#### Test Case: REL-014 - View Religion Invitations
**Objective:** Verify player can see pending invitations
**Preconditions:** Player has been invited to religion(s)
**Steps:**
1. Navigate to Religion > Invites tab
2. Observe invitation list

**Expected Results:**
- All pending invitations are listed
- Each shows: religion name, deity, inviter name
- Accept and Decline buttons visible
- Empty state message if no invitations

**Priority:** High
**Type:** Functional, Manual

---

#### Test Case: REL-015 - Accept Religion Invitation
**Objective:** Verify player can accept invitation
**Preconditions:** Player has pending invitation, not in religion
**Steps:**
1. Navigate to Religion > Invites
2. Click "Accept" on an invitation
3. Wait for server response

**Expected Results:**
- Loading state appears
- On success: Player joins religion
- On success: Invitation removed from list
- On success: UI switches to Info tab
- On failure: Error banner (religion full, disbanded, etc.)

**Priority:** High
**Type:** Functional, Manual

---

#### Test Case: REL-016 - Decline Religion Invitation
**Objective:** Verify player can decline invitation
**Preconditions:** Player has pending invitation
**Steps:**
1. Navigate to Religion > Invites
2. Click "Decline" on an invitation
3. Wait for server response

**Expected Results:**
- On success: Invitation removed from list
- No other side effects
- Inviter may be notified
- Can still be invited again later

**Priority:** Medium
**Type:** Functional, Manual

---

#### Test Case: REL-017 - Create New Religion
**Objective:** Verify player can create religion
**Preconditions:** Player not in religion, meets requirements
**Steps:**
1. Navigate to Religion > Create tab
2. Enter religion name
3. Select deity from dropdown
4. Toggle public/private
5. Click "Create Religion" button
6. Wait for server response

**Expected Results:**
- All form fields functional
- Name validation (length, special chars)
- Deity dropdown shows all options
- Public/private toggle works
- Submit button enabled when form valid
- On success: Religion created, player is founder
- On success: UI switches to Info tab
- On failure: Error banner (duplicate name, validation, etc.)

**Priority:** Critical
**Type:** Functional, Manual

---

#### Test Case: REL-018 - Create Religion Name Validation
**Objective:** Verify religion name validation works
**Preconditions:** On Create tab
**Steps:**
1. Try empty name
2. Try name too short
3. Try name too long
4. Try special characters
5. Try duplicate name
6. Try valid name

**Expected Results:**
- Empty: Submit disabled
- Too short: Error message
- Too long: Input truncated or error
- Special chars: Validation error
- Duplicate: Server error on submit
- Valid: Submit enabled

**Priority:** High
**Type:** Functional, Manual

---

#### Test Case: REL-019 - Religion Activity Log
**Objective:** Verify activity log displays (future feature)
**Preconditions:** Player in religion, activity exists
**Steps:**
1. Navigate to Religion > Activity tab
2. Observe log

**Expected Results:**
- Activity entries displayed in chronological order
- Shows: timestamp, player name, action
- Scrollable if many entries
- Placeholder message if no implementation yet

**Priority:** Low
**Type:** Functional, Manual

---

### 5.4 Civilization System Tests

#### Test Case: CIV-001 - Browse Civilizations
**Objective:** Verify civilization list works
**Preconditions:** Dialog open, Civilization tab
**Steps:**
1. Navigate to Civilization tab
2. Observe civilization list

**Expected Results:**
- Similar behavior to religion browse
- List shows available civilizations
- Can filter and search

**Priority:** Medium
**Type:** Functional, Manual

---

#### Test Case: CIV-002 - Create Civilization
**Objective:** Verify civilization creation
**Preconditions:** Player meets requirements
**Steps:**
1. Navigate to create civilization
2. Fill form
3. Submit

**Expected Results:**
- Form validation works
- Civilization created on success
- Error handling for failures

**Priority:** Medium
**Type:** Functional, Manual

---

#### Test Case: CIV-003 - Manage Civilization
**Objective:** Verify civilization management works
**Preconditions:** Player is leader
**Steps:**
1. View civilization info
2. Test management actions

**Expected Results:**
- Similar to religion management
- Leader can invite, kick, edit
- Members can leave

**Priority:** Medium
**Type:** Functional, Manual

---

### 5.5 UI Component Tests

#### Test Case: UI-001 - Button States
**Objective:** Verify all button types work correctly
**Preconditions:** Various buttons visible
**Steps:**
1. Test primary button (hover, click)
2. Test secondary button
3. Test small button
4. Test action button (danger style)
5. Test close button
6. Test disabled buttons

**Expected Results:**
- Hover effect appears on mouseover
- Click triggers action
- Disabled buttons don't respond to clicks
- Visual feedback on click
- Sound plays on click (if configured)

**Priority:** High
**Type:** Functional, Manual

---

#### Test Case: UI-002 - Text Input (Single-Line)
**Objective:** Verify text input works
**Preconditions:** Text input field visible
**Steps:**
1. Click input field
2. Type text
3. Use backspace/delete
4. Try paste (Ctrl+V)
5. Tab to next field
6. Test placeholder text

**Expected Results:**
- Cursor appears on focus
- Text appears as typed
- Editing operations work
- Placeholder disappears when typing
- Character limit enforced (if applicable)
- Keyboard focus management works

**Priority:** High
**Type:** Functional, Manual

---

#### Test Case: UI-003 - Text Input (Multiline)
**Objective:** Verify multiline text area works
**Preconditions:** Multiline input visible (e.g., religion description)
**Steps:**
1. Click text area
2. Type multiple lines
3. Use Enter for new lines
4. Test scrolling within area
5. Test word wrap

**Expected Results:**
- Multiple lines supported
- Enter creates new line
- Scrollbar appears if content exceeds area
- Text wraps appropriately
- Character limit enforced

**Priority:** High
**Type:** Functional, Manual

---

#### Test Case: UI-004 - Dropdown Selection
**Objective:** Verify dropdown works
**Preconditions:** Dropdown visible (e.g., deity selection)
**Steps:**
1. Click dropdown
2. Observe options list
3. Select an option
4. Close without selecting

**Expected Results:**
- Dropdown expands on click
- All options visible
- Scrollable if many options
- Selection updates display
- Closing without selection preserves previous value

**Priority:** High
**Type:** Functional, Manual

---

#### Test Case: UI-005 - Checkbox Toggle
**Objective:** Verify checkbox works
**Preconditions:** Checkbox visible (e.g., public/private toggle)
**Steps:**
1. Click checkbox to check
2. Click again to uncheck
3. Observe visual state

**Expected Results:**
- Visual changes on state change
- State persists correctly
- Label click also toggles (if applicable)

**Priority:** Medium
**Type:** Functional, Manual

---

#### Test Case: UI-006 - Scrollbar Functionality
**Objective:** Verify custom scrollbar works
**Preconditions:** Content exceeds visible area
**Steps:**
1. Click and drag scrollbar thumb
2. Click in scrollbar track
3. Use mouse wheel to scroll
4. Test edge cases (top, bottom)

**Expected Results:**
- Thumb dragging scrolls content
- Track clicking jumps to position
- Mouse wheel scrolls content
- Cannot scroll beyond content bounds
- Scrollbar size reflects content size ratio

**Priority:** High
**Type:** Functional, Manual

---

#### Test Case: UI-007 - Confirmation Overlay
**Objective:** Verify confirmation dialogs work
**Preconditions:** Action requiring confirmation available
**Steps:**
1. Trigger action (e.g., Kick member)
2. Observe overlay appears
3. Click "Cancel"
4. Trigger action again
5. Click "Confirm"

**Expected Results:**
- Overlay appears with backdrop dimming
- Main UI is non-interactive while overlay shown
- Message clearly explains action
- Cancel dismisses without action
- Confirm executes action
- Overlay closes after selection

**Priority:** Critical
**Type:** Functional, Manual

---

#### Test Case: UI-008 - Error Banner Display
**Objective:** Verify error banners work
**Preconditions:** Error condition can be triggered
**Steps:**
1. Trigger an error (e.g., network failure)
2. Observe error banner
3. Click "Retry" if available
4. Click "Dismiss"

**Expected Results:**
- Banner appears at top of relevant section
- Error message is clear and actionable
- Retry button triggers operation again
- Dismiss button removes banner
- Context-appropriate error messages
- Banner doesn't obscure critical UI

**Priority:** High
**Type:** Functional, Manual

---

#### Test Case: UI-009 - Tab Control Behavior
**Objective:** Verify tab controls work
**Preconditions:** Multiple tabs visible
**Steps:**
1. Click various tabs
2. Hover over tabs
3. Observe selection state

**Expected Results:**
- Hover effect on mouseover
- Selected tab is highlighted
- Content switches immediately
- Only one tab selected at a time
- Visual feedback is clear

**Priority:** High
**Type:** Functional, Manual

---

#### Test Case: UI-010 - Progress Bar Display
**Objective:** Verify progress bars render correctly
**Preconditions:** Progress bars visible (favor/prestige)
**Steps:**
1. Observe progress bars at various fill levels
2. Watch for updates when values change

**Expected Results:**
- Bar fills proportionally to value
- Text shows current/max values
- Color is appropriate
- Smooth updates when value changes
- Rank display updates correctly

**Priority:** Medium
**Type:** Functional, Manual

---

### 5.6 Workflow Tests (End-to-End)

#### Test Case: FLOW-001 - Complete New Player Workflow
**Objective:** Test typical new player experience
**Preconditions:** New player, no religion/blessings
**Steps:**
1. Open GUI (Shift+G)
2. Browse religions
3. Create new religion
4. Navigate to Blessings tab
5. Unlock first blessing
6. Invite another player
7. Close GUI

**Expected Results:**
- All steps complete without errors
- Data persists between tab switches
- No visual glitches
- Performance is acceptable

**Priority:** Critical
**Type:** End-to-End, Manual

---

#### Test Case: FLOW-002 - Religion Management Workflow
**Objective:** Test religion management tasks
**Preconditions:** Player is founder with members
**Steps:**
1. Open GUI
2. Edit religion description
3. Invite a player
4. Kick a member
5. Ban a troublesome member
6. Review member list
7. Check ban list
8. Unban a player

**Expected Results:**
- All actions succeed
- State updates correctly
- Confirmations appear when needed
- No data loss

**Priority:** High
**Type:** End-to-End, Manual

---

#### Test Case: FLOW-003 - Blessing Progression Workflow
**Objective:** Test blessing unlock progression
**Preconditions:** Player with some favor
**Steps:**
1. Open GUI > Blessings
2. Review blessing tree
3. Unlock tier 1 blessing
4. Meet prerequisites for tier 2
5. Unlock tier 2 blessing
6. Check for rank requirement on tier 3
7. Gain more favor
8. Return and unlock tier 3

**Expected Results:**
- Prerequisite chain works correctly
- Rank gates function
- Visual feedback is clear
- Tree updates reflect progress

**Priority:** High
**Type:** End-to-End, Manual

---

## 6. Unit Testing

### 6.1 State Management Tests

#### Test Suite: BlessingStateManager
```csharp
// PantheonWars.Tests/GUI/State/BlessingStateManagerTests.cs

[TestClass]
public class BlessingStateManagerTests
{
    [TestMethod]
    public void ProcessEvent_BlessingSelected_UpdatesSelectedBlessing()
    {
        // Test that selecting a blessing updates state
    }

    [TestMethod]
    public void ProcessEvent_UnlockClicked_ValidatesPrerequisites()
    {
        // Test prerequisite validation
    }

    [TestMethod]
    public void ProcessEvent_UnlockClicked_ValidatesRank()
    {
        // Test rank requirement validation
    }

    [TestMethod]
    public void ProcessEvent_UnlockClicked_SendsNetworkRequest()
    {
        // Test network request is sent with correct data
    }

    [TestMethod]
    public void ProcessEvent_ScrollChanged_UpdatesScrollPosition()
    {
        // Test scroll position updates
    }

    [TestMethod]
    public void GetViewModel_ReturnsCorrectData()
    {
        // Test ViewModel transformation
    }

    [TestMethod]
    public void Reset_ClearsState()
    {
        // Test state reset functionality
    }
}
```

**Priority:** High
**Estimated Tests:** 15-20

---

#### Test Suite: ReligionStateManager
```csharp
// PantheonWars.Tests/GUI/State/ReligionStateManagerTests.cs

[TestClass]
public class ReligionStateManagerTests
{
    // Browse Tab Events
    [TestMethod]
    public void ProcessEvent_DeityFilterChanged_UpdatesFilter() { }

    [TestMethod]
    public void ProcessEvent_JoinReligionClicked_SendsNetworkRequest() { }

    // Info Tab Events
    [TestMethod]
    public void ProcessEvent_SaveDescriptionClicked_ValidatesDescription() { }

    [TestMethod]
    public void ProcessEvent_InviteClicked_ValidatesPlayerName() { }

    [TestMethod]
    public void ProcessEvent_KickConfirmed_RemovesMember() { }

    [TestMethod]
    public void ProcessEvent_BanConfirmed_BansAndRemovesMember() { }

    [TestMethod]
    public void ProcessEvent_UnbanClicked_RemovesFromBanList() { }

    [TestMethod]
    public void ProcessEvent_LeaveClicked_LeavesReligion() { }

    [TestMethod]
    public void ProcessEvent_DisbandConfirmed_DeleetsReligion() { }

    // Create Tab Events
    [TestMethod]
    public void ProcessEvent_SubmitClicked_ValidatesForm() { }

    [TestMethod]
    public void ProcessEvent_SubmitClicked_CreatesReligion() { }

    // Invites Tab Events
    [TestMethod]
    public void ProcessEvent_AcceptClicked_JoinsReligion() { }

    [TestMethod]
    public void ProcessEvent_DeclineClicked_RemovesInvitation() { }

    // Error Handling
    [TestMethod]
    public void ProcessEvent_NetworkError_SetsErrorState() { }

    [TestMethod]
    public void ProcessEvent_RetryRequested_RetriesLastAction() { }

    [TestMethod]
    public void ProcessEvent_DismissError_ClearsErrorState() { }

    // Sub-tab Navigation
    [TestMethod]
    public void ProcessEvent_TabChanged_UpdatesCurrentTab() { }

    // ViewModel Tests
    [TestMethod]
    public void GetViewModel_BrowseTab_ReturnsFilteredReligions() { }

    [TestMethod]
    public void GetViewModel_InfoTab_ReturnsReligionDetails() { }

    [TestMethod]
    public void GetViewModel_InvitesTab_ReturnsPendingInvitations() { }

    [TestMethod]
    public void GetViewModel_CreateTab_ReturnsFormState() { }
}
```

**Priority:** High
**Estimated Tests:** 30-40

---

#### Test Suite: GuiDialogManager
```csharp
// PantheonWars.Tests/GUI/State/GuiDialogManagerTests.cs

[TestClass]
public class GuiDialogManagerTests
{
    [TestMethod]
    public void Initialize_LoadsAllSubManagers() { }

    [TestMethod]
    public void ProcessBlessingEvent_DelegatesToBlessingManager() { }

    [TestMethod]
    public void ProcessReligionEvent_DelegatesToReligionManager() { }

    [TestMethod]
    public void ProcessCivilizationEvent_DelegatesToCivilizationManager() { }

    [TestMethod]
    public void GetViewModel_CombinesSubManagerViewModels() { }

    [TestMethod]
    public void Reset_ResetsAllSubManagers() { }
}
```

**Priority:** Medium
**Estimated Tests:** 8-10

---

### 6.2 ViewModel Tests

#### Test Suite: ViewModel Transformation
```csharp
// PantheonWars.Tests/GUI/Models/ViewModelTests.cs

[TestClass]
public class BlessingTabViewModelTests
{
    [TestMethod]
    public void Create_FromState_MapsAllProperties() { }

    [TestMethod]
    public void Create_WithNullBlessing_HandlesGracefully() { }

    [TestMethod]
    public void Create_ComputesUnlockableState() { }
}

[TestClass]
public class ReligionInfoViewModelTests
{
    [TestMethod]
    public void Create_FromState_MapsAllProperties() { }

    [TestMethod]
    public void Create_NonFounder_HidesFounderActions() { }

    [TestMethod]
    public void Create_Founder_ShowsFounderActions() { }
}
```

**Priority:** Medium
**Estimated Tests:** 10-15

---

### 6.3 Validation Logic Tests

#### Test Suite: Validation
```csharp
// PantheonWars.Tests/GUI/Validation/ValidationTests.cs

[TestClass]
public class BlessingValidationTests
{
    [TestMethod]
    public void CanUnlock_WithValidPrerequisites_ReturnsTrue() { }

    [TestMethod]
    public void CanUnlock_WithMissingPrerequisites_ReturnsFalse() { }

    [TestMethod]
    public void CanUnlock_WithInsufficientRank_ReturnsFalse() { }

    [TestMethod]
    public void CanUnlock_AlreadyUnlocked_ReturnsFalse() { }
}

[TestClass]
public class ReligionValidationTests
{
    [TestMethod]
    public void ValidateReligionName_Valid_ReturnsTrue() { }

    [TestMethod]
    public void ValidateReligionName_Empty_ReturnsFalse() { }

    [TestMethod]
    public void ValidateReligionName_TooShort_ReturnsFalse() { }

    [TestMethod]
    public void ValidateReligionName_TooLong_ReturnsFalse() { }

    [TestMethod]
    public void ValidateReligionName_InvalidCharacters_ReturnsFalse() { }

    [TestMethod]
    public void CanEditDescription_Founder_ReturnsTrue() { }

    [TestMethod]
    public void CanEditDescription_Member_ReturnsFalse() { }

    [TestMethod]
    public void CanKickMember_Founder_TargetNotSelf_ReturnsTrue() { }

    [TestMethod]
    public void CanKickMember_Founder_TargetIsSelf_ReturnsFalse() { }

    [TestMethod]
    public void CanKickMember_Member_ReturnsFalse() { }
}
```

**Priority:** High
**Estimated Tests:** 15-20

---

### 6.4 Utility Tests

#### Test Suite: ColorPalette (Already exists - expand)
```csharp
// PantheonWars.Tests/GUI/UI/Utilities/ColorPaletteTests.cs

[TestClass]
public class ColorPaletteTests
{
    [TestMethod]
    public void Lighten_IncreasesValue() { }

    [TestMethod]
    public void Darken_DecreasesValue() { }

    [TestMethod]
    public void Lighten_MaxValue_ClampedAt1() { }

    [TestMethod]
    public void Darken_MinValue_ClampedAt0() { }
}
```

**Priority:** Low (already exists)
**Estimated Tests:** 4-6

---

#### Test Suite: DeityHelper (Already exists - expand)
```csharp
// PantheonWars.Tests/GUI/UI/Utilities/DeityHelperTests.cs

[TestClass]
public class DeityHelperTests
{
    [TestMethod]
    public void GetDeityColor_Khoras_ReturnsRed() { }

    [TestMethod]
    public void GetDeityColor_Lysa_ReturnsBlue() { }

    [TestMethod]
    public void GetDeityColor_Aethra_ReturnsGreen() { }

    [TestMethod]
    public void GetDeityColor_Gaia_ReturnsBrown() { }

    [TestMethod]
    public void GetDeityName_ReturnsCorrectName() { }
}
```

**Priority:** Low (already exists)
**Estimated Tests:** 5-8

---

#### Test Suite: TextRenderer
```csharp
// PantheonWars.Tests/GUI/UI/Utilities/TextRendererTests.cs

[TestClass]
public class TextRendererTests
{
    [TestMethod]
    public void WrapText_ShortText_NoWrapping() { }

    [TestMethod]
    public void WrapText_LongText_WrapsAtWidth() { }

    [TestMethod]
    public void MeasureText_ReturnsCorrectSize() { }

    [TestMethod]
    public void WrapText_WithNewlines_PreservesNewlines() { }
}
```

**Priority:** Medium
**Estimated Tests:** 5-8

---

### 6.5 Event Tests

#### Test Suite: Event Creation
```csharp
// PantheonWars.Tests/GUI/Events/EventTests.cs

[TestClass]
public class BlessingEventTests
{
    [TestMethod]
    public void UnlockClicked_CreatesEventWithBlessingId() { }

    [TestMethod]
    public void BlessingSelected_CreatesEventWithBlessingId() { }
}

[TestClass]
public class ReligionEventTests
{
    [TestMethod]
    public void InviteClicked_CreatesEventWithPlayerName() { }

    [TestMethod]
    public void KickConfirmed_CreatesEventWithPlayerId() { }

    [TestMethod]
    public void SubmitClicked_CreatesEventWithFormData() { }
}
```

**Priority:** Low
**Estimated Tests:** 10-15

---

## 7. Integration Testing

### 7.1 State Flow Tests

#### Test Case: INT-001 - Blessing Unlock Flow
**Objective:** Test complete blessing unlock flow
**Components:** BlessingStateManager, Network layer, State updates
**Steps:**
1. Mock network layer
2. Create state manager
3. Trigger unlock event
4. Verify network call
5. Simulate server response
6. Verify state updated

**Expected Results:**
- Event processed correctly
- Network request sent with correct data
- State updates reflect success
- ViewModel reflects new state

**Priority:** High
**Type:** Integration, Automated

---

#### Test Case: INT-002 - Religion Creation Flow
**Objective:** Test complete religion creation flow
**Components:** ReligionStateManager, Form validation, Network layer
**Steps:**
1. Create state manager
2. Update form fields via events
3. Submit form
4. Verify validation
5. Verify network call
6. Simulate success response
7. Verify state transition

**Expected Results:**
- Form validation works
- Network call made
- State transitions to Info tab
- Religion data populated

**Priority:** High
**Type:** Integration, Automated

---

#### Test Case: INT-003 - Error Recovery Flow
**Objective:** Test error handling and retry
**Components:** State managers, Error state, Network retry
**Steps:**
1. Trigger action
2. Simulate network failure
3. Verify error state set
4. Trigger retry
5. Simulate success
6. Verify error cleared

**Expected Results:**
- Error state set correctly
- Retry repeats operation
- Success clears error
- UI reflects all states

**Priority:** High
**Type:** Integration, Automated

---

### 7.2 Component Interaction Tests

#### Test Case: INT-004 - Tab State Preservation
**Objective:** Verify state preserved when switching tabs
**Components:** GuiDialogManager, Tab renderers, State managers
**Steps:**
1. Open religion browse, filter by deity
2. Switch to blessings tab
3. Select a blessing
4. Switch back to religion tab
5. Verify deity filter still applied
6. Switch back to blessings
7. Verify blessing still selected

**Expected Results:**
- All tab states preserved
- No data loss on tab switch
- ViewModels reflect saved state

**Priority:** High
**Type:** Integration, Manual

---

#### Test Case: INT-005 - Concurrent State Updates
**Objective:** Test handling of rapid state changes
**Components:** State managers, Event queue
**Steps:**
1. Trigger multiple events rapidly
2. Observe state updates
3. Verify final state is correct

**Expected Results:**
- No race conditions
- Events processed in order
- Final state is consistent
- No crashes or exceptions

**Priority:** Medium
**Type:** Integration, Automated

---

## 8. UI/UX Testing

### 8.1 Usability Tests

#### Test Case: UX-001 - First-Time User Experience
**Objective:** Evaluate ease of use for new users
**Method:** User observation study
**Participants:** 5-10 users unfamiliar with mod
**Tasks:**
1. Open the GUI
2. Browse religions
3. Join a religion
4. View blessings
5. Unlock a blessing

**Metrics:**
- Time to complete each task
- Number of errors/confusion points
- User satisfaction rating
- Qualitative feedback

**Priority:** Medium
**Type:** Usability, Manual

---

#### Test Case: UX-002 - Visual Clarity
**Objective:** Verify UI elements are clear and understandable
**Method:** Expert review + user feedback
**Areas to evaluate:**
- Button labels are clear
- Icons are recognizable
- Text is readable at all sizes
- Color coding is consistent and intuitive
- Hierarchy is clear (headings, sections)
- Important actions are prominent

**Expected Results:**
- 90%+ users understand UI without instruction
- No confusion about what actions do
- Visual hierarchy guides user attention

**Priority:** Medium
**Type:** UX, Manual

---

#### Test Case: UX-003 - Error Message Clarity
**Objective:** Verify error messages are helpful
**Method:** Review all error messages
**Steps:**
1. Trigger each type of error
2. Read error message
3. Evaluate: Is cause clear? Is solution provided?

**Expected Results:**
- Error messages explain what went wrong
- Messages suggest how to fix
- Technical jargon minimized
- Tone is helpful, not blaming

**Priority:** Medium
**Type:** UX, Manual

---

#### Test Case: UX-004 - Confirmation Dialog Clarity
**Objective:** Verify confirmations are clear about consequences
**Method:** Review all confirmation dialogs
**Areas to check:**
- Disband religion (permanent, affects all members)
- Kick member
- Ban member
- Leave religion

**Expected Results:**
- Consequences clearly explained
- Destructive actions have clear warnings
- User can make informed decision

**Priority:** High
**Type:** UX, Manual

---

### 8.2 Responsiveness Tests

#### Test Case: RESP-001 - Resolution Support (1920x1080)
**Objective:** Verify UI works at 1920x1080
**Steps:**
1. Set game resolution to 1920x1080
2. Open GUI
3. Test all features
4. Check for layout issues

**Expected Results:**
- Dialog fits on screen
- No element overflow
- Scrolling works where needed
- Text is readable
- Buttons are clickable

**Priority:** High
**Type:** Responsive, Manual

---

#### Test Case: RESP-002 - Resolution Support (2560x1440)
**Objective:** Verify UI works at 2560x1440
**Steps:** Same as RESP-001

**Expected Results:** Same as RESP-001

**Priority:** High
**Type:** Responsive, Manual

---

#### Test Case: RESP-003 - Resolution Support (3840x2160 / 4K)
**Objective:** Verify UI works at 4K resolution
**Steps:** Same as RESP-001

**Expected Results:**
- Same as RESP-001
- Text remains readable (not too small)
- UI scales appropriately

**Priority:** Medium
**Type:** Responsive, Manual

---

#### Test Case: RESP-004 - Resolution Support (Low Resolution)
**Objective:** Verify UI works at minimum supported resolution
**Steps:**
1. Set to lowest playable resolution (e.g., 1280x720)
2. Open GUI
3. Test features

**Expected Results:**
- Dialog may be large relative to screen
- Critical elements still accessible
- Scrolling compensates for space
- No critical functionality lost

**Priority:** Medium
**Type:** Responsive, Manual

---

#### Test Case: RESP-005 - Window Resizing
**Objective:** Verify GUI adapts to window resize
**Steps:**
1. Open GUI
2. Resize game window
3. Observe GUI layout
4. Test functionality

**Expected Results:**
- Layout adapts to new size
- Dialog repositions if needed
- Proportions maintained
- No visual glitches

**Priority:** Low
**Type:** Responsive, Manual

---

### 8.3 Accessibility Tests

#### Test Case: ACC-001 - Text Readability
**Objective:** Verify text is readable
**Criteria:**
- Font size is adequate (not too small)
- Font is legible (clear, not decorative)
- Text contrast is sufficient (4.5:1 for normal text)
- Important text is not color-only coded

**Expected Results:**
- All text passes readability criteria
- Users with mild vision impairment can read

**Priority:** Medium
**Type:** Accessibility, Manual

---

#### Test Case: ACC-002 - Color Blindness
**Objective:** Verify UI works for color-blind users
**Method:** Use color blindness simulator
**Types to test:**
- Deuteranopia (red-green)
- Protanopia (red-green)
- Tritanopia (blue-yellow)

**Expected Results:**
- UI states distinguishable without color
- Deity colors have additional indicators if needed
- Blessing states use shape/icon, not just color

**Priority:** Low
**Type:** Accessibility, Manual

---

#### Test Case: ACC-003 - Keyboard Navigation
**Objective:** Verify keyboard navigation works
**Steps:**
1. Open GUI
2. Attempt to navigate using Tab
3. Attempt to activate using Enter/Space
4. Attempt to close using Escape

**Expected Results:**
- Tab cycles through interactive elements
- Enter/Space activates buttons
- Escape closes dialog
- Focus indicator is visible

**Priority:** Low
**Type:** Accessibility, Manual

---

## 9. Performance Testing

### 9.1 Rendering Performance

#### Test Case: PERF-001 - Frame Rate Impact (Idle)
**Objective:** Measure FPS impact when GUI open but idle
**Method:** FPS monitoring tool
**Steps:**
1. Measure baseline FPS (GUI closed)
2. Open GUI
3. Leave idle for 60 seconds
4. Measure FPS

**Expected Results:**
- FPS drop is < 5 FPS
- No gradual performance degradation
- CPU usage is minimal

**Priority:** High
**Type:** Performance, Automated/Manual

---

#### Test Case: PERF-002 - Frame Rate Impact (Active Use)
**Objective:** Measure FPS during active GUI use
**Steps:**
1. Open GUI
2. Rapidly switch tabs
3. Scroll through lists
4. Open/close overlays
5. Measure FPS throughout

**Expected Results:**
- FPS drop is < 10 FPS during active use
- No stuttering or frame hitches
- Smooth animations

**Priority:** High
**Type:** Performance, Manual

---

#### Test Case: PERF-003 - Large List Rendering
**Objective:** Test performance with large data sets
**Setup:** Create test data with 100+ religions, 1000+ blessing nodes
**Steps:**
1. Open religion browse with 100+ religions
2. Scroll through list
3. Measure FPS and scroll smoothness
4. Repeat for blessing tree

**Expected Results:**
- Scrolling is smooth (60 FPS)
- No lag when rendering large lists
- Virtual scrolling works efficiently

**Priority:** Medium
**Type:** Performance, Manual

---

#### Test Case: PERF-004 - Memory Usage
**Objective:** Measure memory consumption
**Method:** Memory profiling tool
**Steps:**
1. Measure baseline memory (GUI closed)
2. Open GUI
3. Navigate all tabs
4. Close GUI
5. Measure memory after close

**Expected Results:**
- Memory increase is < 50 MB
- Memory is released on close (no leaks)
- No gradual memory growth during use

**Priority:** High
**Type:** Performance, Automated

---

#### Test Case: PERF-005 - Open/Close Speed
**Objective:** Measure GUI open/close time
**Steps:**
1. Time opening GUI (Shift+G to visible)
2. Time closing GUI (click to hidden)
3. Repeat 10 times

**Expected Results:**
- Open time is < 200ms
- Close time is < 100ms
- Consistent timing (no outliers)

**Priority:** Medium
**Type:** Performance, Automated

---

### 9.2 Network Performance

#### Test Case: PERF-006 - Network Request Timeout
**Objective:** Verify timeout handling
**Steps:**
1. Simulate slow network
2. Trigger action requiring network
3. Observe behavior

**Expected Results:**
- Request times out after reasonable period (5-10s)
- Timeout error is displayed
- UI remains responsive during wait
- Retry option available

**Priority:** Medium
**Type:** Performance, Manual

---

#### Test Case: PERF-007 - Concurrent Requests
**Objective:** Test multiple simultaneous requests
**Steps:**
1. Trigger multiple network actions rapidly
2. Observe request handling

**Expected Results:**
- Requests are queued or parallelized appropriately
- No race conditions
- All requests complete
- UI reflects all responses

**Priority:** Medium
**Type:** Performance, Automated

---

## 10. Edge Cases and Error Handling

### 10.1 Edge Cases

#### Test Case: EDGE-001 - Empty States
**Objective:** Test UI with no data
**Scenarios:**
1. No religions exist (browse tab)
2. Player not in religion (info tab)
3. No pending invitations
4. No blessed nodes unlocked
5. Religion with no members

**Expected Results:**
- Appropriate empty state messages
- No crashes or blank screens
- UI still functional
- Clear guidance for next steps

**Priority:** High
**Type:** Edge Case, Manual

---

#### Test Case: EDGE-002 - Maximum Values
**Objective:** Test UI with maximum data
**Scenarios:**
1. Religion with 1000+ members
2. 100+ pending invitations
3. All blessings unlocked
4. Maximum prestige/favor rank
5. Extremely long religion names/descriptions

**Expected Results:**
- UI handles large data sets
- Scrolling works for long lists
- No performance degradation
- Text truncation works correctly
- No UI layout breaking

**Priority:** Medium
**Type:** Edge Case, Manual

---

#### Test Case: EDGE-003 - Special Characters
**Objective:** Test input handling with special chars
**Inputs to test:**
- Unicode characters (emoji, foreign languages)
- Special symbols (!@#$%^&*())
- SQL injection attempts
- HTML/script tags
- Very long strings

**Expected Results:**
- Input validation rejects invalid chars
- Safe characters are accepted
- No injection vulnerabilities
- UI doesn't break with long strings
- Backend validates server-side

**Priority:** High
**Type:** Edge Case, Security

---

#### Test Case: EDGE-004 - Rapid Actions
**Objective:** Test rapid repeated actions
**Steps:**
1. Click button rapidly 20 times
2. Scroll rapidly back and forth
3. Switch tabs rapidly

**Expected Results:**
- No double-processing of actions
- UI remains responsive
- No crashes or freezes
- State remains consistent

**Priority:** Medium
**Type:** Edge Case, Manual

---

#### Test Case: EDGE-005 - State Conflicts
**Objective:** Test conflicting state changes
**Scenarios:**
1. Religion disbanded while viewing info
2. Kicked from religion while in UI
3. Blessing unlocked by religion while viewing
4. Invited to religion while browsing

**Expected Results:**
- UI detects state change
- Appropriate notification shown
- UI updates or redirects
- No crashes or stale data

**Priority:** High
**Type:** Edge Case, Manual

---

### 10.2 Error Handling

#### Test Case: ERR-001 - Network Errors
**Objective:** Test network error handling
**Error types:**
1. Connection timeout
2. Server unavailable (503)
3. Server error (500)
4. Forbidden (403)
5. Not found (404)
6. Bad request (400)

**Expected Results:**
- User-friendly error message for each type
- Retry option where applicable
- UI remains functional
- No data corruption

**Priority:** High
**Type:** Error Handling, Manual

---

#### Test Case: ERR-002 - Validation Errors
**Objective:** Test client-side validation
**Scenarios:**
1. Empty required fields
2. Invalid format (too long, wrong chars)
3. Failed business rules (insufficient rank, prereqs not met)

**Expected Results:**
- Inline validation messages
- Form submit disabled when invalid
- Clear explanation of requirements
- Fields highlighted with errors

**Priority:** High
**Type:** Error Handling, Manual

---

#### Test Case: ERR-003 - Concurrent Modification
**Objective:** Test handling of simultaneous changes
**Scenario:** Two players try to modify same data
**Example:** Founder disbands religion while member tries to leave

**Expected Results:**
- Optimistic locking or conflict detection
- Clear error message explaining conflict
- UI refreshes with current state
- No data corruption

**Priority:** Medium
**Type:** Error Handling, Manual

---

#### Test Case: ERR-004 - Asset Loading Failures
**Objective:** Test missing assets
**Steps:**
1. Temporarily remove deity icon
2. Open GUI
3. Navigate to pages using icon

**Expected Results:**
- Fallback icon or placeholder shown
- No crashes
- Error logged but not shown to user
- Rest of UI functions normally

**Priority:** Low
**Type:** Error Handling, Manual

---

#### Test Case: ERR-005 - Malformed Server Response
**Objective:** Test handling of bad server data
**Steps:**
1. Mock server responses with invalid data
2. Trigger various actions
3. Observe error handling

**Expected Results:**
- Invalid data is detected
- Default/safe values used where possible
- Error logged
- User sees generic error message
- No crashes

**Priority:** Medium
**Type:** Error Handling, Automated

---

## 11. Acceptance Criteria

### 11.1 Functional Completeness
- ✅ All main features work as specified
- ✅ All tabs and sub-tabs are accessible
- ✅ All buttons and inputs are functional
- ✅ All workflows can be completed
- ✅ Data persists correctly

### 11.2 Quality Standards
- ✅ No critical bugs (crashes, data loss)
- ✅ No high-priority bugs in release build
- ✅ Medium bugs have workarounds
- ✅ Performance meets targets (FPS, memory)
- ✅ Visual polish is complete (no placeholder text/colors)

### 11.3 Test Coverage
- ✅ 80%+ code coverage for business logic
- ✅ All state managers have unit tests
- ✅ All critical workflows tested end-to-end
- ✅ All edge cases documented and tested
- ✅ All error paths tested

### 11.4 Documentation
- ✅ User guide exists (how to use GUI)
- ✅ Developer documentation exists (architecture, extending)
- ✅ Test documentation complete
- ✅ Known issues documented

### 11.5 User Experience
- ✅ 90%+ user satisfaction rating
- ✅ New users can complete basic tasks without help
- ✅ UI is responsive at common resolutions
- ✅ Error messages are helpful
- ✅ No confusing or unclear UI elements

---

## 12. Test Schedule

### Phase 1: Unit Testing (Week 1-2)
**Focus:** State managers, validation, utilities
**Deliverables:**
- State manager test suites complete
- Validation test suites complete
- Utility test suites complete
- 80%+ coverage of target areas

---

### Phase 2: Integration Testing (Week 2-3)
**Focus:** Component interaction, state flow
**Deliverables:**
- State flow tests complete
- Event processing tests complete
- ViewModel transformation tests complete
- Integration test suite passing

---

### Phase 3: Functional Testing (Week 3-4)
**Focus:** Manual testing of all features
**Deliverables:**
- All functional test cases executed
- Bug reports filed for issues found
- Critical and high priority bugs fixed
- Test results documented

---

### Phase 4: UI/UX Testing (Week 4-5)
**Focus:** Usability, responsiveness, accessibility
**Deliverables:**
- Usability testing with 5+ users
- Responsiveness tested at all resolutions
- Accessibility audit complete
- UX issues addressed

---

### Phase 5: Performance Testing (Week 5)
**Focus:** FPS, memory, network performance
**Deliverables:**
- Performance benchmarks established
- Performance test cases executed
- Performance issues identified and fixed
- Performance acceptance criteria met

---

### Phase 6: Edge Cases and Error Handling (Week 5-6)
**Focus:** Edge cases, error scenarios
**Deliverables:**
- All edge case test cases executed
- All error handling test cases executed
- Edge case bugs fixed
- Error handling improved

---

### Phase 7: Regression Testing (Week 6)
**Focus:** Ensure fixes didn't break anything
**Deliverables:**
- All test suites re-run
- No new regressions
- All acceptance criteria met
- Test completion report

---

### Phase 8: User Acceptance Testing (Week 7)
**Focus:** Final validation with real users
**Deliverables:**
- Beta testing with 10+ users
- Feedback collected and prioritized
- Critical feedback addressed
- Sign-off for release

---

## Test Metrics and Reporting

### Metrics to Track
- **Test Coverage:** Code coverage percentage
- **Defect Density:** Bugs per 1000 lines of code
- **Test Pass Rate:** Percentage of tests passing
- **Bug Status:** Open/In Progress/Resolved/Closed
- **Bug Severity Distribution:** Critical/High/Medium/Low
- **Test Execution Rate:** Tests executed vs. planned
- **Defect Detection Rate:** Bugs found per phase
- **User Satisfaction Score:** From usability testing

### Reporting Cadence
- **Daily:** Standup with test progress update
- **Weekly:** Test status report with metrics
- **Phase End:** Comprehensive phase summary
- **Project End:** Final test completion report

---

## Risks and Mitigation

### Risk: ImGui Testing Limitations
**Impact:** High
**Likelihood:** High
**Mitigation:**
- Mark renderers as [ExcludeFromCodeCoverage]
- Focus testing on state management and event processing
- Use manual testing for visual verification
- Create visual regression tests if tooling allows

### Risk: Network Testing Complexity
**Impact:** Medium
**Likelihood:** Medium
**Mitigation:**
- Mock network layer in tests
- Use fake data providers in DEBUG mode
- Create network testing utilities
- Test with actual backend in integration phase

### Risk: Performance Issues Found Late
**Impact:** High
**Likelihood:** Low
**Mitigation:**
- Include performance testing early (Phase 5)
- Profile regularly during development
- Set performance budgets upfront
- Monitor FPS during manual testing

### Risk: User Feedback Requires Major Changes
**Impact:** High
**Likelihood:** Medium
**Mitigation:**
- Conduct usability testing early (Phase 4)
- Iterate on feedback quickly
- Prioritize critical UX issues
- Have buffer time for changes

---

## Appendix A: Test Data Setup

### Fake Data Providers (DEBUG Mode)
The mod includes fake data providers for testing:
- `FakeReligionDataProvider`: Generates test religions
- `FakeBlessingDataProvider`: Generates blessing trees
- `FakePlayerDataProvider`: Generates test players

### Test Accounts
Create test accounts representing:
- New player (no religion, no blessings)
- Established player (in religion, some blessings)
- Religion founder (owns religion)
- Religion member (in religion, not founder)
- High-rank player (max favor/prestige)

### Test Religions
Create test religions:
- Small (2-3 members)
- Medium (10-20 members)
- Large (50+ members)
- Public religion
- Private religion
- Each deity represented

---

## Appendix B: Bug Severity Definitions

### Critical
- Application crash
- Data loss or corruption
- Complete feature failure
- Security vulnerability
- Blocks further testing

### High
- Major feature not working as specified
- Serious performance issue
- Error with no workaround
- Significant UI breaking issue
- Incorrect data displayed

### Medium
- Minor feature not working
- Cosmetic issue affecting usability
- Error with workaround available
- Performance issue with minor impact
- Confusing UI element

### Low
- Cosmetic issue not affecting usability
- Minor text/formatting issue
- Enhancement request
- Documentation error
- Rare edge case

---

## Appendix C: Test Tools

### Recommended Tools
- **Unit Testing:** NUnit or xUnit
- **Mocking:** Moq, NSubstitute
- **Coverage:** Coverlet, dotCover
- **Performance:** Unity Profiler, dotTrace
- **Memory:** dotMemory
- **Visual Testing:** Manual inspection
- **Bug Tracking:** GitHub Issues, Jira

---

## Sign-Off

**Test Plan Approved By:**

- **Developer Lead:** __________________ Date: __________
- **QA Lead:** __________________ Date: __________
- **Product Owner:** __________________ Date: __________

---

**Document Version:** 1.0
**Last Updated:** 2025-12-09
**Next Review:** After Phase 3 completion
