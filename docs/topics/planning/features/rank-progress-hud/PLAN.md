# Rank Progress HUD Overlay Implementation Plan

## Overview

Implement a persistent HUD overlay in the bottom-right corner showing:
1. **Favor rank progress** (Initiate → Avatar)
2. **Prestige progress** (Fledgling → Mythic)
3. **Current favor balance** (spendable points)

The overlay is always visible when the player has a religion.

## Visual Design

```
+----------------------------------+
| Disciple (320/500)          [==>]|  <- Favor rank + progress bar
| Established (1240/2500)     [==>]|  <- Prestige + progress bar
| [coin] 45                        |  <- Spendable favor
+----------------------------------+
```

- **Position:** Bottom-right, 20px from edges
- **Size:** 220px x 85px
- **Style:** Semi-transparent dark background, gold border, rounded corners

## Files to Create

### 1. `DivineAscension/GUI/State/RankProgressHudState.cs`
State container for HUD-specific state (visibility toggle, compact mode for future).

### 2. `DivineAscension/GUI/Models/Hud/RankProgressHudViewModel.cs`
Immutable record containing all display data:
- Favor: rank name, next rank, total earned, required, progress %, max flag
- Prestige: rank name, next rank, current, required, progress %, max flag
- Favor balance, compact mode, screen dimensions

### 3. `DivineAscension/GUI/UI/Components/Overlays/RankProgressHudOverlay.cs`
Static renderer class with `Draw(RankProgressHudViewModel vm)` method:
- Uses `ImGui.GetBackgroundDrawList()` for behind-UI rendering
- Draws panel background + border
- Draws two progress bars (reuses `ProgressBarRenderer.DrawProgressBar()`)
- Draws favor balance with coin icon

## Files to Modify

### 1. `DivineAscension/GUI/State/GuiDialogState.cs`
- Add `RankProgressHudState HudState { get; set; } = new();`
- Add to `Reset()`: `HudState.Reset();`

### 2. `DivineAscension/GUI/Managers/ReligionStateManager.cs`
Add helper method:
```csharp
public RankProgressHudViewModel BuildHudViewModel(float screenWidth, float screenHeight, bool isCompact)
```
Builds ViewModel from existing cached data (CurrentFavor, CurrentFavorRank, TotalFavorEarned, etc.)

### 3. `DivineAscension/GUI/GuiDialog.cs`
- Add `OnDrawRankProgressHud()` draw callback method
- Register callback in `StartClientSide()`: `_imguiModSystem.Draw += OnDrawRankProgressHud;`
- Unregister in `Dispose()`: `_imguiModSystem.Draw -= OnDrawRankProgressHud;`

## Implementation Steps

1. **Create state container** (`RankProgressHudState.cs`)
2. **Create ViewModel record** (`RankProgressHudViewModel.cs`)
3. **Update GuiDialogState** - add HudState property
4. **Add BuildHudViewModel** to ReligionStateManager
5. **Create overlay renderer** (`RankProgressHudOverlay.cs`)
6. **Register draw callback** in GuiDialog

## Key Patterns to Follow

- **Overlay pattern:** Follow `RankUpNotificationOverlay.cs` structure
- **Progress bar:** Reuse `ProgressBarRenderer.DrawProgressBar()`
- **Colors:** Use `ColorPalette` constants (Gold, Background, DarkBrown, White)
- **Text:** Use `TextRenderer.DrawLabel()` for text rendering
- **Rank names:** Use `RankRequirements.GetFavorRankName()` / `GetPrestigeRankName()`

## Critical Files Reference

| Purpose | Path |
|---------|------|
| Draw callback registration | `DivineAscension/GUI/GuiDialog.cs` |
| Data source | `DivineAscension/GUI/Managers/ReligionStateManager.cs` |
| Overlay pattern | `DivineAscension/GUI/UI/Components/Overlays/RankUpNotificationOverlay.cs` |
| Progress bar | `DivineAscension/GUI/UI/Renderers/Components/ProgressBarRenderer.cs` |
| Rank names | `DivineAscension/Systems/RankRequirements.cs` |
| Colors | `DivineAscension/GUI/UI/Utilities/ColorPalette.cs` |

## Verification

1. **Build:** `dotnet build DivineAscension.sln`
2. **Manual test in-game:**
   - Create/join a religion
   - Verify HUD appears in bottom-right
   - Earn favor (pray at altar) and confirm progress bar updates
   - Verify HUD hides when player leaves religion
3. **Check edge cases:**
   - Max rank display (Avatar/Mythic shows full bar)
   - No religion = no HUD
   - Progress bar glow at >80%
