# PantheonWars Test Suite

This directory contains comprehensive unit and integration tests for the PantheonWars mod, following industry best practices with proper mocking and dependency injection.

## Quick Links

- **📊 [Code Coverage Guide](../docs/testing/CODE_COVERAGE.md)** - Generate and view coverage reports
- **📋 [Test Plan](../docs/testing/TEST_PLAN.md)** - Complete testing strategy and roadmap

## Test Infrastructure

### TestFixtures (`Helpers/TestFixtures.cs`)

Provides reusable mock objects and test data for all test classes.

**Usage Example:**
```csharp
public class MySystemTests
{
    private readonly Mock<ICoreServerAPI> _mockAPI;
    private readonly Mock<IDeityRegistry> _mockDeityRegistry;

    public MySystemTests()
    {
        _mockAPI = TestFixtures.CreateMockServerAPI();
        _mockDeityRegistry = TestFixtures.CreateMockDeityRegistry();
    }
}
```

### Available Mock Factories

#### API Mocks
- `CreateMockCoreAPI()` - Basic ICoreAPI with logger
- `CreateMockServerAPI()` - ICoreServerAPI with logger, world, and events
- `CreateMockClientAPI()` - ICoreClientAPI with logger

#### Player Mocks
- `CreateMockServerPlayer(uid, name)` - IServerPlayer with UID and name

#### System Interface Mocks
- `CreateMockDeityRegistry()` - Pre-configured with Khoras and Lysa
- `CreateMockPlayerDataManager()`
- `CreateMockPlayerReligionDataManager()`
- `CreateMockReligionManager()`
- `CreateMockReligionPrestigeManager()`
- `CreateMockBlessingRegistry()`
- `CreateMockBlessingEffectSystem()`
- `CreateMockBuffManager()`
- `CreateMockFavorSystem()`

#### Test Data Objects
- `CreateTestDeity(type, name, domain)` - Creates a Deity with default values
- `CreateTestPlayerReligionData(...)` - Creates PlayerReligionData
- `CreateTestReligion(...)` - Creates ReligionData
- `CreateTestBlessing(...)` - Creates a Blessing
- `CreateMockEntity()` - Creates mock EntityAgent for buff testing

#### Assertion Helpers
- `VerifyLoggerNotification(mockLogger, expectedSubstring)` - Verify log messages
- `VerifyLoggerDebug(mockLogger, expectedSubstring)` - Verify debug logs

## Test Organization

```
PantheonWars.Tests/
├── Helpers/
│   └── TestFixtures.cs           # Reusable mock factories and test data
├── Systems/
│   ├── DeityRegistryTests.cs     # Example: Testing with real implementations
│   ├── FavorSystemIntegrationTests.cs  # Example: Testing with mocked dependencies
│   ├── BuffSystem/
│   │   └── BuffManagerTests.cs   # Example: Testing with entity mocks
│   └── ...
├── Models/                        # Model/domain tests
├── Data/                          # Data persistence tests
├── Network/                       # Network packet tests
├── Commands/                      # Command handler tests
└── README.md                      # This file
```

## Test Examples

### Example 1: Testing with Initialization

```csharp
[Fact]
public void Initialize_RegistersKhorasAndLysa_Successfully()
{
    // Arrange
    var mockApi = TestFixtures.CreateMockCoreAPI();
    var registry = new DeityRegistry(mockApi.Object);

    // Act
    registry.Initialize();

    // Assert
    Assert.NotNull(registry.GetDeity(DeityType.Khoras));
    Assert.NotNull(registry.GetDeity(DeityType.Lysa));
    Assert.Equal(2, registry.GetAllDeities().Count());
}
```

### Example 2: Testing with Mocked Dependencies

```csharp
[Fact]
public void ProcessPvPKill_BetweenRivalDeities_Awards2xFavor()
{
    // Arrange
    var mockAPI = TestFixtures.CreateMockServerAPI();
    var mockDeityRegistry = TestFixtures.CreateMockDeityRegistry();
    var mockPlayerReligionDataManager = TestFixtures.CreateMockPlayerReligionDataManager();

    var attackerData = TestFixtures.CreateTestPlayerReligionData("attacker-uid", DeityType.Khoras);
    var victimData = TestFixtures.CreateTestPlayerReligionData("victim-uid", DeityType.Morthen);

    mockPlayerReligionDataManager
        .Setup(m => m.GetOrCreatePlayerData("attacker-uid"))
        .Returns(attackerData);

    mockDeityRegistry
        .Setup(r => r.GetFavorMultiplier(DeityType.Khoras, DeityType.Morthen))
        .Returns(2.0f);

    var favorSystem = new FavorSystem(
        mockAPI.Object,
        mockPlayerDataManager.Object,
        mockPlayerReligionDataManager.Object,
        mockDeityRegistry.Object,
        mockReligionManager.Object
    );

    var mockAttacker = TestFixtures.CreateMockServerPlayer("attacker-uid", "Attacker");
    var mockVictim = TestFixtures.CreateMockServerPlayer("victim-uid", "Victim");

    // Act
    favorSystem.ProcessPvPKill(mockAttacker.Object, mockVictim.Object);

    // Assert - Should award 20 favor (BASE * 2.0)
    mockPlayerReligionDataManager.Verify(
        m => m.AddFavor("attacker-uid", 20, It.IsAny<string>()),
        Times.Once()
    );
}
```

### Example 3: Testing with Entity Mocks

```csharp
[Fact]
public void ApplyEffect_WithValidParameters_ReturnsTrue()
{
    // Arrange
    var mockAPI = TestFixtures.CreateMockServerAPI();
    var buffManager = new BuffManager(mockAPI.Object);

    var mockEntity = TestFixtures.CreateMockEntity();
    var mockBuffTracker = new Mock<EntityBehaviorBuffTracker>(mockEntity.Object);

    mockEntity
        .Setup(e => e.GetBehavior<EntityBehaviorBuffTracker>())
        .Returns(mockBuffTracker.Object);

    var statModifiers = new Dictionary<string, float> { { "walkspeed", 0.2f } };

    // Act
    var result = buffManager.ApplyEffect(
        mockEntity.Object,
        "speed_buff",
        10.0f,
        "swift_feet",
        "player-uid",
        statModifiers,
        true
    );

    // Assert
    Assert.True(result);
    mockBuffTracker.Verify(
        bt => bt.AddEffect(It.IsAny<ActiveEffect>()),
        Times.Once()
    );
}
```

## Best Practices

### 1. AAA Pattern
Always follow **Arrange-Act-Assert**:
```csharp
[Fact]
public void MethodName_Scenario_ExpectedBehavior()
{
    // Arrange - Set up test data and mocks
    // Act - Execute the method under test
    // Assert - Verify the outcome
}
```

### 2. Test Naming Convention
```
MethodName_Scenario_ExpectedBehavior
```
Examples:
- `GetDeity_WithValidType_ReturnsDeity()`
- `ProcessPvPKill_BetweenAlliedDeities_AwardsReducedFavor()`
- `ApplyEffect_WithNullTarget_ReturnsFalse()`

### 3. Mock Verification
```csharp
// Verify a method was called
mockObject.Verify(
    m => m.MethodName(expectedParam),
    Times.Once()
);

// Verify with predicate
mockObject.Verify(
    m => m.MethodName(It.Is<string>(s => s.Contains("expected"))),
    Times.AtLeastOnce()
);
```

### 4. Test Isolation
- Each test should be independent
- Use constructor for common setup
- Don't rely on test execution order
- Clean up resources if needed

### 5. Use TestFixtures
Always use `TestFixtures` helper methods instead of creating mocks manually:

✅ **Good:**
```csharp
var mockAPI = TestFixtures.CreateMockServerAPI();
var mockRegistry = TestFixtures.CreateMockDeityRegistry();
var playerData = TestFixtures.CreateTestPlayerReligionData("player-uid");
```

❌ **Bad:**
```csharp
var mockAPI = new Mock<ICoreServerAPI>();
var mockLogger = new Mock<ILogger>();
mockAPI.Setup(a => a.Logger).Returns(mockLogger.Object);
// ... lots of setup code
```

## Running Tests

### Command Line
```bash
# Run all tests
dotnet test

# Run specific test class
dotnet test --filter "FullyQualifiedName~DeityRegistryTests"

# Run with code coverage
dotnet test --collect:"XPlat Code Coverage"

# Run with detailed output
dotnet test --verbosity detailed
```

### Visual Studio / Rider
- Use the Test Explorer window
- Right-click on test class/method to run
- Enable code coverage in settings

## Coverage Goals

| Component | Target Coverage |
|-----------|----------------|
| Core Systems | 85%+ |
| Models & Data | 90%+ |
| Network Packets | 95%+ |
| Commands | 75%+ |
| GUI State | 70%+ |

## Contributing

When adding new tests:

1. ✅ Use `TestFixtures` helper methods
2. ✅ Follow AAA pattern
3. ✅ Use descriptive test names
4. ✅ Add `[ExcludeFromCodeCoverage]` attribute to test classes
5. ✅ Group related tests in `#region` blocks
6. ✅ Mock external dependencies via interfaces
7. ✅ Verify mock interactions when testing behavior
8. ✅ Keep tests focused and atomic

## References

- **Test Plan:** `/docs/testing/TEST_PLAN.md` - Comprehensive testing strategy
- **Moq Documentation:** https://github.com/moq/moq4
- **xUnit Documentation:** https://xunit.net/

## Example Test Classes

See these files for comprehensive examples:
- `Systems/DeityRegistryTests.cs` - Testing with real implementations
- `Systems/FavorSystemIntegrationTests.cs` - Testing with full mocking
- `Systems/BuffSystem/BuffManagerTests.cs` - Testing with entity mocks
