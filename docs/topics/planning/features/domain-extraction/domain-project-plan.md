# .NET 9 Project Extraction Plan

## Overview

This document explores extracting code into separate .NET 9 projects to leverage `System.Threading.Lock` for improved thread-safety performance. Two approaches are analyzed:

1. **Service Project Approach** (Recommended) - Extract managers with VS API abstractions
2. **Domain-Only Approach** - Extract only data models and enums

## Background

### Current State

The thread-safety implementation (PR #157) uses traditional `lock(object)` patterns:

```csharp
// Current pattern in .NET 8
private object? _lock;
private object Lock => _lock ??= new object();

public ReligionData? CreateReligion(...)
{
    lock (Lock)
    {
        // business logic
        _sapi.Logger.Notification("Created religion");
    }
}
```

### .NET 9 System.Threading.Lock

.NET 9 introduces `System.Threading.Lock` ([documentation](https://learn.microsoft.com/en-us/dotnet/api/system.threading.lock)), which provides:

1. **Better Performance**: Optimized synchronization primitives with reduced overhead
2. **Type Safety**: Dedicated lock type prevents accidental misuse
3. **Scoped Locking**: `EnterScope()` returns a ref struct for deterministic cleanup
4. **Reduced Allocations**: More efficient than boxing `object` references

```csharp
// .NET 9 pattern
private readonly Lock _lock = new();

public ReligionData? CreateReligion(...)
{
    using (_lock.EnterScope())
    {
        // business logic
        _logger.Info("Created religion");
    }
}
```

### Constraint: Vintage Story API

**Critical**: The main mod project must remain on .NET 8 because Vintage Story's API targets .NET 8. Extracted projects would be dependencies referenced by the mod.

---

## Approach 1: Service Project (Recommended)

### Rationale

The managers are where heavy locking occurs - they coordinate multi-step operations and are the hot path for multiplayer concurrency. Extracting managers provides:

- **Maximum lock optimization** - Manager locks are the performance bottleneck
- **Better testability** - Pure C# managers with no VS API mocking required
- **Cleaner architecture** - Business logic fully isolated from game engine

### VS API Dependencies in Managers

| VS API Usage | Purpose | Abstraction |
|--------------|---------|-------------|
| `_sapi.Logger.*` | Logging | `ILogger` interface |
| `_sapi.WorldManager.SaveGame.GetData/StoreData` | Persistence | `IWorldPersistence` interface |
| `_sapi.Event.SaveGameLoaded/GameWorldSave` | Lifecycle events | Callback delegates |
| `_sapi.World.PlayerByUid()` | Player lookup | `IPlayerProvider` interface |

### Proposed Architecture

```
DivineAscension.sln
│
├── DivineAscension.Services/      # NEW - .NET 9
│   │
│   ├── Abstractions/
│   │   ├── ILogger.cs
│   │   ├── IWorldPersistence.cs
│   │   └── IPlayerProvider.cs
│   │
│   ├── Managers/                  # Extracted with System.Threading.Lock
│   │   ├── ReligionManager.cs
│   │   ├── CivilizationManager.cs
│   │   ├── DiplomacyManager.cs
│   │   ├── PlayerProgressionDataManager.cs
│   │   ├── ReligionPrestigeManager.cs
│   │   └── ActivityLogManager.cs
│   │
│   ├── Data/                      # Data classes also moved here
│   │   ├── ReligionData.cs
│   │   ├── CivilizationData.cs
│   │   ├── PlayerProgressionData.cs
│   │   └── ...
│   │
│   ├── Models/
│   │   ├── Blessing.cs
│   │   ├── RolePermissions.cs
│   │   └── ...
│   │
│   └── Enums/
│       ├── DeityDomain.cs
│       ├── FavorRank.cs
│       └── ...
│
├── DivineAscension/               # .NET 8 (VS integration layer)
│   │
│   ├── Adapters/                  # VS API implementations
│   │   ├── VintageStoryLogger.cs         : ILogger
│   │   ├── VintageStoryPersistence.cs    : IWorldPersistence
│   │   └── VintageStoryPlayerProvider.cs : IPlayerProvider
│   │
│   ├── Wiring/
│   │   └── ServiceConfiguration.cs  # Wires adapters to managers
│   │
│   ├── Commands/                  # Stay here (use VS command API)
│   ├── GUI/                       # Stay here (use ImGui + VS)
│   ├── Network/                   # Stay here (use VS networking)
│   └── Systems/
│       └── DivineAscensionModSystem.cs  # Entry point
│
└── DivineAscension.Tests/         # .NET 9
    ├── Services/                  # Test managers directly (no mocking!)
    └── Integration/               # VS integration tests
```

### Abstraction Interfaces

```csharp
// DivineAscension.Services/Abstractions/ILogger.cs
namespace DivineAscension.Services.Abstractions;

public interface ILogger
{
    void Debug(string message);
    void Info(string message);
    void Warning(string message);
    void Error(string message);
}

// DivineAscension.Services/Abstractions/IWorldPersistence.cs
namespace DivineAscension.Services.Abstractions;

public interface IWorldPersistence
{
    byte[]? GetData(string key);
    void StoreData(string key, byte[] data);
}

// DivineAscension.Services/Abstractions/IPlayerProvider.cs
namespace DivineAscension.Services.Abstractions;

public interface IPlayerProvider
{
    string? GetPlayerName(string playerUid);
    bool IsPlayerOnline(string playerUid);
}
```

### VS Adapter Implementations

```csharp
// DivineAscension/Adapters/VintageStoryLogger.cs
namespace DivineAscension.Adapters;

internal class VintageStoryLogger(ICoreAPI api) : ILogger
{
    public void Debug(string message) => api.Logger.Debug($"[DivineAscension] {message}");
    public void Info(string message) => api.Logger.Notification($"[DivineAscension] {message}");
    public void Warning(string message) => api.Logger.Warning($"[DivineAscension] {message}");
    public void Error(string message) => api.Logger.Error($"[DivineAscension] {message}");
}

// DivineAscension/Adapters/VintageStoryPersistence.cs
namespace DivineAscension.Adapters;

internal class VintageStoryPersistence(ICoreServerAPI sapi) : IWorldPersistence
{
    public byte[]? GetData(string key) => sapi.WorldManager.SaveGame.GetData(key);
    public void StoreData(string key, byte[] data) => sapi.WorldManager.SaveGame.StoreData(key, data);
}

// DivineAscension/Adapters/VintageStoryPlayerProvider.cs
namespace DivineAscension.Adapters;

internal class VintageStoryPlayerProvider(ICoreServerAPI sapi) : IPlayerProvider
{
    public string? GetPlayerName(string playerUid) =>
        (sapi.World.PlayerByUid(playerUid) as IServerPlayer)?.PlayerName;

    public bool IsPlayerOnline(string playerUid) =>
        sapi.World.PlayerByUid(playerUid)?.ConnectionState == EnumClientState.Playing;
}
```

### Transformed Manager Example

```csharp
// DivineAscension.Services/Managers/ReligionManager.cs
namespace DivineAscension.Services.Managers;

public class ReligionManager : IReligionManager
{
    private readonly Lock _lock = new();
    private readonly ILogger _logger;
    private readonly IWorldPersistence _persistence;
    private readonly ConcurrentDictionary<string, ReligionData> _religions = new();
    private readonly ConcurrentDictionary<string, string> _playerToReligionIndex = new();

    public ReligionManager(ILogger logger, IWorldPersistence persistence)
    {
        _logger = logger;
        _persistence = persistence;
    }

    public ReligionData? CreateReligion(string name, DeityDomain domain, string deityName,
        string founderUID, string founderName)
    {
        using (_lock.EnterScope())
        {
            // Validation
            if (_playerToReligionIndex.ContainsKey(founderUID))
            {
                _logger.Warning($"Player {founderUID} already in a religion");
                return null;
            }

            // Create religion
            var religionUID = Guid.NewGuid().ToString();
            var religion = new ReligionData(religionUID, name, domain, deityName, founderUID, founderName);

            _religions[religionUID] = religion;
            _playerToReligionIndex[founderUID] = religionUID;

            _logger.Info($"Created religion '{name}' with founder {founderName}");
            return religion;
        }
    }

    public void LoadFromPersistence()
    {
        var data = _persistence.GetData("divineascension:religions");
        if (data != null)
        {
            // Deserialize and populate
        }
    }

    public void SaveToPersistence()
    {
        using (_lock.EnterScope())
        {
            var data = SerializeReligions();
            _persistence.StoreData("divineascension:religions", data);
        }
    }
}
```

### Wiring in ModSystem

```csharp
// DivineAscension/Wiring/ServiceConfiguration.cs
namespace DivineAscension.Wiring;

internal static class ServiceConfiguration
{
    public static IReligionManager CreateReligionManager(ICoreServerAPI sapi)
    {
        var logger = new VintageStoryLogger(sapi);
        var persistence = new VintageStoryPersistence(sapi);
        return new ReligionManager(logger, persistence);
    }

    // Similar for other managers...
}

// DivineAscension/Systems/DivineAscensionModSystem.cs
public override void StartServerSide(ICoreServerAPI sapi)
{
    var religionManager = ServiceConfiguration.CreateReligionManager(sapi);

    // Wire up VS events
    sapi.Event.SaveGameLoaded += religionManager.LoadFromPersistence;
    sapi.Event.GameWorldSave += religionManager.SaveToPersistence;

    // Continue initialization...
}
```

### Benefits of Service Approach

| Benefit | Description |
|---------|-------------|
| **Lock optimization** | Manager locks are the hot path - optimizing them has maximum impact |
| **Pure unit tests** | Test managers with simple interface mocks, no VS API complexity |
| **Separation of concerns** | Business logic completely isolated from game engine |
| **Easier debugging** | Can step through manager code without VS runtime |
| **Future portability** | Core logic could theoretically work with other game engines |

### Migration Phases (Service Approach)

#### Phase 1: Create Services Project
1. Create `DivineAscension.Services` project targeting .NET 9
2. Add protobuf-net dependency
3. Add project reference from main mod
4. Configure build to copy DLL to mod output

#### Phase 2: Define Abstractions
1. Create `ILogger`, `IWorldPersistence`, `IPlayerProvider` interfaces
2. Create VS adapter implementations in main mod
3. Build and verify

#### Phase 3: Extract Enums and Models
1. Move `Models/Enum/*.cs` to services project
2. Move `Models/*.cs` (non-GUI) to services project
3. Keep same namespaces
4. Build and verify

#### Phase 4: Extract Data Classes
1. Move `Data/*.cs` to services project
2. Convert `object` locks to `System.Threading.Lock`
3. Build and verify

#### Phase 5: Extract Managers (One at a Time)
1. Start with `ReligionManager` - most critical
2. Refactor to use abstractions instead of `ICoreServerAPI`
3. Convert `object` locks to `System.Threading.Lock`
4. Wire up in `ServiceConfiguration`
5. Test thoroughly before proceeding
6. Repeat for remaining managers

#### Phase 6: Update Tests
1. Move manager tests to test services directly
2. Remove VS API mocking complexity
3. Add integration tests for VS adapter layer

#### Phase 7: Documentation
1. Update CLAUDE.md with new architecture
2. Document abstraction contracts
3. Update contribution guidelines

---

## Approach 2: Domain-Only (Alternative)

### Rationale

A simpler approach that only extracts data models and enums. Managers stay in the main project but data class locks are optimized.

### What Can Be Extracted

| Category | Files | Notes |
|----------|-------|-------|
| **Data/** | `ReligionData`, `CivilizationData`, `PlayerProgressionData`, `ActivityLogEntry`, `BanEntry`, `MemberEntry`, `RoleData`, `DiplomaticProposal`, `DiplomaticRelationship`, `*WorldData` | No VS dependencies |
| **Models/** | `Blessing`, `RolePermissions`, `RoleDefaults`, `*Progress` | No VS dependencies |
| **Enums/** | All enums | Pure C# |
| **Utilities/** | `ThreadSafetyUtils` | Needs logger abstraction |

### Proposed Architecture (Domain-Only)

```
DivineAscension.sln
├── DivineAscension.Domain/        # NEW - .NET 9
│   ├── Data/
│   ├── Models/
│   ├── Enums/
│   └── Threading/
│
├── DivineAscension/               # .NET 8
│   ├── Systems/                   # Managers stay here (object locks)
│   ├── Commands/
│   ├── GUI/
│   └── ...
│
└── DivineAscension.Tests/
```

### Limitations

- Manager locks remain as `object` locks (.NET 8)
- Less performance benefit since manager locks are the hot path
- Still need VS API mocking for manager tests

---

## Comparison

| Aspect | Service Project | Domain-Only |
|--------|-----------------|-------------|
| **Lock optimization scope** | Managers + Data | Data only |
| **Performance impact** | High (hot path) | Moderate |
| **Testability improvement** | Significant | Minimal |
| **Migration complexity** | Higher | Lower |
| **Abstraction overhead** | 3 interfaces | 1 interface |
| **Code changes** | ~2000 lines | ~500 lines |

## Performance Expectations

Based on .NET 9 benchmarks, `System.Threading.Lock` shows:
- **~20-30% faster** lock acquisition in low-contention scenarios
- **~10-15% faster** in high-contention scenarios
- **Reduced GC pressure** from avoiding object allocations

**Service approach**: Optimizes manager locks (the bottleneck) → significant multiplayer improvement
**Domain-only approach**: Optimizes data locks (less critical) → modest improvement

## CI/CD Impact

### Current Setup

- **build.yml**: Uses `dotnet-version: '8.0.x'`
- **release.yml**: Uses `dotnet-version: '8.0.x'`
- **Stub assemblies**: `lib/ci-stubs/` contains VS API stubs for CI builds

### Required Changes

#### 1. Add .NET 9 SDK to Workflows

```yaml
# .github/workflows/build.yml (and release.yml)
- name: Setup .NET
  uses: actions/setup-dotnet@v4
  with:
    dotnet-version: |
      8.0.x
      9.0.x
```

#### 2. Verify Services DLL Packaging

The Services project output must be copied to `bin/Release/Mods/mod/` alongside the main DLL. This should happen automatically via `<ProjectReference>`, but verify during Phase 1.

If needed, add to main project's `.csproj`:

```xml
<Target Name="CopyServicesDll" AfterTargets="Build">
  <Copy SourceFiles="$(OutputPath)DivineAscension.Services.dll"
        DestinationFolder="$(OutputPath)Mods/mod/" />
</Target>
```

#### 3. No Stub Changes Required

The Services project won't need VS API stubs since it only depends on abstractions. This is a **benefit** - the Services project can build and test without any VS dependencies.

### Impact Summary

| Aspect | Impact | Effort |
|--------|--------|--------|
| SDK version | Add `9.0.x` to setup-dotnet | 2 lines per workflow |
| Build command | No change (`dotnet build` handles multi-target) | None |
| Test command | No change | None |
| Package step | Verify DLL included (likely automatic) | Verify only |
| Stub assemblies | No change needed for Services project | None |
| Cache key | Update to include new `.csproj` (automatic via glob) | None |

### Updated Workflow Example

```yaml
# .github/workflows/build.yml
name: Build and Test

on:
  push:
    branches: [main, master, develop]
  pull_request:
    branches: [main, master]

jobs:
  build:
    runs-on: ubuntu-latest

    steps:
      - name: Checkout repository
        uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: |
            8.0.x
            9.0.x

      - name: Cache NuGet packages
        uses: actions/cache@v4
        with:
          path: ~/.nuget/packages
          key: ${{ runner.os }}-nuget-${{ hashFiles('**/*.csproj') }}
          restore-keys: |
            ${{ runner.os }}-nuget-

      - name: Verify stub assemblies exist
        run: |
          # ... existing stub verification (unchanged)

      - name: Restore dependencies
        run: dotnet restore DivineAscension.sln

      - name: Build
        run: dotnet build DivineAscension.sln -c Release --no-restore

      - name: Test
        run: dotnet test DivineAscension.sln -c Release --no-build --verbosity normal
```

---

## Risks and Mitigations

| Risk | Impact | Mitigation |
|------|--------|------------|
| **Assembly loading issues** | Runtime failures | Test with VS mod loading; ensure compatible assembly versions |
| **ProtoBuf compatibility** | Save/load failures | Extensive testing with existing save files |
| **Build complexity** | Developer friction | Clear documentation; CI/CD validation |
| **Abstraction leakage** | Tight coupling returns | Code review; interface segregation |
| **.NET 9 availability** | Blocks implementation | Wait for .NET 9 GA (Nov 2024) |
| **CI/CD SDK mismatch** | Build failures | Add .NET 9 SDK to workflows before merging Services project |

## Decision Points

Before proceeding, confirm:

1. **Approach**: Service project (recommended) or domain-only?
2. **Timing**: Wait for .NET 9 GA or use previews?
3. **Migration strategy**: All at once or incremental?
4. **Namespace strategy**: Keep original or use new namespaces?

## Recommendation

**Proceed with Service Project approach** because:

1. Manager locks are the performance bottleneck - optimizing them has maximum impact
2. Dramatically improves testability (pure C# tests, no VS mocking)
3. Creates cleaner architecture with proper separation of concerns
4. One-time migration effort pays dividends in maintainability

**Suggested timeline**:
1. Wait for .NET 9 GA (November 2024)
2. Start with abstractions + one manager (ReligionManager)
3. Validate approach with VS mod loading
4. Incrementally migrate remaining managers
5. Update documentation and tests

## Appendix: Project File Configuration

```xml
<!-- DivineAscension.Services/DivineAscension.Services.csproj -->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <LangVersion>13</LangVersion>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="protobuf-net" Version="3.2.30" />
  </ItemGroup>
</Project>
```

```xml
<!-- DivineAscension/DivineAscension.csproj (add reference) -->
<ItemGroup>
  <ProjectReference Include="..\DivineAscension.Services\DivineAscension.Services.csproj" />
</ItemGroup>
```