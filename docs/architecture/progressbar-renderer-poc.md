# ProgressBarRenderer - Testability Analysis & POC

## Current Implementation Analysis

**File**: `PantheonWars/GUI/UI/Renderers/Components/ProgressBarRenderer.cs`

### What's Already Good ✅

The `ProgressBarRenderer` is **already an excellent example** of testable renderer design:

```csharp
internal static class ProgressBarRenderer
{
    public static void DrawProgressBar(
        ImDrawListPtr drawList,
        float x, float y, float width, float height,
        float percentage,
        Vector4 fillColor,
        Vector4 backgroundColor,
        string labelText,
        bool showGlow = false)
    {
        // Pure rendering logic
        // No state mutations
        // No side effects (except drawing)
    }
}
```

#### Excellent Design Characteristics

1. **✅ Stateless**: Static method, no instance state
2. **✅ Pure function**: Same inputs → same output
3. **✅ No dependencies**: Only depends on ImGui (which can be mocked)
4. **✅ No mutations**: Doesn't modify any external state
5. **✅ No side effects**: No network calls, no sounds, no file I/O
6. **✅ Single responsibility**: Just renders a progress bar
7. **✅ All inputs explicit**: Every piece of data needed is a parameter
8. **✅ Simple API**: Easy to understand what it does

### Why It's Already Testable

Unlike renderers like `ReligionBrowseRenderer`, this renderer:
- ❌ Does NOT mutate state objects
- ❌ Does NOT make network requests
- ❌ Does NOT play sounds
- ❌ Does NOT navigate between screens
- ❌ Does NOT depend on managers or API instances

The **only** dependency is `ImDrawListPtr` for drawing commands, which can be mocked.

## Current Testability Assessment

### Easy to Test ✅

**You can already test this renderer** with minimal infrastructure:

```csharp
[Test]
public void DrawProgressBar_WithHalfProgress_DrawsFilledToHalfWidth()
{
    // Arrange
    var mockDrawList = new MockImDrawList();
    var percentage = 0.5f;
    var width = 200f;
    var height = 20f;

    // Act
    ProgressBarRenderer.DrawProgressBar(
        mockDrawList,
        x: 0, y: 0, width: width, height: height,
        percentage: percentage,
        fillColor: new Vector4(1, 0.84f, 0, 1), // Gold
        backgroundColor: new Vector4(0.2f, 0.15f, 0.1f, 1), // Dark brown
        labelText: "50%",
        showGlow: false
    );

    // Assert
    var filledRect = mockDrawList.FilledRects
        .FirstOrDefault(r => r.Color == ColorPalette.Gold);

    Assert.IsNotNull(filledRect);
    Assert.AreEqual(0, filledRect.Min.X);
    Assert.AreEqual(100f, filledRect.Max.X); // Half of 200
}

[Test]
public void DrawProgressBar_WithHighProgress_ShowsGlowEffect()
{
    // Arrange
    var mockDrawList = new MockImDrawList();

    // Act
    ProgressBarRenderer.DrawProgressBar(
        mockDrawList,
        x: 0, y: 0, width: 200f, height: 20f,
        percentage: 0.9f, // >80%
        fillColor: ColorPalette.Gold,
        backgroundColor: ColorPalette.DarkBrown,
        labelText: "90%",
        showGlow: true // Enable glow
    );

    // Assert - should have glow border
    var glowBorder = mockDrawList.Rects
        .Where(r => r.LineThickness == 2f)
        .FirstOrDefault();

    Assert.IsNotNull(glowBorder);
    // Glow uses animated alpha
}

[Test]
public void DrawProgressBar_ClampsPercentageToValidRange()
{
    // Arrange
    var mockDrawList = new MockImDrawList();

    // Act - try to draw with invalid percentage
    ProgressBarRenderer.DrawProgressBar(
        mockDrawList,
        x: 0, y: 0, width: 200f, height: 20f,
        percentage: 1.5f, // Invalid: >100%
        fillColor: ColorPalette.Gold,
        backgroundColor: ColorPalette.DarkBrown,
        labelText: "150%",
        showGlow: false
    );

    // Assert - should clamp to 100%
    var filledRect = mockDrawList.FilledRects
        .FirstOrDefault(r => r.Color == ColorPalette.Gold);

    Assert.AreEqual(200f, filledRect.Max.X); // Clamped to full width
}

[Test]
public void DrawProgressBar_CentersLabelText()
{
    // Arrange
    var mockDrawList = new MockImDrawList();
    var labelText = "Test Label";
    var width = 200f;
    var height = 20f;

    // Mock text size calculation
    mockDrawList.MockTextSize(labelText, new Vector2(80f, 14f));

    // Act
    ProgressBarRenderer.DrawProgressBar(
        mockDrawList,
        x: 0, y: 0, width: width, height: height,
        percentage: 0.5f,
        fillColor: ColorPalette.Gold,
        backgroundColor: ColorPalette.DarkBrown,
        labelText: labelText,
        showGlow: false
    );

    // Assert - text should be centered
    var textDrawCall = mockDrawList.TextCalls.First();

    var expectedX = (width - 80f) / 2; // Centered horizontally
    var expectedY = (height - 14f) / 2; // Centered vertically

    Assert.AreEqual(expectedX, textDrawCall.Position.X, 0.01f);
    Assert.AreEqual(expectedY, textDrawCall.Position.Y, 0.01f);
}
```

## Optional Enhancement: ViewModel Approach

While the current design is excellent, you could make it even more structured with a ViewModel:

### Option A: Keep Current Design (Recommended)

**Keep it as-is** - it's already simple and testable. Adding a ViewModel would add unnecessary complexity for such a simple renderer.

### Option B: ViewModel for Complex Bars (Optional)

Only add a ViewModel if you have **complex progress bar logic**:

```csharp
/// <summary>
/// View model for progress bar rendering
/// Use only if you have complex calculations or multiple bar types
/// </summary>
public readonly struct ProgressBarViewModel
{
    public ProgressBarViewModel(
        float x, float y, float width, float height,
        int currentValue, int maxValue,
        string label,
        ProgressBarType type)
    {
        X = x;
        Y = y;
        Width = width;
        Height = height;
        CurrentValue = currentValue;
        MaxValue = maxValue;
        Label = label;
        Type = type;
    }

    public float X { get; }
    public float Y { get; }
    public float Width { get; }
    public float Height { get; }
    public int CurrentValue { get; }
    public int MaxValue { get; }
    public string Label { get; }
    public ProgressBarType Type { get; }

    // Calculated properties (business logic)
    public float Percentage => MaxValue > 0
        ? Math.Clamp((float)CurrentValue / MaxValue, 0f, 1f)
        : 0f;

    public Vector4 FillColor => Type switch
    {
        ProgressBarType.Favor => ColorPalette.Gold,
        ProgressBarType.Prestige => new Vector4(0.48f, 0.41f, 0.93f, 1f),
        ProgressBarType.Health => ColorPalette.Red,
        _ => ColorPalette.White
    };

    public Vector4 BackgroundColor => ColorPalette.DarkBrown;

    public bool ShowGlow => Percentage > 0.8f;

    public string FormattedLabel => MaxValue > 0
        ? $"{Label} ({CurrentValue}/{MaxValue})"
        : Label;
}

public enum ProgressBarType
{
    Favor,
    Prestige,
    Health,
    Generic
}

// Updated renderer
internal static class ProgressBarRenderer
{
    // Keep original method for backward compatibility
    public static void DrawProgressBar(
        ImDrawListPtr drawList,
        float x, float y, float width, float height,
        float percentage,
        Vector4 fillColor,
        Vector4 backgroundColor,
        string labelText,
        bool showGlow = false)
    {
        // Original implementation
    }

    // New ViewModel-based method
    public static void DrawProgressBar(
        ImDrawListPtr drawList,
        ProgressBarViewModel viewModel)
    {
        DrawProgressBar(
            drawList,
            viewModel.X, viewModel.Y,
            viewModel.Width, viewModel.Height,
            viewModel.Percentage,
            viewModel.FillColor,
            viewModel.BackgroundColor,
            viewModel.FormattedLabel,
            viewModel.ShowGlow
        );
    }
}
```

**When to use ViewModel approach:**
- ✅ Multiple progress bar types with different colors
- ✅ Complex label formatting logic
- ✅ Business rules for when to show glow
- ✅ Need to test calculation logic separately from rendering

**When NOT to use:**
- ❌ Simple, one-off progress bars
- ❌ Caller already has all values calculated
- ❌ Current approach works fine

## Comparison with Problematic Renderers

### ProgressBarRenderer (Good ✅)

```csharp
// All inputs explicit, no side effects
ProgressBarRenderer.DrawProgressBar(
    drawList,
    x: 100, y: 50, width: 200, height: 20,
    percentage: 0.75f,
    fillColor: ColorPalette.Gold,
    backgroundColor: ColorPalette.DarkBrown,
    labelText: "75%",
    showGlow: true
);

// Easy to test:
// - Mock drawList
// - Verify draw commands
// - No state to set up
// - No side effects to check
```

### ReligionBrowseRenderer (Problematic ❌)

```csharp
// Hidden dependencies, side effects everywhere
ReligionBrowseRenderer.Draw(
    manager,  // Mutable state object
    api,      // Sound, network, etc.
    x, y, width, height
);

// Inside the renderer:
state.DeityFilter = newFilter;        // State mutation
manager.RequestReligionList(...);     // Network call
api.World.PlaySoundAt(...);           // Sound effect
state.CurrentSubTab = ...;            // Navigation

// Hard to test:
// - Need full manager setup
// - Need to mock API
// - Side effects mixed with rendering
// - State mutations hard to verify
```

## Testing Infrastructure Needed

### MockImDrawList Implementation

```csharp
public class MockImDrawList : ImDrawListPtr
{
    public List<FilledRect> FilledRects { get; } = new();
    public List<Rect> Rects { get; } = new();
    public List<TextCall> TextCalls { get; } = new();
    public List<CircleCall> Circles { get; } = new();

    public override void AddRectFilled(Vector2 min, Vector2 max, uint color, float rounding)
    {
        FilledRects.Add(new FilledRect(min, max, color, rounding));
    }

    public override void AddRect(Vector2 min, Vector2 max, uint color, float rounding, ImDrawFlags flags, float thickness)
    {
        Rects.Add(new Rect(min, max, color, rounding, thickness));
    }

    public override void AddText(Vector2 pos, uint color, string text)
    {
        TextCalls.Add(new TextCall(pos, color, text));
    }

    // Mock text size calculation
    private Dictionary<string, Vector2> _textSizes = new();

    public void MockTextSize(string text, Vector2 size)
    {
        _textSizes[text] = size;
    }

    public Vector2 CalcTextSize(string text)
    {
        return _textSizes.TryGetValue(text, out var size)
            ? size
            : new Vector2(text.Length * 8, 14); // Default estimate
    }
}

public record FilledRect(Vector2 Min, Vector2 Max, uint Color, float Rounding);
public record Rect(Vector2 Min, Vector2 Max, uint Color, float Rounding, float LineThickness);
public record TextCall(Vector2 Position, uint Color, string Text);
```

## Recommendations

### For ProgressBarRenderer

**✅ KEEP CURRENT DESIGN** - it's already excellent

Only consider ViewModel if you need:
1. Complex bar type logic (multiple color schemes)
2. Business rules for glow/animations
3. Label formatting logic
4. Reusable bar configurations

### For Other Renderers

**Use ProgressBarRenderer as your template**:

1. Make all inputs explicit parameters
2. No state mutations
3. No side effects (network, sound, navigation)
4. Return values/events instead of mutating state
5. Keep rendering logic separate from business logic

### Testing Strategy

1. Create `MockImDrawList` helper (shared across all renderer tests)
2. Write unit tests for ProgressBarRenderer as examples
3. Use these tests as templates for refactored renderers
4. Focus on testing visual logic, not just coverage

## Next Steps

1. ✅ Use ProgressBarRenderer as "already good" example
2. Create MockImDrawList test infrastructure
3. Write example unit tests for ProgressBarRenderer
4. Use pattern to refactor ReligionInvitesRenderer next
5. Document patterns for team

## Key Takeaway

**ProgressBarRenderer proves that simple, testable renderers are possible without complex architecture.**

The pattern is simple:
```
Pure Function: (DrawList + Parameters) → Drawing Commands
```

No ViewModels needed for simple cases. Just:
- Explicit parameters
- No state mutations
- No side effects
- Single responsibility

Apply this same simplicity to more complex renderers by extracting side effects and state management into event handlers.
