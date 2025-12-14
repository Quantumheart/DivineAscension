# Plan: Persistent Player Names in ReligionData

## Problem

Player names are only available when players are online. When players go offline:

- **Roles member dialog** shows UIDs instead of names (critical issue)
- **Info tab member list** shows UIDs for offline players
- **Ban list** shows UIDs for banned players who are offline
- **Founder display** falls back to UID if founder is offline

## Root Cause

- `ReligionData` only stores UIDs, not player names
- Network handlers use `_sapi.World.PlayerByUid()` to resolve names at request time
- When players are offline, this returns null and UIs display UIDs as fallback

## Solution: Persistent Player Name Storage

Store player names directly in `ReligionData` using a new `MemberEntry` class (following the proven `BanEntry` pattern),
so names are always available even when players are offline.

---

## Implementation Overview

### Phase 1: Create Core Data Structure

**New File:** `PantheonWars/Data/MemberEntry.cs`

Create a ProtoContract class to store member information:

```csharp
[ProtoContract]
public class MemberEntry
{
    [ProtoMember(1)] public string PlayerUID { get; set; } = string.Empty;
    [ProtoMember(2)] public string PlayerName { get; set; } = string.Empty;
    [ProtoMember(3)] public DateTime JoinDate { get; set; } = DateTime.UtcNow;
    [ProtoMember(4)] public DateTime LastNameUpdate { get; set; } = DateTime.UtcNow;

    public MemberEntry() { } // Required for ProtoBuf

    public MemberEntry(string playerUID, string playerName)
    {
        PlayerUID = playerUID;
        PlayerName = playerName;
        JoinDate = DateTime.UtcNow;
        LastNameUpdate = DateTime.UtcNow;
    }

    public void UpdateName(string newName)
    {
        if (PlayerName != newName)
        {
            PlayerName = newName;
            LastNameUpdate = DateTime.UtcNow;
        }
    }
}
```

**Design Pattern:** Follows `BanEntry.cs` exactly - same ProtoContract structure

---

### Phase 2: Update ReligionData Schema

**File:** `PantheonWars/Data/ReligionData.cs`

**Add Two New Fields:**

1. **Members Dictionary** (ProtoMember 16):

```csharp
[ProtoMember(16)]
public Dictionary<string, MemberEntry> Members { get; set; } = new();
```

2. **Founder Name Cache** (ProtoMember 17):

```csharp
[ProtoMember(17)]
public string FounderName { get; set; } = string.Empty;
```

**Update Constructor** (line ~19):

```csharp
public ReligionData(string religionUID, string religionName, DeityType deity,
                   string founderUID, string founderName)
{
    // ... existing fields ...
    FounderName = founderName;
    Members = new Dictionary<string, MemberEntry>
    {
        [founderUID] = new MemberEntry(founderUID, founderName)
    };
}
```

**Update AddMember Method** (line ~131) - add overload:

```csharp
public void AddMember(string playerUID, string playerName)
{
    if (!MemberUIDs.Contains(playerUID))
        MemberUIDs.Add(playerUID);

    if (!Members.ContainsKey(playerUID))
        Members[playerUID] = new MemberEntry(playerUID, playerName);
    else
        Members[playerUID].UpdateName(playerName);
}
```

**Add Helper Methods:**

```csharp
public string GetMemberName(string playerUID)
{
    return Members.TryGetValue(playerUID, out var entry) ? entry.PlayerName : playerUID;
}

public void UpdateMemberName(string playerUID, string playerName)
{
    if (Members.TryGetValue(playerUID, out var entry))
        entry.UpdateName(playerName);
}

public void UpdateFounderName(string founderName)
{
    FounderName = founderName;
    UpdateMemberName(FounderUID, founderName);
}
```

**Design Decision:** Keep both `MemberUIDs` (list) and `Members` (dictionary) for:

- Backward compatibility with existing code
- Ordered member list (founder first)
- Fast O(1) name lookups

---

### Phase 3: Update ReligionManager

**File:** `PantheonWars/Systems/ReligionManager.cs`

**CreateReligion** (line ~51) - capture founder name:

```csharp
public ReligionData CreateReligion(string name, DeityType deity, string founderUID, bool isPublic)
{
    // ... validation ...

    var founderPlayer = _sapi.World.PlayerByUid(founderUID);
    var founderName = founderPlayer?.PlayerName ?? founderUID;

    var religion = new ReligionData(religionUID, name, deity, founderUID, founderName)
    {
        // ... existing initialization ...
    };
    // ... rest ...
}
```

**AddMember** (line ~85) - capture player name:

```csharp
public void AddMember(string religionUID, string playerUID)
{
    if (!_religions.TryGetValue(religionUID, out var religion)) return;

    var player = _sapi.World.PlayerByUid(playerUID);
    var playerName = player?.PlayerName ?? playerUID;

    religion.AddMember(playerUID, playerName);
    SaveAllReligions();
}
```

**HandleFounderLeaving** (line ~462) - update founder name on transfer:

```csharp
private void HandleFounderLeaving(ReligionData religion)
{
    if (religion.GetMemberCount() > 0)
    {
        var newFounderUID = religion.MemberUIDs[0];
        religion.FounderUID = newFounderUID;

        var newFounderName = religion.GetMemberName(newFounderUID);
        religion.UpdateFounderName(newFounderName);
        religion.MemberRoles[newFounderUID] = RoleDefaults.FOUNDER_ROLE_ID;
    }
}
```

---

### Phase 4: Update Network Packets

**File:** `PantheonWars/Network/ReligionRolesPackets.cs`

Add MemberNames to ReligionRolesResponse:

```csharp
[ProtoContract]
public class ReligionRolesResponse
{
    [ProtoMember(1)] public bool Success { get; set; }
    [ProtoMember(2)] public List<RoleData> Roles { get; set; }
    [ProtoMember(3)] public Dictionary<string, string> MemberRoles { get; set; }
    [ProtoMember(4)] public string ErrorMessage { get; set; } = string.Empty;
    [ProtoMember(5)] public Dictionary<string, string> MemberNames { get; set; } = new(); // NEW
}
```

---

### Phase 5: Update Network Handlers

**File:** `PantheonWars/Systems/Networking/Server/ReligionNetworkHandler.cs`

**Member List Building** (line ~107) - use cached names with opportunistic updates:

```csharp
foreach (var memberUID in religion.MemberUIDs)
{
    var memberPlayerData = _playerReligionDataManager!.GetOrCreatePlayerData(memberUID);

    // Use cached name from Members dictionary
    var memberName = religion.GetMemberName(memberUID);

    // Opportunistically update name if player is online
    var memberPlayer = _sapi!.World.PlayerByUid(memberUID);
    if (memberPlayer != null)
    {
        religion.UpdateMemberName(memberUID, memberPlayer.PlayerName);
        memberName = memberPlayer.PlayerName;
    }

    response.Members.Add(new PlayerReligionInfoResponsePacket.MemberInfo
    {
        PlayerUID = memberUID,
        PlayerName = memberName,
        // ... rest ...
    });
}
```

**Roles Request Handler** (line ~644) - populate MemberNames:

```csharp
var response = new ReligionRolesResponse
{
    Success = true,
    Roles = _roleManager.GetReligionRoles(packet.ReligionUID),
    MemberRoles = religion.MemberRoles,
    MemberNames = new Dictionary<string, string>(),
    ErrorMessage = null
};

// Populate member names from cached data
foreach (var uid in religion.MemberUIDs)
{
    response.MemberNames[uid] = religion.GetMemberName(uid);

    // Opportunistic update if online
    var player = _sapi!.World.PlayerByUid(uid);
    if (player != null)
    {
        religion.UpdateMemberName(uid, player.PlayerName);
        response.MemberNames[uid] = player.PlayerName;
    }
}
```

---

### Phase 6: Update Client UI

**File:** `PantheonWars/GUI/Models/Religion/Roles/ReligionRolesViewModel.cs`

Add property to expose member names:

```csharp
public IReadOnlyDictionary<string, string> MemberNames =>
    (IReadOnlyDictionary<string, string>?)RolesData?.MemberNames ?? new Dictionary<string, string>();
```

**File:** `PantheonWars/GUI/UI/Renderers/Religion/ReligionRolesRenderer.cs` (line ~507)

Update to display names instead of UIDs:

```csharp
foreach (var memberUID in membersWithRole)
{
    var memberName = viewModel.MemberNames.TryGetValue(memberUID, out var name) ? name : memberUID;
    TextRenderer.DrawInfoText(drawList, $"• {memberName}", dlgX + padding, currentY, dialogWidth - padding * 2);
    currentY += 20f;
}
```

---

## Critical Files to Modify

1. **`PantheonWars/Data/MemberEntry.cs`** (NEW FILE)
2. **`PantheonWars/Data/ReligionData.cs`** - Add Members dict, FounderName, helper methods
3. **`PantheonWars/Systems/ReligionManager.cs`** - Capture names when adding members, migration
4. **`PantheonWars/Network/ReligionRolesPackets.cs`** - Add MemberNames to response
5. **`PantheonWars/Systems/Networking/Server/ReligionNetworkHandler.cs`** - Use cached names, opportunistic updates
6. **`PantheonWars/GUI/Models/Religion/Roles/ReligionRolesViewModel.cs`** - Expose MemberNames
7. **`PantheonWars/GUI/UI/Renderers/Religion/ReligionRolesRenderer.cs`** - Display names (line 507)

---

## Benefits

1. **Always Available Names** - Works for offline players
2. **Performance** - Eliminates repeated `PlayerByUid()` calls
3. **Consistency** - Follows proven `BanEntry` pattern
4. **Future-Proof** - JoinDate enables tenure tracking
5. **Opportunistic Updates** - Names stay fresh when players online

---

## Testing Checklist

- [ ] Create new religion - verify founder name stored
- [ ] Add member - verify name stored
- [ ] Player goes offline - verify name still shows
- [ ] Transfer founder - verify new founder name updates
- [ ] View role members - verify names show instead of UIDs
