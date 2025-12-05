# Renderer Testability Refactoring - Summary Guide

## Overview

This document summarizes the three proof-of-concept documents and provides guidance on applying testability patterns to UI renderers.

## POC Documents

1. **[renderer-testability-refactor-poc.md](./renderer-testability-refactor-poc.md)** - General concepts and ReligionBrowseRenderer example
2. **[progressbar-renderer-poc.md](./progressbar-renderer-poc.md)** - Analysis of already-good design (simple renderer)
3. **[religion-invites-renderer-refactor-poc.md](./religion-invites-renderer-refactor-poc.md)** - Complete refactoring example (interactive renderer)

## When to Use Each Pattern

### Pattern A: Simple Pure Function (Like ProgressBarRenderer)

**Use when:**
- ✅ No user interactions (buttons, clicks)
- ✅ No state changes
- ✅ Just visual rendering
- ✅ Simple, reusable components

**Example renderers:**
- ProgressBarRenderer ✅
- TooltipRenderer
- TextRenderer
- Simple decorative elements

**Pattern:**
```csharp
public static void Draw(
    ImDrawListPtr drawList,
    float x, float y, float width, float height,
    // All data as explicit parameters
    float percentage,
    Vector4 fillColor,
    string labelText)
{
    // Pure rendering logic
    // No state, no events, no side effects
}
```

**Benefits:**
- ✅ Simplest approach
- ✅ No boilerplate
- ✅ Easy to understand
- ✅ Already testable

**Testing:**
```csharp
var mockDrawList = new MockImDrawList();
ProgressBarRenderer.Draw(mockDrawList, x, y, width, height, 0.5f, gold, brown, "50%");
Assert.AreEqual(expectedWidth, mockDrawList.FilledRects[0].Width);
```

---

### Pattern B: ViewModel + Events (Like ReligionInvitesRenderer)

**Use when:**
- ✅ User interactions (buttons, inputs, clicks)
- ✅ State changes (scroll, selection)
- ✅ Side effects (network, sound, navigation)
- ✅ Complex business logic

**Example renderers:**
- ReligionInvitesRenderer ✅
- ReligionBrowseRenderer ✅
- ReligionCreateRenderer
- Any form or interactive list

**Pattern:**
```csharp
// 1. ViewModel (immutable data)
public readonly struct MyViewModel
{
    public IReadOnlyList<ItemData> Items { get; }
    public bool IsLoading { get; }
    public float ScrollY { get; }
    // Layout dimensions
    public float X, Y, Width, Height { get; }
}

// 2. Events (user interactions)
public abstract record MyRendererEvent
{
    public record ItemClicked(string ItemId) : MyRendererEvent;
    public record ScrollChanged(float NewScrollY) : MyRendererEvent;
}

// 3. Result (output)
public readonly struct MyRenderResult
{
    public IReadOnlyList<MyRendererEvent> Events { get; }
    public float RenderedHeight { get; }
}

// 4. Pure renderer
public static MyRenderResult Draw(
    MyViewModel viewModel,
    ImDrawListPtr drawList)
{
    var events = new List<MyRendererEvent>();

    // Rendering logic
    if (buttonClicked)
        events.Add(new ItemClicked(itemId));

    return new MyRenderResult(events, height);
}

// 5. Event handler (in Manager)
private void ProcessEvents(IReadOnlyList<MyRendererEvent> events)
{
    foreach (var evt in events)
    {
        switch (evt)
        {
            case ItemClicked e:
                PlayClickSound();
                SendNetworkRequest(e.ItemId);
                break;
            case ScrollChanged e:
                state.ScrollY = e.NewScrollY;
                break;
        }
    }
}
```

**Benefits:**
- ✅ Highly testable
- ✅ Clear separation of concerns
- ✅ Events provide audit trail
- ✅ Easy to reason about data flow

**Testing:**
```csharp
var viewModel = new MyViewModel(items, isLoading: false, scrollY: 0f, ...);
var mockDrawList = new MockImDrawList();
mockDrawList.SimulateButtonClick("Accept");

var result = MyRenderer.Draw(viewModel, mockDrawList);

var clickEvent = result.Events.OfType<ItemClicked>().First();
Assert.AreEqual("item123", clickEvent.ItemId);
```

---

## Decision Tree

```
Is this renderer interactive?
│
├─ NO → Use Pattern A (Simple Pure Function)
│       Examples: ProgressBar, Tooltip, Label
│       Just draw stuff, no events needed
│
└─ YES → Use Pattern B (ViewModel + Events)
         Examples: Forms, Lists with buttons, Tabs
         Need to track user interactions

         Does it need side effects?
         │
         ├─ NO → Still use Pattern B, but events might be simple
         │       (e.g., SelectionChanged, ScrollChanged)
         │
         └─ YES → Definitely use Pattern B
                  Events trigger: network, sound, navigation
```

## Migration Priority

### Tier 1: Easy Wins (Start Here)
1. **ProgressBarRenderer** - Already good ✅
2. **ReligionActivityRenderer** - Simple placeholder, already stateless ✅
3. **ReligionInvitesRenderer** - Good POC example 📋

### Tier 2: Medium Complexity
4. **ReligionCreateRenderer** - Form with validation
5. **ReligionBrowseRenderer** - Tabs + list + buttons
6. **CivilizationInvitesRenderer** - Similar to ReligionInvites

### Tier 3: Complex
7. **ReligionMyReligionRenderer** - Multiple sub-sections
8. **CivilizationManageRenderer** - Complex interactions
9. **BlessingTreeRenderer** - Graph rendering + interactions

### Tier 4: Don't Touch Unless Needed
- **ReligionHeaderRenderer** - Mostly display, some complex layout
- **TooltipRenderer** - Already simple
- **TextRenderer** - Already pure utilities

## Common Patterns

### Scroll Handling

```csharp
// In ViewModel
public float ScrollY { get; }

// In Renderer
var newScrollY = ScrollableList.Draw(..., viewModel.ScrollY, ...);

if (newScrollY != viewModel.ScrollY)
    events.Add(new ScrollChanged(newScrollY));

// In Manager
case ScrollChanged e:
    state.ScrollY = e.NewScrollY;
    break;
```

### Button Clicks with Validation

```csharp
// In ViewModel
public bool CanSubmit => !string.IsNullOrEmpty(Name) && Name.Length >= 3;

// In Renderer
if (ButtonRenderer.DrawButton(drawList, "Submit", ..., enabled: viewModel.CanSubmit))
{
    if (viewModel.CanSubmit)
        events.Add(new SubmitClicked());
}

// In Manager
case SubmitClicked:
    PlayClickSound();
    SendRequest(state.Name);
    break;
```

### Loading States

```csharp
// In ViewModel
public bool IsLoading { get; }

// In Renderer
var buttonsEnabled = !viewModel.IsLoading;

if (ButtonRenderer.DrawButton(drawList, "Action", ..., enabled: buttonsEnabled))
    events.Add(new ActionClicked());

// In Manager
case ActionClicked:
    state.IsLoading = true;
    await SendNetworkRequest();
    state.IsLoading = false;
    break;
```

### Tab Selection

```csharp
// In ViewModel
public string CurrentTab { get; }
public string[] AvailableTabs { get; }

public int CurrentTabIndex => Array.IndexOf(AvailableTabs, CurrentTab);

// In Renderer
var newTabIndex = TabControl.Draw(
    drawList,
    viewModel.X, viewModel.Y,
    viewModel.AvailableTabs,
    viewModel.CurrentTabIndex);

if (newTabIndex != viewModel.CurrentTabIndex)
    events.Add(new TabChanged(viewModel.AvailableTabs[newTabIndex]));

// In Manager
case TabChanged e:
    PlayClickSound();
    state.CurrentTab = e.TabName;
    // Reset tab-specific state
    state.ScrollY = 0f;
    state.SelectedItem = null;
    break;
```

## Testing Infrastructure

### Required Mocks

```csharp
// MockImDrawList.cs
public class MockImDrawList : ImDrawListPtr
{
    public List<FilledRect> FilledRects { get; } = new();
    public List<Rect> Rects { get; } = new();
    public List<TextCall> TextCalls { get; } = new();
    public List<ButtonDrawCall> Buttons { get; } = new();

    // Simulation methods
    public void SimulateButtonClick(string buttonLabel);
    public void SimulateScroll(float deltaY);
    public void SimulateTabClick(int tabIndex);
}
```

### Test Categories

1. **Visual Tests** - Assert draw commands
2. **Interaction Tests** - Simulate clicks, assert events
3. **State Tests** - Different ViewModels produce different output
4. **Edge Cases** - Empty lists, loading, errors

### Example Test Structure

```csharp
[TestFixture]
public class MyRendererTests
{
    private MockImDrawList _drawList;

    [SetUp]
    public void Setup()
    {
        _drawList = new MockImDrawList();
    }

    [Test]
    public void Draw_WithEmptyList_ShowsEmptyMessage()
    {
        // Arrange
        var viewModel = CreateViewModel(items: Array.Empty<Item>());

        // Act
        var result = MyRenderer.Draw(viewModel, _drawList);

        // Assert
        Assert.That(_drawList.TextCalls, Has.Some.Matches<TextCall>(
            t => t.Text.Contains("No items")));
        Assert.That(result.Events, Is.Empty);
    }

    [Test]
    public void Draw_WhenButtonClicked_EmitsEvent()
    {
        // Arrange
        var viewModel = CreateViewModel(items: new[] { CreateItem("item1") });
        _drawList.SimulateButtonClick("Action");

        // Act
        var result = MyRenderer.Draw(viewModel, _drawList);

        // Assert
        var evt = result.Events.OfType<ActionClicked>().FirstOrDefault();
        Assert.IsNotNull(evt);
        Assert.AreEqual("item1", evt.ItemId);
    }
}
```

## Anti-Patterns to Avoid

### ❌ Don't: Mix rendering with side effects

```csharp
// BAD
public static void Draw(Manager manager, ...)
{
    if (ButtonClicked)
    {
        manager.SendNetworkRequest(); // Side effect in renderer!
        api.PlaySound();              // Side effect in renderer!
    }
}
```

### ✅ Do: Emit events instead

```csharp
// GOOD
public static RenderResult Draw(ViewModel vm, DrawList dl)
{
    if (ButtonClicked)
        events.Add(new ButtonClickedEvent());

    return new RenderResult(events, ...);
}
```

---

### ❌ Don't: Mutate state in renderer

```csharp
// BAD
public static void Draw(Manager manager, ...)
{
    state.ScrollY = newScrollY; // State mutation!
    state.SelectedItem = item;  // State mutation!
}
```

### ✅ Do: Return state changes as events

```csharp
// GOOD
public static RenderResult Draw(ViewModel vm, DrawList dl)
{
    if (scrollChanged)
        events.Add(new ScrollChanged(newScrollY));

    if (selectionChanged)
        events.Add(new SelectionChanged(itemId));

    return new RenderResult(events, ...);
}
```

---

### ❌ Don't: Access state directly in renderer

```csharp
// BAD
public static void Draw(Manager manager, ...)
{
    var items = manager.ReligionState.MyInvites; // Direct state access
    var loading = manager.ReligionState.IsLoading;
}
```

### ✅ Do: Pass data via ViewModel

```csharp
// GOOD
public static RenderResult Draw(ViewModel vm, DrawList dl)
{
    var items = vm.Invites;      // Data from ViewModel
    var loading = vm.IsLoading;
}
```

---

### ❌ Don't: Create ViewModels for everything

```csharp
// BAD - Overkill for simple renderer
public struct ProgressBarViewModel { ... }
public record ProgressBarEvent { ... }
public struct ProgressBarResult { ... }

// Just draw a simple bar!
```

### ✅ Do: Use simple parameters for simple cases

```csharp
// GOOD - Keep it simple
public static void DrawProgressBar(
    ImDrawListPtr drawList,
    float x, float y, float width, float height,
    float percentage,
    Vector4 fillColor,
    string label)
{
    // Simple, pure, testable
}
```

## Benefits Summary

| Aspect | Before | After |
|--------|--------|-------|
| **Testability** | ❌ Need full Manager + API setup | ✅ Just ViewModel + Mock DrawList |
| **Predictability** | ❌ Hidden side effects | ✅ Pure function |
| **Debuggability** | ❌ Hard to trace state changes | ✅ Event log shows all interactions |
| **Maintainability** | ❌ Concerns mixed together | ✅ Clear separation |
| **Refactorability** | ❌ Change one thing, break everything | ✅ Change isolated pieces |

## Next Steps

1. ✅ Read all three POC documents
2. ✅ Understand when to use each pattern
3. 🔄 Implement refactoring for ReligionInvitesRenderer
4. 🔄 Write unit tests for ReligionInvitesRenderer
5. 🔄 Create MockImDrawList infrastructure
6. 🔄 Apply pattern to ReligionCreateRenderer
7. 🔄 Document lessons learned
8. 🔄 Continue with remaining renderers

## Questions?

- **Q: Do I need to refactor all renderers?**
  - A: No. Start with interactive renderers that have side effects. Simple renderers like ProgressBarRenderer are fine as-is.

- **Q: Is this too much boilerplate?**
  - A: For complex renderers with lots of interactions, the benefits outweigh the cost. For simple renderers, keep it simple.

- **Q: Can I mix patterns?**
  - A: Yes! Use Pattern A for simple components, Pattern B for complex ones. They can coexist.

- **Q: What about performance?**
  - A: Minimal overhead. Creating small structs and event lists is cheap. Only optimize if profiling shows issues.

- **Q: How do I handle async operations?**
  - A: Renderers emit events synchronously. Managers handle async operations in event handlers.

---

**Remember**: The goal is testable, maintainable code - not perfect architecture. Start simple, refactor when needed.
