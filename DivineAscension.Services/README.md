# DivineAscension.Services

This project contains core business logic extracted from the main mod for improved testability and performance
optimization.

## Target Frameworks

This project **multi-targets** both .NET 8 and .NET 9:

- **net8.0**: Used by the main mod (DivineAscension) and tests
- **net9.0**: Enables performance optimizations with `System.Threading.Lock`

## Architecture

### Current State (Phases 1-3 Complete)

The project infrastructure is set up with abstractions, enums, and models extracted:

- **Abstractions/**: ✅ Interfaces for VS API dependencies
  - `ILogger`: Logging abstraction
  - `IWorldPersistence`: Save game data storage abstraction
  - `IPlayerProvider`: Player information lookup abstraction

- **Models/Enum/**: ✅ All enum definitions
  - `DeityDomain`, `FavorRank`, `PrestigeRank`: Core progression enums
  - `BlessingCategory`, `BlessingKind`, `AbilityType`: Blessing system enums
  - `DiplomaticStatus`, `NotificationType`, `DeityAlignment`, `DevotionRank`

- **Models/**: ✅ Core business logic models
  - `Blessing`: Blessing definition with stats and effects
  - `PlayerFavorProgress`: Player progression data
  - `ReligionPrestigeProgress`: Religion progression data
  - `RolePermissions`: Permission constants and helpers

Corresponding adapters are implemented in the main mod (`DivineAscension/Adapters/`):

- `VintageStoryLogger`: Adapts VS logger to ILogger
- `VintageStoryPersistence`: Adapts VS save game API to IWorldPersistence
- `VintageStoryPlayerProvider`: Adapts VS player API to IPlayerProvider

Remaining in main mod (`DivineAscension/Models/`):

- `BlessingTooltipData`: GUI tooltip formatting
- `BlessingNodeState`: UI state for blessing tree nodes
- `RoleDefaults`: Default role creation (moves with RoleData in Phase 4)

### Future State (Phases 4-7)

Will also contain:

- **Data/**: Data classes (ReligionData, CivilizationData, RoleData, etc.) (Phase 4)
- **Managers/**: Core business logic managers with optimized locking (Phase 5)

## Performance Optimizations

In .NET 9 builds, managers will use `System.Threading.Lock` instead of traditional `lock(object)`:

```csharp
#if NET9_0_OR_GREATER
    private readonly Lock _lock = new();

    public void DoSomething()
    {
        using (_lock.EnterScope())
        {
            // business logic
        }
    }
#else
    private object? _lock;
    private object Lock => _lock ??= new object();

    public void DoSomething()
    {
        lock (Lock)
        {
            // business logic
        }
    }
#endif
```

**Expected benefits:**

- ~20-30% faster lock acquisition in low-contention scenarios
- ~10-15% faster in high-contention scenarios
- Reduced GC pressure

## Dependencies

- **protobuf-net**: For data serialization/deserialization

## Testing

This project enables pure C# unit tests without VS API mocking complexity. Tests can target .NET 9 to validate lock
optimizations.

## References

- Full plan: `docs/topics/planning/features/domain-extraction/domain-project-plan.md`
- Tracking issue: #160
- .NET 9 Lock API: https://learn.microsoft.com/en-us/dotnet/api/system.threading.lock
