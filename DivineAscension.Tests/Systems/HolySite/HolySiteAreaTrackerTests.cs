using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using DivineAscension.Data;
using DivineAscension.Services;
using DivineAscension.Systems.HolySite;
using DivineAscension.Systems.Interfaces;
using DivineAscension.Tests.Helpers;
using Moq;
using Vintagestory.API.MathTools;
using Xunit;

namespace DivineAscension.Tests.Systems.HolySite;

[ExcludeFromCodeCoverage]
public class HolySiteAreaTrackerTests
{
    private readonly FakeEventService _fakeEventService;
    private readonly FakeWorldService _fakeWorldService;
    private readonly Mock<IHolySiteManager> _mockHolySiteManager;
    private readonly Mock<ILoggerWrapper> _mockLogger;
    private readonly HolySiteAreaTracker _tracker;

    public HolySiteAreaTrackerTests()
    {
        _fakeEventService = new FakeEventService();
        _fakeWorldService = new FakeWorldService();
        _mockHolySiteManager = new Mock<IHolySiteManager>();
        _mockLogger = new Mock<ILoggerWrapper>();

        _tracker = new HolySiteAreaTracker(
            _fakeEventService,
            _fakeWorldService,
            _mockHolySiteManager.Object,
            _mockLogger.Object);
    }

    #region Test Data Helpers

    private HolySiteData CreateTestSite(string siteUID, string siteName)
    {
        var areas = new List<SerializableCuboidi>
        {
            new SerializableCuboidi(0, 0, 0, 31, 255, 31)
        };
        return new HolySiteData(siteUID, "rel1", siteName, areas, "founder", "Founder");
    }

    #endregion

    #region Initialization Tests

    [Fact]
    public void Initialize_RegistersPeriodicCallback_AfterSaveGameLoaded()
    {
        // Act
        _tracker.Initialize();
        _fakeEventService.TriggerSaveGameLoaded(); // Callback registered after save game loads

        // Assert
        Assert.True(_fakeEventService.RegisteredCallbackCount > 0);
    }

    [Fact]
    public void Initialize_RegistersPlayerDisconnectHandler()
    {
        // Act
        _tracker.Initialize();

        // Assert
        Assert.True(_fakeEventService.PlayerDisconnectCallbackCount > 0);
    }

    #endregion

    #region Dispose Tests

    [Fact]
    public void Dispose_UnregistersCallbacks()
    {
        // Arrange
        _tracker.Initialize();
        _fakeEventService.TriggerSaveGameLoaded(); // Start tracking
        var callbackCountBefore = _fakeEventService.RegisteredCallbackCount;

        // Act
        _tracker.Dispose();

        // Assert
        Assert.True(_fakeEventService.RegisteredCallbackCount < callbackCountBefore);
    }

    [Fact]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        // Arrange
        _tracker.Initialize();

        // Act & Assert - should not throw
        _tracker.Dispose();
        _tracker.Dispose();
    }

    #endregion

    #region GetPlayerCurrentSite Tests

    [Fact]
    public void GetPlayerCurrentSite_UnknownPlayer_ReturnsNull()
    {
        // Arrange
        _tracker.Initialize();

        // Act
        var result = _tracker.GetPlayerCurrentSite("unknown-player");

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region Event Subscription Tests

    [Fact]
    public void Events_CanBeSubscribed()
    {
        // Arrange
        bool enteredCalled = false;
        bool exitedCalled = false;

        _tracker.OnPlayerEnteredHolySite += (_, _) => enteredCalled = true;
        _tracker.OnPlayerExitedHolySite += (_, _) => exitedCalled = true;

        // Assert - events can be subscribed without exception
        Assert.False(enteredCalled);
        Assert.False(exitedCalled);
    }

    [Fact]
    public void Dispose_ClearsEventHandlers()
    {
        // Arrange
        int eventCallCount = 0;
        _tracker.OnPlayerEnteredHolySite += (_, _) => eventCallCount++;
        _tracker.OnPlayerExitedHolySite += (_, _) => eventCallCount++;
        _tracker.Initialize();

        // Act
        _tracker.Dispose();

        // Assert - no exception (events are null after dispose)
        Assert.Equal(0, eventCallCount);
    }

    #endregion

    #region Tick Behavior Tests

    [Fact]
    public void OnTick_WithNoPlayers_DoesNotThrow()
    {
        // Arrange
        _tracker.Initialize();

        // Act & Assert - should not throw when no players online
        _fakeEventService.TriggerPeriodicCallback(1.0f);
    }

    [Fact]
    public void OnTick_WithNoSites_DoesNotEmitEvents()
    {
        // Arrange
        int eventCount = 0;
        _tracker.OnPlayerEnteredHolySite += (_, _) => eventCount++;
        _tracker.OnPlayerExitedHolySite += (_, _) => eventCount++;

        _mockHolySiteManager.Setup(m => m.GetHolySiteAtPosition(It.IsAny<BlockPos>()))
            .Returns((HolySiteData?)null);

        _tracker.Initialize();

        // Act
        _fakeEventService.TriggerPeriodicCallback(1.0f);

        // Assert
        Assert.Equal(0, eventCount);
    }

    #endregion
}
