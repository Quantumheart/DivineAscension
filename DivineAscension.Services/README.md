# DivineAscension.Services

This project contains core business logic extracted from the main mod for improved testability and performance
optimization.

## Target Frameworks

This project **multi-targets** both .NET 8 and .NET 9:

- **net8.0**: Used by the main mod (DivineAscension) and tests
- **net9.0**: Enables performance optimizations with `System.Threading.Lock`

## Architecture

### Current State (Phase 1)

The project is currently empty - this is the infrastructure setup phase.

### Future State (Phases 2-7)

Will contain:

- **Abstractions/**: Interfaces for VS API dependencies (ILogger, IWorldPersistence, IPlayerProvider)
- **Managers/**: Core business logic managers with optimized locking
- **Data/**: Data classes (ReligionData, CivilizationData, etc.)
- **Models/**: Domain models (Blessing, RolePermissions, etc.)
- **Enums/**: Enumerations (DeityDomain, FavorRank, etc.)

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
