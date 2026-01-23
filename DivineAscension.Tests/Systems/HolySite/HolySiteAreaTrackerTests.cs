using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using DivineAscension.Data;
using DivineAscension.Services;
using DivineAscension.Systems.HolySite;
using DivineAscension.Systems.Interfaces;
using DivineAscension.Tests.Helpers;
using Moq;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
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

    #region CheckPlayerPosition Tests

    [Fact]
    public void CheckPlayerPosition_NullEntity_DoesNotThrow()
    {
        // Arrange - Entity.Pos is not virtual so we can only test null entity case
        var mockPlayer = new Mock<IServerPlayer>();
        mockPlayer.Setup(p => p.Entity).Returns((EntityPlayer?)null);
        mockPlayer.Setup(p => p.PlayerUID).Returns("player1");

        _tracker.Initialize();

        // Act & Assert - should not throw and should return early
        _tracker.CheckPlayerPosition(mockPlayer.Object);
    }

    #endregion

    #region CheckPlayerPositionAt Tests

    [Fact]
    public void CheckPlayerPositionAt_EnteringHolySite_EmitsEnteredEvent()
    {
        // Arrange
        var mockPlayer = new Mock<IServerPlayer>();
        mockPlayer.Setup(p => p.PlayerUID).Returns("player1");
        mockPlayer.Setup(p => p.PlayerName).Returns("TestPlayer");

        var site = CreateTestSite("site1", "Test Temple");
        var position = new BlockPos(10, 64, 10);

        _mockHolySiteManager.Setup(m => m.GetHolySiteAtPosition(position)).Returns(site);

        HolySiteData? enteredSite = null;
        _tracker.OnPlayerEnteredHolySite += (_, s) => enteredSite = s;
        _tracker.Initialize();

        // Act
        _tracker.CheckPlayerPositionAt(mockPlayer.Object, position);

        // Assert
        Assert.NotNull(enteredSite);
        Assert.Equal("site1", enteredSite.SiteUID);
    }

    [Fact]
    public void CheckPlayerPositionAt_ExitingHolySite_EmitsExitedEvent()
    {
        // Arrange
        var mockPlayer = new Mock<IServerPlayer>();
        mockPlayer.Setup(p => p.PlayerUID).Returns("player1");
        mockPlayer.Setup(p => p.PlayerName).Returns("TestPlayer");

        var site = CreateTestSite("site1", "Test Temple");
        var insidePosition = new BlockPos(10, 64, 10);
        var outsidePosition = new BlockPos(500, 64, 500);

        _mockHolySiteManager.Setup(m => m.GetHolySiteAtPosition(insidePosition)).Returns(site);
        _mockHolySiteManager.Setup(m => m.GetHolySiteAtPosition(outsidePosition)).Returns((HolySiteData?)null);
        _mockHolySiteManager.Setup(m => m.GetHolySite("site1")).Returns(site);

        HolySiteData? exitedSite = null;
        _tracker.OnPlayerExitedHolySite += (_, s) => exitedSite = s;
        _tracker.Initialize();

        // First enter the site
        _tracker.CheckPlayerPositionAt(mockPlayer.Object, insidePosition);

        // Act - now exit the site
        _tracker.CheckPlayerPositionAt(mockPlayer.Object, outsidePosition);

        // Assert
        Assert.NotNull(exitedSite);
        Assert.Equal("site1", exitedSite.SiteUID);
    }

    [Fact]
    public void CheckPlayerPositionAt_MovingBetweenSites_EmitsBothEvents()
    {
        // Arrange
        var mockPlayer = new Mock<IServerPlayer>();
        mockPlayer.Setup(p => p.PlayerUID).Returns("player1");
        mockPlayer.Setup(p => p.PlayerName).Returns("TestPlayer");

        var site1 = CreateTestSite("site1", "Temple One");
        var site2 = CreateTestSite("site2", "Temple Two");
        var position1 = new BlockPos(10, 64, 10);
        var position2 = new BlockPos(200, 64, 200);

        _mockHolySiteManager.Setup(m => m.GetHolySiteAtPosition(position1)).Returns(site1);
        _mockHolySiteManager.Setup(m => m.GetHolySiteAtPosition(position2)).Returns(site2);
        _mockHolySiteManager.Setup(m => m.GetHolySite("site1")).Returns(site1);
        _mockHolySiteManager.Setup(m => m.GetHolySite("site2")).Returns(site2);

        var enteredSites = new List<string>();
        var exitedSites = new List<string>();
        _tracker.OnPlayerEnteredHolySite += (_, s) => enteredSites.Add(s.SiteUID);
        _tracker.OnPlayerExitedHolySite += (_, s) => exitedSites.Add(s.SiteUID);
        _tracker.Initialize();

        // Enter first site
        _tracker.CheckPlayerPositionAt(mockPlayer.Object, position1);

        // Act - move to second site
        _tracker.CheckPlayerPositionAt(mockPlayer.Object, position2);

        // Assert
        Assert.Equal(2, enteredSites.Count);
        Assert.Contains("site1", enteredSites);
        Assert.Contains("site2", enteredSites);
        Assert.Single(exitedSites);
        Assert.Equal("site1", exitedSites[0]);
    }

    [Fact]
    public void CheckPlayerPositionAt_StayingInSameSite_NoEvents()
    {
        // Arrange
        var mockPlayer = new Mock<IServerPlayer>();
        mockPlayer.Setup(p => p.PlayerUID).Returns("player1");
        mockPlayer.Setup(p => p.PlayerName).Returns("TestPlayer");

        var site = CreateTestSite("site1", "Test Temple");
        var position1 = new BlockPos(10, 64, 10);
        var position2 = new BlockPos(15, 64, 15); // Still inside same site

        _mockHolySiteManager.Setup(m => m.GetHolySiteAtPosition(It.IsAny<BlockPos>())).Returns(site);

        int enteredCount = 0;
        int exitedCount = 0;
        _tracker.OnPlayerEnteredHolySite += (_, _) => enteredCount++;
        _tracker.OnPlayerExitedHolySite += (_, _) => exitedCount++;
        _tracker.Initialize();

        // Enter site
        _tracker.CheckPlayerPositionAt(mockPlayer.Object, position1);

        // Act - move within same site
        _tracker.CheckPlayerPositionAt(mockPlayer.Object, position2);

        // Assert - only one enter event, no exit
        Assert.Equal(1, enteredCount);
        Assert.Equal(0, exitedCount);
    }

    [Fact]
    public void CheckPlayerPositionAt_StayingOutsideAllSites_NoEvents()
    {
        // Arrange
        var mockPlayer = new Mock<IServerPlayer>();
        mockPlayer.Setup(p => p.PlayerUID).Returns("player1");
        mockPlayer.Setup(p => p.PlayerName).Returns("TestPlayer");

        var position1 = new BlockPos(500, 64, 500);
        var position2 = new BlockPos(600, 64, 600);

        _mockHolySiteManager.Setup(m => m.GetHolySiteAtPosition(It.IsAny<BlockPos>()))
            .Returns((HolySiteData?)null);

        int enteredCount = 0;
        int exitedCount = 0;
        _tracker.OnPlayerEnteredHolySite += (_, _) => enteredCount++;
        _tracker.OnPlayerExitedHolySite += (_, _) => exitedCount++;
        _tracker.Initialize();

        // Act
        _tracker.CheckPlayerPositionAt(mockPlayer.Object, position1);
        _tracker.CheckPlayerPositionAt(mockPlayer.Object, position2);

        // Assert
        Assert.Equal(0, enteredCount);
        Assert.Equal(0, exitedCount);
    }

    [Fact]
    public void CheckPlayerPositionAt_AfterEntering_GetPlayerCurrentSiteReturnsCorrectSite()
    {
        // Arrange
        var mockPlayer = new Mock<IServerPlayer>();
        mockPlayer.Setup(p => p.PlayerUID).Returns("player1");
        mockPlayer.Setup(p => p.PlayerName).Returns("TestPlayer");

        var site = CreateTestSite("site1", "Test Temple");
        var position = new BlockPos(10, 64, 10);

        _mockHolySiteManager.Setup(m => m.GetHolySiteAtPosition(position)).Returns(site);
        _mockHolySiteManager.Setup(m => m.GetHolySite("site1")).Returns(site);

        _tracker.Initialize();
        _tracker.CheckPlayerPositionAt(mockPlayer.Object, position);

        // Act
        var currentSite = _tracker.GetPlayerCurrentSite("player1");

        // Assert
        Assert.NotNull(currentSite);
        Assert.Equal("site1", currentSite.SiteUID);
    }

    [Fact]
    public void CheckPlayerPositionAt_AfterExiting_GetPlayerCurrentSiteReturnsNull()
    {
        // Arrange
        var mockPlayer = new Mock<IServerPlayer>();
        mockPlayer.Setup(p => p.PlayerUID).Returns("player1");
        mockPlayer.Setup(p => p.PlayerName).Returns("TestPlayer");

        var site = CreateTestSite("site1", "Test Temple");
        var insidePosition = new BlockPos(10, 64, 10);
        var outsidePosition = new BlockPos(500, 64, 500);

        _mockHolySiteManager.Setup(m => m.GetHolySiteAtPosition(insidePosition)).Returns(site);
        _mockHolySiteManager.Setup(m => m.GetHolySiteAtPosition(outsidePosition)).Returns((HolySiteData?)null);
        _mockHolySiteManager.Setup(m => m.GetHolySite("site1")).Returns(site);

        _tracker.Initialize();
        _tracker.CheckPlayerPositionAt(mockPlayer.Object, insidePosition);
        _tracker.CheckPlayerPositionAt(mockPlayer.Object, outsidePosition);

        // Act
        var currentSite = _tracker.GetPlayerCurrentSite("player1");

        // Assert
        Assert.Null(currentSite);
    }

    #endregion

    #region HandlePlayerExitFromHolySite Tests

    [Fact]
    public void HandlePlayerExitFromHolySite_PlayerNotTracked_NoEventEmitted()
    {
        // Arrange
        var mockPlayer = new Mock<IServerPlayer>();
        mockPlayer.Setup(p => p.PlayerUID).Returns("player1");

        int exitCount = 0;
        _tracker.OnPlayerExitedHolySite += (_, _) => exitCount++;
        _tracker.Initialize();

        // Act - player was never tracked
        _tracker.HandlePlayerExitFromHolySite(mockPlayer.Object);

        // Assert
        Assert.Equal(0, exitCount);
    }

    [Fact]
    public void HandlePlayerExitFromHolySite_PlayerTrackedInSite_EmitsExitEvent()
    {
        // Arrange
        var mockPlayer = new Mock<IServerPlayer>();
        mockPlayer.Setup(p => p.PlayerUID).Returns("player1");
        mockPlayer.Setup(p => p.PlayerName).Returns("TestPlayer");

        var site = CreateTestSite("site1", "Test Temple");
        var position = new BlockPos(10, 64, 10);

        _mockHolySiteManager.Setup(m => m.GetHolySiteAtPosition(position)).Returns(site);
        _mockHolySiteManager.Setup(m => m.GetHolySite("site1")).Returns(site);

        HolySiteData? exitedSite = null;
        _tracker.OnPlayerExitedHolySite += (_, s) => exitedSite = s;
        _tracker.Initialize();

        // First enter the site
        _tracker.CheckPlayerPositionAt(mockPlayer.Object, position);

        // Act - simulate disconnect while in site
        _tracker.HandlePlayerExitFromHolySite(mockPlayer.Object);

        // Assert
        Assert.NotNull(exitedSite);
        Assert.Equal("site1", exitedSite.SiteUID);
    }

    [Fact]
    public void HandlePlayerExitFromHolySite_RemovesPlayerFromTracking()
    {
        // Arrange
        var mockPlayer = new Mock<IServerPlayer>();
        mockPlayer.Setup(p => p.PlayerUID).Returns("player1");

        _tracker.Initialize();

        // Act
        _tracker.HandlePlayerExitFromHolySite(mockPlayer.Object);

        // Assert - GetPlayerCurrentSite should return null (player not tracked)
        Assert.Null(_tracker.GetPlayerCurrentSite("player1"));
    }

    [Fact]
    public void HandlePlayerExitFromHolySite_AfterTracked_ClearsFromTracking()
    {
        // Arrange
        var mockPlayer = new Mock<IServerPlayer>();
        mockPlayer.Setup(p => p.PlayerUID).Returns("player1");
        mockPlayer.Setup(p => p.PlayerName).Returns("TestPlayer");

        var site = CreateTestSite("site1", "Test Temple");
        var position = new BlockPos(10, 64, 10);

        _mockHolySiteManager.Setup(m => m.GetHolySiteAtPosition(position)).Returns(site);
        _mockHolySiteManager.Setup(m => m.GetHolySite("site1")).Returns(site);

        _tracker.Initialize();
        _tracker.CheckPlayerPositionAt(mockPlayer.Object, position);

        // Verify player is tracked
        Assert.NotNull(_tracker.GetPlayerCurrentSite("player1"));

        // Act
        _tracker.HandlePlayerExitFromHolySite(mockPlayer.Object);

        // Assert - player should no longer be tracked
        Assert.Null(_tracker.GetPlayerCurrentSite("player1"));
    }

    #endregion
}
