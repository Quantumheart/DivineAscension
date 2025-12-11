# Religion Activity Tracking - Implementation Plan

**Date**: 2025-12-10
**Branch**: `system_refactoring`
**Status**: Planning Phase

---

## Executive Summary

This document outlines the implementation plan for adding a **persistent activity tracking system** to the PantheonWars
religion feature. The system will log and display membership changes (invite, join, leave, kick), ban/unban actions,
prestige milestones, and favor milestones.

### Current State

- ✅ Religion system with membership management (join/leave/kick/invite)
- ✅ Ban/unban system implemented
- ✅ Prestige and favor progression systems
- ✅ Activity renderer placeholder exists (`ReligionActivityRenderer.cs`)
- ❌ No activity logging on server-side
- ❌ No persistence of activity events
- ❌ Activity tab shows "Coming Soon" placeholder

### Proposed Solution

Implement a comprehensive activity tracking system that:

- Logs all membership changes (invite, join, leave, kick) server-side
- Tracks ban/unban actions with reasons
- Records prestige milestones and rank changes
- Records individual favor milestones and rank changes
- Persists activity log (last 50 events per religion) to disk
- Displays activity feed in client UI with filtering capabilities
- Maintains existing Event-Driven Architecture (EDA) pattern

---

## User Requirements

Based on user input:

- **Persistence**: Activity log must be saved to disk and persist across server restarts
- **Tracked Events**:
    - Membership changes (invite, join, leave, kick)
    - Ban/unban actions
    - Prestige milestones (500, 1000, 2500, 5000, 7500, 10000, 15000, 20000)
    - Favor milestones (500, 1000, 2500, 5000, 7500, 10000, 15000, 20000)
- **History Limit**: Keep last 50 events per religion (FIFO queue)
- **UI Features**: Filter by event type in the activity feed renderer

---

## Architecture Overview

### Data Flow

```
Server Action (join/kick/ban/prestige/favor)
    ↓
ReligionActivityLogger.LogActivity()
    ↓
Create ReligionActivityEntry
    ↓
religion.AddActivity() → FIFO queue (max 50)
    ↓
Save to ReligionData (ProtoBuf)
    ↓
[Client Requests Religion Info]
    ↓
Server builds ActivityEntryInfo list (resolve player names)
    ↓
Send in PlayerReligionInfoResponsePacket
    ↓
Client ActivityState.UpdateActivities()
    ↓
ReligionActivityRenderer.Draw() with filtering
```

### Key Design Decisions

1. **Storage**: Activity entries stored directly in `ReligionData.ActivityLog` (List<ReligionActivityEntry>)
2. **Limit Enforcement**: `AddActivity()` method maintains 50-event limit using FIFO (remove oldest)
3. **Serialization**: ProtoBuf for efficient persistence
4. **Player Name Resolution**: Server resolves UIDs to names when sending to client (avoids stale names)
5. **Filtering**: Client-side filtering using HashSet<ReligionActivityType> (empty = show all)
6. **Milestone Detection**: Check threshold crossings when adding prestige/favor

---

## Implementation Phases

### Phase 1: Data Models

#### 1.1 Create Activity Type Enum

**New File**: `PantheonWars/Models/Enum/ReligionActivityType.cs`

```csharp
public enum ReligionActivityType
{
    MemberJoined = 0,
    MemberLeft = 1,
    MemberKicked = 2,
    MemberInvited = 3,
    PlayerBanned = 4,
    PlayerUnbanned = 5,
    PrestigeRankUp = 6,
    PrestigeMilestone = 7,
    FavorRankUp = 8,
    FavorMilestone = 9
}
```

#### 1.2 Create Activity Entry Model

**New File**: `PantheonWars/Data/ReligionActivityEntry.cs`

```csharp
[ProtoContract]
public class ReligionActivityEntry
{
    [ProtoMember(1)] public string ActivityId { get; set; } = string.Empty;
    [ProtoMember(2)] public ReligionActivityType ActivityType { get; set; }
    [ProtoMember(3)] public DateTime Timestamp { get; set; }
    [ProtoMember(4)] public string ActorPlayerUID { get; set; } = string.Empty;
    [ProtoMember(5)] public string? TargetPlayerUID { get; set; }
    [ProtoMember(6)] public Dictionary<string, string> Metadata { get; set; } = new();
}
```

**Metadata Examples**:

- Bans: `"Reason"`, `"ExpiryDays"`
- Prestige: `"OldRank"`, `"NewRank"`, `"Amount"`
- Favor: `"OldRank"`, `"NewRank"`, `"TotalFavor"`

#### 1.3 Update ReligionData

**File**: `PantheonWars/Data/ReligionData.cs`

Add after line 113 (after `BannedPlayers`):

```csharp
/// <summary>
///     Activity log for this religion (last 50 events)
/// </summary>
[ProtoMember(14)]
public List<ReligionActivityEntry> ActivityLog { get; set; } = new();

/// <summary>
///     Adds an activity entry and maintains the 50-event limit (FIFO)
/// </summary>
public void AddActivity(ReligionActivityEntry entry)
{
    ActivityLog.Add(entry);

    // Keep only last 50 entries
    if (ActivityLog.Count > 50)
    {
        ActivityLog.RemoveAt(0);  // Remove oldest
    }
}
```

### Phase 2: Server-Side Activity Logger

#### 2.1 Create Activity Logger Service

**New File**: `PantheonWars/Systems/ReligionActivityLogger.cs`

Interface `IReligionActivityLogger` with logging methods for each activity type.

Implementation:

- Creates `ReligionActivityEntry` with GUID, timestamp, actor/target UIDs
- Calls `_religionManager.GetReligion()` to get religion
- Calls `religion.AddActivity(entry)`
- Saves religion data

**Milestone Thresholds**: 500, 1000, 2500, 5000, 7500, 10000, 15000, 20000

#### 2.2 Hook Into Existing Handlers

**File**: `PantheonWars/Systems/Networking/Server/ReligionNetworkHandler.cs`

Add logging calls in `OnReligionActionRequest`:

- Line ~195 (after join): `_activityLogger.LogMemberJoined()`
- Line ~223 (after accept): `_activityLogger.LogMemberJoined()`
- Line ~238 (after leave): `_activityLogger.LogMemberLeft()`
- Line ~265 (after kick): `_activityLogger.LogMemberKicked()`
- Line ~336 (after ban): `_activityLogger.LogPlayerBanned()`
- Line ~376 (after unban): `_activityLogger.LogPlayerUnbanned()`
- Line ~423 (after invite): `_activityLogger.LogMemberInvited()`

**File**: `PantheonWars/Systems/ReligionPrestigeManager.cs`

Add after prestige addition (line ~70):

- Check for milestone crossings
- Log `PrestigeMilestone` when threshold crossed
- Log `PrestigeRankUp` when rank changes (line ~77)

**File**: `PantheonWars/Systems/PlayerReligionDataManager.cs`

Add after favor addition (line ~86):

- Check for favor milestone crossings
- Log `FavorMilestone` when threshold crossed
- Log `FavorRankUp` when rank changes (line ~92)

### Phase 3: Network Layer

#### 3.1 Create Activity Data Packet

**New File**: `PantheonWars/Network/ReligionActivityDataPacket.cs`

```csharp
[ProtoContract]
public class ReligionActivityDataPacket
{
    [ProtoMember(1)] public List<ActivityEntryInfo> Activities { get; set; } = new();

    [ProtoContract]
    public class ActivityEntryInfo
    {
        [ProtoMember(1)] public string ActivityId { get; set; } = string.Empty;
        [ProtoMember(2)] public string ActivityType { get; set; } = string.Empty;
        [ProtoMember(3)] public DateTime Timestamp { get; set; }
        [ProtoMember(4)] public string ActorPlayerUID { get; set; } = string.Empty;
        [ProtoMember(5)] public string ActorPlayerName { get; set; } = string.Empty;
        [ProtoMember(6)] public string? TargetPlayerUID { get; set; }
        [ProtoMember(7)] public string? TargetPlayerName { get; set; }
        [ProtoMember(8)] public Dictionary<string, string> Metadata { get; set; } = new();
    }
}
```

#### 3.2 Update Response Packet

**File**: `PantheonWars/Network/PlayerReligionInfoResponsePacket.cs`

Add field:

```csharp
[ProtoMember(14)] public List<ReligionActivityDataPacket.ActivityEntryInfo> Activities { get; set; } = new();
```

#### 3.3 Populate Activities in Response

**File**: `PantheonWars/Systems/Networking/Server/ReligionNetworkHandler.cs`

In `OnPlayerReligionInfoRequest` (line ~133, before sending response):

```csharp
// Build activity list
foreach (var activityEntry in religion.ActivityLog.OrderByDescending(a => a.Timestamp))
{
    var actorPlayer = _sapi.World.PlayerByUid(activityEntry.ActorPlayerUID);
    var targetPlayer = activityEntry.TargetPlayerUID != null
        ? _sapi.World.PlayerByUid(activityEntry.TargetPlayerUID)
        : null;

    response.Activities.Add(new ReligionActivityDataPacket.ActivityEntryInfo
    {
        ActivityId = activityEntry.ActivityId,
        ActivityType = activityEntry.ActivityType.ToString(),
        Timestamp = activityEntry.Timestamp,
        ActorPlayerUID = activityEntry.ActorPlayerUID,
        ActorPlayerName = actorPlayer?.PlayerName ?? activityEntry.ActorPlayerUID,
        TargetPlayerUID = activityEntry.TargetPlayerUID,
        TargetPlayerName = targetPlayer?.PlayerName,
        Metadata = activityEntry.Metadata
    });
}
```

### Phase 4: Client-Side State

#### 4.1 Update ActivityState

**File**: `PantheonWars/GUI/State/Religion/ActivityState.cs`

Replace implementation:

```csharp
public class ActivityState
{
    public List<ReligionActivityDataPacket.ActivityEntryInfo> Activities { get; set; } = new();
    public float ActivityScrollY { get; set; }
    public HashSet<ReligionActivityType> ActiveFilters { get; set; } = new();

    public void Reset()
    {
        Activities.Clear();
        ActivityScrollY = 0f;
        ActiveFilters.Clear();
    }

    public void UpdateActivities(List<ReligionActivityDataPacket.ActivityEntryInfo> newActivities)
    {
        Activities = newActivities;
    }

    public void ToggleFilter(ReligionActivityType filterType)
    {
        if (ActiveFilters.Contains(filterType))
            ActiveFilters.Remove(filterType);
        else
            ActiveFilters.Add(filterType);
    }
}
```

#### 4.2 Update ViewModel

**File**: `PantheonWars/GUI/Models/Religion/Activity/ReligionActivityViewModel.cs`

```csharp
public readonly struct ReligionActivityViewModel
{
    public float X { get; }
    public float Y { get; }
    public float Width { get; }
    public float Height { get; }
    public IReadOnlyList<ReligionActivityDataPacket.ActivityEntryInfo> Activities { get; }
    public float ScrollY { get; }
    public IReadOnlySet<ReligionActivityType> ActiveFilters { get; }

    public ReligionActivityViewModel(
        float x, float y, float width, float height,
        IReadOnlyList<ReligionActivityDataPacket.ActivityEntryInfo> activities,
        float scrollY,
        IReadOnlySet<ReligionActivityType> activeFilters)
    {
        X = x;
        Y = y;
        Width = width;
        Height = height;
        Activities = activities;
        ScrollY = scrollY;
        ActiveFilters = activeFilters;
    }
}
```

#### 4.3 Define Activity Events

**File**: `PantheonWars/GUI/Events/ReligionActivityEvent.cs`

```csharp
public abstract record ReligionActivityEvent
{
    public record ScrollChanged(float ScrollY) : ReligionActivityEvent;
    public record FilterToggled(ReligionActivityType FilterType) : ReligionActivityEvent;
}
```

### Phase 5: UI Renderer Implementation

#### 5.1 Implement Activity Renderer

**File**: `PantheonWars/GUI/UI/Renderers/Religion/ReligionActivityRenderer.cs`

Replace placeholder with full implementation.

**Layout**:

```
┌─────────────────────────────────────────┐
│ [Filter Buttons Row - 40px height]     │
├─────────────────────────────────────────┤
│ [Scrollable Activity List]             │
│   [Icon] ActorName action (timestamp)  │
│          Additional details             │
│   [Icon] ActorName action (timestamp)  │
│          Additional details             │
│   ...                                   │
└─────────────────────────────────────────┘
```

**Activity Icons & Colors**:

- MemberJoined: `"+"` (green)
- MemberLeft: `"-"` (gray)
- MemberKicked: `"X"` (red)
- MemberInvited: `"✉"` (blue)
- PlayerBanned: `"⊗"` (dark red)
- PlayerUnbanned: `"○"` (yellow)
- PrestigeRankUp: `"↑"` (gold)
- PrestigeMilestone: `"★"` (gold)
- FavorRankUp: `"↑"` (cyan)
- FavorMilestone: `"◆"` (cyan)

**Key Methods**:

```csharp
public static ReligionActivityRenderResult Draw(ReligionActivityViewModel viewModel)
{
    // Main entry point
    // 1. Draw filter buttons
    // 2. Filter activities based on active filters
    // 3. Draw scrollable activity list
    // 4. Return events (scroll changes, filter toggles)
}

private static List<ActivityEntryInfo> FilterActivities(
    IReadOnlyList<ActivityEntryInfo> activities,
    IReadOnlySet<ReligionActivityType> activeFilters)
{
    // Empty filters = show all
    // Otherwise filter by active types
}

private static void DrawActivityItem(
    ImDrawListPtr drawList,
    ActivityEntryInfo activity,
    float x, float y, float width, float height)
{
    // Draw icon circle with color
    // Format message based on activity type
    // Draw timestamp (relative: "2h ago", "3d ago")
}

private static string FormatActivityMessage(ActivityEntryInfo activity, ReligionActivityType type)
{
    // Return formatted message per activity type
    // Examples:
    // - "PlayerName joined the religion"
    // - "PlayerName kicked TargetName"
    // - "PlayerName banned TargetName\nReason: Griefing"
    // - "Religion ranked up: Established → Renowned"
    // - "Religion reached 5000 prestige!"
    // - "PlayerName reached Champion rank"
}

private static string FormatTimestamp(DateTime timestamp)
{
    // Relative time:
    // - "Just now" (< 1 minute)
    // - "5m ago" (< 1 hour)
    // - "2h ago" (< 1 day)
    // - "3d ago" (< 7 days)
    // - "Nov 15" (>= 7 days)
}
```

Follow scrolling pattern from `MemberListRenderer` and `BanListRenderer`:

- Calculate scroll limits based on item count and height
- Handle mouse wheel input
- Draw scrollbar when content exceeds visible area
- Use clip rect for visibility culling

#### 5.2 Update State Manager

**File**: `PantheonWars/GUI/Managers/ReligionStateManager.cs`

Update `DrawReligionActivity` (lines 466-484):

```csharp
private void DrawReligionActivity(float x, float contentY, float width, float contentHeight)
{
    var vm = new ReligionActivityViewModel(
        x, contentY, width, contentHeight,
        _state.Activity.Activities,
        _state.Activity.ActivityScrollY,
        _state.Activity.ActiveFilters
    );

    var result = ReligionActivityRenderer.Draw(vm);
    ProcessActivityEvents(result.Events);
}

private void ProcessActivityEvents(IReadOnlyList<ReligionActivityEvent> resultEvents)
{
    if (resultEvents == null || resultEvents.Count == 0) return;

    foreach (var ev in resultEvents)
    {
        switch (ev)
        {
            case ReligionActivityEvent.ScrollChanged(var scrollY):
                _state.Activity.ActivityScrollY = scrollY;
                break;
            case ReligionActivityEvent.FilterToggled(var filterType):
                _state.Activity.ToggleFilter(filterType);
                break;
        }
    }
}
```

Add activity data loading in packet response handler:

```csharp
// When receiving PlayerReligionInfoResponsePacket
_state.Activity.UpdateActivities(packet.Activities);
```

### Phase 6: Integration & Testing

#### 6.1 Dependency Injection

**File**: `PantheonWars/PantheonWarsMod.cs`

Register in DI container:

```csharp
services.AddSingleton<IReligionActivityLogger, ReligionActivityLogger>();
```

Inject into:

- `ReligionNetworkHandler`
- `ReligionPrestigeManager`
- `PlayerReligionDataManager`

#### 6.2 Data Migration

No migration needed - ProtoBuf will automatically initialize empty `ActivityLog` lists for existing religions.

#### 6.3 Testing Checklist

**Server-Side**:

- [ ] Join religion → activity logged
- [ ] Leave religion → activity logged
- [ ] Kick member → activity logged
- [ ] Invite player → activity logged
- [ ] Ban player → activity logged with reason
- [ ] Unban player → activity logged
- [ ] Prestige milestones → logged at thresholds (500, 1000, 2500, etc.)
- [ ] Prestige rank up → logged with old/new rank
- [ ] Favor milestones → logged at thresholds
- [ ] Favor rank up → logged with old/new rank
- [ ] 50-event limit → oldest entries removed
- [ ] Activities persist across server restart

**Network**:

- [ ] Activities sent in `PlayerReligionInfoResponsePacket`
- [ ] Player names resolved correctly
- [ ] Metadata transmitted properly (ban reason, ranks, amounts)

**Client-Side**:

- [ ] Activities display in correct order (newest first)
- [ ] Filter buttons work (toggle on/off)
- [ ] Filters correctly show/hide activity types
- [ ] Scrolling works smoothly
- [ ] Mouse wheel scrolling works
- [ ] Scrollbar appears when needed
- [ ] Icons display correctly
- [ ] Colors match activity types
- [ ] Timestamps formatted correctly
- [ ] Empty state handled gracefully
- [ ] All activity messages formatted correctly

---

## Critical Files

### New Files (4)

1. `PantheonWars/Models/Enum/ReligionActivityType.cs` - Activity type enum
2. `PantheonWars/Data/ReligionActivityEntry.cs` - Activity data model
3. `PantheonWars/Systems/ReligionActivityLogger.cs` - Activity logging service
4. `PantheonWars/Network/ReligionActivityDataPacket.cs` - Network packet for activities

### Modified Files (10)

1. `PantheonWars/Data/ReligionData.cs` - Add ActivityLog list and AddActivity method
2. `PantheonWars/Systems/Networking/Server/ReligionNetworkHandler.cs` - Hook logging, send activities
3. `PantheonWars/Systems/ReligionPrestigeManager.cs` - Log prestige milestones/rankups
4. `PantheonWars/Systems/PlayerReligionDataManager.cs` - Log favor milestones/rankups
5. `PantheonWars/Network/PlayerReligionInfoResponsePacket.cs` - Add Activities field
6. `PantheonWars/GUI/State/Religion/ActivityState.cs` - Complete rewrite with filtering
7. `PantheonWars/GUI/Models/Religion/Activity/ReligionActivityViewModel.cs` - Add activity fields
8. `PantheonWars/GUI/Events/ReligionActivityEvent.cs` - Define event records
9. `PantheonWars/GUI/UI/Renderers/Religion/ReligionActivityRenderer.cs` - Replace placeholder
10. `PantheonWars/GUI/Managers/ReligionStateManager.cs` - Wire up rendering/events
11. `PantheonWars/PantheonWarsMod.cs` - DI registration

---

## Implementation Order

1. **Data Layer** (Phase 1)
    - Create `ReligionActivityType` enum
    - Create `ReligionActivityEntry` model
    - Update `ReligionData` with ActivityLog and AddActivity method

2. **Server Logic** (Phase 2)
    - Create `ReligionActivityLogger` service with interface
    - Hook into `ReligionNetworkHandler` action handlers
    - Hook into `ReligionPrestigeManager` for prestige tracking
    - Hook into `PlayerReligionDataManager` for favor tracking

3. **Network** (Phase 3)
    - Create `ReligionActivityDataPacket`
    - Update `PlayerReligionInfoResponsePacket`
    - Populate activities in `OnPlayerReligionInfoRequest`

4. **Client State** (Phase 4)
    - Update `ActivityState`
    - Update `ReligionActivityViewModel`
    - Define `ReligionActivityEvent` records

5. **UI** (Phase 5)
    - Implement `ReligionActivityRenderer` (filter buttons, scrollable list, item rendering)
    - Update `ReligionStateManager` (draw method, event processing, data loading)

6. **Integration** (Phase 6)
    - DI setup in `PantheonWarsMod`
    - Testing

---

## Milestone Thresholds Reference

**Prestige Milestones**: 500, 1000, 2500, 5000, 7500, 10000, 15000, 20000

**Favor Milestones**: 500, 1000, 2500, 5000, 7500, 10000, 15000, 20000

**Prestige Ranks**:

- Fledgling (0-499)
- Established (500-1,999)
- Renowned (2,000-4,999)
- Legendary (5,000-9,999)
- Mythic (10,000+)

**Favor Ranks**:

- Initiate (0-499)
- Disciple (500-1,999)
- Zealot (2,000-4,999)
- Champion (5,000-9,999)
- Avatar (10,000+)

---

## Notes

- Maintain existing EDA (Event-Driven Architecture) pattern throughout
- Follow pure functional rendering approach (immutable ViewModels)
- Use ProtoBuf for all data serialization
- Resolve player UIDs to names on server-side (avoid stale client-side names)
- Empty filters HashSet means "show all" activities
- FIFO queue ensures oldest activities removed when exceeding 50 events
- Relative timestamps for recent events, absolute dates for older events
