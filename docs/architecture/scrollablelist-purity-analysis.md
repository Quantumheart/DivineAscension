# ScrollableList - Purity Analysis

## Quick Answer

ScrollableList is **mostly pure** with some pragmatic ImGui interactions. It's already well-designed and follows good patterns.

## Analysis

### Current Implementation

```csharp
public static float Draw<T>(
    ImDrawListPtr drawList,
    float x, float y, float width, float height,
    List<T> items,
    float itemHeight,
    float itemSpacing,
    float scrollY,  // ✅ Takes scroll as parameter
    Action<T, float, float, float, float> itemRenderer,
    // ... other params
)
{
    // Handle mouse wheel scrolling (line 82-90)
    var mousePos = ImGui.GetMousePos();  // ⚠️ Reads global state
    if (isMouseOver)
    {
        var wheel = ImGui.GetIO().MouseWheel;  // ⚠️ Reads global state
        if (wheel != 0)
            scrollY = Math.Clamp(scrollY - wheel * wheelSpeed, 0f, maxScroll);
    }

    // ... rendering logic

    return scrollY;  // ✅ Returns new scroll value
}
```

## Purity Assessment

### ✅ What's Good (Pure Patterns)

1. **Returns state changes** instead of mutating:
   ```csharp
   float scrollY = ScrollableList.Draw(..., currentScrollY, ...);
   // Caller decides whether to update state
   ```

2. **All data as parameters**: No hidden state, everything explicit

3. **No business logic side effects**:
   - ❌ No network calls
   - ❌ No sound effects
   - ❌ No navigation
   - ❌ No state object mutations

4. **Predictable rendering**: Same inputs → same visual output

### ⚠️ What's Impure (But Acceptable)

1. **Reads mouse input from ImGui global state**:
   ```csharp
   var mousePos = ImGui.GetMousePos();        // Global read
   var wheel = ImGui.GetIO().MouseWheel;      // Global read
   ```

2. **This is unavoidable in ImGui** - you have to read input from somewhere

## Comparison with Other Renderers

### ScrollableList (Good Pattern ✅)
```csharp
// Takes scroll position, returns new position
float newScrollY = ScrollableList.Draw(
    drawList,
    x, y, width, height,
    items,
    itemHeight, spacing,
    scrollY,  // Current state
    itemRenderer
);

// Caller controls what to do with new value
if (newScrollY != scrollY)
    events.Add(new ScrollChanged(newScrollY));
```

### ReligionInvitesRenderer - Old (Bad Pattern ❌)
```csharp
// Directly mutates state object
ReligionInvitesRenderer.Draw(manager, api, x, y, width, height)
{
    // Buried inside:
    state.InvitesScrollY = ScrollableList.Draw(...);  // Hidden mutation!
}
```

### ReligionInvitesRenderer - New (Good Pattern ✅)
```csharp
// Returns state change as event
var result = ReligionInvitesRenderer.Draw(viewModel, drawList);
// Returns events, caller handles them
```

## Why ScrollableList's Approach is Acceptable

### 1. Input Reading is UI Framework Concern
```csharp
// This is OK - UI components need to read input
var mousePos = ImGui.GetMousePos();
var wheel = ImGui.GetIO().MouseWheel;

// This is NOT OK - business logic side effects
manager.SendNetworkRequest();  // ❌ Wrong layer!
api.PlaySound();               // ❌ Wrong layer!
```

### 2. Returns State Rather Than Mutating
```csharp
// Good - caller owns the decision
float newScroll = component.Draw(..., oldScroll);
state.ScrollY = newScroll;  // Caller updates state

// Bad - component makes the decision
component.Draw(state, ...);
// Buried inside: state.ScrollY = newValue;  ❌
```

### 3. Testable Despite Input Reading
```csharp
[Test]
public void ScrollableList_WithScroll_ReturnsNewPosition()
{
    // Arrange
    var mockDrawList = new MockImDrawList();
    var items = CreateTestItems(10);

    // Can mock mouse wheel input in ImGui context
    ImGuiTestContext.SetMouseWheel(1.0f);

    // Act
    var newScrollY = ScrollableList.Draw(
        mockDrawList,
        0, 0, 800, 600,
        items,
        itemHeight: 50,
        spacing: 10,
        scrollY: 0,
        drawItem: (item, x, y, w, h) => { /* draw */ }
    );

    // Assert
    Assert.Greater(newScrollY, 0); // Scrolled down
}

[Test]
public void ScrollableList_WithEmptyList_ShowsEmptyText()
{
    // Arrange
    var mockDrawList = new MockImDrawList();
    var emptyItems = new List<Item>();

    // Act
    var scrollY = ScrollableList.Draw(
        mockDrawList,
        0, 0, 800, 600,
        emptyItems,
        itemHeight: 50,
        spacing: 10,
        scrollY: 0,
        drawItem: (item, x, y, w, h) => { /* draw */ },
        emptyText: "No items"
    );

    // Assert
    var text = mockDrawList.TextCalls
        .FirstOrDefault(t => t.Text == "No items");
    Assert.IsNotNull(text);
    Assert.AreEqual(0, scrollY); // Scroll unchanged
}
```

## Pattern Classification

ScrollableList follows **Pattern A (Simple Pure Function)** with pragmatic ImGui integration:

```
Pattern A+: Pure with Framework Integration
- Takes state as parameter
- Returns new state
- Reads framework input (mouse, keyboard)
- No business logic side effects
- Testable with framework mocking
```

This is the same category as `ProgressBarRenderer` - already good design.

## Recommendation

**✅ Keep ScrollableList as-is** - it's already following best practices:

1. ✅ Returns state changes instead of mutating
2. ✅ All inputs explicit
3. ✅ No business logic side effects
4. ✅ Generic and reusable
5. ✅ Caller controls state updates

## Using ScrollableList in Event-Based Renderers

### Good Usage Pattern

```csharp
public static RenderResult Draw(
    ViewModel viewModel,
    ImDrawListPtr drawList)
{
    var events = new List<Event>();

    // ScrollableList returns new scroll position
    var newScrollY = ScrollableList.Draw(
        drawList,
        viewModel.X, viewModel.Y,
        viewModel.Width, viewModel.Height,
        viewModel.Items,
        itemHeight: 50,
        spacing: 10,
        scrollY: viewModel.ScrollY,  // Current scroll from ViewModel
        itemRenderer: (item, x, y, w, h) =>
            DrawItem(item, x, y, w, h, drawList, events)
    );

    // Emit event if scroll changed
    if (newScrollY != viewModel.ScrollY)
    {
        events.Add(new ScrollChanged(newScrollY));
    }

    return new RenderResult(events, height);
}
```

### Why This Works

1. **ScrollableList** handles UI concerns (mouse wheel, scrollbar drawing)
2. **Renderer** captures state changes as events
3. **Manager** decides what to do with events
4. **Clear separation** between UI framework and business logic

## Key Insight

There's a hierarchy of purity:

```
Level 1: Perfect Purity (Unrealistic for UI)
- No global state reads at all
- Would need to pass mouse/keyboard state explicitly
- Too cumbersome for real UI code

Level 2: Framework Purity (ScrollableList ✅)
- Reads framework input (mouse, keyboard)
- No business logic side effects
- Returns state changes
- Testable with framework mocking

Level 3: State Mutation (Old Renderers ❌)
- Mutates state objects
- Mixes rendering with business logic
- Hard to test

Level 4: Side Effect Hell (Worst ❌)
- Network calls in rendering code
- Sound effects mixed with drawing
- State mutations + side effects
- Impossible to test
```

ScrollableList is at **Level 2** - which is the right balance for UI components.

## Conclusion

ScrollableList is **already well-designed** and should be used as a building block for other renderers. It demonstrates the right balance between purity and pragmatism:

- ✅ Pure enough to be testable
- ✅ Practical enough to handle UI concerns
- ✅ Returns state changes for caller to handle
- ✅ No business logic mixed in

Use it as a model for other UI components!
