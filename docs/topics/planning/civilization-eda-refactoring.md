# Civilization System EDA Refactoring Plan

## Overview

Refactor the Civilization system to follow the Event-Driven Architecture (EDA) pattern established in the Religion tab.
The system currently uses callbacks in renderers but needs to adopt the proven State → ViewModel → Renderer → Events →
ProcessEvents pattern.

## Current State

**Working Components:**

- ✅ State structure exists (`CivilizationTabState` + 5 sub-states)
- ✅ `CivilizationStateManager` with request methods
- ✅ `IUiService` has civilization methods
- ✅ Network events fire in `PantheonWarsNetworkClient`

**Problems:**

- ❌ No event definitions for user interactions
- ❌ No ViewModels (renderers read state directly)
- ❌ Renderers use callbacks instead of returning events
- ❌ Network event handlers in `GuiDialogHandlers.cs` instead of `StateManager`
- ❌ No event processing methods in `StateManager`

## EDA Pattern (Reference: Religion Tab)

```
State → ViewModel (immutable) → Renderer (pure) → RenderResult (events) → ProcessEvents (side effects) → State
```

**Key Files to Follow:**

- Events: `GUI/Events/ReligionBrowseEvent.cs` (abstract record pattern)
- ViewModel: `GUI/Models/Religion/Browse/ReligionBrowseViewModel.cs` (readonly struct)
- RenderResult: `GUI/Models/Religion/Browse/ReligionBrowseRenderResult.cs`
- Renderer: Pure function returning `RenderResult`
- Manager: `GUI/Managers/ReligionStateManager.cs` (ProcessBrowseEvents method)

---

## Implementation Steps

### Step 1: Create Event Definitions (6 files)

Create discriminated union events using abstract records:

**Files to Create:**

1. `GUI/Events/CivilizationBrowseEvent.cs`
    - `DeityFilterChanged(string NewFilter)`
    - `ScrollChanged(float NewScrollY)`
    - `ViewDetailsClicked(string CivId)`
    - `RefreshClicked()`
    - `DeityDropdownToggled(bool IsOpen)`

2. `GUI/Events/CivilizationDetailEvent.cs`
    - `MemberScrollChanged(float NewScrollY)`
    - `BackToBrowseClicked()`
    - `RequestToJoinClicked(string CivId)`

3. `GUI/Events/CivilizationInfoEvent.cs`
    - `ScrollChanged(float NewScrollY)`
    - `MemberScrollChanged(float NewScrollY)`
    - `InviteReligionNameChanged(string Text)`
    - `InviteReligionClicked(string ReligionName)`
    - `LeaveClicked()`
    - `DisbandOpen()`, `DisbandConfirm()`, `DisbandCancel()`
    - `KickOpen(string ReligionId, string ReligionName)`
    - `KickConfirm(string ReligionId)`, `KickCancel()`

4. `GUI/Events/CivilizationInvitesEvent.cs`
    - `ScrollChanged(float NewScrollY)`
    - `AcceptInviteClicked(string InviteId)`
    - `DeclineInviteClicked(string InviteId)`

5. `GUI/Events/CivilizationCreateEvent.cs`
    - `NameChanged(string NewName)`
    - `SubmitClicked()`
    - `ClearClicked()`

6. `GUI/Events/CivilizationSubTabEvent.cs`
    - `TabChanged(CivilizationSubTab NewSubTab)`
    - `DismissActionError()`
    - `DismissContextError(CivilizationSubTab SubTab)`
    - `RetryRequested(CivilizationSubTab SubTab)`

**Pattern:**

```csharp
public abstract record CivilizationBrowseEvent
{
    public sealed record DeityFilterChanged(string NewFilter) : CivilizationBrowseEvent;
    public sealed record ScrollChanged(float NewScrollY) : CivilizationBrowseEvent;
    // ... more events
}
```

### Step 2: Create ViewModels (6 files)

Create immutable ViewModels as readonly structs:

**Files to Create:**

1. `GUI/Models/Civilization/Browse/CivilizationBrowseViewModel.cs`
    - Properties: deityFilters, currentDeityFilter, civilizations, isLoading, scrollY, isDeityDropdownOpen,
      userHasReligion, userInCivilization, x, y, width, height
    - Helper: `GetCurrentFilterIndex()`

2. `GUI/Models/Civilization/Detail/CivilizationDetailViewModel.cs`
    - Properties: isLoading, civId, civName, founderName, founderReligionName, memberReligions, createdDate,
      memberScrollY, canRequestToJoin, x, y, width, height
    - Helpers: `MemberCount`, `IsFull`

3. `GUI/Models/Civilization/Info/CivilizationInfoViewModel.cs`
    - Properties: isLoading, hasCivilization, civId, civName, founderName, isFounder, memberReligions,
      inviteReligionName, showDisbandConfirm, kickConfirmReligionId, scrollY, memberScrollY, x, y, width, height
    - Helpers: `CanInvite`, `CanDisband`, `CanLeave`, `IsKickConfirmOpen`

4. `GUI/Models/Civilization/Invites/CivilizationInvitesViewModel.cs`
    - Properties: invites, isLoading, scrollY, x, y, width, height
    - Helper: `HasInvites`

5. `GUI/Models/Civilization/Create/CivilizationCreateViewModel.cs`
    - Properties: civilizationName, errorMessage, userIsReligionFounder, userInCivilization, x, y, width, height
    - Helper: `CanCreate`

6. `GUI/Models/Civilization/Tab/CivilizationTabViewModel.cs`
    - Properties: currentSubTab, lastActionError, browseError, infoError, invitesError, isViewingDetails, hasReligion,
      hasCivilization, x, y, width, height
    - Helpers: `ShowInvitesTab`, `ShowCreateTab`

**Pattern:**

```csharp
public readonly struct CivilizationBrowseViewModel(
    string[] deityFilters,
    string currentDeityFilter,
    // ... more parameters
    float x, float y, float width, float height)
{
    public string[] DeityFilters { get; } = deityFilters;
    public string CurrentDeityFilter { get; } = currentDeityFilter;
    // ... more properties

    // Helper methods (no side effects)
    public int GetCurrentFilterIndex() => Array.IndexOf(DeityFilters, CurrentDeityFilter);
}
```

### Step 3: Create RenderResults (6 files)

Create RenderResult structures to wrap events:

**Files to Create:**

1. `GUI/Models/Civilization/Browse/CivilizationBrowseRenderResult.cs`
2. `GUI/Models/Civilization/Detail/CivilizationDetailRenderResult.cs`
3. `GUI/Models/Civilization/Info/CivilizationInfoRenderResult.cs`
4. `GUI/Models/Civilization/Invites/CivilizationInvitesRenderResult.cs`
5. `GUI/Models/Civilization/Create/CivilizationCreateRenderResult.cs`
6. `GUI/Models/Civilization/Tab/CivilizationTabRenderResult.cs`

**Pattern:**

```csharp
public readonly struct CivilizationBrowseRenderResult(
    IReadOnlyList<CivilizationBrowseEvent> events,
    float renderedHeight)
{
    public IReadOnlyList<CivilizationBrowseEvent> Events { get; } = events;
    public float RenderedHeight { get; } = renderedHeight;
}
```

### Step 4: Refactor Renderers to Pure Functions (6 files)

Convert existing renderers to pure functions that return `RenderResult`:

**Files to Modify:**

1. `GUI/UI/Renderers/Civilization/CivilizationTabRenderer.cs`
    - Change signature:
      `public static CivilizationTabRenderResult Draw(CivilizationTabViewModel vm, ImDrawListPtr drawList, ICoreClientAPI api)`
    - Collect events in `List<CivilizationSubTabEvent>`
    - Return `new CivilizationTabRenderResult(events, height)`

2. `GUI/UI/Renderers/Civilization/CivilizationBrowseRenderer.cs`
    - Change signature:
      `public static CivilizationBrowseRenderResult Draw(CivilizationBrowseViewModel vm, ImDrawListPtr drawList)`
    - Replace callbacks with event collection
    - Example: `if (ButtonClicked) events.Add(new CivilizationBrowseEvent.RefreshClicked());`

3. `GUI/UI/Renderers/Civilization/CivilizationDetailViewRenderer.cs` → Rename to `CivilizationDetailRenderer.cs`
    - Change signature:
      `public static CivilizationDetailRenderResult Draw(CivilizationDetailViewModel vm, ImDrawListPtr drawList)`

4. `GUI/UI/Renderers/Civilization/CivilizationManageRenderer.cs` → Rename to `CivilizationInfoRenderer.cs`
    - Change signature:
      `public static CivilizationInfoRenderResult Draw(CivilizationInfoViewModel vm, ImDrawListPtr drawList)`

5. `GUI/UI/Renderers/Civilization/CivilizationInvitesRenderer.cs`
    - Change signature:
      `public static CivilizationInvitesRenderResult Draw(CivilizationInvitesViewModel vm, ImDrawListPtr drawList)`

6. `GUI/UI/Renderers/Civilization/CivilizationCreateRenderer.cs`
    - Change signature:
      `public static CivilizationCreateRenderResult Draw(CivilizationCreateViewModel vm, ImDrawListPtr drawList)`

**Key Changes:**

- Remove manager/API parameters (renderers become pure)
- Read from ViewModel properties instead of state
- Replace callback invocations with event collection
- Remove side effects (sounds, network calls, state mutations)
- Return RenderResult with collected events

### Step 5: Add Event Processing to StateManager

Modify `GUI/Managers/CivilizationStateManager.cs`:

**Add Event Processor Methods:**

```csharp
// Main orchestrator (entry point from GuiDialog)
public void DrawCivilizationTab(float x, float y, float width, float height)
{
    // Build ViewModel → Render → Process Events
    var tabVm = new CivilizationTabViewModel(/* ... */);
    var tabResult = CivilizationTabRenderer.Draw(tabVm, drawList, _coreClientApi);
    ProcessTabEvents(tabResult.Events);

    // Route to sub-renderer based on current tab
    switch (State.CurrentSubTab)
    {
        case CivilizationSubTab.Browse:
            DrawCivilizationBrowse(/* ... */);
            break;
        // ... other tabs
    }
}

// Event processors (handle side effects)
private void ProcessTabEvents(IReadOnlyList<CivilizationSubTabEvent> events)
{
    foreach (var evt in events)
    {
        switch (evt)
        {
            case CivilizationSubTabEvent.TabChanged tc:
                State.CurrentSubTab = tc.NewSubTab;
                // Trigger refresh if needed
                break;
            // ... handle other events
        }
    }
}

private void ProcessBrowseEvents(IReadOnlyList<CivilizationBrowseEvent> events)
{
    foreach (var evt in events)
    {
        switch (evt)
        {
            case CivilizationBrowseEvent.DeityFilterChanged dfc:
                State.BrowseState.DeityFilter = dfc.NewFilter == "All" ? "" : dfc.NewFilter;
                RequestCivilizationList(State.BrowseState.DeityFilter);
                PlayClickSound();
                break;
            // ... handle other events
        }
    }
}

// Similar for: ProcessDetailEvents, ProcessInfoEvents, ProcessInvitesEvents, ProcessCreateEvents
```

**Sub-renderer orchestrators:**

```csharp
private void DrawCivilizationBrowse(float x, float y, float width, float height)
{
    // Check if viewing details (overlay mode)
    if (!string.IsNullOrEmpty(State.DetailState.ViewingCivilizationId))
    {
        DrawCivilizationDetail(x, y, width, height);
        return;
    }

    // Build ViewModel
    var vm = new CivilizationBrowseViewModel(/* map state to ViewModel */);

    // Render
    var result = CivilizationBrowseRenderer.Draw(vm, drawList);

    // Process events
    ProcessBrowseEvents(result.Events);
}

// Similar for: DrawCivilizationDetail, DrawCivilizationInfo, DrawCivilizationInvites, DrawCivilizationCreate
```

**Add Network Event Handlers:**

```csharp
public void OnCivilizationListReceived(CivilizationListResponsePacket packet)
{
    State.BrowseState.AllCivilizations = packet.Civilizations;
    State.BrowseState.IsLoading = false;
    State.BrowseState.ErrorMsg = null;
}

public void OnCivilizationInfoReceived(CivilizationInfoResponsePacket packet)
{
    UpdateCivilizationState(packet.Details);

    // Update loading flags based on context
    if (State.DetailState.ViewingCivilizationId == packet.Details?.CivId)
        State.DetailState.IsLoading = false;
    else
    {
        State.InfoState.IsLoading = false;
        State.InviteState.IsLoading = false;
    }
}

public void OnCivilizationActionCompleted(CivilizationActionResponsePacket packet)
{
    if (packet.Success)
    {
        PlaySuccessSound();
        RequestCivilizationList(State.BrowseState.DeityFilter);
        RequestCivilizationInfo();
    }
    else
    {
        PlayErrorSound();
        State.LastActionError = packet.Message;
    }
}
```

### Step 6: Wire Network Events to StateManager

Modify `GUI/GuiDialogHandlers.cs`:

**Change from:**

```csharp
private void OnCivilizationListReceived(CivilizationListResponsePacket packet)
{
    // Direct state updates
}
```

**Change to:**

```csharp
private void OnCivilizationListReceived(CivilizationListResponsePacket packet)
{
    _manager!.CivilizationManager.OnCivilizationListReceived(packet);
}
```

**Apply to all 3 network handlers:**

- `OnCivilizationListReceived`
- `OnCivilizationInfoReceived`
- `OnCivilizationActionCompleted`

### Step 7: Update State Classes (Minor Additions)

**Modify `GUI/State/CivilizationTabState.cs`:**

- Add `public string? CreateError { get; set; }`

**Modify `GUI/State/Civilization/InfoState.cs`:**

- Add `public float ScrollY { get; set; }`

**Modify `GUI/State/Civilization/DetailState.cs`:**

- Add `public float MemberScrollY { get; set; }`

**Modify `GUI/State/Civilization/BrowseState.cs`:**

- Add `public bool IsDeityFilterOpen { get; set; }`

### Step 8: Update GuiDialog Entry Point

Modify `GUI/GuiDialog.cs`:

**Change from:**

```csharp
// Old callback-based rendering
```

**Change to:**

```csharp
// Civilization tab
if (/* civilization tab active */)
{
    _manager.CivilizationManager.DrawCivilizationTab(x, y, width, height);
}
```

---

## Migration Order (Incremental)

Execute in this order to minimize risk:

1. **Create → Invites** (simplest tabs, good warm-up)
2. **Browse** (medium complexity, no detail overlay yet)
3. **Detail View** (overlay logic within Browse)
4. **Info/Manage** (most complex, multiple confirmation dialogs)
5. **Tab Header** (integrates all sub-tabs)
6. **Network Handlers** (move to StateManager)

Test thoroughly after each step before proceeding.

---

## Critical Files Summary

### New Files (18 total)

- **Events:** 6 files in `GUI/Events/Civilization*.cs`
- **ViewModels:** 6 files in `GUI/Models/Civilization/*/*.cs`
- **RenderResults:** 6 files in `GUI/Models/Civilization/*/*.cs`

### Modified Files (11 total)

- **Renderers:** 6 files in `GUI/UI/Renderers/Civilization/*.cs`
- **State:** 4 files in `GUI/State/Civilization*.cs`
- **Managers:** `GUI/Managers/CivilizationStateManager.cs` (add ~500 lines of event processors)
- **Handlers:** `GUI/GuiDialogHandlers.cs` (delegate to StateManager)
- **Dialog:** `GUI/GuiDialog.cs` (call orchestrator)

---

## Validation Checklist

After implementation, verify:

- ✅ All renderers are pure functions (no side effects)
- ✅ All ViewModels are immutable readonly structs
- ✅ All events use abstract record pattern
- ✅ All state mutations happen in Process*Events methods
- ✅ Network handlers delegate to StateManager
- ✅ Sounds play at correct times (in event processors, not renderers)
- ✅ Tab switching preserves state correctly
- ✅ Error handling works (action errors, context errors)
- ✅ Confirmation dialogs work (disband, kick)
- ✅ All CRUD operations function (create, invite, accept, leave, kick, disband)

---

## Reference Files (Religion Tab - Gold Standard)

Study these before implementing:

- `GUI/Events/ReligionBrowseEvent.cs` - Event pattern
- `GUI/Models/Religion/Browse/ReligionBrowseViewModel.cs` - ViewModel pattern
- `GUI/Models/Religion/Browse/ReligionBrowseRenderResult.cs` - RenderResult pattern
- `GUI/UI/Renderers/Religion/ReligionBrowseRenderer.cs` - Pure renderer pattern
- `GUI/Managers/ReligionStateManager.cs` - Event processing pattern

---

## Notes

- **Deity diversity:** Civilizations require different deities per religion (max 1 per deity type)
- **Founder permissions:** Only founder can invite, kick, disband
- **Member permissions:** Any member can leave (except founder)
- **Cooldowns:** 7-day cooldown after leave/kick (data model exists but not enforced)
- **Detail view:** Overlay within Browse tab (toggle via `ViewingCivilizationId` state)
- **Invites:** Sent to religions (not individual players), only religion founder can accept

---

## Success Criteria

The refactoring is complete when:

1. All renderers return `RenderResult` objects with events
2. All state mutations happen through event processors
3. No callbacks exist in ViewModels or renderers
4. Network events update state through StateManager methods
5. Pattern matches Religion tab architecture 1:1
6. All civilization features work without regressions
