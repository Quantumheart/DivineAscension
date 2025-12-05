# Renderer Testability Refactoring - Proof of Concept

## Problem Statement

Current renderer classes (like `ReligionBrowseRenderer`) have several testability issues:

1. **Tight coupling to external dependencies**: Direct calls to `ImGui`, `ICoreClientAPI`, and `BlessingDialogManager`
2. **State mutation**: Directly modifies state objects passed in (e.g., `state.DeityFilter`, `state.SelectedReligionUID`)
3. **Side effects**: Plays sounds, sends network requests, all mixed with rendering logic
4. **Hard to test**: Cannot test rendering logic without mocking ImGui, sound system, and network layer

## Proposed Architecture

### Core Principles

1. **Separation of Concerns**: Split rendering (pure) from state management and side effects
2. **Minimal State**: Renderers hold no state - they are pure functions
3. **Unidirectional Data Flow**: State → ViewModel → Renderer → Events → State
4. **Testability**: Pure rendering functions can be tested without UI framework

### Architecture Layers

```
┌─────────────────────────────────────────────────────────┐
│ Manager (BlessingDialogManager)                         │
│ - Owns all state                                         │
│ - Processes events from renderer                         │
│ - Triggers side effects (network, sound)                 │
└─────────────┬───────────────────────────────────────────┘
              │
              │ Creates ViewModel
              ▼
┌─────────────────────────────────────────────────────────┐
│ ViewModel (ReligionBrowseViewModel)                     │
│ - Immutable struct                                       │
│ - Contains only data needed for rendering                │
│ - No behavior, no methods (except simple queries)        │
└─────────────┬───────────────────────────────────────────┘
              │
              │ Passed to Renderer
              ▼
┌─────────────────────────────────────────────────────────┐
│ Renderer (ReligionBrowseRenderer)                       │
│ - Pure function: ViewModel → RenderCommands + Events     │
│ - No state, no mutations                                 │
│ - Returns events representing user interactions          │
└─────────────┬───────────────────────────────────────────┘
              │
              │ Returns Events
              ▼
┌─────────────────────────────────────────────────────────┐
│ Event Handler (in Manager)                              │
│ - Processes events                                       │
│ - Updates state                                          │
│ - Triggers side effects                                  │
└─────────────────────────────────────────────────────────┘
```

## Example: ReligionBrowseRenderer Refactoring

### Current Issues (from ReligionBrowseRenderer.cs)

```csharp
// Line 51: Direct state mutation
state.DeityFilter = newFilter == "All" ? "" : newFilter;
state.SelectedReligionUID = null;
state.BrowseScrollY = 0f;

// Line 56: Side effect - network request
manager.RequestReligionList(state.DeityFilter);

// Line 58: Side effect - sound playback
api.World.PlaySoundAt(new AssetLocation("pantheonwars:sounds/click"), ...);

// Line 96: Direct state mutation
state.CurrentSubTab = ReligionSubTab.Create;

// Line 108: Side effect - network request
manager.RequestReligionAction("join", state.SelectedReligionUID!);
```

### Proposed Refactoring

#### Step 1: Define ViewModel (Immutable Data)

```csharp
public readonly struct ReligionBrowseViewModel
{
    public string[] DeityFilters { get; }
    public string CurrentDeityFilter { get; }
    public IReadOnlyList<ReligionInfo> Religions { get; }
    public bool IsLoading { get; }
    public float ScrollY { get; }
    public string? SelectedReligionUID { get; }
    public bool UserHasReligion { get; }
    public float X, Y, Width, Height { get; }

    // Helper methods (no side effects)
    public int GetCurrentFilterIndex() =>
        Array.IndexOf(DeityFilters, CurrentDeityFilter);

    public bool CanJoinReligion =>
        !string.IsNullOrEmpty(SelectedReligionUID);
}
```

#### Step 2: Define Event Types (User Interactions)

```csharp
public abstract record ReligionBrowseEvent
{
    // User clicked a deity filter tab
    public record DeityFilterChanged(string NewFilter) : ReligionBrowseEvent;

    // User selected a different religion from the list
    public record ReligionSelected(string? ReligionUID, float NewScrollY)
        : ReligionBrowseEvent;

    // User scrolled the religion list
    public record ScrollChanged(float NewScrollY) : ReligionBrowseEvent;

    // User clicked "Create Religion" button
    public record CreateReligionClicked() : ReligionBrowseEvent;

    // User clicked "Join Religion" button
    public record JoinReligionClicked(string ReligionUID) : ReligionBrowseEvent;

    // User hovered over a religion (for tooltip)
    public record ReligionHovered(ReligionInfo? Religion) : ReligionBrowseEvent;
}
```

#### Step 3: Define Result Type

```csharp
public readonly struct RenderResult
{
    public IReadOnlyList<ReligionBrowseEvent> Events { get; }
    public ReligionInfo? HoveredReligion { get; }
    public float RenderedHeight { get; }
}
```

#### Step 4: Refactor Renderer to Pure Function

```csharp
internal static class ReligionBrowseRenderer
{
    // Pure function: ViewModel + DrawList → RenderResult
    public static RenderResult Draw(
        ReligionBrowseViewModel viewModel,
        ImDrawListPtr drawList)
    {
        var events = new List<ReligionBrowseEvent>();
        var currentY = viewModel.Y;

        // === DEITY FILTER TABS ===
        const float tabHeight = 32f;
        var currentFilterIndex = viewModel.GetCurrentFilterIndex();

        var newFilterIndex = TabControl.Draw(
            drawList,
            viewModel.X, currentY, viewModel.Width, tabHeight,
            viewModel.DeityFilters,
            currentFilterIndex);

        // Emit event if filter changed (NO STATE MUTATION)
        if (newFilterIndex != currentFilterIndex)
        {
            var newFilter = viewModel.DeityFilters[newFilterIndex];
            events.Add(new DeityFilterChanged(newFilter));
        }

        currentY += tabHeight + 8f;

        // === RELIGION LIST ===
        var listHeight = viewModel.Height - (currentY - viewModel.Y) - 50f;
        var listResult = ReligionListRenderer.Draw(
            drawList,
            viewModel.X, currentY, viewModel.Width, listHeight,
            viewModel.Religions, viewModel.IsLoading,
            viewModel.ScrollY, viewModel.SelectedReligionUID);

        // Capture list interactions as events
        if (listResult.ScrollY != viewModel.ScrollY)
            events.Add(new ScrollChanged(listResult.ScrollY));

        if (listResult.SelectedUID != viewModel.SelectedReligionUID)
            events.Add(new ReligionSelected(
                listResult.SelectedUID,
                listResult.ScrollY));

        currentY += listHeight + 10f;

        // === ACTION BUTTONS ===
        const float buttonWidth = 180f;
        const float buttonHeight = 36f;
        const float buttonSpacing = 12f;

        if (!viewModel.UserHasReligion)
        {
            // Show both Create and Join buttons
            var totalWidth = buttonWidth * 2 + buttonSpacing;
            var startX = viewModel.X + (viewModel.Width - totalWidth) / 2;

            // Create Religion button
            if (ButtonRenderer.DrawButton(
                drawList, "Create Religion",
                startX, currentY, buttonWidth, buttonHeight, true))
            {
                events.Add(new CreateReligionClicked());
            }

            // Join Religion button
            var joinX = startX + buttonWidth + buttonSpacing;
            var canJoin = viewModel.CanJoinReligion;

            if (ButtonRenderer.DrawButton(
                drawList,
                canJoin ? "Join Religion" : "Select a religion",
                joinX, currentY, buttonWidth, buttonHeight,
                false, canJoin))
            {
                if (canJoin)
                    events.Add(new JoinReligionClicked(
                        viewModel.SelectedReligionUID!));
                // Note: Error sound would be handled by event processor
            }
        }
        else
        {
            // Show centered Join button only
            var joinX = viewModel.X + (viewModel.Width - buttonWidth) / 2;
            var canJoin = viewModel.CanJoinReligion;

            if (ButtonRenderer.DrawButton(
                drawList,
                canJoin ? "Join Religion" : "Select a religion",
                joinX, currentY, buttonWidth, buttonHeight,
                false, canJoin))
            {
                if (canJoin)
                    events.Add(new JoinReligionClicked(
                        viewModel.SelectedReligionUID!));
            }
        }

        return new RenderResult
        {
            Events = events,
            HoveredReligion = listResult.HoveredReligion,
            RenderedHeight = viewModel.Height
        };
    }
}
```

#### Step 5: Event Processing in Manager

```csharp
public class BlessingDialogManager
{
    public void DrawReligionBrowse(float x, float y, float width, float height)
    {
        // Build view model from state
        var viewModel = new ReligionBrowseViewModel(
            deityFilters: new[] { "All", "Khoras", "Lysa", "Aethra", "Gaia" },
            currentDeityFilter: ReligionState.DeityFilter,
            religions: ReligionState.AllReligions,
            isLoading: ReligionState.IsBrowseLoading,
            scrollY: ReligionState.BrowseScrollY,
            selectedReligionUID: ReligionState.SelectedReligionUID,
            userHasReligion: HasReligion(),
            x: x, y: y, width: width, height: height
        );

        // Render (pure function call)
        var drawList = ImGui.GetWindowDrawList();
        var result = ReligionBrowseRenderer.Draw(viewModel, drawList);

        // Process events (side effects)
        ProcessReligionBrowseEvents(result.Events);

        // Draw tooltip if needed
        if (result.HoveredReligion != null)
        {
            var mousePos = ImGui.GetMousePos();
            ReligionListRenderer.DrawTooltip(
                result.HoveredReligion,
                mousePos.X, mousePos.Y, width, height);
        }
    }

    private void ProcessReligionBrowseEvents(
        IReadOnlyList<ReligionBrowseEvent> events)
    {
        foreach (var evt in events)
        {
            switch (evt)
            {
                case DeityFilterChanged e:
                    HandleDeityFilterChanged(e.NewFilter);
                    break;

                case ReligionSelected e:
                    ReligionState.SelectedReligionUID = e.ReligionUID;
                    ReligionState.BrowseScrollY = e.NewScrollY;
                    break;

                case ScrollChanged e:
                    ReligionState.BrowseScrollY = e.NewScrollY;
                    break;

                case CreateReligionClicked:
                    PlayClickSound();
                    ReligionState.CurrentSubTab = ReligionSubTab.Create;
                    break;

                case JoinReligionClicked e:
                    PlayClickSound();
                    RequestReligionAction("join", e.ReligionUID);
                    break;
            }
        }
    }

    private void HandleDeityFilterChanged(string newFilter)
    {
        var normalizedFilter = newFilter == "All" ? "" : newFilter;
        ReligionState.DeityFilter = normalizedFilter;
        ReligionState.SelectedReligionUID = null;
        ReligionState.BrowseScrollY = 0f;

        PlayClickSound();
        RequestReligionList(normalizedFilter);
    }
}
```

## Benefits of This Approach

### 1. **Pure Rendering Logic**
- `ReligionBrowseRenderer.Draw()` is a pure function
- Given the same ViewModel, always produces the same render output
- No hidden state, no mutations, no side effects

### 2. **Easy Unit Testing**

```csharp
[Test]
public void Draw_WithNoReligionSelected_DisablesJoinButton()
{
    // Arrange
    var viewModel = new ReligionBrowseViewModel(
        deityFilters: new[] { "All", "Khoras" },
        currentDeityFilter: "All",
        religions: CreateFakeReligions(),
        isLoading: false,
        scrollY: 0f,
        selectedReligionUID: null, // No selection
        userHasReligion: false,
        x: 0, y: 0, width: 800, height: 600
    );

    var mockDrawList = new MockImDrawList();

    // Act
    var result = ReligionBrowseRenderer.Draw(viewModel, mockDrawList);

    // Assert
    Assert.IsFalse(viewModel.CanJoinReligion);

    // Verify button was drawn in disabled state
    var joinButton = mockDrawList.Buttons
        .FirstOrDefault(b => b.Label.Contains("Select a religion"));
    Assert.IsNotNull(joinButton);
    Assert.IsFalse(joinButton.Enabled);
}

[Test]
public void Draw_WhenDeityFilterChanges_EmitsFilterChangedEvent()
{
    // Arrange - user clicks Khoras tab
    var viewModel = new ReligionBrowseViewModel(
        currentDeityFilter: "All",
        // ... other params
    );

    var mockDrawList = new MockImDrawList();
    mockDrawList.SimulateTabClick(1); // Click Khoras

    // Act
    var result = ReligionBrowseRenderer.Draw(viewModel, mockDrawList);

    // Assert
    var filterEvent = result.Events
        .OfType<DeityFilterChanged>()
        .FirstOrDefault();

    Assert.IsNotNull(filterEvent);
    Assert.AreEqual("Khoras", filterEvent.NewFilter);
}

[Test]
public void Draw_WithUserHasReligion_HidesCreateButton()
{
    // Arrange
    var viewModel = new ReligionBrowseViewModel(
        userHasReligion: true,
        // ... other params
    );

    var mockDrawList = new MockImDrawList();

    // Act
    var result = ReligionBrowseRenderer.Draw(viewModel, mockDrawList);

    // Assert
    var createButton = mockDrawList.Buttons
        .FirstOrDefault(b => b.Label == "Create Religion");
    Assert.IsNull(createButton); // Should not be rendered
}
```

### 3. **Isolation of Concerns**
- **Rendering**: Pure visual logic, testable with mock draw list
- **State Management**: Centralized in Manager/State classes
- **Side Effects**: Network, sound, navigation - all in event handlers
- **Business Logic**: In event processors, easy to test

### 4. **Better Debugging**
- Can log/inspect events to see exactly what user did
- Can replay events to reproduce bugs
- Can time-travel debug state changes

### 5. **Easier Refactoring**
- Change rendering without touching state/side effects
- Change state structure without touching rendering
- Swap out ImGui for different UI framework more easily

## Migration Strategy

### Phase 1: Single Renderer POC
- Refactor `ReligionBrowseRenderer` as proof of concept
- Create example unit tests
- Measure complexity and benefits

### Phase 2: Shared Infrastructure
- Create base event types
- Create mock/test helpers for ImGui draw list
- Document patterns and guidelines

### Phase 3: Gradual Migration
- Migrate other renderers one at a time
- Priority: Complex renderers with most business logic
- Low priority: Simple renderers (headers, static content)

### Phase 4: Consolidation
- Extract common patterns into base classes/utilities
- Create renderer composition helpers
- Optimize performance if needed

## Trade-offs

### Pros
✅ Highly testable without UI framework
✅ Clear separation of concerns
✅ Easier to reason about data flow
✅ No hidden state mutations
✅ Events provide audit trail

### Cons
❌ More boilerplate (ViewModels, Events, Handlers)
❌ Slight performance overhead (creating events, VMs)
❌ Learning curve for team
❌ More files to maintain

### Recommendation
Worth it for complex renderers with significant business logic and user interactions. For simple renderers (static text, headers), the current approach is fine.

## Next Steps

1. Get feedback on this approach
2. Create working POC with `ReligionBrowseRenderer`
3. Write example unit tests
4. Measure effort vs. benefit
5. Decide on rollout plan

