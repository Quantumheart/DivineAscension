# Thread Safety Implementation Plan

**Created:** 2026-01-17
**Status:** Ready for Implementation
**Estimated Effort:** 10 days
**Priority:** CRITICAL - Production Blocker

---

## Executive Summary

This plan addresses **67+ thread safety vulnerabilities** identified in the Divine Ascension codebase. Without these fixes, the mod will crash under normal multiplayer load (10+ concurrent players) due to race conditions and collection modification exceptions.

**Impact if not fixed:**
- Server crashes during peak activity
- Player progression data corruption/loss
- Religion membership index corruption
- Activity log corruption

**Success Criteria:**
- All managers use thread-safe collections
- All iterations create defensive snapshots
- Concurrent stress test (50+ simulated players) runs without crashes
- Zero `InvalidOperationException` errors in production

---

## Phase 1: Core Manager Thread Safety (CRITICAL - 2 days)

### 1.1 ReligionManager.cs

**File:** `DivineAscension/Systems/ReligionManager.cs`

#### Changes Required:

**A. Replace Dictionaries (Lines 22-23)**

```csharp
// BEFORE:
private readonly Dictionary<string, string> _playerToReligionIndex = new();
private readonly Dictionary<string, ReligionData> _religions = new();

// AFTER:
using System.Collections.Concurrent;

private readonly ConcurrentDictionary<string, string> _playerToReligionIndex = new();
private readonly ConcurrentDictionary<string, ReligionData> _religions = new();
```

**B. Update All Dictionary Operations**

| Line | Current Code | Fixed Code |
|------|-------------|------------|
| 93 | `_playerToReligionIndex.Add(founderUID, religionUID);` | `_playerToReligionIndex.TryAdd(founderUID, religionUID);` |
| 157 | `_playerToReligionIndex.Add(playerUID, religionUID);` | `_playerToReligionIndex.TryAdd(playerUID, religionUID);` |
| 208 | `_playerToReligionIndex.Remove(playerUID);` | `_playerToReligionIndex.TryRemove(playerUID, out _);` |
| 509 | `_religions.Remove(religionUID);` | `_religions.TryRemove(religionUID, out _);` |

**C. Fix Iteration Patterns**

**Lines 452, 460, 260** - All LINQ queries over `_religions`:

```csharp
// BEFORE:
public List<ReligionData> GetAllReligions()
{
    return _religions.Values.ToList();
}

// AFTER (no change needed - ConcurrentDictionary.Values is safe to iterate):
public List<ReligionData> GetAllReligions()
{
    return _religions.Values.ToList();
}
```

**Note:** `ConcurrentDictionary.Values` returns a snapshot, so LINQ queries are safe.

**D. Fix RebuildPlayerIndex (Lines 768-790)**

```csharp
// BEFORE:
foreach (var religion in _religions)
{
    foreach (var userId in religion.Value.MemberUIDs)
    {
        if (_playerToReligionIndex.ContainsKey(userId))
        {
            // ...
            continue;
        }
        _playerToReligionIndex[userId] = religion.Key;
    }
}

// AFTER:
foreach (var religion in _religions)  // Safe - ConcurrentDictionary
{
    var memberSnapshot = religion.Value.MemberUIDs.ToList();  // Defensive copy
    foreach (var userId in memberSnapshot)
    {
        // Use TryAdd instead of Contains + indexer
        if (!_playerToReligionIndex.TryAdd(userId, religion.Key))
        {
            // Already exists - log conflict
            _sapi.Logger.Warning($"Player {userId} already in index...");
        }
    }
}
```

**E. Fix ValidateAllMemberships (Lines 823-834)**

```csharp
// BEFORE:
foreach (var playerUID in _playerToReligionIndex.Keys)
{
    allPlayerUIDs.Add(playerUID);
}

foreach (var religion in _religions.Values)
{
    foreach (var memberUID in religion.MemberUIDs)
    {
        allPlayerUIDs.Add(memberUID);
    }
}

// AFTER:
// Keys snapshot is safe with ConcurrentDictionary
foreach (var playerUID in _playerToReligionIndex.Keys)
{
    allPlayerUIDs.Add(playerUID);
}

foreach (var religion in _religions.Values)  // Safe snapshot
{
    var memberSnapshot = religion.MemberUIDs.ToList();  // Defensive copy
    foreach (var memberUID in memberSnapshot)
    {
        allPlayerUIDs.Add(memberUID);
    }
}
```

**Files to modify:** `ReligionManager.cs`
**Estimated time:** 4 hours
**Test coverage:** Add `ReligionManagerConcurrencyTests.cs`

---

### 1.2 PlayerProgressionDataManager.cs

**File:** `DivineAscension/Systems/PlayerProgressionDataManager.cs`

#### Changes Required:

**A. Replace Collections (Lines 23-24)**

```csharp
// BEFORE:
private readonly HashSet<string> _initializedPlayers = new();
private readonly Dictionary<string, PlayerProgressionData> _playerData = new();

// AFTER:
using System.Collections.Concurrent;

private readonly ConcurrentDictionary<string, byte> _initializedPlayers = new();  // byte = dummy value
private readonly ConcurrentDictionary<string, PlayerProgressionData> _playerData = new();
```

**Note:** No `ConcurrentHashSet`, so use `ConcurrentDictionary<string, byte>` where key is the set element.

**B. Fix GetOrCreatePlayerData (Lines 70-94) - CRITICAL**

```csharp
// BEFORE:
public PlayerProgressionData GetOrCreatePlayerData(string playerUID)
{
    if (!_playerData.TryGetValue(playerUID, out var data))
    {
        if (!_initializedPlayers.Contains(playerUID))
        {
            LoadPlayerData(playerUID);

            if (_playerData.TryGetValue(playerUID, out data))
            {
                return data;
            }
        }

        data = new PlayerProgressionData(playerUID);
        _playerData[playerUID] = data;
        _sapi.Logger.Debug($"[DivineAscension] Created new player progression data for {playerUID}");
    }

    return data;
}

// AFTER:
public PlayerProgressionData GetOrCreatePlayerData(string playerUID)
{
    // Try to get existing data first
    if (_playerData.TryGetValue(playerUID, out var data))
    {
        return data;
    }

    // Ensure we load from disk only once
    if (_initializedPlayers.TryAdd(playerUID, 0))  // Returns true if added (first time)
    {
        LoadPlayerData(playerUID);

        // Check if LoadPlayerData added it
        if (_playerData.TryGetValue(playerUID, out data))
        {
            return data;
        }
    }

    // Create new data atomically
    data = _playerData.GetOrAdd(playerUID, uid =>
    {
        var newData = new PlayerProgressionData(uid);
        _sapi.Logger.Debug($"[DivineAscension] Created new player progression data for {uid}");
        return newData;
    });

    return data;
}
```

**C. Fix SaveAllPlayerData (Line 438)**

```csharp
// BEFORE:
foreach (var playerUID in _playerData.Keys) SavePlayerData(playerUID);

// AFTER (safe - ConcurrentDictionary.Keys returns snapshot):
foreach (var playerUID in _playerData.Keys) SavePlayerData(playerUID);
```

**D. Fix OnPlayerDisconnect (Line 330)**

```csharp
// BEFORE:
_initializedPlayers.Remove(player.PlayerUID);

// AFTER:
_initializedPlayers.TryRemove(player.PlayerUID, out _);
```

**Files to modify:** `PlayerProgressionDataManager.cs`
**Estimated time:** 3 hours
**Test coverage:** Add `PlayerProgressionDataManagerConcurrencyTests.cs`

---

### 1.3 CivilizationManager.cs

**File:** `DivineAscension/Systems/CivilizationManager.cs`

#### Changes Required:

**A. Add Lock Object (Line 28)**

```csharp
// BEFORE:
private CivilizationWorldData _data = new();

// AFTER:
private CivilizationWorldData _data = new();
private readonly object _dataLock = new();
```

**Note:** We use a lock here instead of ConcurrentDictionary because `CivilizationWorldData` has multiple related collections that need to be updated atomically (Civilizations, ReligionToCivMap, PendingInvites).

**B. Wrap All Public Methods with Locks**

**Example - GetCivilizationById (Line ~90):**

```csharp
// BEFORE:
public Civilization? GetCivilizationById(string civId)
{
    return _data.Civilizations.TryGetValue(civId, out var civ) ? civ : null;
}

// AFTER:
public Civilization? GetCivilizationById(string civId)
{
    lock (_dataLock)
    {
        return _data.Civilizations.TryGetValue(civId, out var civ) ? civ : null;
    }
}
```

**Example - CreateCivilization (Lines ~140-180):**

```csharp
// BEFORE:
public Civilization CreateCivilization(...)
{
    // ... validation ...

    var civilization = new Civilization(...);
    _data.Civilizations[civilization.CivId] = civilization;
    _data.ReligionToCivMap[founderReligionId] = civilization.CivId;

    // ... save ...
    return civilization;
}

// AFTER:
public Civilization CreateCivilization(...)
{
    // Validation OUTSIDE lock
    // ... validation ...

    var civilization = new Civilization(...);

    lock (_dataLock)
    {
        _data.Civilizations[civilization.CivId] = civilization;
        _data.ReligionToCivMap[founderReligionId] = civilization.CivId;
    }

    // Save OUTSIDE lock to avoid holding it too long
    SaveCivilizations();
    return civilization;
}
```

**C. Fix PendingInvites Iterations**

**Lines 619, 656 - RemoveAll:**

```csharp
// BEFORE:
_data.PendingInvites.RemoveAll(i => i.CivId == civId);

// AFTER:
lock (_dataLock)
{
    _data.PendingInvites.RemoveAll(i => i.CivId == civId);
}
```

**D. Fix GetAllCivilizations (Line ~200)**

```csharp
// BEFORE:
public List<Civilization> GetAllCivilizations()
{
    return _data.Civilizations.Values.ToList();
}

// AFTER:
public List<Civilization> GetAllCivilizations()
{
    lock (_dataLock)
    {
        return _data.Civilizations.Values.ToList();
    }
}
```

**Files to modify:** `CivilizationManager.cs`
**Estimated time:** 4 hours
**Methods to lock:** ~15 public methods
**Test coverage:** Add `CivilizationManagerConcurrencyTests.cs`

---

### 1.4 DiplomacyManager.cs

**File:** `DivineAscension/Systems/DiplomacyManager.cs`

#### Changes Required:

**Similar to CivilizationManager - add lock object and wrap all methods:**

```csharp
// Line 21:
private DiplomacyWorldData _data = new();
private readonly object _dataLock = new();

// Wrap all public methods with lock (_dataLock)
```

**Critical methods to lock:**
- GetRelationship
- ProposeRelationship
- AcceptProposal
- DeclareWar
- DeclarePeace
- GetPendingProposals (Line 343-346)
- CleanupExpiredRelationships (Lines 537-547)

**Files to modify:** `DiplomacyManager.cs`
**Estimated time:** 3 hours
**Test coverage:** Add `DiplomacyManagerConcurrencyTests.cs`

---

### 1.5 ActivityLogManager.cs

**File:** `DivineAscension/Systems/ActivityLogManager.cs`

#### Strategy: Use locks since we need atomic Insert + RemoveRange

**Add lock to ReligionData for ActivityLog:**

We'll handle this in Phase 2 (Data Models).

**Files to modify:** Defer to Phase 2
**Estimated time:** Included in Phase 2

---

**Phase 1 Total Time:** ~14 hours (~2 days)

---

## Phase 2: Data Model Thread Safety (CRITICAL - 2 days)

### 2.1 ReligionData.cs

**File:** `DivineAscension/Data/ReligionData.cs`

#### Strategy: Add lock-based wrappers for all mutable collections

**A. Add Lock Objects (after ProtoMembers)**

```csharp
[ProtoContract(DataFormat = DataFormat.Default)]
public class ReligionData
{
    // Locks (not serialized)
    [ProtoIgnore] private readonly object _memberLock = new();
    [ProtoIgnore] private readonly object _blessingLock = new();
    [ProtoIgnore] private readonly object _roleLock = new();
    [ProtoIgnore] private readonly object _activityLock = new();
    [ProtoIgnore] private readonly object _banLock = new();
```

**B. Convert MemberUIDs to Thread-Safe Property (Lines 70-71)**

```csharp
// BEFORE:
[ProtoMember(5)]
public List<string> MemberUIDs { get; set; } = new();

// AFTER:
[ProtoMember(5)]
private List<string> _memberUIDs = new();

[ProtoIgnore]
public IReadOnlyList<string> MemberUIDs
{
    get
    {
        lock (_memberLock)
        {
            return _memberUIDs.ToList();  // Always return snapshot
        }
    }
}

// Thread-safe methods for modification
public void AddMember(string uid)
{
    lock (_memberLock)
    {
        if (!_memberUIDs.Contains(uid))
        {
            _memberUIDs.Add(uid);
        }
    }
}

public bool RemoveMember(string uid)
{
    lock (_memberLock)
    {
        return _memberUIDs.Remove(uid);
    }
}

public bool HasMember(string uid)
{
    lock (_memberLock)
    {
        return _memberUIDs.Contains(uid);
    }
}

public int MemberCount
{
    get
    {
        lock (_memberLock)
        {
            return _memberUIDs.Count;
        }
    }
}
```

**C. Fix AddMember Method (Lines 174-184) - CRITICAL**

```csharp
// BEFORE:
public void AddMember(string playerUID, string playerName)
{
    if (!MemberUIDs.Contains(playerUID))
        MemberUIDs.Add(playerUID);

    if (!Members.ContainsKey(playerUID))
        Members[playerUID] = new MemberEntry(playerUID, playerName);
}

// AFTER:
public void AddMember(string playerUID, string playerName)
{
    lock (_memberLock)
    {
        if (!_memberUIDs.Contains(playerUID))
        {
            _memberUIDs.Add(playerUID);
        }

        if (!Members.ContainsKey(playerUID))
        {
            Members[playerUID] = new MemberEntry(playerUID, playerName);
        }
    }
}
```

**D. Convert Members Dictionary (Line 160)**

```csharp
// BEFORE:
[ProtoMember(15)]
public Dictionary<string, MemberEntry> Members { get; set; } = new();

// AFTER:
[ProtoMember(15)]
private Dictionary<string, MemberEntry> _members = new();

[ProtoIgnore]
public IReadOnlyDictionary<string, MemberEntry> Members
{
    get
    {
        lock (_memberLock)
        {
            return new Dictionary<string, MemberEntry>(_members);
        }
    }
}

public bool TryGetMember(string uid, out MemberEntry member)
{
    lock (_memberLock)
    {
        return _members.TryGetValue(uid, out member);
    }
}

internal void SetMember(string uid, MemberEntry entry)
{
    lock (_memberLock)
    {
        _members[uid] = entry;
    }
}

internal bool RemoveMemberEntry(string uid)
{
    lock (_memberLock)
    {
        return _members.Remove(uid);
    }
}
```

**E. Convert UnlockedBlessings (Line 102)**

```csharp
// BEFORE:
[ProtoMember(10)]
public Dictionary<string, bool> UnlockedBlessings { get; set; } = new();

// AFTER:
[ProtoMember(10)]
private Dictionary<string, bool> _unlockedBlessings = new();

[ProtoIgnore]
public IReadOnlyDictionary<string, bool> UnlockedBlessings
{
    get
    {
        lock (_blessingLock)
        {
            return new Dictionary<string, bool>(_unlockedBlessings);
        }
    }
}

public void UnlockBlessing(string blessingId)
{
    lock (_blessingLock)
    {
        _unlockedBlessings[blessingId] = true;
    }
}

public bool IsBlessingUnlocked(string blessingId)
{
    lock (_blessingLock)
    {
        return _unlockedBlessings.TryGetValue(blessingId, out var unlocked) && unlocked;
    }
}

public List<string> GetUnlockedBlessingIds()
{
    lock (_blessingLock)
    {
        return _unlockedBlessings
            .Where(kvp => kvp.Value)
            .Select(kvp => kvp.Key)
            .ToList();
    }
}
```

**F. Convert ActivityLog (Line 160)**

```csharp
// BEFORE:
[ProtoMember(16)]
public List<ActivityLogEntry> ActivityLog { get; set; } = new();

// AFTER:
[ProtoMember(16)]
private List<ActivityLogEntry> _activityLog = new();

[ProtoIgnore]
public IReadOnlyList<ActivityLogEntry> ActivityLog
{
    get
    {
        lock (_activityLock)
        {
            return _activityLog.ToList();
        }
    }
}

public void AddActivityEntry(ActivityLogEntry entry, int maxEntries = 100)
{
    lock (_activityLock)
    {
        _activityLog.Insert(0, entry);  // Insert at front

        if (_activityLog.Count > maxEntries)
        {
            _activityLog.RemoveRange(maxEntries, _activityLog.Count - maxEntries);
        }
    }
}

public List<ActivityLogEntry> GetRecentActivity(int limit)
{
    lock (_activityLock)
    {
        return _activityLog.Take(limit).ToList();
    }
}
```

**G. Convert Role Dictionaries (Lines 127, 133)**

```csharp
// BEFORE:
[ProtoMember(12)]
public Dictionary<string, RoleData> Roles { get; set; } = new();

[ProtoMember(13)]
public Dictionary<string, string> MemberRoles { get; set; } = new();

// AFTER:
[ProtoMember(12)]
private Dictionary<string, RoleData> _roles = new();

[ProtoMember(13)]
private Dictionary<string, string> _memberRoles = new();

[ProtoIgnore]
public IReadOnlyDictionary<string, RoleData> Roles
{
    get
    {
        lock (_roleLock)
        {
            return new Dictionary<string, RoleData>(_roles);
        }
    }
}

[ProtoIgnore]
public IReadOnlyDictionary<string, string> MemberRoles
{
    get
    {
        lock (_roleLock)
        {
            return new Dictionary<string, string>(_memberRoles);
        }
    }
}

public void SetRole(string roleId, RoleData role)
{
    lock (_roleLock)
    {
        _roles[roleId] = role;
    }
}

public void AssignMemberRole(string memberUid, string roleId)
{
    lock (_roleLock)
    {
        _memberRoles[memberUid] = roleId;
    }
}

public bool TryGetMemberRole(string memberUid, out string roleId)
{
    lock (_roleLock)
    {
        return _memberRoles.TryGetValue(memberUid, out roleId);
    }
}
```

**H. Convert BannedPlayers (Line 121)**

```csharp
// BEFORE:
[ProtoMember(11)]
public Dictionary<string, BanEntry> BannedPlayers { get; set; } = new();

// AFTER:
[ProtoMember(11)]
private Dictionary<string, BanEntry> _bannedPlayers = new();

[ProtoIgnore]
public IReadOnlyDictionary<string, BanEntry> BannedPlayers
{
    get
    {
        lock (_banLock)
        {
            return new Dictionary<string, BanEntry>(_bannedPlayers);
        }
    }
}

public void BanPlayer(string uid, BanEntry entry)
{
    lock (_banLock)
    {
        _bannedPlayers[uid] = entry;
    }
}

public bool IsPlayerBanned(string uid)
{
    lock (_banLock)
    {
        return _bannedPlayers.ContainsKey(uid);
    }
}

public bool UnbanPlayer(string uid)
{
    lock (_banLock)
    {
        return _bannedPlayers.Remove(uid);
    }
}
```

**Files to modify:** `ReligionData.cs`
**Estimated time:** 6 hours
**Breaking changes:** YES - all direct property access must be replaced with method calls
**Refactoring needed:** ~50+ call sites across codebase

---

### 2.2 PlayerProgressionData.cs

**File:** `DivineAscension/Systems/PlayerProgressionData.cs`

#### Changes Required:

**A. Add Lock (Line 50)**

```csharp
[ProtoContract]
public class PlayerProgressionData
{
    [ProtoIgnore] private readonly object _blessingLock = new();

    [ProtoMember(5)]
    private HashSet<string> _unlockedBlessings = new();

    [ProtoIgnore]
    public IReadOnlyCollection<string> UnlockedBlessings
    {
        get
        {
            lock (_blessingLock)
            {
                return _unlockedBlessings.ToList();
            }
        }
    }

    public void UnlockBlessing(string blessingId)
    {
        lock (_blessingLock)
        {
            _unlockedBlessings.Add(blessingId);
        }
    }

    public bool IsBlessingUnlocked(string blessingId)
    {
        lock (_blessingLock)
        {
            return _unlockedBlessings.Contains(blessingId);
        }
    }
}
```

**Files to modify:** `PlayerProgressionData.cs`
**Estimated time:** 2 hours

---

**Phase 2 Total Time:** ~8 hours (~1 day with refactoring)

**Note:** Phase 2 requires extensive refactoring of call sites. Budget extra time for fixing compilation errors.

---

## Phase 3: Network Handler Iteration Fixes (CRITICAL - 1 day)

### Strategy: Add `.ToList()` snapshots before ALL foreach loops

### 3.1 ReligionNetworkHandler.cs

**File:** `DivineAscension/Systems/Networking/Server/ReligionNetworkHandler.cs`

**Locations to fix:**

| Line | Current Code | Fixed Code |
|------|-------------|------------|
| 117 | `foreach (var member in religion.Members)` | `foreach (var member in religion.Members.ToList())` |
| 230 | `foreach (var member in religion.Members)` | `foreach (var member in religion.Members.ToList())` |
| 440 | `foreach (var memberUID in religion.MemberUIDs)` | `foreach (var memberUID in religion.MemberUIDs.ToList())` |
| 508 | `foreach (var memberUID in religion.MemberUIDs)` | `foreach (var memberUID in religion.MemberUIDs.ToList())` |
| 784 | `foreach (var memberUID in religion.MemberUIDs)` | `foreach (var memberUID in religion.MemberUIDs.ToList())` |
| 831 | `foreach (var memberUID in religion.MemberUIDs)` | `foreach (var memberUID in religion.MemberUIDs.ToList())` |
| 847 | `foreach (var memberUID in religion.MemberUIDs)` | `foreach (var memberUID in religion.MemberUIDs.ToList())` |
| 851 | `foreach (var memberUID in religion.MemberUIDs)` | `foreach (var memberUID in religion.MemberUIDs.ToList())` |

**Note:** After Phase 2, `religion.MemberUIDs` will already return a snapshot, but explicit `.ToList()` is still good practice for clarity.

**Files to modify:** `ReligionNetworkHandler.cs` (8 locations)
**Estimated time:** 1 hour

---

### 3.2 BlessingNetworkHandler.cs

**File:** `DivineAscension/Systems/Networking/Server/BlessingNetworkHandler.cs`

**Line 117:**
```csharp
// BEFORE:
foreach (var memberUid in religion.MemberUIDs)
{
    playerProgressionDataManager.NotifyPlayerDataChanged(memberUid);
}

// AFTER:
var memberSnapshot = religion.MemberUIDs.ToList();
foreach (var memberUid in memberSnapshot)
{
    playerProgressionDataManager.NotifyPlayerDataChanged(memberUid);
}
```

**Files to modify:** `BlessingNetworkHandler.cs` (1 location)
**Estimated time:** 15 minutes

---

### 3.3 CivilizationNetworkHandler.cs

**File:** `DivineAscension/Systems/Networking/Server/CivilizationNetworkHandler.cs`

**Line 316:**
```csharp
// BEFORE:
foreach (var memberUID in targetReligion.MemberUIDs)

// AFTER:
foreach (var memberUID in targetReligion.MemberUIDs.ToList())
```

**Files to modify:** `CivilizationNetworkHandler.cs` (1 location)
**Estimated time:** 15 minutes

---

### 3.4 PlayerDataNetworkHandler.cs

**File:** `DivineAscension/Systems/Networking/Server/PlayerDataNetworkHandler.cs`

Search for any iterations and add snapshots.

**Files to modify:** `PlayerDataNetworkHandler.cs`
**Estimated time:** 30 minutes

---

**Phase 3 Total Time:** ~2 hours

---

## Phase 4: Command Handler Iteration Fixes (HIGH - 1 day)

### 4.1 BlessingCommands.cs

**File:** `DivineAscension/Commands/BlessingCommands.cs`

**Locations to fix:**

| Line | Pattern |
|------|---------|
| 542 | `foreach (var memberUID in religion.MemberUIDs)` |
| 736 | `foreach (var memberUID in religion.MemberUIDs)` |
| 835 | `foreach (var memberUID in religion.MemberUIDs)` |
| 966 | `foreach (var memberUID in religion.MemberUIDs)` |

**Apply `.ToList()` to all.**

**Files to modify:** `BlessingCommands.cs` (4 locations)
**Estimated time:** 30 minutes

---

### 4.2 ReligionCommands.cs

**File:** `DivineAscension/Commands/ReligionCommands.cs`

**Locations to fix:**

| Line | Pattern |
|------|---------|
| 443 | `foreach (var memberUID in religion.MemberUIDs)` |
| 1160 | `foreach (var memberUID in religion.MemberUIDs)` |
| 1361 | `foreach (var memberUID in religion.MemberUIDs)` |
| 1365 | `foreach (var memberUID in religion.MemberUIDs)` |
| 1471 | `foreach (var memberUID in religion.MemberUIDs)` |
| 1475 | `foreach (var memberUID in religion.MemberUIDs)` |
| 1550 | `foreach (var memberUID in religion.MemberUIDs)` |
| 1554 | `foreach (var memberUID in religion.MemberUIDs)` |

**Note:** Line 796 already uses `.ToList()` - good example!

**Files to modify:** `ReligionCommands.cs` (8 locations)
**Estimated time:** 45 minutes

---

### 4.3 RoleCommands.cs

**File:** `DivineAscension/Commands/RoleCommands.cs`

**Line 605:** Add snapshot.

**Files to modify:** `RoleCommands.cs` (1 location)
**Estimated time:** 15 minutes

---

### 4.4 CivilizationCommands.cs

**File:** `DivineAscension/Commands/CivilizationCommands.cs`

Search for iterations and add snapshots.

**Files to modify:** `CivilizationCommands.cs`
**Estimated time:** 30 minutes

---

**Phase 4 Total Time:** ~2 hours

---

## Phase 5: Cache and Secondary Systems (MEDIUM - 1 day)

### 5.1 BlessingEffectSystem.cs

**File:** `DivineAscension/Systems/BlessingEffectSystem.cs`

**A. Replace Cache Dictionaries (Lines 27, 30)**

```csharp
// BEFORE:
private readonly Dictionary<string, Dictionary<string, float>> _playerModifierCache = new();
private readonly Dictionary<string, Dictionary<string, float>> _religionModifierCache = new();

// AFTER:
using System.Collections.Concurrent;

private readonly ConcurrentDictionary<string, Dictionary<string, float>> _playerModifierCache = new();
private readonly ConcurrentDictionary<string, Dictionary<string, float>> _religionModifierCache = new();
```

**B. Update Cache Operations**

| Line | Current | Fixed |
|------|---------|-------|
| 92 | `_playerModifierCache[playerUID] = ...` | (No change - indexer is safe) |
| 246 | `_playerModifierCache.Remove(playerUID);` | `_playerModifierCache.TryRemove(playerUID, out _);` |
| 249 | `_religionModifierCache.Remove(...);` | `_religionModifierCache.TryRemove(..., out _);` |
| 325-326 | `_playerModifierCache.Clear();` | (Safe with ConcurrentDictionary) |

**C. Fix RefreshReligionBlessings (Line 275)**

```csharp
// BEFORE:
foreach (var memberUID in religion.MemberUIDs) RefreshPlayerBlessings(memberUID);

// AFTER:
var memberSnapshot = religion.MemberUIDs.ToList();
foreach (var memberUID in memberSnapshot) RefreshPlayerBlessings(memberUID);
```

**Files to modify:** `BlessingEffectSystem.cs`
**Estimated time:** 1 hour

---

### 5.2 ReligionPrestigeManager.cs

**File:** `DivineAscension/Systems/ReligionPrestigeManager.cs`

**Lines 318, 344:** Add `.ToList()` snapshots

**Files to modify:** `ReligionPrestigeManager.cs` (2 locations)
**Estimated time:** 30 minutes

---

### 5.3 ActivityLogManager.cs

**File:** `DivineAscension/Systems/ActivityLogManager.cs`

**Lines 66-74:** Replace with ReligionData.AddActivityEntry

```csharp
// BEFORE:
religion.ActivityLog.Insert(0, entry);

if (religion.ActivityLog.Count > MAX_ENTRIES_PER_RELIGION)
{
    religion.ActivityLog.RemoveRange(...);
}

// AFTER (using ReligionData's thread-safe method from Phase 2):
religion.AddActivityEntry(entry, MAX_ENTRIES_PER_RELIGION);
```

**Files to modify:** `ActivityLogManager.cs`
**Estimated time:** 30 minutes

---

### 5.4 FavorSystem and Trackers

**Files:** `DivineAscension/Systems/FavorSystem.cs` and `FavorTrackers/*`

**Search for:** Any iterations over player lists or religion collections.

**Estimated time:** 1 hour (inspection + fixes)

---

**Phase 5 Total Time:** ~3 hours

---

## Phase 6: Thread Safety Testing (CRITICAL - 2 days)

### 6.1 Unit Tests for Concurrent Access

**Create new test files:**

#### A. `ReligionManagerConcurrencyTests.cs`

```csharp
public class ReligionManagerConcurrencyTests
{
    [Fact]
    public void CreateReligion_ConcurrentCalls_AllSucceed()
    {
        // Arrange
        var tasks = new List<Task<ReligionData>>();

        // Act - 50 threads creating religions simultaneously
        for (int i = 0; i < 50; i++)
        {
            var playerUID = $"player_{i}";
            tasks.Add(Task.Run(() =>
                _religionManager.CreateReligion(
                    $"Religion_{i}",
                    DeityDomain.Craft,
                    $"Deity_{i}",
                    playerUID,
                    true
                )
            ));
        }

        var results = Task.WhenAll(tasks).Result;

        // Assert
        Assert.Equal(50, results.Length);
        Assert.Equal(50, _religionManager.GetAllReligions().Count);
        Assert.All(results, r => Assert.NotNull(r));
    }

    [Fact]
    public void AddMember_ConcurrentAddsToSameReligion_NoExceptions()
    {
        // Arrange
        var religion = CreateTestReligion();
        var tasks = new List<Task>();

        // Act - 100 threads adding members to same religion
        for (int i = 0; i < 100; i++)
        {
            var playerUID = $"member_{i}";
            tasks.Add(Task.Run(() =>
                _religionManager.AddMemberToReligion(religion.ReligionUID, playerUID)
            ));
        }

        Task.WaitAll(tasks.ToArray());

        // Assert - no duplicates, all members added
        var members = religion.MemberUIDs.ToList();
        Assert.Equal(101, members.Count);  // 100 + founder
        Assert.Equal(members.Count, members.Distinct().Count());  // No duplicates
    }

    [Fact]
    public void GetAllReligions_WhileCreatingAndDeleting_NoExceptions()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        var tasks = new List<Task>();

        // Act - Concurrent creates, deletes, and reads
        tasks.Add(Task.Run(() =>
        {
            while (!cts.Token.IsCancellationRequested)
            {
                var religions = _religionManager.GetAllReligions();
            }
        }));

        tasks.Add(Task.Run(() =>
        {
            for (int i = 0; i < 100; i++)
            {
                _religionManager.CreateReligion($"Temp_{i}", DeityDomain.Wild, "Deity", $"p_{i}", true);
            }
        }));

        tasks.Add(Task.Run(() =>
        {
            Thread.Sleep(50);
            var religions = _religionManager.GetAllReligions();
            foreach (var r in religions)
            {
                _religionManager.DeleteReligion(r.ReligionUID);
            }
        }));

        Task.WhenAll(tasks.Take(2)).Wait();
        cts.Cancel();

        // Assert - no exceptions thrown
        Assert.True(true);
    }
}
```

#### B. `PlayerProgressionDataManagerConcurrencyTests.cs`

```csharp
public class PlayerProgressionDataManagerConcurrencyTests
{
    [Fact]
    public void GetOrCreatePlayerData_ConcurrentCalls_ReturnsSameInstance()
    {
        // Arrange
        var playerUID = "test_player";
        var tasks = new List<Task<PlayerProgressionData>>();

        // Act - 50 threads getting/creating same player data
        for (int i = 0; i < 50; i++)
        {
            tasks.Add(Task.Run(() =>
                _manager.GetOrCreatePlayerData(playerUID)
            ));
        }

        var results = Task.WhenAll(tasks).Result;

        // Assert - all return same instance (or at least same UID)
        Assert.All(results, data => Assert.Equal(playerUID, data.PlayerUID));

        // Verify only one instance in internal storage
        var allData = _manager.GetAllPlayerData();
        Assert.Single(allData.Where(d => d.PlayerUID == playerUID));
    }

    [Fact]
    public void UnlockBlessing_ConcurrentUnlocks_AllRecorded()
    {
        // Arrange
        var playerData = _manager.GetOrCreatePlayerData("test_player");
        var tasks = new List<Task>();

        // Act - 100 threads unlocking different blessings
        for (int i = 0; i < 100; i++)
        {
            var blessingId = $"blessing_{i}";
            tasks.Add(Task.Run(() =>
                playerData.UnlockBlessing(blessingId)
            ));
        }

        Task.WaitAll(tasks.ToArray());

        // Assert - all blessings unlocked, no duplicates
        var unlocked = playerData.UnlockedBlessings.ToList();
        Assert.Equal(100, unlocked.Count);
        Assert.Equal(100, unlocked.Distinct().Count());
    }
}
```

#### C. `ReligionDataConcurrencyTests.cs`

```csharp
public class ReligionDataConcurrencyTests
{
    [Fact]
    public void AddMember_ConcurrentCalls_NoDuplicates()
    {
        // Arrange
        var religion = new ReligionData(/*...*/);
        var tasks = new List<Task>();

        // Act
        for (int i = 0; i < 100; i++)
        {
            var uid = $"member_{i}";
            tasks.Add(Task.Run(() =>
                religion.AddMember(uid, $"Player {i}")
            ));
        }

        Task.WaitAll(tasks.ToArray());

        // Assert
        var members = religion.MemberUIDs.ToList();
        Assert.Equal(100, members.Distinct().Count());
    }

    [Fact]
    public void AddMember_WhileIterating_NoException()
    {
        // Arrange
        var religion = new ReligionData(/*...*/);
        for (int i = 0; i < 50; i++)
        {
            religion.AddMember($"initial_{i}", $"Player {i}");
        }

        var cts = new CancellationTokenSource();

        // Act
        var iterateTask = Task.Run(() =>
        {
            while (!cts.Token.IsCancellationRequested)
            {
                foreach (var uid in religion.MemberUIDs)
                {
                    // Iterate
                }
            }
        });

        var addTask = Task.Run(() =>
        {
            for (int i = 50; i < 150; i++)
            {
                religion.AddMember($"new_{i}", $"Player {i}");
                Thread.Sleep(1);
            }
        });

        addTask.Wait();
        cts.Cancel();
        iterateTask.Wait();

        // Assert - no exceptions
        Assert.True(true);
    }
}
```

**Files to create:** 3 new test files
**Estimated time:** 6 hours

---

### 6.2 Integration Stress Test

**Create:** `StressTests/MultiplayerConcurrencyStressTest.cs`

```csharp
public class MultiplayerConcurrencyStressTest
{
    [Fact]
    public void SimulateMultiplayerServer_50Players_NoExceptions()
    {
        // Simulate 50 players all doing different actions simultaneously:
        // - Creating religions
        // - Joining/leaving religions
        // - Unlocking blessings
        // - Earning favor
        // - Creating civilizations

        var tasks = new List<Task>();
        var random = new Random();

        for (int playerId = 0; playerId < 50; playerId++)
        {
            var pid = playerId;
            tasks.Add(Task.Run(() =>
            {
                var playerUID = $"player_{pid}";

                // Each player performs random actions
                for (int action = 0; action < 100; action++)
                {
                    switch (random.Next(0, 5))
                    {
                        case 0: // Create religion
                            _religionManager.CreateReligion(/*...*/);
                            break;
                        case 1: // Join religion
                            var religions = _religionManager.GetAllReligions();
                            if (religions.Any())
                                _religionManager.AddMemberToReligion(religions.First().ReligionUID, playerUID);
                            break;
                        case 2: // Unlock blessing
                            _playerProgressionDataManager.GetOrCreatePlayerData(playerUID)
                                .UnlockBlessing($"blessing_{action}");
                            break;
                        case 3: // Earn favor
                            _favorSystem.AwardFavor(playerUID, 10, DeityDomain.Craft);
                            break;
                        case 4: // Read data
                            _religionManager.GetAllReligions();
                            _playerProgressionDataManager.GetOrCreatePlayerData(playerUID);
                            break;
                    }

                    Thread.Sleep(random.Next(1, 10));
                }
            }));
        }

        // Wait for all tasks to complete (timeout after 60 seconds)
        var completed = Task.WaitAll(tasks.ToArray(), TimeSpan.FromSeconds(60));

        Assert.True(completed, "Stress test timed out");

        // Verify data integrity
        var allReligions = _religionManager.GetAllReligions();
        foreach (var religion in allReligions)
        {
            // No duplicate members
            var members = religion.MemberUIDs.ToList();
            Assert.Equal(members.Count, members.Distinct().Count());
        }
    }
}
```

**Estimated time:** 4 hours

---

**Phase 6 Total Time:** ~10 hours (~1.5 days)

---

## Phase 7: Stress Testing & Documentation (1 day)

### 7.1 Manual Stress Testing

**Test scenarios:**

1. **50 concurrent player joins**
   - Spawn 50 test clients
   - All join server simultaneously
   - All create religions
   - Monitor for crashes

2. **Religion membership stress**
   - 100 players joining/leaving same religion repeatedly
   - Monitor for index corruption

3. **Blessing unlock storm**
   - All players unlocking blessings simultaneously
   - Check for cache corruption

4. **Save/load under load**
   - Trigger manual save while 50 players are active
   - Restart server and verify data integrity

**Estimated time:** 4 hours

---

### 7.2 Create Server Admin Guide

**File:** `docs/topics/deployment/server-admin-guide.md`

**Contents:**
- Installation instructions
- Environment setup (`VINTAGE_STORY` variable)
- Configuration file locations
- Performance tuning recommendations
- Monitoring for thread safety issues
- Troubleshooting common errors
- Backup/restore procedures
- Upgrade procedures

**Estimated time:** 3 hours

---

### 7.3 Update CLAUDE.md

Add section on thread safety:

```markdown
## Thread Safety

All core managers (`ReligionManager`, `PlayerProgressionDataManager`, etc.) use thread-safe collections:
- `ConcurrentDictionary<>` for manager-level dictionaries
- Lock-based wrappers for data model collections (`ReligionData`, `PlayerProgressionData`)

**Important:** Always iterate over defensive snapshots using `.ToList()` when accessing collections from data models.

**Pattern:**
```csharp
// CORRECT:
var memberSnapshot = religion.MemberUIDs.ToList();
foreach (var uid in memberSnapshot) { /* ... */ }

// WRONG:
foreach (var uid in religion.MemberUIDs) { /* ... */ }  // May throw if modified during iteration
```
```

**Estimated time:** 1 hour

---

**Phase 7 Total Time:** ~8 hours (~1 day)

---

## Implementation Checklist

### Phase 1: Core Managers ✅
- [ ] ReligionManager.cs - ConcurrentDictionary (4 hours)
- [ ] PlayerProgressionDataManager.cs - ConcurrentDictionary (3 hours)
- [ ] CivilizationManager.cs - Lock-based (4 hours)
- [ ] DiplomacyManager.cs - Lock-based (3 hours)

### Phase 2: Data Models ✅
- [ ] ReligionData.cs - Lock wrappers (6 hours)
- [ ] PlayerProgressionData.cs - Lock wrappers (2 hours)

### Phase 3: Network Handlers ✅
- [ ] ReligionNetworkHandler.cs - 8 locations (1 hour)
- [ ] BlessingNetworkHandler.cs - 1 location (15 min)
- [ ] CivilizationNetworkHandler.cs - 1 location (15 min)
- [ ] PlayerDataNetworkHandler.cs - inspection (30 min)

### Phase 4: Command Handlers ✅
- [ ] BlessingCommands.cs - 4 locations (30 min)
- [ ] ReligionCommands.cs - 8 locations (45 min)
- [ ] RoleCommands.cs - 1 location (15 min)
- [ ] CivilizationCommands.cs - inspection (30 min)

### Phase 5: Secondary Systems ✅
- [ ] BlessingEffectSystem.cs (1 hour)
- [ ] ReligionPrestigeManager.cs (30 min)
- [ ] ActivityLogManager.cs (30 min)
- [ ] FavorSystem/Trackers (1 hour)

### Phase 6: Testing ✅
- [ ] ReligionManagerConcurrencyTests.cs (2 hours)
- [ ] PlayerProgressionDataManagerConcurrencyTests.cs (2 hours)
- [ ] ReligionDataConcurrencyTests.cs (2 hours)
- [ ] MultiplayerConcurrencyStressTest.cs (4 hours)

### Phase 7: Validation ✅
- [ ] Manual stress testing (4 hours)
- [ ] Server admin guide (3 hours)
- [ ] Update CLAUDE.md (1 hour)

---

## Total Estimated Time: 10 days

**Breakdown:**
- Phase 1: 2 days
- Phase 2: 2 days (includes refactoring)
- Phase 3: 0.5 days
- Phase 4: 0.5 days
- Phase 5: 0.5 days
- Phase 6: 1.5 days
- Phase 7: 1 day
- **Buffer:** 2 days for unexpected issues

---

## Success Metrics

✅ **Zero collection modification exceptions** in stress test
✅ **50+ concurrent players** can join/leave religions without crashes
✅ **All unit tests pass** with 100+ concurrent threads
✅ **Data integrity verified** after stress test (no duplicates, no lost data)
✅ **Performance acceptable** (no major slowdowns from locking)

---

## Risks and Mitigation

### Risk 1: Breaking Changes in ReligionData
**Mitigation:** Make changes backward-compatible by keeping property names but changing implementation.

### Risk 2: Performance Degradation from Locking
**Mitigation:** Use fine-grained locks (separate locks for members, blessings, roles) and keep lock duration minimal.

### Risk 3: Deadlocks from Nested Locks
**Mitigation:** Always acquire locks in consistent order. Document lock hierarchy.

### Risk 4: ProtoBuf Serialization Issues
**Mitigation:** Keep private backing fields with `[ProtoMember]`, use `[ProtoIgnore]` on lock objects.

---

## Post-Implementation

After all phases complete:

1. **Performance profiling** - Verify no significant slowdown
2. **Memory profiling** - Check for leaks from snapshot creation
3. **Beta testing** - Deploy to test server with real players
4. **Monitoring** - Watch logs for any remaining thread safety exceptions
5. **Documentation** - Update all developer guides

---

## Questions for Stakeholders

1. Can we tolerate breaking API changes in `ReligionData` properties?
2. What is acceptable performance overhead for thread safety? (Target: <5% slowdown)
3. Should we implement telemetry to monitor lock contention in production?
4. Do we need migration scripts for existing save files?

---

**This plan is ready for implementation. Proceed phase by phase to minimize risk.**
