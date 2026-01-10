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

### 1. Vintage Story API

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

### 2. Git Hooks (Optional but Recommended)

**Automatic Version Bumping:** Install Git hooks to automatically bump versions and update changelog on commit:

```bash
./tools/hooks/install-hooks.sh
```

This installs a `commit-msg` hook that:
- Detects conventional commit messages (`feat:`, `fix:`, etc.)
- Bumps version semantically in `modinfo.json` and `AssemblyInfo.cs`
- Updates `CHANGELOG.md` with categorized entries
- Stages the changes automatically

**Usage:**
```bash
git commit -m "fix: resolve bug"         # → patch bump (1.23.0 → 1.23.1)
git commit -m "feat: add feature"        # → minor bump (1.23.0 → 1.24.0)
git commit -m "feat!: breaking change"   # → major bump (1.23.0 → 2.0.0)
```

See `tools/hooks/README.md` for details.

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

## Architecture Overview

### Initialization Flow

**Entry Point:** `DivineAscensionModSystem.cs` (inherits `ModSystem`)
- **Start (Common):** Registers Harmony patches, registers 20+ network packet types on channel `"divineascension"`
- **StartServerSide:** Calls `DivineAscensionSystemInitializer.InitializeServerSystems()` to initialize all managers
- **StartClientSide:** Sets up `DivineAscensionNetworkClient` and `UiService` for UI dialogs
- **Dispose:** Unpatches Harmony, disposes managers and network handlers

**CRITICAL INITIALIZATION ORDER** (in `DivineAscensionSystemInitializer.cs`):
1. `DeityRegistry` - Register deities (Khoras, Lysa, Aethra, Gaia)
2. `ReligionManager` - Religion CRUD and membership with O(1) player-to-religion index
3. `CivilizationManager` - Civilization management (depends on ReligionManager)
4. `PlayerProgressionDataManager` - Per-player data
5. `ReligionPrestigeManager` - Religion-level progression
6. `FavorSystem` - Divine favor rewards (**depends on PrestigeManager**)
7. `PvPManager` - PvP favor rewards
8. `BlessingRegistry` - Blessing definitions
9. `BlessingEffectSystem` - Stat modifiers and effects (**must register with PrestigeManager after initialization**)
10. Command handlers (Favor, Blessing, Religion, Role, Civilization)
11. Network handlers (PlayerData, Blessing, Religion, Civilization)
12. **Membership validation** - Validates and repairs player-to-religion index consistency

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
- Queries ReligionManager for player's current religion and deity

**CivilizationManager** (`/Systems/CivilizationManager.cs`):
- Alliances of 1-4 religions with different deities
- Invite system (7-day expiry)
- Handles cascading deletion when religions disband

**FavorSystem** (`/Systems/FavorSystem.cs`):
- Awards favor for deity-aligned activities
- Passive favor generation (0.5/hour)
- Manages 7 sub-trackers: `MiningFavorTracker`, `AnvilFavorTracker`, `HuntingFavorTracker`, `ForagingFavorTracker`, `AethraFavorTracker`, `GaiaFavorTracker`, `SmeltingFavorTracker`
- Each tracker implements `IFavorTracker` with deity-specific logic

**BlessingRegistry** (`/Systems/BlessingRegistry.cs`):
- Loads all blessings from `BlessingDefinitions.cs` (1000+ lines)
- Query by deity, type (player/religion)
- Validates unlock eligibility (rank, prerequisites, deity match)

**BlessingEffectSystem** (`/Systems/BlessingEffectSystem.cs`):
- Applies stat modifiers from unlocked blessings
- Manages special effects via handlers in `/Systems/BlessingEffects/Handlers/`
- Caches stat modifiers per player for performance
- Special effect handlers: `AethraEffectHandlers`, `GaiaEffectHandlers`, `KhorasEffectHandlers`, `LysaEffectHandlers`

**ReligionPrestigeManager** (`/Systems/ReligionPrestigeManager.cs`):
- Religion-level progression (Fledgling → Divine)
- Awards prestige for member actions
- Unlocks religion-level blessings

**PvPManager** (`/Systems/PvPManager.cs`):
- PvP favor and prestige rewards on kills
- Death penalties

**DeityRegistry** (`/Systems/DeityRegistry.cs`):
- Registry of 4 deities (only Khoras and Lysa fully implemented)
- Ability ID lookup

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
- `AnvilPatches`, `PitKilnPatches`, `MoldPourPatches`, `ClayFormingPatches`, `CookingPatches`, `EatingPatches`, `KhorasPatches`
- **Pattern:** Intercept game methods with Harmony, raise events that favor trackers subscribe to

### Data Persistence

All data stored via Vintage Story's world save system with ProtoBuf serialization (`[ProtoContract]` attributes):
- `ReligionWorldData` - All religions
- `CivilizationWorldData` - All civilizations
- `PlayerProgressionData` - Per-player progression data (loaded on join, DataVersion=3)
  - Player religion membership tracked via ReligionManager's player-to-religion index

Events: `SaveGameLoaded` (load), `GameWorldSave` (persist)

## Key Architectural Patterns

1. **Manager/Registry Pattern** - All systems use manager classes for state/operations; registries provide lookups
2. **Event-Driven Architecture** - Systems communicate via events (e.g., `OnReligionDeleted`, `OnPlayerDataChanged`)
3. **Dependency Injection** - Managers passed via constructor, loosely coupled via interfaces
4. **Server-Client Separation** - Server manages state, client handles UI; communication via async packets
5. **Caching** - `BlessingEffectSystem` caches stat modifiers per player
6. **Data Models (Immutable Records)** - Clean separation of data from logic, ProtoBuf serializable
7. **ViewModel/Renderer Pattern (UI)** - ViewModels compute state, renderers handle ImGui drawing

## Important Constraints

1. **Initialization order is critical** - See initialization section above
2. **Single network channel** - All packets share `"divineascension"` channel
3. **Civilization limits** - 1-4 religions per civilization
4. **Favor rank persistence** - Favor rank tied to lifetime favor earned, persists across religion changes
5. **Blessing prerequisites** - Can require other blessings unlocked first
6. **Deity-bound blessings** - Only unlock if player matches deity
7. **Single source of truth** - ReligionManager is authoritative for membership; PlayerProgressionData queries it
8. **InternalsVisibleTo** - Main project exposes internals to tests via `[assembly: InternalsVisibleTo("DivineAscension.Tests")]`

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
