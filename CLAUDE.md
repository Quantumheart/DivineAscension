# CLAUDE.md

Guidance for Claude Code when working in this repo.

## Project

Divine Ascension — Vintage Story mod adding religions, deities, civilizations, blessings, PvP hooks, and an ImGui UI. .NET 10, C# 12, VS API 1.22.x, VSImGui, protobuf-net, Harmony, xUnit v3, Cake.

## Environment

Set `VINTAGE_STORY` to a VS install containing `VintagestoryAPI.dll`, `Lib/*` (0Harmony, cairo-sharp, Newtonsoft.Json, protobuf-net), and `Mods/VSEssentials.dll`, `Mods/VSSurvivalMod.dll`. CI builds use reference assemblies in `lib/ci-stubs/` (regenerate via `./tools/generate-stubs.sh`).

## Commands

```bash
# Build
dotnet build DivineAscension.sln -c Debug        # quick
./build.sh                                       # full Cake build → Releases/

# Test
dotnet test
dotnet test --filter FullyQualifiedName~<Pattern>
./generate-coverage.sh                           # opens coverage-report/index.html
```

Coverage script mentions "PantheonWars" (legacy name) but works.

## Layout

```
DivineAscension/         main mod, output → bin/<Config>/Mods/mod/
DivineAscension.Tests/   xUnit v3 suite
CakeBuild/               build bootstrapper
docs/                    see docs/README.md
lib/ci-stubs/            VS reference assemblies for CI
```

Feature plans go in `docs/topics/planning/features/<name>/`.

## Entry point

`DivineAscensionModSystem.cs` (`ModSystem`):
- **Start (common):** ProfanityFilterService, Harmony patches, ~20 network packet types on channel `"divineascension"`.
- **StartServerSide:** delegates to `DivineAscensionSystemInitializer.InitializeServerSystems()`, plus `LocalizationService.Initialize`.
- **StartClientSide:** `DivineAscensionNetworkClient`, `UiService`, localization.
- **Dispose:** unpatch Harmony, dispose managers.

## Initialization order (critical)

`DivineAscensionSystemInitializer.cs` constructs systems in a strict order. Dependencies are noted in comments at each step — **do not reorder.** Notable chains:

- `LocalizationService` before any localized message use.
- `CooldownManager` early (anti-grief).
- `ReligionManager` before `ActivityLogManager`, `CivilizationManager`, `HolySiteManager`.
- `ReligionPrestigeManager` before `FavorSystem`.
- `OfferingLoader`, `RitualLoader`, `MilestoneDefinitionLoader` before consumers.
- Loaders → `BuffManager` → `PlayerProgressionService` → `AltarPrayerHandler`.
- `CivilizationMilestoneManager` → `CivilizationBonusSystem`; the bonus system is wired back into `FavorSystem`, `HolySiteManager`, `PvPManager` via `Set*()` calls to resolve circular deps.
- `BlessingEffectSystem` registers with the prestige manager after construction.
- Final step validates and repairs the player-to-religion index.

## Architecture

- **Managers/Registries.** State and lookups in dedicated classes.
- **Events.** Cross-system communication (e.g. `ReligionManager.OnReligionDeleted` cascades cleanup). Periodic work via `IEventService.RegisterGameTickListener` / `RegisterCallback`.
- **DI.** Managers and wrappers injected via constructor.
- **Server is authoritative.** Clients send requests; server responds via channel `"divineascension"`. Never trust client for permissions.
- **Single source of truth:** `ReligionManager` owns membership; `PlayerProgressionData` queries it.
- **Domain vs DeityName.** `DeityDomain` enum (Craft/Wild/Conquest/Harvest/Stone) drives mechanics; `DeityName` is a display string.

## Permission model

- **Religion** — role-based via `RoleManager`; founder-only actions (disband, transfer) check `religion.FounderUID == playerUID`.
- **Civilization** — founder-only for all admin actions, same UID check.
- Validation happens server-side in managers/commands/network handlers. UI mirrors the check to hide controls. **Never** compare religion UIDs to decide founder status — religion UIDs are shared by members.

## Networking

One channel: `"divineascension"`. Server handlers under `Systems/Networking/Server/` (per area). Client handler `DivineAscensionNetworkClient.cs` raises events for the UI. Packet contracts in `/Network/`.

## UI

- Dialog: `GuiDialog.cs` (ImGui, hotkey Shift+G).
- Flow: events → state managers (`GuiDialogState`, `ReligionTabState`, etc.) → ViewModel → renderers under `GUI/UI/Renderers/`.
- Helpers under `GUI/UI/Utilities/` (icon loaders, deity/domain helpers).
- Inject `UiService` to open dialogs.

## Block behaviors

Attached to vanilla blocks via JSON patches in `assets/divineascension/patches/`. BlockBehavior constructors can't take DI, so they emit events through a static service-locator (`AltarEventEmitter`, etc.) initialized in `DivineAscensionSystemInitializer`. Handlers (`AltarPrayerHandler`, `AltarPlacementHandler`, `AltarDestructionHandler`) keep full DI and subscribe.

Implemented: `BlockBehaviorAltar`, `BlockBehaviorStone`, `BlockBehaviorOre`. Behavior class must be `public` and registered in `ModSystem.Start()` via `api.RegisterBlockBehaviorClass(...)`. Use `DoPlaceBlock` (has player context) instead of `OnBlockPlaced` (no context, fires for worldgen too).

JSON patch tips: use `"op": "addmerge"` (1.19.4+), capitalised `"side"` (`"Server"`/`"Client"`), `game:` prefix for vanilla files. Use `behaviorsByType` paths for items/blocks that don't have a flat `behaviors` array.

## Harmony

Patch classes under `Systems/Patches/` (Anvil, Cooking, ClayForming, etc.) intercept game methods and raise events that favor trackers subscribe to. `StonePatches` provides stone/clay yield bonuses via prefix on `Block.OnBlockBroken`. Altar logic lives in BlockBehavior, not patches.

## Persistence

ProtoBuf via the world save: `ReligionWorldData`, `CivilizationWorldData`, `PlayerProgressionData` (DataVersion=4). Load on `SaveGameLoaded`, persist on `GameWorldSave`.

## API wrappers (`/API/`)

Thin wrappers around VS APIs to keep domain code testable:

- Tier 1: `IEventService`, `IWorldService`, `IPersistenceService`, `INetworkService`, `ITimeService`.
- Tier 2: `IPlayerMessengerService`.
- Tier 3: `IModLoaderService`, `IInputService`, `IChatCommandService`.

Test doubles (`FakeEventService`, `FakeWorldService`, `FakePersistenceService`, `SpyNetworkService`, `SpyPlayerMessenger`, …) live in `DivineAscension.Tests/Helpers/`. Prefer these to mocking the raw VS API — see `docs/topics/implementation/api-wrappers.md`.

## Tag matching (VS 1.22)

`Entity.HasTags(string...)` is deprecated. Resolve `TagSetFast` once via `ICoreAPI.EntityTagRegistry.CreateTagSet([...])` and match with `IsFullyContainedIn` (AND) or `Overlaps` (OR). Trackers store a lazy `TagSetFast?` field initialised on first use; static patch classes get an `Initialize(api)` called from the system initializer.

## Conventions

- C# 12, nullable + implicit usings on.
- `internal` is fine — `[assembly: InternalsVisibleTo("DivineAscension.Tests")]`.
- Inject clocks/random for determinism in tests; tests mirror folder structure under `DivineAscension.Tests/<Area>/`.
- Don't reuse a name across types/files unless it's a true rename — VS API identifiers (`itemSlot` vs `itemslot`) are case-sensitive in Harmony param matching.

## Troubleshooting

- **Missing VS DLLs** → check `VINTAGE_STORY`, restart shell so `dotnet` inherits it.
- **CI stub build failures** → regenerate `lib/ci-stubs/` after VS upgrades (`./tools/generate-stubs.sh`).
- **Harmony "Parameter X not found"** → VS renamed/reorderd that method's params; verify with `ilspycmd "$VINTAGE_STORY/VintagestoryAPI.dll" | grep -B1 "<MethodName>("`.
- **Time/random flakes** → inject clocks/RNG.
- **Coverage report doesn't open** → open `coverage-report/index.html` manually.
