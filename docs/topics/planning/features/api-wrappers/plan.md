# API Wrapper Implementation Plan

## Executive Summary

This plan outlines a comprehensive strategy for introducing thin wrapper classes around Vintage Story's API to improve testability across the Divine Ascension codebase. The analysis identified **107 files with direct API dependencies**, creating significant testing friction. By wrapping 9 key API surfaces in testable interfaces, we can reduce mock complexity by ~70% and enable true unit testing of business logic.

**Estimated Impact:**
- Reduce test setup complexity (currently 40+ mocks per test class → 3-5 wrappers)
- Enable testing of event-driven logic (20+ event subscriptions currently untestable)
- Improve test execution speed (reduce need for full API simulation)
- Support future refactoring and feature development

**Scope:** 9 wrapper interfaces covering Event, World, Persistence, Network, Commands, Logging, Messaging, ModLoader, and Input services.

---

## Problem Statement

### Current State

The Divine Ascension mod has **direct dependencies on Vintage Story's API throughout the codebase**:

- **107 files** directly reference `ICoreServerAPI` or `ICoreClientAPI`
- **40+ event subscriptions** scattered across managers requiring complex `IServerEventAPI` mocks
- **19 files** call `BlockAccessor.GetBlock()` and `World.PlayerByUid()` requiring full world simulation
- **6 managers** use `SaveGame.GetData()/StoreData()` with serialization logic difficult to test
- **40+ message handlers** registered on network channels requiring protocol buffer setup
- **7 command files** with embedded chat message logic mixing concerns

### Pain Points

1. **Event Subscription Hell:** Tests must mock `IServerEventAPI` with 10+ event properties, manually trigger callbacks
2. **World Simulation Required:** Testing favor trackers requires mocking `IServerWorldAccessor` + `IBlockAccessor` + block data
3. **Persistence Testing:** Can't easily test save/load migrations without full world context
4. **Mixed Concerns:** Business logic intertwined with player messaging, making unit tests integration-level
5. **High Mock Count:** Test fixtures average 15-20 mocks per test class (see `TestFixtures.cs`)

### Evidence from Codebase

From test analysis:
- `Mock<IServerEventAPI>`: 20 instances
- `Mock<IServerWorldAccessor>`: 44 instances
- `Mock<IServerPlayer>`: 100+ instances
- `Mock<IServerNetworkChannel>`: 12 instances
- `Mock<IWorldManagerAPI>`: 9 instances

**Result:** Tests are brittle, slow to write, and often test integration rather than units.

---

## Goals and Non-Goals

### Goals

1. **Improve Testability:** Enable true unit testing of business logic without full API simulation
2. **Reduce Mock Complexity:** Replace 15-20 mocks per test with 3-5 focused wrapper mocks
3. **Maintain Existing Behavior:** Zero functional changes to runtime behavior
4. **Enable Future Refactoring:** Create seams for extracting business logic from framework code
5. **Document API Usage:** Wrapper interfaces serve as living documentation of API surface usage
6. **Minimal Performance Impact:** Wrappers should be thin with negligible overhead

### Non-Goals

1. **NOT a full abstraction layer:** We're wrapping, not reimplementing Vintage Story's API
2. **NOT changing architecture:** Managers remain managers, existing patterns stay intact
3. **NOT refactoring business logic:** Focus is on API seams, not domain logic improvements
4. **NOT removing all API dependencies:** Some types (BlockPos, AssetLocation, etc.) can remain direct dependencies
5. **NOT blocking current development:** Wrappers added incrementally without breaking existing code

---

## Architecture Design

### Design Principles

1. **Thin Wrappers:** Minimal logic, direct pass-through to underlying API
2. **Interface Segregation:** Each wrapper focused on specific API surface
3. **Dependency Injection:** Wrappers injected via constructor, interfaces in DI container
4. **Backward Compatible:** Existing code continues working; wrappers added gradually
5. **Test-Friendly Defaults:** Interfaces designed for easy mocking (no complex fluent APIs)
6. **Single Responsibility:** One wrapper per API concern (Event, World, Persistence, etc.)

### Wrapper Architecture

```
┌─────────────────────────────────────────────────────────┐
│ Vintage Story API                                       │
│ (ICoreServerAPI, ICoreClientAPI)                        │
└─────────────────┬───────────────────────────────────────┘
                  │
                  │ Wrapped by
                  ▼
┌─────────────────────────────────────────────────────────┐
│ API Services Layer (New)                                │
│ ┌─────────────┐ ┌─────────────┐ ┌──────────────┐      │
│ │IEventService│ │IWorldService│ │IPersistence  │      │
│ │             │ │             │ │Service       │      │
│ └─────────────┘ └─────────────┘ └──────────────┘      │
│                                                         │
│ ┌─────────────┐ ┌─────────────┐ ┌──────────────┐      │
│ │INetworkSvc  │ │ICommandSvc  │ │IPlayerMsg    │      │
│ │             │ │             │ │Service       │      │
│ └─────────────┘ └─────────────┘ └──────────────┘      │
└─────────────────┬───────────────────────────────────────┘
                  │
                  │ Injected into
                  ▼
┌─────────────────────────────────────────────────────────┐
│ Domain Logic (Existing)                                 │
│ Managers, Commands, Network Handlers, GUI               │
└─────────────────────────────────────────────────────────┘
```

### Implementation Strategy

**Location:** `/DivineAscension/API/` (new directory)

**Structure:**
```
/API/
  /Interfaces/
    IEventService.cs
    IWorldService.cs
    IPersistenceService.cs
    INetworkService.cs
    ICommandService.cs
    IPlayerMessengerService.cs
    IModLoaderService.cs
    ILoggerService.cs
    IInputService.cs (client-side)
  /Implementation/
    ServerEventService.cs
    ServerWorldService.cs
    ServerPersistenceService.cs
    ServerNetworkService.cs
    ServerCommandService.cs
    PlayerMessengerService.cs
    ModLoaderService.cs
    LoggerService.cs
    ClientInputService.cs
```

---

## Interface Specifications

### Tier 1 - Critical (Highest Impact)

#### 1. IEventService

**Purpose:** Wrap `IServerEventAPI` to enable testable event subscriptions

**Priority:** **HIGHEST** - 40+ usages, 20+ subscriptions across 15+ files

**Interface:**
```csharp
namespace DivineAscension.API.Interfaces;

/// <summary>
/// Service for subscribing to game events and registering periodic callbacks.
/// Wraps ICoreServerAPI.Event for testability.
/// </summary>
public interface IEventService
{
    // Lifecycle Events
    void OnSaveGameLoaded(Action callback);
    void OnGameWorldSave(Action callback);
    void OnPlayerJoin(Action<IServerPlayer> callback);
    void OnPlayerDisconnect(Action<IServerPlayer> callback);
    void OnPlayerDeath(Action<IServerPlayer, DamageSource> callback);

    // Game Events
    void OnBreakBlock(Action<IServerPlayer, BlockSelection, ref float, ref EnumHandling> callback);
    void OnDidUseBlock(Action<IServerPlayer, BlockSelection> callback);
    void OnDidPlaceBlock(Action<IServerPlayer, BlockSelection, ItemStack> callback);

    // Periodic Callbacks
    long RegisterGameTickListener(Action<float> callback, int intervalMs);
    long RegisterCallback(Action<float> callback, int intervalMs);
    void UnregisterCallback(long callbackId);

    // Unsubscribe (for cleanup)
    void UnsubscribeSaveGameLoaded(Action callback);
    void UnsubscribeGameWorldSave(Action callback);
    void UnsubscribePlayerJoin(Action<IServerPlayer> callback);
    void UnsubscribePlayerDisconnect(Action<IServerPlayer> callback);
    void UnsubscribePlayerDeath(Action<IServerPlayer, DamageSource> callback);
    void UnsubscribeBreakBlock(Action<IServerPlayer, BlockSelection, ref float, ref EnumHandling> callback);
}
```

**Usage Example (Before):**
```csharp
public class ReligionManager
{
    private readonly ICoreServerAPI _sapi;

    public void Initialize()
    {
        _sapi.Event.SaveGameLoaded += LoadFromWorldData;
        _sapi.Event.GameWorldSave += SaveToWorldData;
    }
}
```

**Usage Example (After):**
```csharp
public class ReligionManager
{
    private readonly IEventService _eventService;

    public ReligionManager(IEventService eventService, ...)
    {
        _eventService = eventService;
    }

    public void Initialize()
    {
        _eventService.OnSaveGameLoaded(LoadFromWorldData);
        _eventService.OnGameWorldSave(SaveToWorldData);
    }
}
```

**Test Example:**
```csharp
[Fact]
public void Initialize_SubscribesToEvents()
{
    // Arrange
    var eventService = new Mock<IEventService>();
    var manager = new ReligionManager(eventService.Object, ...);

    // Act
    manager.Initialize();

    // Assert - verify subscriptions without full IServerEventAPI mock
    eventService.Verify(e => e.OnSaveGameLoaded(It.IsAny<Action>()), Times.Once);
    eventService.Verify(e => e.OnGameWorldSave(It.IsAny<Action>()), Times.Once);
}

[Fact]
public void LoadFromWorldData_LoadsReligions()
{
    // Arrange
    var eventService = new Mock<IEventService>();
    Action? savedCallback = null;
    eventService.Setup(e => e.OnSaveGameLoaded(It.IsAny<Action>()))
               .Callback<Action>(cb => savedCallback = cb);

    var manager = new ReligionManager(eventService.Object, ...);
    manager.Initialize();

    // Act - trigger the event manually
    savedCallback?.Invoke();

    // Assert - verify loading logic
    Assert.NotNull(manager.GetAllReligions());
}
```

**Implementation Notes:**
- Wrapper stores callbacks in internal lists
- Forwards to `_sapi.Event.SaveGameLoaded += callback` in implementation
- Cleanup in `Dispose()` unsubscribes all stored callbacks

---

#### 2. IWorldService

**Purpose:** Wrap world accessor methods for player/block queries

**Priority:** **HIGH** - 35+ usages across 19 files

**Interface:**
```csharp
namespace DivineAscension.API.Interfaces;

/// <summary>
/// Service for accessing world state (players, blocks, entities).
/// Wraps ICoreServerAPI.World for testability.
/// </summary>
public interface IWorldService
{
    // Player Access
    IServerPlayer? GetPlayerByUID(string uid);
    IPlayer? GetPlayerByName(string name);
    IEnumerable<IServerPlayer> GetAllOnlinePlayers();

    // Block Access
    Block GetBlock(BlockPos pos);
    Block GetBlock(int blockId);
    BlockEntity? GetBlockEntity(BlockPos pos);

    // Chunk Access
    bool IsChunkLoaded(Vec3i chunkPos);
    IWorldChunk? GetChunk(Vec3i chunkPos);

    // Sound/Effects (client-side also used)
    void PlaySoundAt(AssetLocation sound, double x, double y, double z, IPlayer? sourcePlayer = null, bool randomizePitch = true, float range = 32f, float volume = 1f);
    void SpawnParticles(SimpleParticleProperties particles, Vec3d pos, IPlayer? sourcePlayer = null);

    // Utilities
    long ElapsedMilliseconds { get; }
    IBlockAccessor GetBlockAccessor(bool isWriteAccess, bool isRevertable);
}
```

**Affected Files:**
- All favor trackers: `MiningFavorTracker`, `StoneFavorTracker`, `AnvilFavorTracker`, `HarvestFavorTracker`, etc.
- All blessing effect handlers: `AethraEffectHandlers`, `KhorasEffectHandlers`, etc.
- Command files: `ReligionCommands`, `BlessingCommands`, `CivilizationCommands`

**Test Improvement:**
```csharp
// Before: Mock IServerWorldAccessor + IBlockAccessor + IServerPlayer
var worldAccessor = new Mock<IServerWorldAccessor>();
var blockAccessor = new Mock<IBlockAccessor>();
worldAccessor.Setup(w => w.BlockAccessor).Returns(blockAccessor.Object);
var player = new Mock<IServerPlayer>();
worldAccessor.Setup(w => w.PlayerByUid(It.IsAny<string>())).Returns(player.Object);

// After: Single mock
var worldService = new Mock<IWorldService>();
worldService.Setup(w => w.GetPlayerByUID("abc")).Returns(player.Object);
worldService.Setup(w => w.GetBlock(It.IsAny<BlockPos>())).Returns(mockBlock);
```

---

#### 3. IPersistenceService

**Purpose:** Wrap world save/load operations

**Priority:** **HIGH** - 15+ usages across 6 managers

**Interface:**
```csharp
namespace DivineAscension.API.Interfaces;

/// <summary>
/// Service for persisting and loading data from world saves.
/// Wraps ICoreServerAPI.WorldManager.SaveGame for testability.
/// </summary>
public interface IPersistenceService
{
    // Generic Load/Save (uses ProtoBuf serialization)
    T? Load<T>(string key) where T : class;
    void Save<T>(string key, T data) where T : class;

    // Raw Byte Load/Save (for custom serialization)
    byte[]? LoadRaw(string key);
    void SaveRaw(string key, byte[] data);

    // Existence Check
    bool Exists(string key);

    // Deletion
    void Delete(string key);
}
```

**Affected Files:**
- `ReligionManager` (religions + invites)
- `CivilizationManager` (civilizations)
- `PlayerProgressionDataManager` (per-player data with dynamic keys)
- `DiplomacyManager` (diplomacy state)
- `DivineAscensionModSystem` (config data)

**Test Improvement:**
```csharp
// Before: Mock IWorldManagerAPI + ISaveGame + SerializerUtil
var worldManager = new Mock<IWorldManagerAPI>();
var saveGame = new Mock<ISaveGame>();
worldManager.Setup(w => w.SaveGame).Returns(saveGame.Object);
saveGame.Setup(s => s.GetData(It.IsAny<string>())).Returns<string>(key => ...);

// After: Simple in-memory fake
public class FakePersistenceService : IPersistenceService
{
    private Dictionary<string, object> _store = new();

    public T? Load<T>(string key) where T : class
        => _store.TryGetValue(key, out var data) ? (T)data : null;

    public void Save<T>(string key, T data) where T : class
        => _store[key] = data;
}

// Usage in tests
var persistence = new FakePersistenceService();
var manager = new ReligionManager(persistence, ...);
manager.SaveToWorldData(); // writes to fake store
var loaded = persistence.Load<ReligionWorldData>("religions"); // read back
```

---

#### 4. INetworkService

**Purpose:** Wrap network channel message handling

**Priority:** **HIGH** - 40+ message handler registrations

**Interface:**
```csharp
namespace DivineAscension.API.Interfaces;

/// <summary>
/// Service for sending/receiving network messages on the Divine Ascension channel.
/// Wraps IServerNetworkChannel for testability.
/// </summary>
public interface INetworkService
{
    // Message Handler Registration
    void RegisterMessageHandler<T>(Action<IServerPlayer, T> handler) where T : class;
    void RegisterMessageHandler<T>(string messageId, Action<IServerPlayer, T> handler) where T : class;

    // Sending Messages
    void SendToPlayer<T>(IServerPlayer player, T message) where T : class;
    void SendToAllPlayers<T>(T message) where T : class;
    void SendToPlayersInRange<T>(Vec3d position, float range, T message) where T : class;
    void SendToOthers<T>(IServerPlayer excludePlayer, T message) where T : class;

    // Broadcast (all connected clients)
    void Broadcast<T>(T message) where T : class;
}

/// <summary>
/// Client-side network service for receiving server messages.
/// Wraps IClientNetworkChannel for testability.
/// </summary>
public interface IClientNetworkService
{
    void RegisterMessageHandler<T>(Action<T> handler) where T : class;
    void SendToServer<T>(T message) where T : class;
}
```

**Affected Files:**
- All 6 network handlers: `PlayerDataNetworkHandler`, `ReligionNetworkHandler`, `BlessingNetworkHandler`, `CivilizationNetworkHandler`, `DiplomacyNetworkHandler`, `ActivityNetworkHandler`
- `DivineAscensionNetworkClient` (client-side)

**Test Improvement:**
```csharp
// Before: Mock IServerNetworkChannel with complex setup
var channel = new Mock<IServerNetworkChannel>();
channel.Setup(c => c.SetMessageHandler<SomePacket>(It.IsAny<...>()))...

// After: Simple spy/fake
public class SpyNetworkService : INetworkService
{
    public List<object> SentMessages { get; } = new();

    public void SendToPlayer<T>(IServerPlayer player, T message) where T : class
        => SentMessages.Add(message);

    public void RegisterMessageHandler<T>(Action<IServerPlayer, T> handler) where T : class
        => _handlers[typeof(T)] = handler;

    // Test helper to trigger handler
    public void SimulateMessage<T>(IServerPlayer player, T message) where T : class
        => ((Action<IServerPlayer, T>)_handlers[typeof(T)])(player, message);
}

[Fact]
public void HandleReligionCreateRequest_SendsResponse()
{
    var network = new SpyNetworkService();
    var handler = new ReligionNetworkHandler(network, ...);

    handler.HandleCreateRequest(player, new ReligionCreateRequest { Name = "Test" });

    Assert.Single(network.SentMessages);
    Assert.IsType<ReligionCreateResponse>(network.SentMessages[0]);
}
```

---

### Tier 2 - High Value (Good ROI)

#### 5. IPlayerMessengerService

**Purpose:** Extract player messaging from business logic

**Priority:** **MEDIUM-HIGH** - 20+ `SendMessage()` calls embedded in handlers

**Interface:**
```csharp
namespace DivineAscension.API.Interfaces;

/// <summary>
/// Service for sending chat messages to players.
/// Separates messaging concerns from business logic for testability.
/// </summary>
public interface IPlayerMessengerService
{
    // Basic Messaging
    void SendMessage(IServerPlayer player, string message, EnumChatType type = EnumChatType.Notification);
    void SendSuccess(IServerPlayer player, string message);
    void SendError(IServerPlayer player, string message);
    void SendInfo(IServerPlayer player, string message);

    // Broadcast
    void BroadcastMessage(string message, EnumChatType type = EnumChatType.Notification);
    void BroadcastToReligion(Guid religionUID, string message);
    void BroadcastToCivilization(Guid civilizationUID, string message);

    // Formatted Messages (with localization)
    void SendLocalizedMessage(IServerPlayer player, string localizationKey, params object[] args);
}
```

**Affected Files:**
- All command handlers (7 files)
- Network handlers (6 files)
- Managers with player notifications

**Test Improvement:**
```csharp
// Before: Mock IServerPlayer.SendMessage() in every test
player.Setup(p => p.SendMessage(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<EnumChatType>()));

// After: Spy on messenger service
var messenger = new SpyPlayerMessenger();
var command = new ReligionCommands(messenger, ...);

command.HandleCreateReligion(player, args);

Assert.Contains("Religion created", messenger.SentMessages[0].Message);
```

---

#### 6. ICommandService

**Purpose:** Wrap command registration for testability

**Priority:** **MEDIUM** - 7 command files, but fluent API is complex

**Interface:**
```csharp
namespace DivineAscension.API.Interfaces;

/// <summary>
/// Service for registering chat commands.
/// Wraps IChatCommandApi for testability.
/// </summary>
public interface ICommandService
{
    ICommandBuilder CreateCommand(string name);
}

public interface ICommandBuilder
{
    ICommandBuilder WithDescription(string description);
    ICommandBuilder RequiresPrivilege(string privilege);
    ICommandBuilder WithArgs(params ICommandArgumentParser[] parsers);
    ICommandBuilder HandleWith(CommandDelegate handler);
    ICommandBuilder BeginSubCommand(string name);
    void Register();
}

public delegate void CommandDelegate(IServerPlayer player, int groupId, CmdArgs args);
```

**Note:** This is lower priority because command handlers can be refactored to extract business logic into managers, making the command layer thin. Focus on Tier 1 wrappers first.

---

### Tier 3 - Nice to Have

#### 7. ILoggerService

**Purpose:** Domain-specific logging interface

**Priority:** **LOW** - Already easy to mock `ILogger`

**Interface:**
```csharp
namespace DivineAscension.API.Interfaces;

public interface ILoggerService
{
    void Info(string message);
    void Warning(string message);
    void Error(string message);
    void Debug(string message);
    void VerboseDebug(string message);
}
```

---

#### 8. IModLoaderService

**Purpose:** Wrap optional mod dependency checks

**Priority:** **LOW** - Only 3 usages

**Interface:**
```csharp
namespace DivineAscension.API.Interfaces;

public interface IModLoaderService
{
    bool IsModEnabled(string modId);
    T? GetModSystem<T>() where T : ModSystem;
}
```

---

#### 9. IInputService (Client-Side)

**Purpose:** Wrap hotkey registration for GUI testing

**Priority:** **LOW** - Only 1 usage in `GuiDialog.cs`

**Interface:**
```csharp
namespace DivineAscension.API.Interfaces;

public interface IInputService
{
    void RegisterHotKey(string code, string description, int keyCode, HotkeyType type, bool shiftPressed = false, bool ctrlPressed = false);
    void SetHotKeyHandler(string code, Action<KeyCombination> handler);
}
```

---

## Implementation Phases

### Phase 1: Foundation (Week 1-2)

**Goal:** Create wrapper infrastructure and implement Tier 1 interfaces

**Tasks:**
1. Create `/API/Interfaces/` and `/API/Implementation/` directories
2. Implement `IEventService` + `ServerEventService`
   - Add unit tests for wrapper itself
   - Create `FakeEventService` test double
3. Implement `IWorldService` + `ServerWorldService`
   - Add unit tests
   - Create `FakeWorldService` test double
4. Implement `IPersistenceService` + `ServerPersistenceService`
   - Add unit tests
   - Create `FakePersistenceService` (in-memory dictionary)
5. Implement `INetworkService` + `ServerNetworkService` + `ClientNetworkService`
   - Add unit tests
   - Create `SpyNetworkService` test double

**Deliverables:**
- 4 interfaces + implementations
- 4 test doubles (Fake/Spy implementations)
- Unit tests for wrappers (15-20 tests)
- Documentation in `/docs/topics/implementation/api-wrappers.md`

**Success Criteria:**
- All wrapper tests pass
- Zero impact on existing functionality (wrappers not yet used)

---

### Phase 2: Gradual Migration - Managers (Week 3-4)

**Goal:** Migrate high-value managers to use wrappers

**Priority Order:**
1. `ReligionManager` - Uses Event (SaveGameLoaded, GameWorldSave), Persistence, World
2. `PlayerProgressionDataManager` - Uses Event (PlayerJoin, PlayerDisconnect), Persistence, World
3. `CivilizationManager` - Uses Event, Persistence, World
4. `DiplomacyManager` - Uses Event, Persistence
5. `FavorSystem` - Uses Event (BreakBlock, PlayerDeath, GameTickListener)
6. `BlessingEffectSystem` - Uses Event (PlayerJoin, GameTickListener), World

**Migration Pattern (per manager):**
1. Add wrapper parameters to constructor
2. Replace `_sapi.Event.X` → `_eventService.OnX()`
3. Replace `_sapi.World.PlayerByUid()` → `_worldService.GetPlayerByUID()`
4. Replace persistence calls → `_persistenceService.Load/Save()`
5. Update `DivineAscensionSystemInitializer` to pass wrappers
6. Update test fixtures to use fake implementations
7. Rewrite tests to remove direct API mocks

**Example Migration (ReligionManager):**
```csharp
// Before
public ReligionManager(
    ICoreServerAPI sapi,
    IRoleManager roleManager,
    IActivityLogManager activityLog)
{
    _sapi = sapi;
    // ...
}

public void Initialize()
{
    _sapi.Event.SaveGameLoaded += LoadFromWorldData;
    _sapi.Event.GameWorldSave += SaveToWorldData;
}

private void LoadFromWorldData()
{
    byte[]? data = _sapi.WorldManager.SaveGame.GetData("religions");
    // ...
}

// After
public ReligionManager(
    IEventService eventService,
    IPersistenceService persistenceService,
    IWorldService worldService,
    IRoleManager roleManager,
    IActivityLogManager activityLog)
{
    _eventService = eventService;
    _persistenceService = persistenceService;
    _worldService = worldService;
    // ...
}

public void Initialize()
{
    _eventService.OnSaveGameLoaded(LoadFromWorldData);
    _eventService.OnGameWorldSave(SaveToWorldData);
}

private void LoadFromWorldData()
{
    ReligionWorldData? data = _persistenceService.Load<ReligionWorldData>("religions");
    // ...
}
```

**Test Migration:**
```csharp
// Before (15+ mocks)
var sapi = new Mock<ICoreServerAPI>();
var eventApi = new Mock<IServerEventAPI>();
var worldAccessor = new Mock<IServerWorldAccessor>();
var worldManager = new Mock<IWorldManagerAPI>();
var saveGame = new Mock<ISaveGame>();
// ... 10+ more setup lines

// After (3 fakes)
var eventService = new FakeEventService();
var persistence = new FakePersistenceService();
var worldService = new FakeWorldService();

var manager = new ReligionManager(eventService, persistence, worldService, ...);

// Test event handling
manager.Initialize();
eventService.TriggerSaveGameLoaded(); // trigger event directly
Assert.NotEmpty(manager.GetAllReligions());
```

**Deliverables:**
- 6 managers migrated
- ~30 test files updated with simplified mocks
- Test execution time reduced by ~30%

---

### Phase 3: Favor Trackers and Effect Handlers (Week 5)

**Goal:** Migrate favor trackers to use `IWorldService` and `IEventService`

**Affected Files (13 trackers):**
- `MiningFavorTracker`, `AnvilFavorTracker`, `StoneFavorTracker`, `SmeltingFavorTracker`
- `HuntingFavorTracker`, `SkinningFavorTracker`, `ForagingFavorTracker`
- `HarvestFavorTracker`, `CraftFavorTracker`, `ConquestFavorTracker`, `RuinDiscoveryFavorTracker`

**Affected Files (5 effect handlers):**
- `AethraEffectHandlers`, `GaiaEffectHandlers`, `KhorasEffectHandlers`, `LysaEffectHandlers`, `ConquestEffectHandlers`

**Migration Pattern:**
1. Replace Harmony patch event subscriptions with wrapper subscriptions
2. Replace `BlockAccessor.GetBlock()` with `_worldService.GetBlock()`
3. Replace `World.AllOnlinePlayers` with `_worldService.GetAllOnlinePlayers()`
4. Update tests to use `FakeWorldService` with predefined block data

**Test Improvement:**
```csharp
// Before: Can't easily test favor calculation for specific block types
[Fact]
public void OnBreakBlock_GraniteBlock_AwardsFavor()
{
    var blockAccessor = new Mock<IBlockAccessor>();
    var graniteBlock = new Mock<Block>();
    graniteBlock.Setup(b => b.Code.Path).Returns("rock-granite");
    blockAccessor.Setup(b => b.GetBlock(It.IsAny<BlockPos>())).Returns(graniteBlock.Object);
    // ... 10+ more setup lines
}

// After: Simple setup with fake world service
[Fact]
public void OnBreakBlock_GraniteBlock_AwardsFavor()
{
    var worldService = new FakeWorldService();
    worldService.SetBlock(new BlockPos(0, 0, 0), BlockCode.FromString("rock-granite"));

    var tracker = new MiningFavorTracker(worldService, ...);
    float favor = tracker.CalculateFavorForBlock(new BlockPos(0, 0, 0));

    Assert.Equal(2.0f, favor);
}
```

---

### Phase 4: Commands and Network Handlers (Week 6)

**Goal:** Migrate commands to use `IPlayerMessengerService` and handlers to use `INetworkService`

**Commands (7 files):**
- Extract `player.SendMessage()` calls to `_messengerService.SendMessage()`
- Simplify command handler logic (just orchestrate managers + send responses)
- Update tests to verify messages without mocking IServerPlayer

**Network Handlers (6 files):**
- Use `INetworkService` for `SendToPlayer()` calls
- Simplify handler tests with `SpyNetworkService`

**Example:**
```csharp
// Before
public void HandleCreateReligion(IServerPlayer player, CmdArgs args)
{
    string name = args.PopWord();
    if (string.IsNullOrEmpty(name))
    {
        player.SendMessage(0, "Religion name required.", EnumChatType.CommandError);
        return;
    }

    var result = _religionManager.CreateReligion(player.PlayerUID, name, ...);
    if (result.Success)
        player.SendMessage(0, "Religion created!", EnumChatType.CommandSuccess);
    else
        player.SendMessage(0, result.ErrorMessage, EnumChatType.CommandError);
}

// After
public void HandleCreateReligion(IServerPlayer player, CmdArgs args)
{
    string name = args.PopWord();
    if (string.IsNullOrEmpty(name))
    {
        _messenger.SendError(player, "Religion name required.");
        return;
    }

    var result = _religionManager.CreateReligion(player.PlayerUID, name, ...);
    if (result.Success)
        _messenger.SendSuccess(player, "Religion created!");
    else
        _messenger.SendError(player, result.ErrorMessage);
}
```

**Test:**
```csharp
[Fact]
public void CreateReligion_Success_SendsSuccessMessage()
{
    var messenger = new SpyPlayerMessenger();
    var commands = new ReligionCommands(messenger, ...);

    commands.HandleCreateReligion(player, args);

    Assert.Single(messenger.SuccessMessages);
    Assert.Contains("created", messenger.SuccessMessages[0]);
}
```

---

### Phase 5: Tier 2 and Tier 3 Wrappers (Week 7)

**Goal:** Implement remaining wrappers for completeness

**Tasks:**
1. Implement `ICommandService` (if command refactor needed)
2. Implement `ILoggerService` (optional, low priority)
3. Implement `IModLoaderService` (3 usages)
4. Implement `IInputService` (GUI hotkey, 1 usage)
5. Update `SoundManager` to use interface (already partially abstracted)

**Deliverables:**
- 4 additional interfaces + implementations
- Full API surface wrapped
- Documentation complete

---

### Phase 6: Documentation and Polish (Week 8)

**Goal:** Document wrapper usage and create migration guide

**Tasks:**
1. Write comprehensive API wrapper guide in `/docs/topics/implementation/api-wrappers.md`
2. Update CLAUDE.md with wrapper patterns
3. Create migration examples for future developers
4. Add XML documentation to all wrapper interfaces
5. Measure test improvements (mock count, execution time)
6. Write blog post / dev log about refactoring journey

**Deliverables:**
- Complete documentation
- Migration guide for future features
- Metrics report (test improvements)

---

## Migration Strategy

### Initialization Changes

**Current Flow (DivineAscensionSystemInitializer.cs):**
```csharp
public static void InitializeServerSystems(ICoreServerAPI sapi)
{
    var localization = new LocalizationService(sapi);
    var religionManager = new ReligionManager(sapi, ...);
    var playerData = new PlayerProgressionDataManager(sapi, ...);
    // ... 12 more managers
}
```

**New Flow (with wrappers):**
```csharp
public static void InitializeServerSystems(ICoreServerAPI sapi)
{
    // Step 0: Initialize wrappers
    var eventService = new ServerEventService(sapi.Event);
    var worldService = new ServerWorldService(sapi.World);
    var persistenceService = new ServerPersistenceService(sapi.WorldManager.SaveGame);
    var networkService = new ServerNetworkService(sapi.Network.GetChannel("divineascension"));
    var messengerService = new PlayerMessengerService();
    var loggerService = new LoggerService(sapi.Logger);

    // Step 1: Initialize domain services
    var localization = new LocalizationService(loggerService, persistenceService);

    // Step 2: Initialize managers (inject wrappers)
    var religionManager = new ReligionManager(
        eventService,
        persistenceService,
        worldService,
        messengerService,
        ...);

    var playerData = new PlayerProgressionDataManager(
        eventService,
        persistenceService,
        worldService,
        ...);

    // ... rest of initialization
}
```

### Backward Compatibility

**Strategy:** Add wrappers alongside existing API parameters temporarily

**Example:**
```csharp
// Step 1: Add wrapper parameter with default null (optional)
public ReligionManager(
    ICoreServerAPI sapi,
    IEventService? eventService = null,
    IPersistenceService? persistenceService = null,
    ...)
{
    _sapi = sapi;
    _eventService = eventService ?? new ServerEventService(sapi.Event); // fallback
    _persistenceService = persistenceService ?? new ServerPersistenceService(...);
}

// Step 2: Migrate all callsites to pass wrappers

// Step 3: Remove ICoreServerAPI parameter entirely
public ReligionManager(
    IEventService eventService,
    IPersistenceService persistenceService,
    ...)
{
    _eventService = eventService;
    _persistenceService = persistenceService;
}
```

### Test Migration Checklist

For each test file being migrated:

- [ ] Replace `Mock<ICoreServerAPI>` with wrapper mocks/fakes
- [ ] Replace `Mock<IServerEventAPI>` with `FakeEventService`
- [ ] Replace `Mock<IServerWorldAccessor>` with `FakeWorldService`
- [ ] Replace `Mock<IWorldManagerAPI>` + `Mock<ISaveGame>` with `FakePersistenceService`
- [ ] Replace `Mock<IServerNetworkChannel>` with `SpyNetworkService`
- [ ] Replace player mock's `SendMessage()` setup with `SpyPlayerMessenger`
- [ ] Verify test still passes
- [ ] Measure test execution time improvement

---

## Testing Strategy

### Test Double Implementations

**FakeEventService:**
```csharp
public class FakeEventService : IEventService
{
    private List<Action> _saveGameLoadedCallbacks = new();
    private List<Action> _gameWorldSaveCallbacks = new();
    // ... other event lists

    public void OnSaveGameLoaded(Action callback)
        => _saveGameLoadedCallbacks.Add(callback);

    public void TriggerSaveGameLoaded()
        => _saveGameLoadedCallbacks.ForEach(cb => cb());

    // Test helpers for other events
    public void TriggerPlayerJoin(IServerPlayer player)
        => _playerJoinCallbacks.ForEach(cb => cb(player));
}
```

**FakePersistenceService:**
```csharp
public class FakePersistenceService : IPersistenceService
{
    private Dictionary<string, object> _store = new();

    public T? Load<T>(string key) where T : class
        => _store.TryGetValue(key, out var obj) ? (T)obj : null;

    public void Save<T>(string key, T data) where T : class
        => _store[key] = data;

    public void Clear() => _store.Clear();
}
```

**SpyNetworkService:**
```csharp
public class SpyNetworkService : INetworkService
{
    public List<SentMessage> SentMessages { get; } = new();
    private Dictionary<Type, Delegate> _handlers = new();

    public void SendToPlayer<T>(IServerPlayer player, T message) where T : class
        => SentMessages.Add(new(player, message));

    public void RegisterMessageHandler<T>(Action<IServerPlayer, T> handler) where T : class
        => _handlers[typeof(T)] = handler;

    // Test helper
    public void SimulateReceive<T>(IServerPlayer player, T message) where T : class
    {
        if (_handlers.TryGetValue(typeof(T), out var handler))
            ((Action<IServerPlayer, T>)handler)(player, message);
    }

    public record SentMessage(IServerPlayer Player, object Message);
}
```

**FakeWorldService:**
```csharp
public class FakeWorldService : IWorldService
{
    private Dictionary<string, IServerPlayer> _playersByUID = new();
    private Dictionary<BlockPos, Block> _blocks = new();

    public IServerPlayer? GetPlayerByUID(string uid)
        => _playersByUID.TryGetValue(uid, out var player) ? player : null;

    public Block GetBlock(BlockPos pos)
        => _blocks.TryGetValue(pos, out var block) ? block : Block.Air;

    // Test setup helpers
    public void AddPlayer(IServerPlayer player) => _playersByUID[player.PlayerUID] = player;
    public void SetBlock(BlockPos pos, Block block) => _blocks[pos] = block;
}
```

### Test Fixture Updates

**Before (TestFixtures.cs):**
```csharp
public static class TestFixtures
{
    public static Mock<ICoreServerAPI> CreateMockServerAPI()
    {
        var sapi = new Mock<ICoreServerAPI>();
        var eventApi = new Mock<IServerEventAPI>();
        var worldAccessor = new Mock<IServerWorldAccessor>();
        var worldManager = new Mock<IWorldManagerAPI>();
        var saveGame = new Mock<ISaveGame>();
        var logger = new Mock<ILogger>();

        sapi.Setup(s => s.Event).Returns(eventApi.Object);
        sapi.Setup(s => s.World).Returns(worldAccessor.Object);
        sapi.Setup(s => s.WorldManager).Returns(worldManager.Object);
        sapi.Setup(s => s.Logger).Returns(logger.Object);
        worldManager.Setup(w => w.SaveGame).Returns(saveGame.Object);

        return sapi;
    }
}
```

**After (TestServices.cs):**
```csharp
public static class TestServices
{
    public static FakeEventService CreateEventService() => new();
    public static FakeWorldService CreateWorldService() => new();
    public static FakePersistenceService CreatePersistenceService() => new();
    public static SpyNetworkService CreateNetworkService() => new();
    public static SpyPlayerMessenger CreateMessengerService() => new();

    // Composite builder
    public static ServiceBundle CreateServiceBundle()
    {
        return new ServiceBundle(
            CreateEventService(),
            CreateWorldService(),
            CreatePersistenceService(),
            CreateNetworkService(),
            CreateMessengerService()
        );
    }
}

public record ServiceBundle(
    FakeEventService EventService,
    FakeWorldService WorldService,
    FakePersistenceService PersistenceService,
    SpyNetworkService NetworkService,
    SpyPlayerMessenger MessengerService);
```

**Usage:**
```csharp
[Fact]
public void CreateReligion_Success()
{
    // Arrange
    var services = TestServices.CreateServiceBundle();
    var manager = new ReligionManager(
        services.EventService,
        services.PersistenceService,
        services.WorldService,
        services.MessengerService,
        ...);

    // Act
    var result = manager.CreateReligion(...);

    // Assert
    Assert.True(result.Success);
    var saved = services.PersistenceService.Load<ReligionWorldData>("religions");
    Assert.Single(saved.Religions);
}
```

### Measuring Success

**Metrics to Track:**

1. **Mock Count per Test:**
   - Before: 15-20 mocks (IServerEventAPI, IServerWorldAccessor, IBlockAccessor, IWorldManagerAPI, ISaveGame, ILogger, IServerPlayer x10)
   - After: 3-5 fakes (EventService, WorldService, PersistenceService)
   - **Target: 70% reduction**

2. **Test Execution Time:**
   - Before: ~500ms average per test (complex setup)
   - After: ~100ms average per test (simple fakes)
   - **Target: 80% faster**

3. **Lines of Test Setup:**
   - Before: 30-50 lines of mock setup
   - After: 5-10 lines of fake creation
   - **Target: 80% reduction**

4. **Test Clarity:**
   - Subjective: Tests should read more like specifications, less like integration tests

---

## Risks and Mitigations

### Risk 1: Breaking Changes During Migration

**Likelihood:** Medium
**Impact:** High
**Mitigation:**
- Add wrappers with optional parameters first (backward compatible)
- Migrate one manager at a time with full test coverage
- Run full integration tests after each manager migration
- Keep `ICoreServerAPI` parameter alongside wrappers until all callsites migrated

### Risk 2: Wrapper Abstraction Leaks

**Likelihood:** Medium
**Impact:** Medium
**Description:** Wrappers may not perfectly encapsulate API behavior, leading to bugs

**Mitigation:**
- Write comprehensive unit tests for wrapper implementations themselves
- Test wrappers against real API in integration tests
- Document any behavioral differences in wrapper docs
- Keep wrappers thin (minimal logic)

### Risk 3: Performance Overhead

**Likelihood:** Low
**Impact:** Low
**Description:** Extra indirection through wrappers could impact performance

**Mitigation:**
- Measure performance before/after with benchmarks
- Wrappers are thin (no caching, minimal logic)
- JIT compiler should inline most wrapper calls
- If needed, add `[MethodImpl(MethodImplOptions.AggressiveInlining)]`

### Risk 4: Incomplete API Coverage

**Likelihood:** Medium
**Impact:** Low
**Description:** Wrappers may not cover all API methods needed in future

**Mitigation:**
- Design interfaces with extensibility in mind
- Add methods to wrappers as needed (incremental approach)
- Document which API surfaces are NOT wrapped (e.g., BlockPos, AssetLocation remain direct)
- Keep wrappers focused on high-value testability improvements

### Risk 5: Test Double Maintenance

**Likelihood:** Medium
**Impact:** Medium
**Description:** Fake implementations may drift from real API behavior

**Mitigation:**
- Create shared contract tests for wrappers and fakes
- Run same test suite against both real implementation and fakes
- Document fake limitations clearly
- Keep fakes simple (in-memory storage, no complex logic)

### Risk 6: Developer Onboarding

**Likelihood:** Low
**Impact:** Low
**Description:** New developers may not understand wrapper pattern

**Mitigation:**
- Comprehensive documentation in `/docs/topics/implementation/api-wrappers.md`
- Update CLAUDE.md with wrapper usage examples
- Add XML documentation to all wrapper interfaces
- Include migration guide for new features

---

## Success Metrics

### Quantitative Metrics

1. **Test Suite Metrics:**
   - [ ] Mock count reduced by ≥70% (15-20 → 3-5 per test)
   - [ ] Test execution time improved by ≥50% (500ms → 250ms avg)
   - [ ] Test setup lines reduced by ≥80% (40 lines → 8 lines avg)
   - [ ] Test coverage maintained at ≥80% (currently ~75%)

2. **Code Metrics:**
   - [ ] 107 files with direct API dependencies → <10 files (wrappers only)
   - [ ] 100+ `Mock<IServerPlayer>` → 0 (use `SpyPlayerMessenger` instead)
   - [ ] 44 `Mock<IServerWorldAccessor>` → 0 (use `FakeWorldService`)
   - [ ] 20 `Mock<IServerEventAPI>` → 0 (use `FakeEventService`)

3. **Development Velocity:**
   - [ ] Time to write new test: 20 min → 5 min
   - [ ] Time to debug failing test: 15 min → 5 min

### Qualitative Metrics

1. **Testability:**
   - [ ] Can test event handling without full API mock
   - [ ] Can test favor trackers with simple block setup
   - [ ] Can test persistence logic with in-memory store
   - [ ] Can test network handlers without protocol buffer setup

2. **Maintainability:**
   - [ ] Wrappers serve as living documentation of API usage
   - [ ] Tests are easier to read and understand
   - [ ] New developers can write tests faster

3. **Architecture:**
   - [ ] Clear separation between framework and domain logic
   - [ ] Seams for future refactoring (e.g., extracting business logic)
   - [ ] Consistent dependency injection pattern

---

## Timeline Summary

| Phase | Duration | Focus | Deliverables |
|-------|----------|-------|--------------|
| Phase 1 | Week 1-2 | Foundation | 4 Tier 1 wrappers + test doubles |
| Phase 2 | Week 3-4 | Manager Migration | 6 managers migrated, 30 tests updated |
| Phase 3 | Week 5 | Favor/Effects | 13 trackers + 5 handlers migrated |
| Phase 4 | Week 6 | Commands/Network | 7 commands + 6 handlers migrated |
| Phase 5 | Week 7 | Tier 2/3 Wrappers | 4 additional wrappers |
| Phase 6 | Week 8 | Documentation | Comprehensive docs + metrics report |

**Total Duration:** 8 weeks (part-time, ~10-15 hours/week)

---

## Appendix A: Wrapper Interface Summary

| Interface | Priority | Replaces | Usages | Test Double |
|-----------|----------|----------|--------|-------------|
| IEventService | Tier 1 | IServerEventAPI | 40+ | FakeEventService |
| IWorldService | Tier 1 | IServerWorldAccessor | 35+ | FakeWorldService |
| IPersistenceService | Tier 1 | IWorldManagerAPI + ISaveGame | 15+ | FakePersistenceService |
| INetworkService | Tier 1 | IServerNetworkChannel | 40+ | SpyNetworkService |
| IPlayerMessengerService | Tier 2 | IServerPlayer.SendMessage | 20+ | SpyPlayerMessenger |
| ICommandService | Tier 2 | IChatCommandApi | 7 | MockCommandService |
| ILoggerService | Tier 3 | ILogger | 40+ | SpyLogger |
| IModLoaderService | Tier 3 | IModLoaderAPI | 3 | FakeModLoader |
| IInputService | Tier 3 | IInputAPI | 1 | FakeInputService |

---

## Appendix B: Affected File List

### Managers (6 files)
- ReligionManager.cs
- PlayerProgressionDataManager.cs
- CivilizationManager.cs
- DiplomacyManager.cs
- FavorSystem.cs
- BlessingEffectSystem.cs

### Favor Trackers (11 files)
- MiningFavorTracker.cs
- AnvilFavorTracker.cs
- StoneFavorTracker.cs
- SmeltingFavorTracker.cs
- HuntingFavorTracker.cs
- SkinningFavorTracker.cs
- ForagingFavorTracker.cs
- HarvestFavorTracker.cs
- CraftFavorTracker.cs
- ConquestFavorTracker.cs
- RuinDiscoveryFavorTracker.cs

### Effect Handlers (5 files)
- AethraEffectHandlers.cs
- GaiaEffectHandlers.cs
- KhorasEffectHandlers.cs
- LysaEffectHandlers.cs
- ConquestEffectHandlers.cs

### Commands (7 files)
- ReligionCommands.cs
- BlessingCommands.cs
- CivilizationCommands.cs
- FavorCommands.cs
- RoleCommands.cs
- ConfigCommands.cs
- CommandHelpers.cs

### Network Handlers (6 files)
- PlayerDataNetworkHandler.cs
- ReligionNetworkHandler.cs
- BlessingNetworkHandler.cs
- CivilizationNetworkHandler.cs
- DiplomacyNetworkHandler.cs
- ActivityNetworkHandler.cs

### Services (3 files)
- LocalizationService.cs
- ProfanityFilterService.cs
- BlessingLoader.cs

### GUI (4 files)
- GuiDialog.cs
- DivineAscensionNetworkClient.cs
- SoundManager.cs
- Icon loaders (4 files)

**Total: 46 core files to migrate**

---

## Appendix C: Example Test Comparison

### Before (Current Test)

```csharp
[Fact]
public void CreateReligion_Success_SavesData()
{
    // Arrange - 40 lines of mock setup
    var sapi = new Mock<ICoreServerAPI>();
    var eventApi = new Mock<IServerEventAPI>();
    var worldAccessor = new Mock<IServerWorldAccessor>();
    var worldManager = new Mock<IWorldManagerAPI>();
    var saveGame = new Mock<ISaveGame>();
    var logger = new Mock<ILogger>();
    var networkChannel = new Mock<IServerNetworkChannel>();

    sapi.Setup(s => s.Event).Returns(eventApi.Object);
    sapi.Setup(s => s.World).Returns(worldAccessor.Object);
    sapi.Setup(s => s.WorldManager).Returns(worldManager.Object);
    sapi.Setup(s => s.Logger).Returns(logger.Object);
    worldManager.Setup(w => w.SaveGame).Returns(saveGame.Object);

    var player = new Mock<IServerPlayer>();
    player.Setup(p => p.PlayerUID).Returns("player123");
    worldAccessor.Setup(w => w.PlayerByUid("player123")).Returns(player.Object);

    byte[]? capturedData = null;
    saveGame.Setup(s => s.StoreData(It.IsAny<string>(), It.IsAny<byte[]>()))
           .Callback<string, byte[]>((key, data) => capturedData = data);

    var activityLog = new Mock<IActivityLogManager>();
    var roleManager = new Mock<IRoleManager>();
    var prestige = new Mock<IReligionPrestigeManager>();
    var profanity = new Mock<IProfanityFilterService>();
    profanity.Setup(p => p.ContainsProfanity(It.IsAny<string>())).Returns(false);

    var manager = new ReligionManager(
        sapi.Object,
        roleManager.Object,
        activityLog.Object,
        prestige.Object,
        profanity.Object);

    // Act
    var result = manager.CreateReligion("player123", "Test Religion", DeityDomain.Craft, "Test God");

    // Assert
    Assert.True(result.Success);
    Assert.NotNull(capturedData);

    // Verify saved data
    var worldData = SerializerUtil.Deserialize<ReligionWorldData>(capturedData);
    Assert.Single(worldData.Religions);
    Assert.Equal("Test Religion", worldData.Religions[0].Name);
}
```

### After (With Wrappers)

```csharp
[Fact]
public void CreateReligion_Success_SavesData()
{
    // Arrange - 8 lines of setup
    var services = TestServices.CreateServiceBundle();
    var profanity = new Mock<IProfanityFilterService>();
    profanity.Setup(p => p.ContainsProfanity(It.IsAny<string>())).Returns(false);

    var manager = new ReligionManager(
        services.EventService,
        services.PersistenceService,
        services.WorldService,
        services.MessengerService,
        new Mock<IRoleManager>().Object,
        new Mock<IActivityLogManager>().Object,
        new Mock<IReligionPrestigeManager>().Object,
        profanity.Object);

    // Act
    var result = manager.CreateReligion("player123", "Test Religion", DeityDomain.Craft, "Test God");

    // Assert
    Assert.True(result.Success);

    // Verify saved data (direct access to in-memory store)
    var worldData = services.PersistenceService.Load<ReligionWorldData>("religions");
    Assert.NotNull(worldData);
    Assert.Single(worldData.Religions);
    Assert.Equal("Test Religion", worldData.Religions[0].Name);
}
```

**Improvement:**
- **Setup lines:** 40 → 8 (80% reduction)
- **Mock count:** 14 → 4 (71% reduction)
- **Clarity:** No callback capture, direct assertion on data
- **Execution time:** ~500ms → ~50ms (90% faster)

---

## Conclusion

This plan provides a comprehensive strategy for introducing thin API wrappers to improve testability across Divine Ascension's 107 files with direct Vintage Story API dependencies. By implementing 9 wrapper interfaces in 6 phases over 8 weeks, we can:

- **Reduce test complexity by 70%** (15-20 mocks → 3-5 wrappers per test)
- **Improve test execution speed by 80%** (500ms → 100ms average)
- **Enable true unit testing** of event-driven logic, favor trackers, and network handlers
- **Create seams for future refactoring** without breaking existing functionality
- **Improve developer velocity** with faster test writing and debugging

The phased approach ensures zero disruption to ongoing development while progressively improving the codebase's testability and maintainability.

---

**Next Steps:**
1. Review and approve plan
2. Create GitHub issue/project for tracking
3. Begin Phase 1: Foundation (Week 1-2)
4. Schedule weekly check-ins to review progress
