# Rank-Up Notification System - Task Checklist

## Phase 1: Data Models & Utilities

### Task 1.1: Create NotificationType Enum

- [x] Create file `/DivineAscension/Models/Enum/NotificationType.cs`
- [x] Define enum with `FavorRankUp` and `PrestigeRankUp` values

### Task 1.2: Create NotificationState Class

- [x] Create file `/DivineAscension/GUI/State/NotificationState.cs`
- [x] Define `NotificationState` class with properties:
    - [x] `IsVisible` (bool)
    - [x] `Type` (NotificationType)
    - [x] `RankName` (string)
    - [x] `RankDescription` (string)
    - [x] `Deity` (DeityType)
    - [x] `DisplayDuration` (float, default 8f)
    - [x] `ElapsedTime` (float)
    - [x] `PendingNotifications` (Queue)
- [x] Define `PendingNotification` record with Type, RankName, RankDescription, Deity

### Task 1.3: Create FavorRankDescriptions Utility

- [x] Create file `/DivineAscension/GUI/Utilities/FavorRankDescriptions.cs`
- [x] Implement static `GetDescription(FavorRank)` method
- [x] Add descriptions for all ranks: Initiate, Disciple, Zealot, Champion, Avatar

### Task 1.4: Create PrestigeRankDescriptions Utility

- [x] Create file `/DivineAscension/GUI/Utilities/PrestigeRankDescriptions.cs`
- [x] Implement static `GetDescription(PrestigeRank)` method
- [x] Add descriptions for all ranks: Fledgling, Established, Renowned, Legendary, Mythic

### Task 1.5: Update GuiDialogState

- [x] Open `/DivineAscension/GUI/State/GuiDialogState.cs`
- [x] Add `NotificationState` property
- [x] Add `PreviousFavorRank` property
- [x] Add `PreviousPrestigeRank` property
- [x] Update `Reset()` method to reset notification state

## Phase 2: Notification Manager

### Task 2.1: Create NotificationManager Class

- [x] Create file `/DivineAscension/GUI/Managers/NotificationManager.cs`
- [x] Add constructor accepting `ISoundManager` and `NotificationState`
- [x] Implement `QueueRankUpNotification(type, rankName, deity)` method
    - [x] Add to queue
    - [x] Show immediately if no active notification
- [x] Implement `ShowNextNotification()` method
    - [x] Dequeue pending notification
    - [x] Set state properties
    - [x] Set `IsVisible = true`
    - [x] Play deity-specific sound
- [x] Implement `Update(deltaTime)` method
    - [x] Increment `ElapsedTime`
    - [x] Auto-dismiss after `DisplayDuration`
- [x] Implement `DismissCurrentNotification()` method
    - [x] Hide current notification
    - [x] Show next in queue if available
- [x] Implement `GetRankDescription(type, rankName)` helper
    - [x] Call appropriate description utility
- [x] Implement `OnViewBlessingsClicked(openCallback, setTabCallback)` method
    - [x] Dismiss current notification
    - [x] Call open callback if needed
    - [x] Set tab to Blessings

### Task 2.2: Add Queue Management Logic

- [x] Implement max queue size limit (5)
- [x] Drop oldest when queue exceeds limit

## Phase 3: Overlay Component

### Task 3.1: Create RankUpNotificationOverlay Component

- [x] Create file `/DivineAscension/GUI/UI/Components/Overlays/RankUpNotificationOverlay.cs`
- [x] Define `Draw()` static method signature:
    - [x] Parameters: `NotificationState`, `out bool dismissed`, `out bool viewBlessingsClicked`, `float windowWidth`,
      `float windowHeight`

### Task 3.2: Implement Overlay Rendering

- [x] Draw semi-transparent backdrop (ColorPalette.BlackOverlay, 0.7f alpha)
- [x] Detect backdrop click for dismiss
- [x] Calculate panel center position
- [x] Calculate panel height based on wrapped text
- [x] Draw centered panel (500px width, auto height, rounded corners)
- [x] Draw deity icon (64×64px, centered, using `DeityIconLoader`)
- [x] Draw "Rank Up!" title (20pt white, using `TextRenderer.DrawLabel`)
- [x] Draw rank name (18pt gold)
- [x] Draw description (word-wrapped, 13pt grey, using `TextRenderer.DrawInfoText`)
- [x] Draw "View Blessings (Shift+G)" button (200×40px, using `ButtonRenderer.DrawButton`)
- [x] Detect ESC key for dismiss
- [x] Set `dismissed` out parameter on dismiss actions
- [x] Set `viewBlessingsClicked` out parameter on button click

## Phase 4: GuiDialogManager Integration

### Task 4.1: Update GuiDialogManager

- [x] Open `/DivineAscension/GUI/GuiDialogManager.cs`
- [x] Add `NotificationManager` property
- [x] Initialize in constructor: `new NotificationManager(soundManager, new NotificationState())`

## Phase 5: Event Handling

### Task 5.1: Create Rank Comparison Event Handler

- [x] Open `/DivineAscension/GUI/GuiDialogHandlers.cs`
- [x] Add `OnPlayerReligionDataUpdated(PlayerReligionDataPacket packet)` handler
- [x] Compare `packet.FavorRank` vs `_state.PreviousFavorRank`
    - [x] If increased, queue favor rank-up notification
- [x] Compare `packet.PrestigeRank` vs `_state.PreviousPrestigeRank`
    - [x] If increased, queue prestige rank-up notification
- [x] Update `_state.PreviousFavorRank` with current rank
- [x] Update `_state.PreviousPrestigeRank` with current rank

### Task 5.2: Wire Event in GuiDialog

- [x] Open `/DivineAscension/GUI/GuiDialog.cs`
- [x] In `StartClientSide()` after line 93:
    - [x] Subscribe: `_divineAscensionModSystem.NetworkClient.PlayerReligionDataUpdated += OnPlayerReligionDataUpdated;`
- [x] In `Dispose()`:
    - [x] Unsubscribe:
      `_divineAscensionModSystem.NetworkClient.PlayerReligionDataUpdated -= OnPlayerReligionDataUpdated;`

## Phase 6: Rendering Integration

### Task 6.1: Update DrawWindow Method

- [x] Open `/DivineAscension/GUI/GuiDialog.cs`
- [x] In `DrawWindow()` after `MainDialogRenderer.Draw()`:
    - [x] Call `_manager!.NotificationManager.Update(deltaTime)`
    - [x] Call `RankUpNotificationOverlay.Draw()` with state and out parameters
    - [x] If `dismissed` is true, call `_manager.NotificationManager.DismissCurrentNotification()`
    - [x] If `viewBlessingsClicked` is true, call `_manager.NotificationManager.OnViewBlessingsClicked()` with callbacks

### Task 6.2: Update Open Method Visibility

- [x] Open `/DivineAscension/GUI/GuiDialog.cs`
- [x] Change `Open()` method from `private` to `internal`

## Phase 7: Edge Cases & Cleanup

### Task 7.1: Handle Religion State Changes

- [x] In GuiDialogHandlers, update `OnReligionStateChanged` (if exists)
- [ ] Clear notification queue when player leaves religion

### Task 7.2: Initialize Previous Ranks

- [x] Ensure `PreviousFavorRank` and `PreviousPrestigeRank` are initialized on first data load
- [x] Set them to current ranks to prevent false positives on first update

## Phase 8: Testing

### Task 8.1: Manual Testing - Favor Rank-Up

- [x] Use `/favor add` command to trigger rank-up
- [x] Verify notification appears with correct deity icon
- [x] Verify rank name and description display correctly
- [ ] Verify deity-specific sound plays

### Task 8.2: Manual Testing - Prestige Rank-Up

- [ ] Perform activities to increase religion prestige
- [ ] Verify notification appears when prestige rank increases
- [ ] Verify correct rank information displays

### Task 8.3: Manual Testing - Queue System

- [ ] Trigger multiple rapid rank-ups
- [ ] Verify notifications display sequentially
- [ ] Verify queue limit (max 5)

### Task 8.4: Manual Testing - Button Interaction

- [x] Click "View Blessings (Shift+G)" button
- [x] Verify dialog opens (if closed)
- [x] Verify navigation to Blessings tab
- [x] Verify notification dismisses

### Task 8.5: Manual Testing - Dismiss Mechanisms

- [ ] Test backdrop click dismiss
- [ ] Test ESC key dismiss
- [ ] Verify next notification shows after dismiss (if queued)

### Task 8.6: Manual Testing - Auto-Dismiss

- [ ] Wait 8 seconds without interaction
- [ ] Verify notification auto-dismisses
- [ ] Verify next notification shows (if queued)

### Task 8.7: Manual Testing - Edge Cases

- [ ] Verify notification doesn't break when dialog is closed
- [ ] Test leaving religion while notification is active
- [ ] Test server lag/delayed packets (rank comparison handles skipped ranks)

## Phase 9: Documentation & Finalization

### Task 9.1: Code Review

- [ ] Review all new files for code quality
- [ ] Verify proper null checks and error handling
- [ ] Ensure consistent naming conventions
- [ ] Verify proper resource disposal

### Task 9.2: Build Verification

- [ ] Run `dotnet build DivineAscension.sln -c Debug`
- [ ] Fix any compilation errors
- [ ] Run `dotnet build DivineAscension.sln -c Release`
- [ ] Verify no warnings

### Task 9.3: Final Testing Pass

- [ ] Complete all manual tests from Phase 8
- [ ] Test in multiplayer scenario (if applicable)
- [ ] Verify no performance issues with notification system

---

## Summary

- **Total New Files:** 6
- **Total Modified Files:** 4
- **Testing Scenarios:** 7
- **Edge Cases Handled:** 5
