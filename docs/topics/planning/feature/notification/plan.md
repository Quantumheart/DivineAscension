# Rank-Up Notification System Implementation Plan

## Overview

Implement visual popup notifications when players rank up (favor or prestige) with a button to open the GUI and navigate
to the Blessings tab using Shift+G.

## Architecture

### Detection Strategy

**Client-side rank comparison** - No new network packets needed:

- Store previous `FavorRank` and `PrestigeRank` in `GuiDialogState`
- Compare ranks when `PlayerReligionDataUpdated` event fires
- Trigger notification if rank increased

### UI Component Structure

**Overlay-based notification** following the `ConfirmOverlay` pattern:

- Semi-transparent backdrop (ColorPalette.BlackOverlay, 0.7f alpha)
- Centered panel: 500px width × auto height
- Deity icon (64×64px), title, rank name, description
- "View Blessings (Shift+G)" button (200×40px)
- Auto-dismiss after 8 seconds OR manual dismiss (backdrop click, ESC, button)

### State Management

**NotificationManager with queue**:

- Single notification visible at a time
- Queue up to 5 notifications (drop oldest if exceeded)
- Sequential display with auto-dismiss timer
- Plays deity-specific sound on display

## Implementation Steps

### 1. Create Data Models & State

**New Files:**

`/DivineAscension/Models/Enum/NotificationType.cs`

```csharp
public enum NotificationType
{
    FavorRankUp,
    PrestigeRankUp
}
```

`/DivineAscension/GUI/State/NotificationState.cs`

```csharp
public class NotificationState
{
    public bool IsVisible { get; set; }
    public NotificationType Type { get; set; }
    public string RankName { get; set; } = "";
    public string RankDescription { get; set; } = "";
    public DeityType Deity { get; set; }
    public float DisplayDuration { get; set; } = 8f;
    public float ElapsedTime { get; set; }
    public Queue<PendingNotification> PendingNotifications { get; set; } = new();
}

public record PendingNotification(
    NotificationType Type,
    string RankName,
    string RankDescription,
    DeityType Deity);
```

`/DivineAscension/GUI/Utilities/FavorRankDescriptions.cs`

```csharp
public static class FavorRankDescriptions
{
    public static string GetDescription(FavorRank rank) => rank switch
    {
        FavorRank.Initiate => "You have begun your journey of devotion.",
        FavorRank.Disciple => "Your faith grows stronger. New blessings await!",
        FavorRank.Zealot => "Your dedication is recognized by the divine.",
        FavorRank.Champion => "You are a true champion of your deity!",
        FavorRank.Avatar => "You have achieved the highest divine favor!",
        _ => "Your devotion has been recognized."
    };
}
```

`/DivineAscension/GUI/Utilities/PrestigeRankDescriptions.cs`

```csharp
public static class PrestigeRankDescriptions
{
    public static string GetDescription(PrestigeRank rank) => rank switch
    {
        PrestigeRank.Fledgling => "Your religion begins its sacred journey.",
        PrestigeRank.Established => "Your religion's influence grows!",
        PrestigeRank.Renowned => "Your religion commands respect across the lands.",
        PrestigeRank.Legendary => "Your religion's legend spreads far and wide!",
        PrestigeRank.Mythic => "Your religion has achieved mythic status!",
        _ => "Your religion's prestige has increased."
    };
}
```

**Modify:** `/DivineAscension/GUI/State/GuiDialogState.cs`

- Add properties: `NotificationState`, `PreviousFavorRank`, `PreviousPrestigeRank`
- Update `Reset()` method to reset notification state

### 2. Create Notification Manager

**New File:** `/DivineAscension/GUI/Managers/NotificationManager.cs`

**Key Methods:**

- `QueueRankUpNotification(type, rankName, deity)` - Add to queue, show immediately if available
- `ShowNextNotification()` - Dequeue and display, play deity sound
- `Update(deltaTime)` - Auto-dismiss timer
- `DismissCurrentNotification()` - Hide and show next
- `GetRankDescription(type, rankName)` - Call appropriate description utility
- `OnViewBlessingsClicked(openCallback, setTabCallback)` - Handle button click

**Dependencies:** `ISoundManager`, `NotificationState`

### 3. Create Overlay Component

**New File:** `/DivineAscension/GUI/UI/Components/Overlays/RankUpNotificationOverlay.cs`

**Signature:**

```csharp
public static void Draw(
    NotificationState state,
    out bool dismissed,
    out bool viewBlessingsClicked,
    float windowWidth,
    float windowHeight)
```

**Rendering:**

1. Draw backdrop (semi-transparent, clickable for dismiss)
2. Calculate panel height (based on wrapped text)
3. Draw centered panel with rounded corners
4. Draw deity icon (64×64, centered, using `DeityIconLoader`)
5. Draw "Rank Up!" title (20pt white, `TextRenderer.DrawLabel`)
6. Draw rank name (18pt gold)
7. Draw description (word-wrapped, 13pt grey, `TextRenderer.DrawInfoText`)
8. Draw button (`ButtonRenderer.DrawButton`)
9. Detect backdrop click/ESC for dismiss
10. Return interaction flags via out parameters

### 4. Integrate with GuiDialog

**Modify:** `/DivineAscension/GUI/GuiDialogManager.cs`

- Add property: `public NotificationManager NotificationManager { get; }`
- Initialize in constructor: `NotificationManager = new NotificationManager(soundManager, new NotificationState())`

**Modify:** `/DivineAscension/GUI/GuiDialogHandlers.cs`

- Add new event handler: `OnPlayerReligionDataUpdated(PlayerReligionDataPacket packet)`
- Compare `packet.FavorRank` vs `_state.PreviousFavorRank`
- Compare `packet.PrestigeRank` vs `_state.PreviousPrestigeRank`
- If rank increased, queue notification via `NotificationManager`
- Update stored ranks in state

**Modify:** `/DivineAscension/GUI/GuiDialog.cs`

**Wire up event in `StartClientSide()`** (after line 93):

```csharp
_divineAscensionModSystem.NetworkClient.PlayerReligionDataUpdated += OnPlayerReligionDataUpdated;
```

**Update `DrawWindow()` method** (after `MainDialogRenderer.Draw()`):

```csharp
// Update notification manager timer
_manager!.NotificationManager.Update(deltaTime);

// Draw notification overlay (if visible)
RankUpNotificationOverlay.Draw(
    _state.NotificationState,
    out bool dismissed,
    out bool viewBlessingsClicked,
    windowWidth,
    windowHeight
);

if (dismissed)
{
    _manager.NotificationManager.DismissCurrentNotification();
}

if (viewBlessingsClicked)
{
    _manager.NotificationManager.OnViewBlessingsClicked(
        openDialogCallback: () => { if (!_state.IsOpen) Open(); },
        setTabCallback: (tab) => { _state.CurrentMainTab = tab; }
    );
}
```

**Change `Open()` visibility** from `private` to `internal`

**Update `Dispose()`** - Unsubscribe from event:

```csharp
_divineAscensionModSystem.NetworkClient.PlayerReligionDataUpdated -= OnPlayerReligionDataUpdated;
```

## Data Flow

```
SERVER: PlayerReligionDataManager.AddFavor()
  ↓ (rank increases)
  ↓ Triggers: OnPlayerDataChanged event
  ↓
SERVER: PlayerDataNetworkHandler sends PlayerReligionDataPacket
  ↓
CLIENT: DivineAscensionNetworkClient.OnServerPlayerDataUpdate()
  ↓ Fires: PlayerReligionDataUpdated event
  ↓
CLIENT: GuiDialog.OnPlayerReligionDataUpdated()
  ↓ Compare ranks, detect increase
  ↓ Call: NotificationManager.QueueRankUpNotification()
  ↓
CLIENT: NotificationManager.ShowNextNotification()
  ↓ Set state.IsVisible = true
  ↓ Play deity sound
  ↓
CLIENT: GuiDialog.DrawWindow()
  ↓ Update timer, draw overlay
  ↓
CLIENT: RankUpNotificationOverlay.Draw()
  ↓ User clicks button
  ↓ Returns: viewBlessingsClicked = true
  ↓
CLIENT: NotificationManager.OnViewBlessingsClicked()
  ↓ Open dialog (if closed)
  ↓ Navigate to MainDialogTab.Blessings
```

## Critical Files

### New Files (7):

1. `/DivineAscension/Models/Enum/NotificationType.cs`
2. `/DivineAscension/GUI/State/NotificationState.cs`
3. `/DivineAscension/GUI/Utilities/FavorRankDescriptions.cs`
4. `/DivineAscension/GUI/Utilities/PrestigeRankDescriptions.cs`
5. `/DivineAscension/GUI/Managers/NotificationManager.cs`
6. `/DivineAscension/GUI/UI/Components/Overlays/RankUpNotificationOverlay.cs`

### Modified Files (4):

1. `/DivineAscension/GUI/State/GuiDialogState.cs` - Add notification state properties
2. `/DivineAscension/GUI/GuiDialogManager.cs` - Add NotificationManager property
3. `/DivineAscension/GUI/GuiDialogHandlers.cs` - Add rank comparison event handler
4. `/DivineAscension/GUI/GuiDialog.cs` - Wire events, render overlay, update timer

## Edge Cases

1. **Player leaves religion mid-notification** - Clear queue on `OnReligionStateChanged`
2. **Multiple rapid rank-ups** - Queue system handles sequential display
3. **Notification spam** - Max 5 in queue, oldest dropped
4. **Dialog closed during notification** - Notification renders independently
5. **Server lag/packet delays** - Comparison-based detection handles skipped ranks

## Testing

**Manual tests:**

- Single favor rank-up via `/favor add` command
- Religion prestige rank-up via activities
- Multiple rapid rank-ups (verify queue)
- Button interaction (open dialog + navigate to Blessings)
- Backdrop/ESC dismiss
- Auto-dismiss after 8 seconds
- Sound playback verification
