# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Divine Ascension is a Vintage Story mod implementing deity-driven religion and civilization systems. Built with .NET 8 (C# 12) and the Vintage Story API, it includes dual progression (Favor/Prestige), blessing trees with stat modifiers, civilization alliances, PvP integration, and a full ImGui-based UI.

**Key Technologies:**
- .NET SDK 8.0, C# 12
- Vintage Story API 1.21.0
- VSImGui 0.0.6 (ImGui integration)
- protobuf-net (networking)
- Harmony (runtime patching)
- xUnit v3 (testing)
- Cake build system

## Environment Setup

### Vintage Story API

**Required:** Set `VINTAGE_STORY` environment variable pointing to a Vintage Story installation containing:
- `VintagestoryAPI.dll`
- `Lib/0Harmony.dll`, `Lib/cairo-sharp.dll`, `Lib/Newtonsoft.Json.dll`, `Lib/protobuf-net.dll`
- `Mods/VSEssentials.dll`, `Mods/VSSurvivalMod.dll`

Examples:
```bash
# Linux/macOS
export VINTAGE_STORY="$HOME/.local/share/Vintagestory"

# Windows PowerShell
$env:VINTAGE_STORY = "C:\Games\Vintagestory"
```

## Common Commands

### Building

```bash
# Full Cake build with packaging (outputs to Releases/)
./build.sh              # Linux/macOS
./build.ps1             # Windows

# Quick solution build
dotnet build DivineAscension.sln -c Debug
dotnet build DivineAscension.sln -c Release
```

### Testing

```bash
# Run all tests
dotnet test

# Run specific test class by fully qualified name (preferred)
dotnet test --filter FullyQualifiedName~DivineAscension.Tests.GUI.UI.Utilities.DeityHelperTests

# Run tests matching a pattern
dotnet test --filter FullyQualifiedName~BlessingRegistry

# Generate coverage report (auto-installs ReportGenerator and opens HTML)
./generate-coverage.sh

# Manual coverage collection
dotnet test --collect:"XPlat Code Coverage" --settings coverlet.runsettings
```

**Note:** The coverage script references "PantheonWars" (legacy name) but works correctly. Coverage HTML opens at `coverage-report/index.html`.

### Project Structure

```
DivineAscension/               # Main mod project (outputs to bin/Debug|Release/Mods/mod/)
DivineAscension.Tests/         # xUnit v3 test suite
CakeBuild/                     # Cake build bootstrapper
docs/                          # Documentation (see docs/README.md)
Releases/                      # Packaged mod artifacts from Cake build
```

### Documentation

See `docs/README.md` for complete documentation structure and guidelines. Key locations:
- `docs/topics/planning/` - Planning notes and phase breakdowns
- `docs/topics/planning/features/` - Feature implementation plans (create subdirectory per feature)
- `docs/topics/implementation/` - Implementation guides and technical deep-dives
- `docs/topics/reference/` - Reference material for game systems

When creating feature plans, place them in `docs/topics/planning/features/<feature-name>/`.

## Architecture Overview

### Initialization Flow

**Entry Point:** `DivineAscensionModSystem.cs` (inherits `ModSystem`)
- **Start (Common):** Initializes ProfanityFilterService, registers Harmony patches, registers 20+ network packet types on channel `"divineascension"`
- **StartServerSide:** Calls `DivineAscensionSystemInitializer.InitializeServerSystems()` to initialize all managers, initializes LocalizationService
- **StartClientSide:** Sets up `DivineAscensionNetworkClient` and `UiService` for UI dialogs, initializes LocalizationService
- **Dispose:** Unpatches Harmony, disposes managers and network handlers

**CRITICAL INITIALIZATION ORDER** (in `DivineAscensionSystemInitializer.cs`):
0. `LocalizationService` - Multi-language support (must initialize before managers that use localized messages)
1. `CooldownManager` - Cooldown tracking (early initialization to prevent griefing attacks)
2. `ReligionManager` - Religion CRUD and membership with O(1) player-to-religion index
3. **Deity name migration** - Migrates existing religions with empty deity names (backward compatibility)
4. `ActivityLogManager` - Activity log tracking (depends on ReligionManager)
5. `CivilizationManager` - Civilization management (depends on ReligionManager)
6. `PlayerMessengerService` - Player messaging (after managers are initialized)
7. `PlayerProgressionDataManager` - Per-player data
8. `ReligionPrestigeManager` - Religion-level progression (**MUST be initialized before FavorSystem**)
9. `HolySiteManager` - Holy site management (depends on ReligionManager)
10. `AltarPlacementHandler` - Automatic holy site creation on altar placement (depends on HolySiteManager)
11. `AltarDestructionHandler` - Automatic holy site deconsecration on altar destruction (depends on HolySiteManager)
12. `FavorSystem` - Divine favor rewards (depends on PrestigeManager and ActivityLogManager)
13. `OfferingLoader` - Loads offering configurations from JSON (must be before AltarPrayerHandler)
14. `BuffManager` - Buff system for temporary stat modifiers (must be before AltarPrayerHandler)
15. `PlayerProgressionService` - Facade for favor/prestige/activity systems (must be before AltarPrayerHandler)
16. `AltarPrayerHandler` - Prayer interactions at altars (depends on many systems above)
17. `DiplomacyManager` - Inter-civilization diplomacy
18. `PvPManager` - PvP favor rewards
19. `BlessingRegistry` - Blessing definitions
20. `BlessingEffectSystem` - Stat modifiers and effects (**must register with PrestigeManager after initialization**)
21. Command handlers (Favor, Blessing, Religion, Role, Civilization, HolySite)
22. Network handlers (PlayerData, Blessing, Religion, Civilization, Diplomacy, Activity, HolySite)
23. **Membership validation** - Validates and repairs player-to-religion index consistency

**Never reorder these** - dependency chains will break.

### Core System Responsibilities

**ReligionManager** (`/Systems/ReligionManager.cs`):
- Religion creation, deletion, membership (add/remove)
- Role-based permissions via `RoleManager`
- Founder transfers
- Events: `OnReligionDeleted` (triggers cleanup cascades)
- Persistence via world save/load

**PlayerProgressionDataManager** (`/Systems/PlayerProgressionDataManager.cs`):
- Per-player favor points and lifetime favor earned
- Favor rank (Initiate → Avatar) computed from lifetime favor
- Unlocked player blessings tracking (HashSet)
- Events: `OnPlayerLeavesReligion`, `OnPlayerDataChanged`
- Queries ReligionManager for player's current religion and domain

**CivilizationManager** (`/Systems/CivilizationManager.cs`):
- Alliances of 1-4 religions with different domains
- Invite system (7-day expiry)
- Handles cascading deletion when religions disband

**HolySiteManager** (`/Systems/HolySiteManager.cs`):
- Holy site CRUD operations (create, query, remove)
- Persistence via `HolySiteWorldData` (ProtoBuf serialization)
- Volume-based tier calculation (Tier 1: <50k blocks³, Tier 2: 50k-200k, Tier 3: 200k+)
- Prayer multipliers by tier (2.0x, 2.5x, 3.0x)
- Position-based queries for altar integration (`GetHolySiteAtPosition`)
- Land claim area tracking with multi-area support
- Events: `OnHolySiteCreated`, `OnHolySiteDeleted`
- Cascading cleanup via subscription to `ReligionManager.OnReligionDeleted`
- Integrated with `AltarPlacementHandler` for automatic holy site creation
- Integrated with `AltarDestructionHandler` for automatic deconsecration

**FavorSystem** (`/Systems/FavorSystem.cs`):
- Awards favor for domain-aligned activities
- Passive favor generation (0.5/hour)
- Manages 10 sub-trackers: `MiningFavorTracker`, `AnvilFavorTracker`, `HuntingFavorTracker`, `SkinningFavorTracker`, `ForagingFavorTracker`, `HarvestFavorTracker`, `StoneFavorTracker`, `SmeltingFavorTracker`, `ConquestFavorTracker`, `RuinDiscoveryFavorTracker`
- Each tracker implements `IFavorTracker` with domain-specific logic

**BlessingRegistry** (`/Systems/BlessingRegistry.cs`):
- Loads all blessings from JSON assets via `BlessingLoader` service
- Blessing definitions stored in `assets/divineascension/config/blessings/*.json` (5 files: craft.json, wild.json, conquest.json, harvest.json, stone.json)
- Query by domain, type (player/religion)
- Validates unlock eligibility (rank, prerequisites, domain match)

**BlessingEffectSystem** (`/Systems/BlessingEffectSystem.cs`):
- Applies stat modifiers from unlocked blessings
- Manages special effects via handlers in `/Systems/BlessingEffects/Handlers/`
- Caches stat modifiers per player for performance
- Special effect handlers: `AethraEffectHandlers`, `GaiaEffectHandlers`, `KhorasEffectHandlers`, `LysaEffectHandlers`, `ConquestEffectHandlers`

**ReligionPrestigeManager** (`/Systems/ReligionPrestigeManager.cs`):
- Religion-level progression (Fledgling → Divine)
- Awards prestige for member actions
- Unlocks religion-level blessings

**ActivityLogManager** (`/Systems/ActivityLogManager.cs`):
- Tracks religion activity feed with favor/prestige awards
- Bounded storage (100 entries per religion, FIFO eviction)
- Logs player actions with timestamps, amounts, and deity domains
- Auto-cleanup on religion deletion via event subscription
- Persisted with `ReligionData.ActivityLog`

**PvPManager** (`/Systems/PvPManager.cs`):
- PvP favor and prestige rewards on kills
- Death penalties

**DiplomacyManager** (`/Systems/DiplomacyManager.cs`):
- Inter-civilization diplomatic relationships (NAP, Alliance, War, Neutral)
- Proposal system with 7-day expiry
- Violation tracking for treaty breaches
- Peace/War declarations

### Command System

**Command handlers** (`/Commands/`):
- `ReligionCommands.cs` - Religion CRUD, membership, bans, prestige, deity name management
- `CivilizationCommands.cs` - Civilization management, invites
- `BlessingCommands.cs` - Blessing unlocks and queries
- `FavorCommands.cs` - Player favor and admin commands
- `RoleCommands.cs` - Role assignment and management
- `ConfigCommands.cs` - Admin-only mod configuration (profanity filter toggle)
- `CommandHelpers.cs` - Shared validation utilities

**Custom Parsers** (`/Commands/Parsers/`):
- `QuotedStringParser.cs` - Handles names with spaces via quotes (e.g., `"My Religion"`)
- `CommandParserExtensions.cs` - Extension methods: `QuotedString()`, `OptionalQuotedString()`

**Usage:** Commands use Vintage Story's fluent API with custom parsers for name arguments:
```csharp
.WithArgs(_sapi.ChatCommands.Parsers.QuotedString("name"))  // Required quoted string
.WithArgs(_sapi.ChatCommands.Parsers.OptionalQuotedString("name"))  // Optional
```

Users can specify names as `"Name With Spaces"` or `SingleWord` (backward compatible).

### Networking Architecture

**All packets use channel:** `"divineascension"`

**Server-side handlers** (in `/Systems/Networking/Server/`):
- `PlayerDataNetworkHandler` - Player sync, info requests
- `ReligionNetworkHandler` - Religion operations (~40KB file with all CRUD)
- `BlessingNetworkHandler` - Blessing unlocks, tree queries
- `CivilizationNetworkHandler` - Civilization management

**Client-side handler:** `DivineAscensionNetworkClient.cs`
- Receives responses, broadcasts events to UI
- Events: `PlayerReligionDataUpdated`, `BlessingUnlocked`, `ReligionStateChanged`, etc.

**Packet types** (20+): Request/response pattern for player data, religions, blessings, civilizations, roles. All in `/Network/` directory.

### GUI/UI System

**Main Dialog:** `GuiDialog.cs` (ImGui-based `ModSystem`)
- Hotkey: Shift+G
- Window size: 1400x900 base
- Architecture: `GuiDialog` → `GuiDialogManager` → Tab-specific state managers

**State Managers** (`/GUI/State/`):
- `GuiDialogState` - Main state machine
- `ReligionTabState`, `BlessingTabState`, `CivilizationTabState`
- Subtabs: `ActivityState`, `BrowseState`, `CreateState`, `InfoState`

**Renderers** (`/GUI/UI/Renderers/`):
- Component renderers: buttons, checkboxes, lists, progress bars
- Domain renderers: religions, blessings, civilizations (browse/create/info/detail)

**Pattern:** Events → ViewModel → RenderResult → Renderer

**UI Service:** `UiService.cs` - Injectable API for dialogs using `DivineAscensionNetworkClient`

**Icon Loaders** (`/GUI/UI/Utilities/`): `DeityIconLoader`, `BlessingIconLoader`, `CivilizationIconLoader`, `GuiIconLoader`

**Domain/Deity Helpers** (`/GUI/UI/Utilities/`): `DomainHelper` (domain enum utilities), `DeityInfoHelper` (deity display info)

### Buff/Stat Modifier System

**BuffManager** (`/Systems/BuffSystem/BuffManager.cs`):
- Applies/removes temporary buffs with stat modifiers
- Manages duration and expiration
- Integrates with blessing stat modifiers

**EntityBehaviorBuffTracker** (`/Systems/BuffSystem/EntityBehaviorBuffTracker.cs`):
- Attached to entities via `"DivineAscensionBuffTracker"` behavior class
- Tracks active effects, applies stat modifiers
- Handles cleanup on expiration

### Harmony Patches

**Patch classes** (`/Systems/Patches/`):
- `AnvilPatches`, `BlockCropPatches`, `ClayFormingPatches`, `CookingPatches`, `CraftPatches`, `CropPlantingPatches`, `EatingPatches`, `FlowerPatches`, `ForagingPatches`, `MoldPourPatches`, `MushroomPatches`, `PitKilnPatches`, `SkinningPatches`
- **Pattern:** Intercept game methods with Harmony, raise events that favor trackers subscribe to

### Data Persistence

All data stored via Vintage Story's world save system with ProtoBuf serialization (`[ProtoContract]` attributes):
- `ReligionWorldData` - All religions
- `CivilizationWorldData` - All civilizations
- `PlayerProgressionData` - Per-player progression data (loaded on join, DataVersion=3)
  - Player religion membership tracked via ReligionManager's player-to-religion index

Events: `SaveGameLoaded` (load), `GameWorldSave` (persist)

### Services

**LocalizationService** (`/Services/LocalizationService.cs`):
- Singleton service for multi-language support
- Loads translations from `assets/divineascension/lang/*.json`
- Server-side: Loads JSON directly
- Client-side: Uses Vintage Story's `Lang` API
- Thread-safe caching for performance
- Format string support with parameter substitution

**ProfanityFilterService** (`/Services/ProfanityFilterService.cs`):
- Singleton service for content moderation
- Filters inappropriate content in religion/civilization names, deity names, and descriptions
- Loads word list from embedded resource or mod assets (server-configurable)
- Case-insensitive whole-word matching with O(1) HashSet lookups
- L33t speak detection (e.g., `$h1t`, `4ss`) and repetition collapse (e.g., `shiiiit`)
- Integrated at all creation entry points (commands and network handlers)
- Can be enabled/disabled per-world via `/da config profanityfilter [on|off]` (admin only)
- See `docs/topics/configuration/profanity-filter.md` for detailed documentation
- Default word lists in `assets/divineascension/config/profanity/` (en.txt, de.txt, es.txt, fr.txt, ru.txt)

**BlessingLoader** (`/Services/BlessingLoader.cs`):
- Loads blessing definitions from JSON assets following `LocalizationService` pattern
- Implements `IBlessingLoader` interface for dependency injection and testing
- Reads from `assets/divineascension/config/blessings/{domain}.json` (craft, wild, conquest, harvest, stone)
- Validates domain, kind, and category enums during deserialization
- Logs warnings for unknown stat keys but still includes blessings
- DTOs: `BlessingJsonDto` (individual blessing), `BlessingFileDto` (file structure with domain and version)

### API Wrapper Layer

**Purpose:** Thin abstraction layer over Vintage Story's API for improved testability and maintainability.

**Location:** `/DivineAscension/API/` directory with `/Interfaces/` and `/Implementation/` subdirectories

**Design Principles:**
- Thin wrappers with minimal logic (direct pass-through to underlying API)
- Interface segregation (each wrapper focuses on specific API surface)
- Constructor-based dependency injection
- Test-friendly interfaces for easy mocking/faking

**Tier 1 - Critical Wrappers:**

1. **IEventService** (`ServerEventService`) - Wraps `IServerEventAPI`
   - Event subscriptions: `OnSaveGameLoaded`, `OnGameWorldSave`, `OnPlayerJoin`, `OnPlayerDisconnect`, `OnPlayerDeath`
   - Game events: `OnBreakBlock`, `OnDidUseBlock`, `OnDidPlaceBlock`
   - Periodic callbacks: `RegisterGameTickListener`, `RegisterCallback`, `UnregisterCallback`
   - Test double: `FakeEventService` with `Trigger*()` methods to manually fire events

2. **IWorldService** (`ServerWorldService`) - Wraps `IServerWorldAccessor`
   - Player access: `GetPlayerByUID`, `GetPlayerByName`, `GetAllOnlinePlayers`
   - Block access: `GetBlock(BlockPos)`, `GetBlock(int)`, `GetBlockEntity`, `HasBlock`
   - World interaction: `SpawnItemEntity`, `PlaySoundAt`
   - Test double: `FakeWorldService` with in-memory player/block storage

3. **IPersistenceService** (`ServerPersistenceService`) - Wraps `IWorldManagerAPI.SaveGame`
   - Generic save/load: `Load<T>(key)`, `Save<T>(key, data)`
   - Raw byte access: `LoadRaw`, `SaveRaw`
   - Utilities: `Exists`, `Delete`
   - Test double: `FakePersistenceService` with in-memory dictionary storage

4. **INetworkService** (`ServerNetworkService`) - Wraps `IServerNetworkChannel`
   - Handler registration: `RegisterMessageHandler<T>(handler)`
   - Sending: `SendToPlayer<T>`, `SendToAllPlayers<T>`, `Broadcast<T>`
   - Test double: `SpyNetworkService` with `SentMessages` list and `SimulateMessage<T>()` helper

5. **ITimeService** (`ServerTimeService`) - Wraps `IServerWorldAccessor` time access
   - Time access: `ElapsedMilliseconds` property
   - Provides monotonically increasing timestamp for absolute time tracking
   - Used for cooldowns, expiry times, and time-dependent logic
   - Test double: `FakeTimeService` with `SetElapsedMilliseconds()`, `AdvanceTimeBy()`, and `Reset()` methods

**Tier 2 - High Value Wrappers:**

6. **IPlayerMessengerService** (`PlayerMessengerService`) - Wraps player messaging
   - Basic messaging: `SendMessage(player, message, type)`, `SendGroupMessage(groupId, message, type)`
   - Test double: `SpyPlayerMessenger` with `SentMessages` list

**Tier 3 - Specialized Wrappers:**

7. **IModLoaderService** (`ModLoaderService`) - Wraps `IModLoader`
   - Mod checks: `IsModEnabled(modId)`
   - Mod system access: `GetModSystem<T>()`, `GetModSystemByName(name)`
   - Test double: `FakeModLoaderService` with configurable enabled mods and registered systems

8. **IInputService** (`ClientInputService`) - Wraps `IInputAPI` (client-side only)
   - Hotkey management: `RegisterHotKey`, `SetHotKeyHandler`, `UnregisterHotKey`
   - Test double: `FakeInputService` with `SimulateHotKeyPress()` helper

9. **IChatCommandService** (`ServerChatCommandService`) - Wraps `IChatCommand` API
   - Command registration: `Create(commandName)` returns fluent builder interface
   - Provides custom parsers: `QuotedString()`, `OptionalQuotedString()` for names with spaces
   - Handles command hierarchy with subcommands via `BeginSubCommand()`/`EndSubCommand()`
   - Test double: `FakeChatCommandService` with `RegisteredCommands` list and `SimulateCommand()` helper

**Usage Pattern:**
```csharp
// Constructor injection
public ReligionManager(
    IEventService eventService,
    IPersistenceService persistenceService,
    IWorldService worldService,
    ...)
{
    _eventService = eventService;
    // ...
}

// Event subscription
public void Initialize()
{
    _eventService.OnSaveGameLoaded(LoadFromWorldData);
    _eventService.OnGameWorldSave(SaveToWorldData);
}

// Persistence
private void LoadFromWorldData()
{
    var data = _persistenceService.Load<ReligionWorldData>("religions");
    // ...
}
```

**Testing Pattern:**
```csharp
[Fact]
public void CreateReligion_Success_SavesData()
{
    // Arrange - use fakes instead of 15+ mocks
    var eventService = new FakeEventService();
    var persistence = new FakePersistenceService();
    var worldService = new FakeWorldService();

    var manager = new ReligionManager(eventService, persistence, worldService, ...);

    // Act
    var result = manager.CreateReligion("player1", "Test Religion", ...);

    // Assert - direct access to in-memory storage
    var saved = persistence.Load<ReligionWorldData>("religions");
    Assert.NotNull(saved);
    Assert.Single(saved.Religions);
}
```

**Test Doubles Location:** `/DivineAscension.Tests/Helpers/`
- `FakeEventService`, `FakeWorldService`, `FakePersistenceService`, `FakeModLoaderService`, `FakeInputService`
- `SpyNetworkService`, `SpyPlayerMessenger`

**Documentation:** See `docs/topics/implementation/api-wrappers.md` for comprehensive guide

**Benefits Achieved:**
- Reduced mock complexity from 15-20 mocks to 3-5 wrappers per test (70% reduction)
- Test execution speed improved by 80% (500ms → 100ms average)
- Event-driven logic now fully testable without complex mock setups
- Clear separation between framework and domain logic

## Key Architectural Patterns

1. **Manager/Registry Pattern** - All systems use manager classes for state/operations; registries provide lookups
2. **Event-Driven Architecture** - Systems communicate via events (e.g., `OnReligionDeleted`, `OnPlayerDataChanged`)
3. **Dependency Injection** - Managers passed via constructor, loosely coupled via interfaces
4. **Server-Client Separation** - Server manages state, client handles UI; communication via async packets
5. **Caching** - `BlessingEffectSystem` caches stat modifiers per player
6. **Data Models (Immutable Records)** - Clean separation of data from logic, ProtoBuf serializable
7. **ViewModel/Renderer Pattern (UI)** - ViewModels compute state, renderers handle ImGui drawing
8. **Permission Systems** - Religion and Civilization use different permission models:
   - **Religion Permissions:**
     - **Role-Based System:** Most operations use `RoleManager` with configurable roles
     - Members can be assigned roles with different permission sets
     - **Founder-Only Actions:** Critical operations (disband, transfer founder) require founder status
     - Founder validation: `religion.FounderUID == playerUID`
   - **Civilization Permissions:**
     - **Founder-Only System:** No role system - all admin actions require founder status
     - Actions include: edit description, edit icon, invite religions, kick religions, disband
     - Founder validation: `civilization.FounderUID == playerUID`
   - **Founder Validation Pattern (Both):**
     - **Server-Side:** Always validates using `entity.FounderUID == playerUID`
       - Single source of truth: `FounderUID` property on data model
       - Checked in managers, commands, and network handlers
       - Never trust client for permission checks
     - **Client-Side (GUI):** Mirrors server validation
       - Compares current player's UID with entity's FounderUID
       - Used to show/hide UI elements (edit buttons, admin panels)
       - Cached in state managers for performance
   - **Anti-Pattern:** Never compare religion UIDs to determine founder status
     - Religion UIDs are shared by all members
     - Only player UIDs uniquely identify the founder
     - Example: `player.ReligionUID == civ.FounderReligionUID` is WRONG
9. **Periodic Callbacks** - Systems can register time-based callbacks for periodic scanning/processing
   - Use `_sapi.Event.RegisterCallback(callback, intervalMs)` to register
   - Returns callback ID for later unregistration
   - Use `_sapi.Event.UnregisterCallback(callbackId)` in Dispose()
   - Example: `RuinDiscoveryFavorTracker` scans for ruins every 500ms

## Important Constraints

1. **Initialization order is critical** - See initialization section above
2. **Single network channel** - All packets share `"divineascension"` channel
3. **Civilization limits** - 1-4 religions per civilization (each must have different domain)
4. **Favor rank persistence** - Favor rank tied to lifetime favor earned, persists across religion changes
5. **Blessing prerequisites** - Can require other blessings unlocked first
6. **Domain-bound blessings** - Only unlock if player's religion matches the blessing's domain
7. **Single source of truth** - ReligionManager is authoritative for membership; PlayerProgressionData queries it
8. **InternalsVisibleTo** - Main project exposes internals to tests via `[assembly: InternalsVisibleTo("DivineAscension.Tests")]`
9. **Domain vs Deity Name** - `DeityDomain` enum (Craft, Wild, Conquest, Harvest, Stone) defines mechanics; `DeityName` string is customizable display name

## Development Practices

- C# 12 with `ImplicitUsings` and `Nullable` enabled
- Keep types `internal` when appropriate (tests have access via `InternalsVisibleTo`)
- Place new tests under `DivineAscension.Tests/<Area>/` with namespaces matching folder structure
- Test framework: xUnit v3 with Moq for mocking
- Inject clocks and random sources for deterministic testing (avoid time/randomness flakiness)

## Troubleshooting

- **Build errors about missing Vintage Story DLLs** → Verify `VINTAGE_STORY` env var is set and points to correct directory. Restart shell so `dotnet` inherits the variable.
- **Coverage script doesn't open report** → Manually open `coverage-report/index.html`
- **Test failures tied to time/randomness** → Inject clocks and random sources for determinism
- **Harmony patches not applying** → Check patch initialization in `DivineAscensionModSystem.Start()` and verify target methods exist in Vintage Story API version
