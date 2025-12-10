# Refactoring Plan: Break Down PantheonWarsSystem.cs

## Overview

Refactor the monolithic `PantheonWarsSystem.cs` (1,699 lines) into focused, maintainable components following
established patterns in the codebase. The main system will be reduced to ~150 lines of orchestration code, with network
handlers and initialization logic extracted into dedicated classes.

**User Preferences:**

- ✅ Feature-based organization (Religion, Blessing, Civilization, PlayerData handlers)
- ✅ Extract initialization logic to separate SystemInitializer class
- ✅ Comprehensive refactoring (~8-10 new files)
- ✅ Breaking changes acceptable (move public API to new client class)

## Target File Structure

```
PantheonWars/
├── PantheonWarsSystem.cs                         (~150 lines) ⬅️ MODIFIED
│
└── Systems/
    ├── Networking/
    │   ├── Interfaces/
    │   │   ├── IServerNetworkHandler.cs         (NEW)
    │   │   └── IClientNetworkHandler.cs         (NEW)
    │   │
    │   ├── Server/
    │   │   ├── ReligionNetworkHandler.cs        (~250 lines, NEW)
    │   │   ├── BlessingNetworkHandler.cs        (~150 lines, NEW)
    │   │   ├── CivilizationNetworkHandler.cs    (~200 lines, NEW)
    │   │   └── PlayerDataNetworkHandler.cs      (~100 lines, NEW)
    │   │
    │   └── Client/
    │       └── PantheonWarsNetworkClient.cs     (~200 lines, NEW)
    │
    └── PantheonWarsSystemInitializer.cs         (~150 lines, NEW)
```

## Critical Files to Modify

### Primary Target

- `/home/quantumheart/RiderProjects/PantheonWars/PantheonWars/PantheonWarsSystem.cs` (1,699 lines)

### New Files to Create

1. `Systems/Networking/Interfaces/IServerNetworkHandler.cs`
2. `Systems/Networking/Interfaces/IClientNetworkHandler.cs`
3. `Systems/Networking/Server/ReligionNetworkHandler.cs`
4. `Systems/Networking/Server/BlessingNetworkHandler.cs`
5. `Systems/Networking/Server/CivilizationNetworkHandler.cs`
6. `Systems/Networking/Server/PlayerDataNetworkHandler.cs`
7. `Systems/Networking/Client/PantheonWarsNetworkClient.cs`
8. `Systems/PantheonWarsSystemInitializer.cs`

## Implementation Steps

### Phase 1: Create Interface Definitions (Low Risk)

**Files:** `Interfaces/IServerNetworkHandler.cs`, `Interfaces/IClientNetworkHandler.cs`

**IServerNetworkHandler Pattern:**

```csharp
public interface IServerNetworkHandler
{
    void Initialize(ICoreServerAPI sapi);
    void RegisterHandlers(IServerNetworkChannel channel);
    void Dispose();
}
```

**IClientNetworkHandler Pattern:**

```csharp
public interface IClientNetworkHandler
{
    void Initialize(ICoreClientAPI capi);
    void RegisterHandlers(IClientNetworkChannel channel);
    void Dispose();
}
```

**Validation:** Project builds successfully

---

### Phase 2: Extract System Initializer (Medium Risk)

**File:** `Systems/PantheonWarsSystemInitializer.cs`

**Extract from:** `PantheonWarsSystem.cs` lines 92-170 (StartServerSide method)

**Key Responsibilities:**

- Ordered manager instantiation (CRITICAL: must preserve exact order)
- Manager initialization (Initialize() calls)
- Cross-reference setup (SetBlessingSystems)
- Command registration
- Network handler creation and registration
- Event subscription (PlayerJoin, PlayerDataChanged)

**Critical Initialization Order (MUST PRESERVE):**

1. Clear static event subscribers
2. Register entity behaviors
3. DeityRegistry.Initialize()
4. ReligionManager.Initialize()
5. CivilizationManager.Initialize()
6. PlayerReligionDataManager.Initialize()
7. **ReligionPrestigeManager.Initialize() ⚠️ BEFORE FavorSystem**
8. FavorSystem.Initialize()
9. PvPManager.Initialize()
10. BlessingRegistry.Initialize()
11. BlessingEffectSystem.Initialize()
12. **ReligionPrestigeManager.SetBlessingSystems() ⚠️ AFTER BlessingEffectSystem**
13. Create Commands
14. Create and register network handlers
15. Subscribe events

**Return Type:**

```csharp
public class InitializationResult
{
    // 9 Managers
    public DeityRegistry DeityRegistry { get; init; }
    public ReligionManager ReligionManager { get; init; }
    public CivilizationManager CivilizationManager { get; init; }
    public PlayerReligionDataManager PlayerReligionDataManager { get; init; }
    public ReligionPrestigeManager ReligionPrestigeManager { get; init; }
    public FavorSystem FavorSystem { get; init; }
    public PvPManager PvPManager { get; init; }
    public BlessingRegistry BlessingRegistry { get; init; }
    public BlessingEffectSystem BlessingEffectSystem { get; init; }

    // 4 Commands
    public FavorCommands FavorCommands { get; init; }
    public BlessingCommands BlessingCommands { get; init; }
    public ReligionCommands ReligionCommands { get; init; }
    public CivilizationCommands CivilizationCommands { get; init; }

    // 4 Network Handlers
    public ReligionNetworkHandler ReligionNetworkHandler { get; init; }
    public BlessingNetworkHandler BlessingNetworkHandler { get; init; }
    public CivilizationNetworkHandler CivilizationNetworkHandler { get; init; }
    public PlayerDataNetworkHandler PlayerDataNetworkHandler { get; init; }
}
```

**Update PantheonWarsSystem.cs:**

```csharp
public override void StartServerSide(ICoreServerAPI api)
{
    base.StartServerSide(api);
    _sapi = api;

    _serverChannel = api.Network.GetChannel(NETWORK_CHANNEL);

    var result = PantheonWarsSystemInitializer.InitializeServerSystems(api, _serverChannel);

    // Store references for disposal
    _religionManager = result.ReligionManager;
    _playerReligionDataManager = result.PlayerReligionDataManager;
    // ... etc

    api.Logger.Notification("[PantheonWars] Server-side initialization complete");
}
```

**Validation:**

- Server starts without errors
- All managers initialize in correct order
- Check logs for initialization messages
- Verify all systems work (test basic commands)

---

### Phase 3: Extract PlayerDataNetworkHandler (Low Risk)

**File:** `Systems/Networking/Server/PlayerDataNetworkHandler.cs`

**Extract from:** PantheonWarsSystem.cs lines 1311-1350

**Methods to Extract:**

- `OnPlayerJoin(IServerPlayer player)`
- `OnPlayerDataChanged(string playerUID)`
- `SendPlayerDataToClient(IServerPlayer player)` (public, called by other handlers)

**Dependencies (constructor injection):**

```csharp
public PlayerDataNetworkHandler(
    ICoreServerAPI sapi,
    IPlayerReligionDataManager playerReligionDataManager,
    IReligionManager religionManager,
    DeityRegistry deityRegistry,
    IServerNetworkChannel serverChannel)
```

**Pattern:** Implement IServerNetworkHandler

- `Initialize()`: Store dependencies
- `RegisterHandlers()`: Subscribe to PlayerJoin event, PlayerDataChanged event
- `Dispose()`: Unsubscribe events

**Validation:**

- Player data syncs on join
- Player data updates on favor/religion changes
- HUD displays correct values

---

### Phase 4: Extract BlessingNetworkHandler (Medium Risk)

**File:** `Systems/Networking/Server/BlessingNetworkHandler.cs`

**Extract from:** PantheonWarsSystem.cs lines 773-956

**Handlers to Extract:**

- `OnBlessingUnlockRequest` (lines 773-870)
- `OnBlessingDataRequest` (lines 875-956)

**Dependencies:**

```csharp
public BlessingNetworkHandler(
    ICoreServerAPI sapi,
    BlessingRegistry blessingRegistry,
    BlessingEffectSystem blessingEffectSystem,
    IPlayerReligionDataManager playerReligionDataManager,
    IReligionManager religionManager,
    IServerNetworkChannel serverChannel,
    PlayerDataNetworkHandler playerDataHandler)
```

**Key Logic:**

- Validate unlock requirements (rank, prerequisites, permissions)
- Distinguish player vs religion blessings
- Founder-only checks for religion blessings
- Refresh effects after unlock
- Broadcast to religion members on religion blessing unlock
- Call `playerDataHandler.SendPlayerDataToClient()` after changes

**Validation:**

- Test player blessing unlock
- Test religion blessing unlock (founder only)
- Verify prerequisites enforced
- Verify rank requirements enforced
- Confirm blessing effects apply immediately
- Confirm all religion members notified

---

### Phase 5: Extract ReligionNetworkHandler (High Risk)

**File:** `Systems/Networking/Server/ReligionNetworkHandler.cs`

**Extract from:** PantheonWarsSystem.cs lines 229-668

**Handlers to Extract:**

- `OnReligionListRequest` (lines 229-251)
- `OnPlayerReligionInfoRequest` (lines 253-336)
- `OnReligionActionRequest` (lines 338-668) ⚠️ LARGE, COMPLEX
- `OnCreateReligionRequest` (lines 670-732)
- `OnEditDescriptionRequest` (lines 734-771)

**Dependencies:**

```csharp
public ReligionNetworkHandler(
    ICoreServerAPI sapi,
    IReligionManager religionManager,
    IPlayerReligionDataManager playerReligionDataManager,
    IServerNetworkChannel serverChannel,
    PlayerDataNetworkHandler playerDataHandler)
```

**OnReligionActionRequest Actions (15 cases):**

- join, leave, kick, invite, accept, decline, ban, unban, disband

**Key Logic:**

- Permission validation (IsFounder checks for kick/ban/disband)
- Ban enforcement (check before join)
- Member notification (send packets on state changes)
- Invitation lifecycle
- State change broadcasting (ReligionStateChangedPacket)
- Call `playerDataHandler.SendPlayerDataToClient()` after changes

**Validation:**

- Test all 15 action types
- Verify founder-only actions blocked for non-founders
- Verify ban prevents join
- Verify invitations work
- Verify all members notified on disband/ban
- Verify state change packets sent

---

### Phase 6: Extract CivilizationNetworkHandler (Medium Risk)

**File:** `Systems/Networking/Server/CivilizationNetworkHandler.cs`

**Extract from:** PantheonWarsSystem.cs lines 961-1309

**Handlers to Extract:**

- `OnCivilizationListRequest` (lines 961-1001)
- `OnCivilizationInfoRequest` (lines 1007-1130)
- `OnCivilizationActionRequest` (lines 1135-1309)

**Dependencies:**

```csharp
public CivilizationNetworkHandler(
    ICoreServerAPI sapi,
    CivilizationManager civilizationManager,
    IReligionManager religionManager,
    IPlayerReligionDataManager playerReligionDataManager,
    IServerNetworkChannel serverChannel)
```

**OnCivilizationActionRequest Actions:**

- create, invite, accept, leave, kick, disband

**Key Logic:**

- Deity filtering for lists
- Religion name to UID resolution
- Empty civId handling (return player's current civilization)
- Invitation notification broadcasting (notify all religion members)
- Founder validation
- Cooldown enforcement

**Validation:**

- Test create civilization
- Test invite religion (verify all members notified)
- Test accept invite
- Test leave (founder cannot leave)
- Test kick (founder only)
- Test disband (founder only)

---

### Phase 7: Extract Client Networking (Low Risk)

**File:** `Systems/Networking/Client/PantheonWarsNetworkClient.cs`

**Extract from:** PantheonWarsSystem.cs lines 1356-1699

**Extract All:**

- `SetupClientNetworking()` logic
- 9 public Request* methods (RequestBlessingData, RequestBlessingUnlock, etc.)
- 8 public events (BlessingDataReceived, BlessingUnlocked, etc.)
- 12 private response handlers (OnBlessingDataResponse, etc.)

**Pattern:** Implement IClientNetworkHandler

**Public API to Preserve:**

```csharp
// Request methods
public void RequestBlessingData()
public void RequestBlessingUnlock(string blessingId)
public void RequestReligionList(string deityFilter = "")
public void RequestReligionAction(string action, string religionUID = "", string targetPlayerUID = "")
public void RequestCreateReligion(string religionName, string deity, bool isPublic)
public void RequestPlayerReligionInfo()
public void RequestEditDescription(string religionUID, string description)
public void RequestCivilizationList(string deityFilter = "")
public void RequestCivilizationInfo(string civId)
public void RequestCivilizationAction(string action, string civId = "", string targetId = "", string name = "")

// Events
public event Action<PlayerReligionDataPacket>? PlayerReligionDataUpdated;
public event Action<BlessingDataResponsePacket>? BlessingDataReceived;
public event Action<string, bool>? BlessingUnlocked;
public event Action<ReligionStateChangedPacket>? ReligionStateChanged;
public event Action<ReligionListResponsePacket>? ReligionListReceived;
public event Action<ReligionActionResponsePacket>? ReligionActionCompleted;
public event Action<PlayerReligionInfoResponsePacket>? PlayerReligionInfoReceived;
public event Action<CivilizationListResponsePacket>? CivilizationListReceived;
public event Action<CivilizationInfoResponsePacket>? CivilizationInfoReceived;
public event Action<CivilizationActionResponsePacket>? CivilizationActionCompleted;
```

**Special Features:**

- Null checks on channel before sending
- Chat message display on responses
- Delayed blessing data refresh after religion creation (100ms timer)
- Sound effects on errors
- Event invocation for UI updates

**Validation:**

- Test all UI dialogs (religion, blessing, civilization)
- Verify all events fire
- Verify requests reach server
- Verify chat messages display

---

### Phase 8: Update Main PantheonWarsSystem.cs

**Reduce to ~150 lines of orchestration:**

**Keep:**

- Field declarations (managers, network client)
- `Start()` - Network channel registration, Harmony patches (lines 53-90)
- `StartServerSide()` - Delegate to initializer
- `StartClientSide()` - Delegate to client
- `Dispose()` - Cleanup coordination

**Remove:**

- All 13 server handler methods (lines 201-1352)
- All 12 client handler methods (lines 1356-1699)
- `SetupServerNetworking()` and `SetupClientNetworking()`
- Public Request* methods and events (moved to client)

**New Structure:**

```csharp
public class PantheonWarsSystem : ModSystem
{
    // Network client (exposed for UI dialogs)
    public PantheonWarsNetworkClient? NetworkClient { get; private set; }

    // Manager references (for backwards compatibility if needed)
    private ReligionManager? _religionManager;
    // ... etc

    public override void Start(ICoreAPI api)
    {
        // Existing: Register network channel, Harmony patches
    }

    public override void StartServerSide(ICoreServerAPI api)
    {
        _serverChannel = api.Network.GetChannel(NETWORK_CHANNEL);
        var result = PantheonWarsSystemInitializer.InitializeServerSystems(api, _serverChannel);
        // Store manager references
    }

    public override void StartClientSide(ICoreClientAPI api)
    {
        _clientChannel = api.Network.GetChannel(NETWORK_CHANNEL);
        NetworkClient = new PantheonWarsNetworkClient();
        NetworkClient.Initialize(api);
        NetworkClient.RegisterHandlers(_clientChannel);
    }

    public override void Dispose()
    {
        // Cleanup: Harmony, managers, handlers
    }
}
```

**Validation:**

- Full system test (all features work)
- Verify line count (~150 lines)
- Check logs for errors
- Backwards compatibility check (if UI dialogs access system directly)

---

## Migration Checklist

### Phase 1: Interfaces

- [x] Create `IServerNetworkHandler.cs`
- [x] Create `IClientNetworkHandler.cs`
- [x] Build succeeds

### Phase 2: Initializer

- [x] Create `PantheonWarsSystemInitializer.cs`
- [x] Implement `InitializeServerSystems()` with correct order
- [x] Update `PantheonWarsSystem.StartServerSide()`
- [x] Server starts without errors
- [x] All managers initialize

### Phase 3: PlayerDataNetworkHandler

- [x] Create `PlayerDataNetworkHandler.cs`
- [x] Extract methods, add constructor, implement interface
- [x] Update initializer to create handler
- [x] Remove from main system
- [x] Test player data sync

### Phase 4: BlessingNetworkHandler

- [x] Create `BlessingNetworkHandler.cs`
- [x] Extract handlers, add dependencies
- [x] Update initializer
- [x] Remove from main system
- [x] Test blessing unlock (player and religion)
- [x] Test blessing data request

### Phase 5: ReligionNetworkHandler

- [x] Create `ReligionNetworkHandler.cs`
- [x] Extract 5 handlers (especially complex OnReligionActionRequest)
- [x] Update initializer
- [x] Remove from main system
- [x] Test all 15 religion actions
- [x] Test permissions (founder-only actions)
- [x] Test ban enforcement

### Phase 6: CivilizationNetworkHandler

- [x] Create `CivilizationNetworkHandler.cs`
- [x] Extract 3 handlers
- [x] Update initializer
- [x] Remove from main system
- [x] Test all civilization actions

### Phase 7: Client Networking

- [x] Create `PantheonWarsNetworkClient.cs`
- [x] Extract all client methods, events, handlers
- [x] Update `PantheonWarsSystem.StartClientSide()`
- [x] Remove from main system
- [x] Test all UI dialogs

### Phase 8: Final Cleanup

- [x] Verify main system ~150 lines
- [x] Remove unused fields
- [x] Update XML documentation
- [x] Full system test

### Post-Migration

- [ ] Performance test
- [ ] Log analysis (no errors)
- [ ] Update documentation
- [ ] Merge to main branch

---

## Common Pitfalls to Avoid

### Critical Issues

1. **Breaking Initialization Order** - ReligionPrestigeManager MUST initialize before FavorSystem
2. **Missing SetBlessingSystems()** - Must call after BlessingEffectSystem initialized
3. **Forgetting Event Cleanup** - All event subscriptions must be unsubscribed in Dispose()
4. **Null Reference Issues** - Preserve all null checks, especially for religions
5. **Packet Registration** - All packet types must be registered in Start() before handlers use them

### Handler Dependencies

- BlessingNetworkHandler needs PlayerDataNetworkHandler reference
- ReligionNetworkHandler needs PlayerDataNetworkHandler reference
- Both call `SendPlayerDataToClient()` after state changes

### Special Cases

- Religion creation: Refresh blessing data after 100ms
- Disband religion: Notify all members and send state change packet
- Civilization invite: Notify all religion members
- Ban: Check before join, kick if member, then ban

---

## Success Metrics

### Quantitative

- Main system < 200 lines ✓
- Each handler < 300 lines ✓
- 25/25 handlers extracted ✓
- 9 request methods + 8 events in client ✓
- Zero build errors ✓

### Qualitative

- Clear handler responsibilities ✓
- Consistent patterns (matches ISpecialEffectHandler style) ✓
- Testable components ✓
- Improved maintainability ✓

### Functional

- All religion operations work ✓
- All blessing operations work ✓
- All civilization operations work ✓
- Player data sync works ✓
- No runtime errors ✓

---

## Estimated Timeline

- Phase 1 (Interfaces): 30 minutes
- Phase 2 (Initializer): 2-3 hours
- Phase 3 (PlayerDataHandler): 1 hour
- Phase 4 (BlessingHandler): 1.5 hours
- Phase 5 (ReligionHandler): 2-3 hours (complex)
- Phase 6 (CivilizationHandler): 1.5 hours
- Phase 7 (ClientNetworking): 2 hours
- Phase 8 (Cleanup & Testing): 2 hours

**Total: 12-16 hours** of focused work
