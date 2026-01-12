# CRITICAL: Player progression reset on every login due to race condition in network handler

## User Report

A user reported: *"The update reset everyone else progress again, this happens every update except to me specifically"*

## Impact

**Severity:** CRITICAL
**Scope:** All players lose progression data (favor, rank, blessings) on every login
**Affected Users:** All players except the server host

## Root Cause

`PlayerDataNetworkHandler.SendPlayerDataToClient()` (line 83) calls `GetOrCreatePlayerData()` during player join, creating empty player data before the world save has finished loading. This empty data is then immediately saved during autosave (~1 second later), permanently overwriting all existing player progression.

## Technical Details

### The Deadly Sequence

1. **Player joins** → Two event handlers fire for `PlayerJoin`:
   - `PlayerProgressionDataManager.OnPlayerJoin` → Attempts `LoadPlayerData()` but world save not ready → Returns null
   - `PlayerDataNetworkHandler.OnPlayerJoin` → Calls `SendPlayerDataToClient()`

2. **Line 83 in PlayerDataNetworkHandler.cs:**
   ```csharp
   var playerReligionData = _playerProgressionDataManager!.GetOrCreatePlayerData(player.PlayerUID);
   ```
   This creates empty player data if none exists in memory (which it doesn't because load failed)

3. **Empty data added to dictionary** → Logged as: `[DivineAscension] Created new player progression data for {playerUID}`

4. **~1 second later** → World autosave fires → `SaveAllPlayerData()` → Saves empty data to disk, **overwriting** existing saved progression

5. **Later** (~20-30 seconds) → World finishes loading → `LoadPlayerData()` succeeds → Loads the empty data that was just saved

### Evidence from Server Logs

Every single day shows the same pattern for player `Major_Problem` (UID: `CqDdOoNRDeH0S1LHMWZnHU0w`):

**Jan 10:**
```
10:27:04 [Debug] [DivineAscension] Created new player progression data for CqDdOoNRDeH0S1LHMWZnHU0w
10:27:39 [Debug] [DivineAscension] Loaded data for player CqDdOoNRDeH0S1LHMWZnHU0w (v3)
```

**Jan 11:**
```
10:50:04 [Debug] [DivineAscension] Created new player progression data for CqDdOoNRDeH0S1LHMWZnHU0w
10:50:23 [Debug] [DivineAscension] Loaded data for player CqDdOoNRDeH0S1LHMWZnHU0w (v3)
```

**Jan 12 (detailed):**
```
10:24:28 [Debug] [DivineAscension] Created new player progression data for CqDdOoNRDeH0S1LHMWZnHU0w
10:24:28.873 [VerboseDebug] Completed area loading/generation
10:24:29 [Debug] [DivineAscension] Saved religion data for player CqDdOoNRDeH0S1LHMWZnHU0w  ← DISASTER!
10:24:51 [Debug] [DivineAscension] Loaded data for player CqDdOoNRDeH0S1LHMWZnHU0w (v3)
```

Notice: Empty data saved at 10:24:29, only 1 second after creation, before the real data loads at 10:24:51.

## Proposed Fix

**File:** `DivineAscension/Systems/Networking/Server/PlayerDataNetworkHandler.cs`

### Option 1: Don't create data if it doesn't exist (RECOMMENDED)

Change line 83 from:
```csharp
var playerReligionData = _playerProgressionDataManager!.GetOrCreatePlayerData(player.PlayerUID);
```

To:
```csharp
// Don't create data if load hasn't completed yet
if (!_playerProgressionDataManager.TryGetPlayerData(player.PlayerUID, out var playerReligionData))
    return; // Data not loaded yet, skip sending packet (will be sent when data loads)
```

This requires adding a `TryGetPlayerData()` method to `IPlayerProgressionDataManager`:
```csharp
bool TryGetPlayerData(string playerUID, out PlayerProgressionData? data);
```

Implementation in `PlayerProgressionDataManager.cs`:
```csharp
public bool TryGetPlayerData(string playerUID, out PlayerProgressionData? data)
{
    return _playerData.TryGetValue(playerUID, out data);
}
```

### Option 2: Wait for data to load before sending

Add a flag/event to track when world data is fully loaded, and defer network packet sending until load completes.

### Option 3: Don't auto-create data in GetOrCreatePlayerData

Make `GetOrCreatePlayerData` return null if data doesn't exist, and update all callers to handle null returns appropriately.

## Why One Player Is Unaffected

The server host likely joins before the autosave trigger fires, or their data loads synchronously due to being the local player, avoiding the race condition.

## Additional Issues Found During Investigation

### 1. LeaveReligion() Destroys Lifetime Stats (Design Violation)

**File:** `PlayerProgressionDataManager.cs:233-234`

```csharp
public void LeaveReligion(string playerUID)
{
    data.Favor = 0;
    data.TotalFavorEarned = 0;  // ❌ Violates design: "Total favor earned (lifetime stat, persists across religion changes)"
}
```

**Expected:** `TotalFavorEarned` should persist (as documented in `PlayerProgressionData.cs:40`)
**Actual:** Gets reset to 0, causing rank loss

**Contrast:** `ApplySwitchPenalty()` correctly preserves `TotalFavorEarned` when switching religions.

**Fix:** Remove the `data.TotalFavorEarned = 0;` line from `LeaveReligion()`.

### 2. QuotedStringParser Casting Exception

**File:** `DivineAscension/Commands/Parsers/QuotedStringParser.cs`

**Issue:** Implements `ICommandArgumentParser` but doesn't inherit from `ArgumentParserBase`, causing handbook generation to crash:
```
Unable to cast object of type 'DivineAscension.Commands.Parsers.QuotedStringParser' to type 'Vintagestory.API.Common.ArgumentParserBase'
```

**Impact:** Command help doesn't appear in handbook (commands still work functionally)

**Fix:** Inherit from `ArgumentParserBase` instead of implementing `ICommandArgumentParser` directly, or wrap the parser in a compatibility adapter.

## Affected Files

- `DivineAscension/Systems/Networking/Server/PlayerDataNetworkHandler.cs` (line 83) - PRIMARY BUG
- `DivineAscension/Systems/PlayerProgressionDataManager.cs` (lines 64-74, 233-234)
- `DivineAscension/Systems/Interfaces/IPlayerProgressionDataManager.cs` (needs TryGetPlayerData method)
- `DivineAscension/Commands/Parsers/QuotedStringParser.cs` (secondary issue)

## Steps to Reproduce

1. Start a server with Divine Ascension mod
2. Player joins and earns progression (favor, rank, blessings)
3. Player disconnects
4. Server restarts
5. Player joins again
6. **Observed:** All progression reset to 0
7. **Expected:** Progression should be preserved

## Environment

- Mod Version: 3.2.1
- Vintage Story: 1.21.0
- Server Type: LAN/Dedicated (affects all types)
- World: Fresh or existing (affects both)

## Priority Justification

This is a **complete data loss bug** that affects every player on every login. It makes progression systems unusable and severely impacts gameplay. Should be treated as a showstopper for the current release.

## Labels

- `bug`
- `critical`
- `data-loss`
- `race-condition`
