# ReligionInvitesRenderer - Testability Refactoring POC

## Overview

This document demonstrates refactoring `ReligionInvitesRenderer` from a stateful, side-effect-heavy renderer into a testable, pure function following the patterns established in the general refactoring POC.

**File**: `PantheonWars/GUI/UI/Renderers/Religion/ReligionInvitesRenderer.cs`

## Current Implementation Analysis

### Current Code Structure

```csharp
internal static class ReligionInvitesRenderer
{
    public static float Draw(
        BlessingDialogManager manager,  // ❌ Manager dependency
        ICoreClientAPI api,              // ❌ API dependency
        float x, float y, float width, float height)
    {
        var state = manager.ReligionState;  // ❌ State access

        // Rendering logic...

        // ❌ Direct state mutation (line 37)
        state.InvitesScrollY = ScrollableList.Draw(...);

        // In DrawInviteCard:
        // ❌ Side effect - network request (line 77)
        manager.RequestReligionAction("accept", string.Empty, invite.InviteId);

        // ❌ Side effect - network request (line 81)
        manager.RequestReligionAction("decline", string.Empty, invite.InviteId);
    }
}
```

### Problems Identified

1. **❌ Tight Coupling**: Depends on `BlessingDialogManager` and `ICoreClientAPI`
2. **❌ State Mutation**: Directly modifies `state.InvitesScrollY`
3. **❌ Side Effects**: Calls `manager.RequestReligionAction()` for network requests
4. **❌ Hard to Test**: Cannot test without full manager and API setup
5. **❌ Mixed Concerns**: Rendering logic mixed with business logic

### What It Does (Requirements)

- Display list of religion invitations
- Show loading state
- Show empty state
- Each invite card shows:
  - Religion name
  - Expiration date
  - Accept button
  - Decline button
- Handle scroll position
- Disable buttons during loading

## Refactored Architecture

### Step 1: Define ViewModel (Immutable Data)

```csharp
using System;
using System.Collections.Generic;

namespace PantheonWars.GUI.UI.Renderers.Religion;

/// <summary>
/// Immutable view model for religion invites display
/// Contains only the data needed to render the invites list
/// </summary>
public readonly struct ReligionInvitesViewModel
{
    public ReligionInvitesViewModel(
        IReadOnlyList<InviteData> invites,
        bool isLoading,
        float scrollY,
        float x, float y, float width, float height)
    {
        Invites = invites;
        IsLoading = isLoading;
        ScrollY = scrollY;
        X = x;
        Y = y;
        Width = width;
        Height = height;
    }

    public IReadOnlyList<InviteData> Invites { get; }
    public bool IsLoading { get; }
    public float ScrollY { get; }

    // Layout
    public float X { get; }
    public float Y { get; }
    public float Width { get; }
    public float Height { get; }

    /// <summary>
    /// Checks if there are any invites to display
    /// </summary>
    public bool HasInvites => Invites.Count > 0;

    /// <summary>
    /// Gets display message for empty state
    /// </summary>
    public string EmptyStateMessage => IsLoading
        ? "Loading invitations..."
        : "No pending invitations.";
}

/// <summary>
/// Simplified invite data for rendering
/// Decouples from network packet structure
/// </summary>
public readonly struct InviteData
{
    public InviteData(
        string inviteId,
        string religionName,
        DateTime expiresAt)
    {
        InviteId = inviteId;
        ReligionName = religionName;
        ExpiresAt = expiresAt;
    }

    public string InviteId { get; }
    public string ReligionName { get; }
    public DateTime ExpiresAt { get; }

    public string FormattedExpiration => $"Expires: {ExpiresAt:yyyy-MM-dd HH:mm}";
}
```

### Step 2: Define Event Types (User Interactions)

```csharp
namespace PantheonWars.GUI.UI.Renderers.Religion;

/// <summary>
/// Events representing user interactions with the invites renderer
/// </summary>
public abstract record ReligionInvitesEvent
{
    /// <summary>
    /// User clicked Accept button for an invite
    /// </summary>
    public record AcceptInviteClicked(string InviteId) : ReligionInvitesEvent;

    /// <summary>
    /// User clicked Decline button for an invite
    /// </summary>
    public record DeclineInviteClicked(string InviteId) : ReligionInvitesEvent;

    /// <summary>
    /// User scrolled the invites list
    /// </summary>
    public record ScrollChanged(float NewScrollY) : ReligionInvitesEvent;
}
```

### Step 3: Define Result Type

```csharp
namespace PantheonWars.GUI.UI.Renderers.Religion;

/// <summary>
/// Result of rendering the invites list
/// Contains events and updated state
/// </summary>
public readonly struct ReligionInvitesRenderResult
{
    public ReligionInvitesRenderResult(
        IReadOnlyList<ReligionInvitesEvent> events,
        float renderedHeight)
    {
        Events = events;
        RenderedHeight = renderedHeight;
    }

    public IReadOnlyList<ReligionInvitesEvent> Events { get; }
    public float RenderedHeight { get; }
}
```

### Step 4: Refactored Renderer (Pure Function)

```csharp
using System.Collections.Generic;
using System.Numerics;
using ImGuiNET;
using PantheonWars.GUI.UI.Components.Buttons;
using PantheonWars.GUI.UI.Components.Lists;
using PantheonWars.GUI.UI.Utilities;

namespace PantheonWars.GUI.UI.Renderers.Religion;

/// <summary>
/// Pure renderer for religion invitations list
/// Takes immutable view model, returns events representing user interactions
/// </summary>
internal static class ReligionInvitesRenderer
{
    /// <summary>
    /// Renders the invites list
    /// Pure function: ViewModel + DrawList → RenderResult
    /// </summary>
    public static ReligionInvitesRenderResult Draw(
        ReligionInvitesViewModel viewModel,
        ImDrawListPtr drawList)
    {
        var events = new List<ReligionInvitesEvent>();
        var currentY = viewModel.Y;

        // === HEADER ===
        TextRenderer.DrawLabel(
            drawList,
            "Your Religion Invitations",
            viewModel.X,
            currentY,
            18f,
            ColorPalette.White);
        currentY += 26f;

        // === HELP TEXT ===
        TextRenderer.DrawInfoText(
            drawList,
            "These are invitations you've received from religions.",
            viewModel.X,
            currentY,
            viewModel.Width);
        currentY += 32f;

        // === EMPTY STATE ===
        if (!viewModel.HasInvites)
        {
            TextRenderer.DrawInfoText(
                drawList,
                viewModel.EmptyStateMessage,
                viewModel.X,
                currentY + 8f,
                viewModel.Width);

            return new ReligionInvitesRenderResult(events, viewModel.Height);
        }

        // === SCROLLABLE LIST ===
        var listHeight = viewModel.Height - (currentY - viewModel.Y);
        var newScrollY = ScrollableList.Draw(
            drawList,
            viewModel.X,
            currentY,
            viewModel.Width,
            listHeight,
            viewModel.Invites,
            itemHeight: 80f,
            spacing: 10f,
            viewModel.ScrollY,
            drawItem: (invite, cx, cy, cw, ch) =>
                DrawInviteCard(invite, cx, cy, cw, ch, drawList, viewModel.IsLoading, events),
            loadingText: viewModel.IsLoading ? "Loading invitations..." : null
        );

        // Emit scroll event if changed
        if (newScrollY != viewModel.ScrollY)
        {
            events.Add(new ReligionInvitesEvent.ScrollChanged(newScrollY));
        }

        return new ReligionInvitesRenderResult(events, viewModel.Height);
    }

    /// <summary>
    /// Draws a single invite card
    /// </summary>
    private static void DrawInviteCard(
        InviteData invite,
        float x, float y, float width, float height,
        ImDrawListPtr drawList,
        bool isLoading,
        List<ReligionInvitesEvent> events)
    {
        // === CARD BACKGROUND ===
        drawList.AddRectFilled(
            new Vector2(x, y),
            new Vector2(x + width, y + height),
            ImGui.ColorConvertFloat4ToU32(ColorPalette.LightBrown),
            4f);

        // === CARD CONTENT ===
        TextRenderer.DrawLabel(
            drawList,
            "Invitation to Religion",
            x + 12f,
            y + 8f,
            16f);

        drawList.AddText(
            ImGui.GetFont(),
            14f,
            new Vector2(x + 14f, y + 30f),
            ImGui.ColorConvertFloat4ToU32(ColorPalette.Grey),
            $"Religion: {invite.ReligionName}");

        drawList.AddText(
            ImGui.GetFont(),
            14f,
            new Vector2(x + 14f, y + 48f),
            ImGui.ColorConvertFloat4ToU32(ColorPalette.Grey),
            invite.FormattedExpiration);

        // === ACTION BUTTONS ===
        var buttonsEnabled = !isLoading;
        var buttonY = y + height - 32f;

        // Accept button
        if (ButtonRenderer.DrawButton(
            drawList,
            "Accept",
            x + width - 180f,
            buttonY,
            80f,
            28f,
            isPrimary: true,
            enabled: buttonsEnabled))
        {
            events.Add(new ReligionInvitesEvent.AcceptInviteClicked(invite.InviteId));
        }

        // Decline button
        if (ButtonRenderer.DrawButton(
            drawList,
            "Decline",
            x + width - 90f,
            buttonY,
            80f,
            28f,
            isPrimary: false,
            enabled: buttonsEnabled))
        {
            events.Add(new ReligionInvitesEvent.DeclineInviteClicked(invite.InviteId));
        }
    }
}
```

### Step 5: Event Processing in Manager

```csharp
// In BlessingDialogManager.cs

public class BlessingDialogManager
{
    /// <summary>
    /// Draws the religion invites tab
    /// </summary>
    public void DrawReligionInvites(float x, float y, float width, float height)
    {
        // Build view model from state
        var viewModel = new ReligionInvitesViewModel(
            invites: ConvertToInviteData(ReligionState.MyInvites),
            isLoading: ReligionState.IsInvitesLoading,
            scrollY: ReligionState.InvitesScrollY,
            x: x, y: y, width: width, height: height
        );

        // Render (pure function call)
        var drawList = ImGui.GetWindowDrawList();
        var result = ReligionInvitesRenderer.Draw(viewModel, drawList);

        // Process events (side effects)
        ProcessInvitesEvents(result.Events);
    }

    /// <summary>
    /// Convert network packet data to view model data
    /// </summary>
    private IReadOnlyList<InviteData> ConvertToInviteData(
        List<PlayerReligionInfoResponsePacket.ReligionInviteInfo> packetInvites)
    {
        return packetInvites
            .Select(i => new InviteData(
                inviteId: i.InviteId,
                religionName: i.ReligionName,
                expiresAt: i.ExpiresAt))
            .ToList();
    }

    /// <summary>
    /// Process events from the invites renderer
    /// </summary>
    private void ProcessInvitesEvents(IReadOnlyList<ReligionInvitesEvent> events)
    {
        foreach (var evt in events)
        {
            switch (evt)
            {
                case ReligionInvitesEvent.AcceptInviteClicked e:
                    HandleAcceptInvite(e.InviteId);
                    break;

                case ReligionInvitesEvent.DeclineInviteClicked e:
                    HandleDeclineInvite(e.InviteId);
                    break;

                case ReligionInvitesEvent.ScrollChanged e:
                    ReligionState.InvitesScrollY = e.NewScrollY;
                    break;
            }
        }
    }

    /// <summary>
    /// Handle accept invite action
    /// All side effects (network, sound) happen here
    /// </summary>
    private void HandleAcceptInvite(string inviteId)
    {
        // Play click sound
        PlayClickSound();

        // Send network request
        RequestReligionAction("accept", string.Empty, inviteId);

        // Optional: Optimistic UI update
        ReligionState.IsInvitesLoading = true;
    }

    /// <summary>
    /// Handle decline invite action
    /// </summary>
    private void HandleDeclineInvite(string inviteId)
    {
        // Play click sound
        PlayClickSound();

        // Send network request
        RequestReligionAction("decline", string.Empty, inviteId);

        // Optional: Optimistic UI update
        ReligionState.IsInvitesLoading = true;
    }
}
```

## Benefits of Refactored Design

### 1. Pure Rendering Logic ✅

```csharp
// Before: Impure function with side effects
public static float Draw(BlessingDialogManager manager, ICoreClientAPI api, ...)
{
    manager.RequestReligionAction("accept", ...); // Side effect!
}

// After: Pure function
public static ReligionInvitesRenderResult Draw(
    ReligionInvitesViewModel viewModel,
    ImDrawListPtr drawList)
{
    // Just returns events, no side effects
    events.Add(new AcceptInviteClicked(inviteId));
}
```

### 2. Easy Unit Testing ✅

```csharp
[Test]
public void Draw_WithNoInvites_ShowsEmptyStateMessage()
{
    // Arrange
    var viewModel = new ReligionInvitesViewModel(
        invites: new List<InviteData>(), // Empty
        isLoading: false,
        scrollY: 0f,
        x: 0, y: 0, width: 800, height: 600
    );
    var mockDrawList = new MockImDrawList();

    // Act
    var result = ReligionInvitesRenderer.Draw(viewModel, mockDrawList);

    // Assert
    var infoText = mockDrawList.TextCalls
        .FirstOrDefault(t => t.Text.Contains("No pending invitations"));
    Assert.IsNotNull(infoText);
    Assert.IsEmpty(result.Events); // No interactions
}

[Test]
public void Draw_WithInvites_RendersInviteCards()
{
    // Arrange
    var invites = new List<InviteData>
    {
        new InviteData("inv1", "Warriors of Khoras", DateTime.Now.AddDays(7)),
        new InviteData("inv2", "Hunters Guild", DateTime.Now.AddDays(3))
    };

    var viewModel = new ReligionInvitesViewModel(
        invites: invites,
        isLoading: false,
        scrollY: 0f,
        x: 0, y: 0, width: 800, height: 600
    );
    var mockDrawList = new MockImDrawList();

    // Act
    var result = ReligionInvitesRenderer.Draw(viewModel, mockDrawList);

    // Assert
    var religionTexts = mockDrawList.TextCalls
        .Where(t => t.Text.Contains("Religion:"))
        .ToList();
    Assert.AreEqual(2, religionTexts.Count);
    Assert.IsTrue(religionTexts.Any(t => t.Text.Contains("Warriors of Khoras")));
    Assert.IsTrue(religionTexts.Any(t => t.Text.Contains("Hunters Guild")));
}

[Test]
public void DrawInviteCard_WhenAcceptClicked_EmitsAcceptEvent()
{
    // Arrange
    var invite = new InviteData("inv123", "Test Religion", DateTime.Now);
    var viewModel = new ReligionInvitesViewModel(
        invites: new[] { invite },
        isLoading: false,
        scrollY: 0f,
        x: 0, y: 0, width: 800, height: 600
    );
    var mockDrawList = new MockImDrawList();
    mockDrawList.SimulateButtonClick("Accept"); // Simulate user clicking Accept

    // Act
    var result = ReligionInvitesRenderer.Draw(viewModel, mockDrawList);

    // Assert
    var acceptEvent = result.Events
        .OfType<ReligionInvitesEvent.AcceptInviteClicked>()
        .FirstOrDefault();

    Assert.IsNotNull(acceptEvent);
    Assert.AreEqual("inv123", acceptEvent.InviteId);
}

[Test]
public void DrawInviteCard_WhenDeclineClicked_EmitsDeclineEvent()
{
    // Arrange
    var invite = new InviteData("inv456", "Test Religion", DateTime.Now);
    var viewModel = new ReligionInvitesViewModel(
        invites: new[] { invite },
        isLoading: false,
        scrollY: 0f,
        x: 0, y: 0, width: 800, height: 600
    );
    var mockDrawList = new MockImDrawList();
    mockDrawList.SimulateButtonClick("Decline");

    // Act
    var result = ReligionInvitesRenderer.Draw(viewModel, mockDrawList);

    // Assert
    var declineEvent = result.Events
        .OfType<ReligionInvitesEvent.DeclineInviteClicked>()
        .FirstOrDefault();

    Assert.IsNotNull(declineEvent);
    Assert.AreEqual("inv456", declineEvent.InviteId);
}

[Test]
public void Draw_WhenLoading_DisablesButtons()
{
    // Arrange
    var invite = new InviteData("inv1", "Test Religion", DateTime.Now);
    var viewModel = new ReligionInvitesViewModel(
        invites: new[] { invite },
        isLoading: true, // Loading state
        scrollY: 0f,
        x: 0, y: 0, width: 800, height: 600
    );
    var mockDrawList = new MockImDrawList();

    // Act
    var result = ReligionInvitesRenderer.Draw(viewModel, mockDrawList);

    // Assert
    var buttons = mockDrawList.Buttons;
    Assert.IsTrue(buttons.All(b => !b.Enabled),
        "All buttons should be disabled during loading");
}

[Test]
public void Draw_WhenScrollChanged_EmitsScrollEvent()
{
    // Arrange
    var invites = CreateManyInvites(20); // Create many to enable scrolling
    var viewModel = new ReligionInvitesViewModel(
        invites: invites,
        isLoading: false,
        scrollY: 0f,
        x: 0, y: 0, width: 800, height: 600
    );
    var mockDrawList = new MockImDrawList();
    mockDrawList.SimulateScroll(100f); // Simulate user scrolling

    // Act
    var result = ReligionInvitesRenderer.Draw(viewModel, mockDrawList);

    // Assert
    var scrollEvent = result.Events
        .OfType<ReligionInvitesEvent.ScrollChanged>()
        .FirstOrDefault();

    Assert.IsNotNull(scrollEvent);
    Assert.AreEqual(100f, scrollEvent.NewScrollY);
}
```

### 3. Isolated Concerns ✅

| Concern | Location | Testable |
|---------|----------|----------|
| **Rendering** | `ReligionInvitesRenderer.Draw()` | ✅ Mock DrawList |
| **State Management** | `BlessingDialogManager` | ✅ No UI needed |
| **Network Calls** | `HandleAcceptInvite()` | ✅ Mock network |
| **Sound Effects** | `HandleAcceptInvite()` | ✅ Mock audio |
| **Business Logic** | `ProcessInvitesEvents()` | ✅ Pure logic |

### 4. Better Debugging ✅

```csharp
// Log events to see exactly what user did
var result = ReligionInvitesRenderer.Draw(viewModel, drawList);
foreach (var evt in result.Events)
{
    Logger.Debug($"User action: {evt}");
    // Example output:
    // User action: AcceptInviteClicked { InviteId = "inv123" }
}

// Can replay events to reproduce bugs
var eventsFromBugReport = LoadEventsFromLog();
foreach (var evt in eventsFromBugReport)
{
    ProcessInvitesEvents(new[] { evt });
}
```

### 5. Easier Refactoring ✅

Want to change network protocol? Only touch event handlers.
Want to redesign UI? Only touch renderer.
Want to change state structure? Only touch ViewModel conversion.

## Comparison: Before vs After

### Before (Problematic)

```csharp
// Hard to test, side effects everywhere
public static float Draw(
    BlessingDialogManager manager,
    ICoreClientAPI api,
    float x, float y, float width, float height)
{
    var state = manager.ReligionState;

    // State mutation
    state.InvitesScrollY = ScrollableList.Draw(...);

    // Side effects buried in rendering
    if (acceptButtonClicked)
        manager.RequestReligionAction("accept", ...);
}

// Testing requires:
// ❌ Full BlessingDialogManager setup
// ❌ Mock ICoreClientAPI
// ❌ Mock network layer
// ❌ Cannot test rendering without triggering side effects
```

### After (Clean)

```csharp
// Easy to test, pure function
public static ReligionInvitesRenderResult Draw(
    ReligionInvitesViewModel viewModel,
    ImDrawListPtr drawList)
{
    var events = new List<ReligionInvitesEvent>();

    // Just collect events
    if (acceptButtonClicked)
        events.Add(new AcceptInviteClicked(inviteId));

    return new ReligionInvitesRenderResult(events, height);
}

// Testing requires:
// ✅ Just create ViewModel
// ✅ Mock DrawList
// ✅ Assert on events
// ✅ No side effects to worry about
```

## Migration Checklist

### Phase 1: Create New Types ✅
- [ ] Create `InviteData` struct
- [ ] Create `ReligionInvitesViewModel` struct
- [ ] Create `ReligionInvitesEvent` hierarchy
- [ ] Create `ReligionInvitesRenderResult` struct

### Phase 2: Refactor Renderer ✅
- [ ] Create new `Draw()` method signature
- [ ] Move rendering logic to use ViewModel
- [ ] Replace side effects with event emission
- [ ] Replace state mutations with event emission
- [ ] Keep old method temporarily (deprecated)

### Phase 3: Update Manager ✅
- [ ] Add `DrawReligionInvites()` method
- [ ] Add `ConvertToInviteData()` helper
- [ ] Add `ProcessInvitesEvents()` handler
- [ ] Add `HandleAcceptInvite()` handler
- [ ] Add `HandleDeclineInvite()` handler
- [ ] Update call sites to use new method

### Phase 4: Add Tests ✅
- [ ] Create `MockImDrawList` if not exists
- [ ] Test empty state
- [ ] Test with invites
- [ ] Test accept button
- [ ] Test decline button
- [ ] Test loading state
- [ ] Test scroll behavior

### Phase 5: Cleanup ✅
- [ ] Remove old `Draw()` method
- [ ] Update documentation
- [ ] Add usage examples

## Key Takeaways

1. **ViewModels** contain immutable data needed for rendering
2. **Events** represent user interactions (not implementation details)
3. **Renderer** is a pure function that returns events
4. **Manager** processes events and handles side effects
5. **Testability** comes from separating concerns

## Next Steps

1. Implement this refactoring for `ReligionInvitesRenderer`
2. Write unit tests to prove testability
3. Use as template for `ReligionCreateRenderer`
4. Document lessons learned
5. Continue with more complex renderers

---

**Pattern Summary**: `State → ViewModel → Renderer → Events → State`

This creates a unidirectional data flow that's easy to reason about, test, and maintain.
