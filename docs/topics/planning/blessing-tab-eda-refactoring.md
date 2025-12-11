# BlessingsTab EDA Pattern Review & Improvement Plan

## Executive Summary

The BlessingsTab has **partial EDA adoption** but contains **6 critical violations** of the established pattern. The
Religion tab demonstrates the correct EDA implementation and should serve as the reference.

### Status Comparison

| Aspect                | Religion Tab         | Blessings Tab                     |
|-----------------------|----------------------|-----------------------------------|
| Pure ViewModels       | ✓ (0 callbacks)      | ✗ (6 callbacks)                   |
| Pure Renderers        | ✓ (returns events)   | ✗ (invokes callbacks)             |
| Event Processing      | ✓ (ProcessXEvents)   | ✗ (inline handling)               |
| Centralized State     | ✓ (ReligionTabState) | ✗ (scattered in GuiDialogManager) |
| Complete Events       | ✓ (19+ event types)  | ✗ (1 empty, 2 partial)            |
| Side Effects Location | ✓ (in Manager)       | ✗ (in Renderer)                   |

---

## Critical EDA Violations

### 1. Callback Anti-Pattern in ViewModel ⚠️ HIGH PRIORITY

**File:** `PantheonWars/GUI/Models/Blessing/Tab/BlessingTabViewModel.cs`

**Problem:** ViewModel contains 6 Action callbacks:

```csharp
Action? onUnlockClicked,
Action? onCloseClicked,
Action<string>? onSelectBlessing,
Action<float, float>? onPlayerTreeScrollChanged,
Action<float, float>? onReligionTreeScrollChanged,
Action<string?>? onHoverChanged
```

**Why This Breaks EDA:**

- ViewModels should be pure data structures (DTOs)
- Callbacks create tight coupling between renderer and state manager
- Breaks unidirectional data flow: State → ViewModel → Renderer → Events → State
- Prevents event history/logging
- Makes testing difficult

**Solution:** Remove all callbacks, keep only pure data properties

---

### 2. Direct Callback Invocation in Renderer ⚠️ HIGH PRIORITY

**File:** `PantheonWars/GUI/UI/Renderers/Blessing/BlessingTabRenderer.cs` (lines 51, 56, 59, 96, 101, 112)

**Problem:** Renderer directly invokes callbacks:

```csharp
vm.OnSelectBlessing?.Invoke(id);
vm.OnPlayerTreeScrollChanged?.Invoke(sx, sy);
vm.OnReligionTreeScrollChanged?.Invoke(sx2, sy2);
```

**Why This Breaks EDA:**

- Renderers should be pure functions that return events
- Direct invocation bypasses event system
- Creates immediate side effects during rendering

**Solution:** Collect events in a list and return them in a `BlessingTabRenderResult`

---

### 3. Missing Event Processing ⚠️ HIGH PRIORITY

**File:** `PantheonWars/GUI/Managers/ReligionStateManager.cs` (lines 523-564)

**Problem:** `DrawBlessingsTab()` passes callbacks instead of processing events:

```csharp
public void DrawBlessingsTab(..., Action? onUnlockClicked, Action? onCloseClicked)
{
    var vm = new BlessingTabViewModel(..., onUnlockClicked: onUnlockClicked, ...);
    BlessingTabRenderer.DrawBlessingsTab(vm);
    // NO event processing here!
}
```

**Compare with Religion Tab:**

```csharp
public void DrawReligionBrowse(...)
{
    var viewModel = new ReligionBrowseViewModel(...); // Pure data
    var result = ReligionBrowseRenderer.Draw(viewModel, drawList);
    ProcessBrowseEvents(result.Events); // Central event processing
}
```

**Solution:** Add `ProcessBlessingTabEvents()` method that handles all side effects

---

### 4. Incomplete Event Definitions

**File:** `PantheonWars/GUI/Events/BlessingInfoEvent.cs`

**Problem:** Empty abstract record with no event cases defined

```csharp
internal abstract record BlessingInfoEvent;
// No event cases!
```

**Compare with Religion Tab:** `ReligionInfoEvent` defines 19+ event types

**Solution:** Define actual event types if info panel needs interactions

---

### 5. Scattered State Management

**File:** `PantheonWars/GUI/GuiDialogManager.cs` (lines 51-58)

**Problem:** Blessing state scattered across GuiDialogManager:

```csharp
public string? SelectedBlessingId { get; set; }
public string? HoveringBlessingId { get; set; }
public float PlayerTreeScrollX { get; set; }
public float PlayerTreeScrollY { get; set; }
public float ReligionTreeScrollX { get; set; }
public float ReligionTreeScrollY { get; set; }
```

**Compare with Religion Tab:** Has dedicated `ReligionTabState` class with nested sub-states and Reset() methods

**Solution:** Create `BlessingTabState` class to consolidate state

---

### 6. Side Effects in Renderer

**File:** `PantheonWars/GUI/UI/Renderers/Blessing/BlessingTabRenderer.cs` (lines 52-53, 97-108)

**Problem:** Renderer plays sounds directly:

```csharp
vm.Api.World.PlaySoundAt(new AssetLocation("pantheonwars:sounds/click"), ...);
```

**Why This Breaks EDA:**

- Renderers should be pure - no side effects
- Side effects should happen in event processors

**Compare with Religion Tab:** Sounds played in `ProcessBrowseEvents()`, not in renderers

**Solution:** Move sound playback to event processing methods

---

## Correct EDA Pattern (Religion Tab Reference)

```
State (ReligionTabState)
  ↓
ViewModel (pure data, no callbacks)
  ↓
Renderer.Draw() (pure function)
  ↓
RenderResult with Events
  ↓
ProcessXEvents() (side effects: state updates, sounds, network)
  ↓
State updated
```

**Reference Files:**

- State: `PantheonWars/GUI/State/Religion/BrowseState.cs`
- Events: `PantheonWars/GUI/Events/ReligionBrowseEvent.cs`
- ViewModel: `PantheonWars/GUI/Models/Religion/Browse/ReligionBrowseViewModel.cs`
- Renderer: `PantheonWars/GUI/UI/Renderers/Religion/ReligionBrowseRenderer.cs`
- Processing: `ReligionStateManager.ProcessBrowseEvents()` (lines 855-899)

---

## Refactoring Recommendations

### Phase 1: Create Proper State Structure

**Create:** `PantheonWars/GUI/State/BlessingTabState.cs`

```csharp
public class BlessingTabState
{
    public BlessingTreeState TreeState { get; } = new();
    public BlessingInfoState InfoState { get; } = new();
    public void Reset() { ... }
}

public class BlessingTreeState
{
    public string? SelectedBlessingId { get; set; }
    public string? HoveringBlessingId { get; set; }
    public float PlayerTreeScrollX { get; set; }
    public float PlayerTreeScrollY { get; set; }
    public float ReligionTreeScrollX { get; set; }
    public float ReligionTreeScrollY { get; set; }
    public void Reset() { ... }
}
```

**Update:** `PantheonWars/GUI/GuiDialogManager.cs`

- Remove blessing-specific properties
- Add `BlessingTabState` reference

---

### Phase 2: Remove Callbacks from ViewModel

**Update:** `PantheonWars/GUI/Models/Blessing/Tab/BlessingTabViewModel.cs`

Remove all 6 Action callbacks, keep only pure data:

- Remove: `onUnlockClicked`, `onCloseClicked`, `onSelectBlessing`, etc.
- Keep: Pure data properties like `selectedBlessingId`, `playerInfo`, etc.

---

### Phase 3: Make Renderer Pure & Return Events

**Create:** `PantheonWars/GUI/Models/Blessing/Tab/BlessingTabRenderResult.cs`

```csharp
public record BlessingTabRenderResult(
    IReadOnlyList<BlessingTabEvent> Events,
    float Height
);
```

**Update:** `PantheonWars/GUI/UI/Renderers/Blessing/BlessingTabRenderer.cs`

```csharp
public static BlessingTabRenderResult DrawBlessingsTab(BlessingTabViewModel vm)
{
    var events = new List<BlessingTabEvent>();

    // ... rendering code ...

    // Collect events from sub-renderers
    var treeResult = BlessingTreeRenderer.Draw(...);
    events.AddRange(treeResult.Events);

    var actionsResult = BlessingActionsRenderer.Draw(...);
    events.AddRange(actionsResult.Events);

    // NO callback invocations
    // NO sound playing

    return new BlessingTabRenderResult(events, height);
}
```

---

### Phase 4: Create BlessingStateManager

**Create:** `PantheonWars/GUI/Managers/BlessingStateManager.cs`

New dedicated manager for blessing state and event processing:

```csharp
public class BlessingStateManager
{
    private readonly ICoreClientAPI _coreClientApi;
    public BlessingTabState State { get; } = new();

    public BlessingStateManager(ICoreClientAPI api)
    {
        _coreClientApi = api;
    }

    public void DrawBlessingsTab(GuiDialogManager manager, ...)
    {
        // Build ViewModel from state (pure data, NO callbacks)
        var vm = new BlessingTabViewModel(
            api: _coreClientApi,
            selectedBlessingId: State.TreeState.SelectedBlessingId,
            hoveringBlessingId: State.TreeState.HoveringBlessingId,
            playerTreeScrollX: State.TreeState.PlayerTreeScrollX,
            playerTreeScrollY: State.TreeState.PlayerTreeScrollY,
            religionTreeScrollX: State.TreeState.ReligionTreeScrollX,
            religionTreeScrollY: State.TreeState.ReligionTreeScrollY,
            // ... all pure data, NO callbacks
        );

        // Render (pure function)
        var result = BlessingTabRenderer.DrawBlessingsTab(vm);

        // Process events (side effects here)
        ProcessBlessingTabEvents(result.Events, manager);
    }

    private void ProcessBlessingTabEvents(
        IReadOnlyList<BlessingTabEvent> events,
        GuiDialogManager manager)
    {
        foreach (var ev in events)
        {
            switch (ev)
            {
                case BlessingTreeEvent.BlessingSelected e:
                    // Update state
                    State.TreeState.SelectedBlessingId = e.BlessingId;
                    // Play sound
                    _coreClientApi.World.PlaySoundAt(
                        new AssetLocation("pantheonwars:sounds/click"), ...);
                    break;

                case BlessingTreeEvent.BlessingHovered e:
                    // Update state
                    State.TreeState.HoveringBlessingId = e.BlessingId;
                    break;

                case BlessingTreeEvent.PlayerTreeScrollChanged e:
                    // Update state
                    State.TreeState.PlayerTreeScrollX = e.ScrollX;
                    State.TreeState.PlayerTreeScrollY = e.ScrollY;
                    break;

                case BlessingTreeEvent.ReligionTreeScrollChanged e:
                    // Update state
                    State.TreeState.ReligionTreeScrollX = e.ScrollX;
                    State.TreeState.ReligionTreeScrollY = e.ScrollY;
                    break;

                case BlessingActionsEvent.UnlockClicked:
                    // Network request
                    SendUnlockBlessingPacket(State.TreeState.SelectedBlessingId);
                    // Play sound
                    _coreClientApi.World.PlaySoundAt(
                        new AssetLocation("pantheonwars:sounds/click"), ...);
                    break;

                case BlessingActionsEvent.UnlockBlockedClicked:
                    // Play error sound
                    _coreClientApi.World.PlaySoundAt(
                        new AssetLocation("pantheonwars:sounds/error"), ...);
                    break;

                case BlessingActionsEvent.CloseClicked:
                    // Close dialog
                    manager.CloseDialog();
                    break;
            }
        }
    }

    private void SendUnlockBlessingPacket(string? blessingId)
    {
        if (blessingId == null) return;
        // Network request logic
        _coreClientApi.Network.SendBlockEntityPacket(...);
    }
}
```

**Update:** `PantheonWars/GUI/GuiDialogManager.cs`

- Add `BlessingStateManager` field
- Initialize it in constructor
- Delegate blessing tab drawing to it
- Remove blessing-specific state properties

---

### Phase 5: BlessingInfoEvent (No Changes Needed)

**Decision:** BlessingInfo panel is display-only, no user interactions needed.

**Action:** Leave `PantheonWars/GUI/Events/BlessingInfoEvent.cs` as-is (empty abstract record).

The BlessingInfoRenderer already returns an empty events list, which is correct for a display-only component.

---

## Files Requiring Changes

### High Priority (EDA Violations)

1. **Create:** `PantheonWars/GUI/Managers/BlessingStateManager.cs`
    - New dedicated manager for blessing state and event processing
    - Contains `DrawBlessingsTab()` and `ProcessBlessingTabEvents()` methods
    - Owns `BlessingTabState` instance

2. `PantheonWars/GUI/Models/Blessing/Tab/BlessingTabViewModel.cs`
    - Remove all 6 Action callbacks
    - Keep only pure data properties

3. `PantheonWars/GUI/UI/Renderers/Blessing/BlessingTabRenderer.cs`
    - Remove callback invocations (lines 51, 56, 59, 96, 101, 112)
    - Remove sound playing (lines 52-53, 97-108)
    - Return `BlessingTabRenderResult` with events instead of void

4. `PantheonWars/GUI/GuiDialogManager.cs`
    - Add `BlessingStateManager` field
    - Initialize `BlessingStateManager` in constructor
    - Delegate blessing tab drawing to `BlessingStateManager.DrawBlessingsTab()`
    - Remove blessing-specific state properties (SelectedBlessingId, HoveringBlessingId, scroll positions)

### Medium Priority (State Organization)

5. **Create:** `PantheonWars/GUI/State/BlessingTabState.cs`
    - Consolidate blessing state (BlessingTreeState, BlessingInfoState)
    - Add Reset() methods

6. **Create:** `PantheonWars/GUI/Models/Blessing/Tab/BlessingTabRenderResult.cs`
    - Return type for BlessingTabRenderer

### Low Priority (No Changes)

7. `PantheonWars/GUI/Events/BlessingInfoEvent.cs`
    - Keep as-is (empty) - info panel is display-only

---

## Benefits of Refactoring

- **Consistency:** Matches Religion tab pattern across entire codebase
- **Testability:** Events can be inspected without mocking callbacks
- **Observability:** Event logging/analytics capability
- **Debugging:** Easier to trace event flow
- **Extensibility:** Potential for undo/redo via event sourcing
- **Separation of Concerns:** Clear boundaries between rendering and logic

---

## Effort Estimate

**Medium complexity:** 4-6 hours

- Most code already follows pattern partially
- Clear reference implementation exists (Religion tab)
- Main work is moving callbacks to events and adding event processing
- BlessingTreeRenderer and BlessingActionsRenderer already return events correctly

---

## Implementation Notes

### Design Decisions

1. **Separate BlessingStateManager:** Creating a dedicated manager for better separation of concerns and future
   extensibility
2. **BlessingInfo Display-Only:** No interactive events needed - existing empty `BlessingInfoEvent` is correct
3. **Simple State Structure:** No complex features planned - keeping state design straightforward

### Migration Strategy

The refactoring should be done in order (Phase 1 → Phase 5) to minimize breaking changes:

1. Create new state structures first
2. Create BlessingStateManager
3. Update ViewModel to remove callbacks
4. Update Renderer to return events
5. Wire up GuiDialogManager to use BlessingStateManager

This ensures each phase builds on the previous one and maintains compilation throughout.
