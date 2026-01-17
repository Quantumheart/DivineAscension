# Domain Project Extraction Plan

## Overview

This document explores extracting core data models and thread-safety infrastructure into a separate `DivineAscension.Domain` project targeting .NET 9 to leverage the new `System.Threading.Lock` type for improved performance.

## Background

### Current State

The thread-safety implementation (merged in PR #157) uses traditional `lock(object)` patterns:

```csharp
// Current pattern in .NET 8
[ProtoIgnore] private object? _lock;
[ProtoIgnore] private object Lock => _lock ??= new object();

public void AddMember(string uid, string name)
{
    lock (Lock)
    {
        _members[uid] = new MemberEntry(uid, name);
        if (!_memberUIDs.Contains(uid))
            _memberUIDs.Add(uid);
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

public void AddMember(string uid, string name)
{
    using (_lock.EnterScope())
    {
        _members[uid] = new MemberEntry(uid, name);
        if (!_memberUIDs.Contains(uid))
            _memberUIDs.Add(uid);
    }
}
```

## Constraint: Vintage Story API

**Critical**: The main mod project must remain on .NET 8 because Vintage Story's API targets .NET 8. The domain project would be a dependency referenced by the mod.

## Analysis: What Can Be Extracted

### Dependency Audit

| File | VS API Dependency | Internal Dependencies | Extractable |
|------|-------------------|----------------------|-------------|
| **Data/** | | | |
| `ReligionData.cs` | ❌ None | `Models.Enum.DeityDomain`, `Models.RolePermissions` | ✅ Yes |
| `CivilizationData.cs` | ❌ None | None | ✅ Yes |
| `PlayerProgressionData.cs` | ❌ None | `Models.Enum.DeityDomain` | ✅ Yes |
| `ActivityLogEntry.cs` | ❌ None | None | ✅ Yes |
| `BanEntry.cs` | ❌ None | None | ✅ Yes |
| `MemberEntry.cs` | ❌ None | None | ✅ Yes |
| `RoleData.cs` | ❌ None | `Models.RolePermissions` | ✅ Yes |
| `DiplomaticProposal.cs` | ❌ None | `Models.Enum.DiplomaticStatus` | ✅ Yes |
| `DiplomaticRelationship.cs` | ❌ None | `Models.Enum.DiplomaticStatus` | ✅ Yes |
| `*WorldData.cs` | ❌ None | Data classes | ✅ Yes |
| **Models/** | | | |
| `Blessing.cs` | ❌ None | Enums | ✅ Yes |
| `RolePermissions.cs` | ❌ None | None | ✅ Yes |
| `RoleDefaults.cs` | ❌ None | `RolePermissions` | ✅ Yes |
| `PlayerFavorProgress.cs` | ❌ None | Enums | ✅ Yes |
| `ReligionPrestigeProgress.cs` | ❌ None | Enums | ✅ Yes |
| **Models/Enum/** | | | |
| All enums | ❌ None | None | ✅ Yes |
| **Utilities/** | | | |
| `ThreadSafetyUtils.cs` | ✅ `ICoreAPI` | None | ⚠️ Needs abstraction |

### Files NOT Extractable (Vintage Story Dependencies)

- All `/Systems/` managers (use `ICoreServerAPI`, world save, etc.)
- All `/Commands/` handlers
- All `/GUI/` components
- All `/Network/` packets (use Vintage Story networking)
- `/Services/` (some use VS APIs)

## Proposed Architecture

```
DivineAscension.sln
├── DivineAscension.Domain/        # NEW - .NET 9
│   ├── Data/
│   │   ├── ReligionData.cs
│   │   ├── CivilizationData.cs    (Civilization class)
│   │   ├── PlayerProgressionData.cs
│   │   ├── ActivityLogEntry.cs
│   │   ├── BanEntry.cs
│   │   ├── MemberEntry.cs
│   │   ├── RoleData.cs
│   │   ├── DiplomaticProposal.cs
│   │   ├── DiplomaticRelationship.cs
│   │   └── *WorldData.cs
│   ├── Models/
│   │   ├── Blessing.cs
│   │   ├── RolePermissions.cs
│   │   ├── RoleDefaults.cs
│   │   └── *Progress.cs
│   ├── Enums/
│   │   ├── DeityDomain.cs
│   │   ├── FavorRank.cs
│   │   ├── PrestigeRank.cs
│   │   └── ...
│   └── Threading/
│       └── LockTelemetry.cs       # Logger abstraction
│
├── DivineAscension/               # .NET 8 (unchanged target)
│   ├── References DivineAscension.Domain
│   ├── Systems/                   # Managers stay here
│   ├── Commands/
│   ├── GUI/
│   └── ...
│
└── DivineAscension.Tests/         # .NET 9 (can upgrade)
    └── References both projects
```

## Implementation Details

### 1. Logger Abstraction for ThreadSafetyUtils

Create an interface in the domain project:

```csharp
// DivineAscension.Domain/Threading/ILockTelemetryLogger.cs
namespace DivineAscension.Domain.Threading;

public interface ILockTelemetryLogger
{
    void LogContention(string operationName, long waitTimeMs);
    void LogLongHold(string operationName, long holdTimeMs);
}
```

Implement in the main mod:

```csharp
// DivineAscension/Utilities/VintageStoryLockLogger.cs
internal class VintageStoryLockLogger : ILockTelemetryLogger
{
    private readonly ICoreAPI _api;

    public VintageStoryLockLogger(ICoreAPI api) => _api = api;

    public void LogContention(string op, long ms) =>
        _api.Logger.Warning($"[DivineAscension] Lock contention: {op} waited {ms}ms");

    public void LogLongHold(string op, long ms) =>
        _api.Logger.Warning($"[DivineAscension] Long lock hold: {op} held {ms}ms");
}
```

### 2. Lock Migration Pattern

```csharp
// Before (.NET 8 object lock)
[ProtoIgnore] private object? _lock;
[ProtoIgnore] private object Lock => _lock ??= new object();

public IReadOnlyList<string> MemberUIDs
{
    get { lock (Lock) { return _memberUIDs.ToList(); } }
}

// After (.NET 9 System.Threading.Lock)
[ProtoIgnore] private readonly Lock _lock = new();

public IReadOnlyList<string> MemberUIDs
{
    get
    {
        using (_lock.EnterScope())
        {
            return _memberUIDs.ToList();
        }
    }
}
```

### 3. ProtoBuf Serialization Across Assemblies

ProtoBuf-net handles cross-assembly serialization well as long as:
- `[ProtoContract]` and `[ProtoMember]` attributes are preserved
- Field numbers remain stable
- Namespace changes are handled via `RuntimeTypeModel` if needed

```csharp
// If namespace changes, configure in mod initialization:
RuntimeTypeModel.Default.Add(typeof(ReligionData), true);
```

### 4. Project File Configuration

```xml
<!-- DivineAscension.Domain/DivineAscension.Domain.csproj -->
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
  <ProjectReference Include="..\DivineAscension.Domain\DivineAscension.Domain.csproj" />
</ItemGroup>
```

## Considerations

### Benefits

1. **Performance**: `System.Threading.Lock` is more efficient than `object` locks
2. **Type Safety**: Prevents accidental lock on wrong object
3. **Modern C#**: Access to C# 13 features in domain code
4. **Separation of Concerns**: Clean boundary between domain logic and VS integration
5. **Testability**: Domain project can be tested without VS API mocks
6. **Future-Proofing**: Easier to adapt if VS API upgrades to .NET 9+

### Risks and Mitigations

| Risk | Impact | Mitigation |
|------|--------|------------|
| **Assembly loading issues** | Runtime failures | Test with VS mod loading; ensure compatible assembly versions |
| **ProtoBuf compatibility** | Save/load failures | Extensive testing with existing save files; version migration code |
| **Build complexity** | Developer friction | Clear documentation; CI/CD validation |
| **Namespace changes** | Breaking changes | Keep namespaces identical or use type forwarding |
| **.NET 9 not yet released** | Blocks implementation | Wait for .NET 9 GA (Nov 2024) or use preview |

### Namespace Strategy Options

**Option A: Keep Original Namespaces**
```csharp
// DivineAscension.Domain/Data/ReligionData.cs
namespace DivineAscension.Data;  // Same as before
```
- ✅ No code changes in consumers
- ❌ Confusing that namespace doesn't match assembly

**Option B: New Namespaces with Type Forwarding**
```csharp
// DivineAscension.Domain/Data/ReligionData.cs
namespace DivineAscension.Domain.Data;

// DivineAscension/TypeForwards.cs
[assembly: TypeForwardedTo(typeof(DivineAscension.Domain.Data.ReligionData))]
```
- ✅ Clean namespace structure
- ✅ Backward compatible
- ❌ More complex setup

**Recommendation**: Option A for simplicity, as the domain project is an internal implementation detail.

## Migration Steps

### Phase 1: Project Setup
1. Create `DivineAscension.Domain` project targeting .NET 9
2. Add protobuf-net dependency
3. Add project reference from main mod
4. Configure build to copy domain DLL to mod output

### Phase 2: Extract Enums
1. Move all `Models/Enum/*.cs` to domain project
2. Keep same namespace
3. Build and verify

### Phase 3: Extract Models
1. Move `Models/*.cs` (non-GUI) to domain project
2. Update internal references
3. Build and verify

### Phase 4: Extract Data Classes
1. Move `Data/*.cs` to domain project
2. Convert `object` locks to `System.Threading.Lock`
3. Build and verify

### Phase 5: Extract Threading Utilities
1. Create `ILockTelemetryLogger` interface in domain
2. Move telemetry logic to domain
3. Implement VS logger adapter in main mod
4. Build and verify

### Phase 6: Testing
1. Run all existing unit tests
2. Run concurrency tests
3. Test with Vintage Story:
   - Load existing saves
   - Create new religions/civilizations
   - Verify multiplayer stability
4. Verify ProtoBuf serialization round-trips

### Phase 7: Documentation
1. Update CLAUDE.md with new architecture
2. Document build process
3. Update contribution guidelines

## Performance Expectations

Based on .NET 9 benchmarks, `System.Threading.Lock` shows:
- **~20-30% faster** lock acquisition in low-contention scenarios
- **~10-15% faster** in high-contention scenarios
- **Reduced GC pressure** from avoiding object allocations

For a game mod with periodic locking (not tight loops), expect modest but measurable improvements in multiplayer scenarios with many concurrent operations.

## Decision Points

Before proceeding, confirm:

1. **Timing**: Wait for .NET 9 GA or use previews?
2. **Namespace strategy**: Keep original or use new with forwarding?
3. **Scope**: Extract everything listed or start with subset?
4. **Testing**: What level of VS integration testing is acceptable?

## Conclusion

Extracting domain logic to a .NET 9 project is architecturally sound and provides tangible benefits. The main constraint (VS API on .NET 8) is easily handled via project references. The migration can be done incrementally with good test coverage.

**Recommended approach**: Wait for .NET 9 GA, then implement in phases starting with enums (lowest risk) and progressing to data classes with lock migration.
