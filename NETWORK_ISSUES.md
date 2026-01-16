# Network Architecture Improvement Issues

Copy each issue below into GitHub's "New Issue" form.

---

## Issue 1: Add request timeout mechanism to network client

**Labels:** `enhancement`, `networking`, `priority:high`

### Description

The `DivineAscensionNetworkClient` sends requests without any timeout mechanism. If the server doesn't respond (crash, network hang, etc.), the UI may wait indefinitely with no feedback to the user.

### Current Behavior

```csharp
// DivineAscensionNetworkClient.cs
public void RequestReligionList(string deityFilter = "") {
    if (!IsNetworkAvailable()) return;
    var request = new ReligionListRequestPacket { DeityDomainFilter = deityFilter };
    _clientChannel.SendPacket(request);
    // No timeout - waits forever
}
```

### Proposed Solution

Add timeout tracking with callback-based expiration:

```csharp
private readonly Dictionary<string, long> _pendingRequests = new();

public void RequestReligionList(string deityFilter = "", int timeoutMs = 5000) {
    if (!IsNetworkAvailable()) return;

    var requestId = Guid.NewGuid().ToString();
    _pendingRequests[requestId] = _capi.World.ElapsedMilliseconds + timeoutMs;

    var request = new ReligionListRequestPacket { DeityDomainFilter = deityFilter };
    _clientChannel.SendPacket(request);

    _capi.Event.RegisterCallback(_ => {
        if (_pendingRequests.Remove(requestId, out _)) {
            _capi.Logger.Warning("[DivineAscension] Religion list request timed out");
            ReligionListReceived?.Invoke(new ReligionListResponsePacket { Religions = new() });
        }
    }, timeoutMs);
}

// In response handler, clear pending request
private void OnReligionListResponse(ReligionListResponsePacket packet) {
    // Clear any matching pending request
    _pendingRequests.Clear(); // Or track by request type
    ReligionListReceived?.Invoke(packet);
}
```

### Affected Files

- `DivineAscension/Systems/Networking/Client/DivineAscensionNetworkClient.cs` (all 19 request methods)

### Acceptance Criteria

- [ ] All request methods have configurable timeout (default 5000ms)
- [ ] Timeout fires appropriate event with empty/error response
- [ ] UI handles timeout gracefully (shows error message)
- [ ] Successful responses cancel pending timeout

---

## Issue 2: Implement rate limiting for network requests

**Labels:** `enhancement`, `networking`, `security`, `priority:high`

### Description

Network handlers have no rate limiting. A malicious or buggy client could spam requests, causing server-side performance issues or enabling abuse (e.g., rapidly creating/deleting religions).

### Current Behavior

All handlers process requests immediately without throttling:

```csharp
// ReligionNetworkHandler.cs
private void OnCreateReligionRequest(IServerPlayer fromPlayer, CreateReligionRequestPacket packet) {
    // No rate check - processes immediately
    // Client could call this 100 times per second
}
```

### Proposed Solution

Add a rate limiting utility and apply to all handlers:

```csharp
// New file: Systems/Networking/Server/RateLimiter.cs
public class RateLimiter {
    private readonly Dictionary<string, Dictionary<string, long>> _lastRequestTime = new();
    private readonly ICoreServerAPI _sapi;

    public RateLimiter(ICoreServerAPI sapi) => _sapi = sapi;

    public bool CanMakeRequest(string playerUID, string requestType, int cooldownMs = 500) {
        var now = _sapi.World.ElapsedMilliseconds;

        if (!_lastRequestTime.TryGetValue(playerUID, out var playerRequests)) {
            playerRequests = new Dictionary<string, long>();
            _lastRequestTime[playerUID] = playerRequests;
        }

        if (playerRequests.TryGetValue(requestType, out var lastTime)) {
            if (now - lastTime < cooldownMs) {
                return false;
            }
        }

        playerRequests[requestType] = now;
        return true;
    }

    public void ClearPlayer(string playerUID) => _lastRequestTime.Remove(playerUID);
}

// Usage in handlers:
private void OnCreateReligionRequest(IServerPlayer fromPlayer, CreateReligionRequestPacket packet) {
    if (!_rateLimiter.CanMakeRequest(fromPlayer.PlayerUID, "create_religion", 2000)) {
        _serverChannel.SendPacket(new CreateReligionResponsePacket {
            Success = false,
            Message = "Please wait before creating another religion"
        }, fromPlayer);
        return;
    }
    // ... existing logic
}
```

### Suggested Cooldowns

| Request Type | Cooldown (ms) | Rationale |
|--------------|---------------|-----------|
| Create religion | 2000 | Expensive operation |
| Join/leave religion | 1000 | State change |
| Blessing unlock | 500 | Moderate |
| List requests | 250 | Read-only, but still limit spam |
| Role changes | 1000 | State change |

### Affected Files

- New: `DivineAscension/Systems/Networking/Server/RateLimiter.cs`
- `DivineAscension/Systems/Networking/Server/ReligionNetworkHandler.cs`
- `DivineAscension/Systems/Networking/Server/BlessingNetworkHandler.cs`
- `DivineAscension/Systems/Networking/Server/CivilizationNetworkHandler.cs`
- `DivineAscension/Systems/Networking/Server/DiplomacyNetworkHandler.cs`
- `DivineAscension/Systems/Networking/Server/ActivityNetworkHandler.cs`

### Acceptance Criteria

- [ ] RateLimiter class created and tested
- [ ] All server handlers use rate limiting
- [ ] Rate-limited requests return user-friendly error message
- [ ] Player rate limit state cleared on disconnect

---

## Issue 3: Replace Dictionary with typed classes in action packets

**Labels:** `enhancement`, `networking`, `code-quality`, `priority:medium`

### Description

`ReligionActionRequestPacket` and `CivilizationActionRequestPacket` use `Dictionary<string, object>?` for extensible data. This is type-unsafe and prone to silent failures from key typos or type mismatches.

### Current Behavior

```csharp
// ReligionActionRequestPacket.cs:31
[ProtoMember(5)]
public Dictionary<string, object>? Data { get; set; }

// Handler extracts with string keys - typo = silent null
var reason = packet.Data.ContainsKey("Reason") ? packet.Data["Reason"]?.ToString() : "";
// What if client sends "reason" (lowercase)? Silent failure.
```

### Proposed Solution

Create strongly-typed action data classes:

```csharp
// New: Network/Actions/ReligionActionData.cs
[ProtoContract]
[ProtoInclude(100, typeof(BanActionData))]
[ProtoInclude(101, typeof(InviteActionData))]
public abstract class ReligionActionData { }

[ProtoContract]
public class BanActionData : ReligionActionData {
    [ProtoMember(1)] public string Reason { get; set; } = "";
    [ProtoMember(2)] public int? ExpiryDays { get; set; }
}

[ProtoContract]
public class InviteActionData : ReligionActionData {
    [ProtoMember(1)] public string Message { get; set; } = "";
}

// Updated packet
[ProtoContract]
public class ReligionActionRequestPacket {
    [ProtoMember(1)] public string Action { get; set; } = "";
    [ProtoMember(2)] public string ReligionUID { get; set; } = "";
    [ProtoMember(3)] public string TargetPlayerUID { get; set; } = "";
    [ProtoMember(5)] public ReligionActionData? ActionData { get; set; }
}

// Handler usage - compile-time type safety
private void HandleBanAction(..., ReligionActionRequestPacket packet) {
    if (packet.ActionData is not BanActionData banData) {
        // Type mismatch caught at compile time
        return;
    }
    var reason = banData.Reason; // No casting, no key lookup
    var expiry = banData.ExpiryDays;
}
```

### Affected Files

- `DivineAscension/Network/ReligionActionRequestPacket.cs`
- `DivineAscension/Network/Civilization/CivilizationActionRequestPacket.cs`
- `DivineAscension/Systems/Networking/Server/ReligionNetworkHandler.cs`
- `DivineAscension/Systems/Networking/Server/CivilizationNetworkHandler.cs`
- `DivineAscension/Systems/Networking/Client/DivineAscensionNetworkClient.cs`
- New: `DivineAscension/Network/Actions/ReligionActionData.cs`
- New: `DivineAscension/Network/Actions/CivilizationActionData.cs`

### Acceptance Criteria

- [ ] Action data classes created with ProtoBuf attributes
- [ ] Packets updated to use typed data instead of Dictionary
- [ ] All handlers updated to use pattern matching
- [ ] Client updated to construct typed action data
- [ ] Existing tests updated and passing

---

## Issue 4: Complete localization coverage in network handlers

**Labels:** `enhancement`, `i18n`, `priority:medium`

### Description

Some error messages in network handlers are hardcoded in English instead of using `LocalizationService` and `LocalizationKeys`. This breaks internationalization for non-English users.

### Examples of Hardcoded Strings

```csharp
// ReligionNetworkHandler.cs:429
"Only the founder can change the deity name"

// ReligionNetworkHandler.cs:459
"Failed to update deity name"

// Various validation messages throughout handlers
```

### Proposed Solution

1. Add missing keys to `LocalizationKeys.cs`:

```csharp
public const string NET_RELIGION_ONLY_FOUNDER_CHANGE_DEITY = "net.religion.only_founder_change_deity";
public const string NET_RELIGION_DEITY_UPDATE_FAILED = "net.religion.deity_update_failed";
// ... etc
```

2. Add translations to `assets/divineascension/lang/en.json`:

```json
{
  "net.religion.only_founder_change_deity": "Only the founder can change the deity name",
  "net.religion.deity_update_failed": "Failed to update deity name"
}
```

3. Update handlers to use localization:

```csharp
// Before
response.Message = "Only the founder can change the deity name";

// After
response.Message = LocalizationService.Instance.Get(LocalizationKeys.NET_RELIGION_ONLY_FOUNDER_CHANGE_DEITY);
```

### Affected Files

- `DivineAscension/Services/LocalizationKeys.cs`
- `DivineAscension/assets/divineascension/lang/en.json`
- `DivineAscension/assets/divineascension/lang/de.json` (German)
- `DivineAscension/assets/divineascension/lang/es.json` (Spanish)
- `DivineAscension/assets/divineascension/lang/fr.json` (French)
- `DivineAscension/assets/divineascension/lang/ru.json` (Russian)
- `DivineAscension/Systems/Networking/Server/ReligionNetworkHandler.cs`
- `DivineAscension/Systems/Networking/Server/CivilizationNetworkHandler.cs`
- `DivineAscension/Systems/Networking/Server/DiplomacyNetworkHandler.cs`

### Acceptance Criteria

- [ ] Audit all handlers for hardcoded strings
- [ ] Add all missing keys to LocalizationKeys.cs
- [ ] Add English translations to en.json
- [ ] Update all handlers to use LocalizationService
- [ ] Placeholder translations added for other languages (can be same as English initially)

---

## Issue 5: Optimize player name to UID lookup in invite handling

**Labels:** `enhancement`, `performance`, `priority:low`

### Description

`HandleInviteAction` performs O(n) linear search through all online players to convert player name to UID:

```csharp
// ReligionNetworkHandler.cs:1175-1177
var targetPlayer = _sapi.World.AllOnlinePlayers
    .FirstOrDefault(p => p.PlayerName.Equals(packet.TargetPlayerUID, StringComparison.OrdinalIgnoreCase));
```

### Impact

- Small servers: Negligible
- Large servers (100+ players): Noticeable latency on invite operations
- Each invite = full player list scan

### Proposed Solutions

**Option A: Client sends UID instead of name**

Update client to resolve name → UID before sending packet. Requires UI changes to store/lookup UIDs.

**Option B: Server-side name → UID cache**

```csharp
// Add to ReligionNetworkHandler or create utility class
private Dictionary<string, string> _playerNameToUID = new(StringComparer.OrdinalIgnoreCase);

// Update on player join/leave
private void OnPlayerJoin(IServerPlayer player) {
    _playerNameToUID[player.PlayerName] = player.PlayerUID;
}

private void OnPlayerLeave(IServerPlayer player) {
    _playerNameToUID.Remove(player.PlayerName);
}

// O(1) lookup
private string? GetPlayerUIDByName(string name) {
    return _playerNameToUID.TryGetValue(name, out var uid) ? uid : null;
}
```

**Option C: Use Vintage Story's built-in lookup (if available)**

Check if `_sapi.PlayerData.GetPlayerDataByLastKnownPlayername()` or similar exists.

### Affected Files

- `DivineAscension/Systems/Networking/Server/ReligionNetworkHandler.cs`
- Possibly `DivineAscension/Systems/Networking/Client/DivineAscensionNetworkClient.cs` (if Option A)

### Acceptance Criteria

- [ ] Invite lookup is O(1) instead of O(n)
- [ ] Cache properly maintained on player join/leave
- [ ] Handles case-insensitive name matching
- [ ] Works for offline player invites (if supported)

---

## Summary

| # | Title | Priority | Effort |
|---|-------|----------|--------|
| 1 | Add request timeout mechanism | High | Medium |
| 2 | Implement rate limiting | High | Medium |
| 3 | Replace Dictionary with typed classes | Medium | High |
| 4 | Complete localization coverage | Medium | Low |
| 5 | Optimize player name → UID lookup | Low | Low |
