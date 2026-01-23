using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using DivineAscension.API.Interfaces;
using DivineAscension.Data;
using DivineAscension.Models.Enum;
using DivineAscension.Services;
using DivineAscension.Systems;
using DivineAscension.Systems.Favor;
using DivineAscension.Systems.HolySite;
using DivineAscension.Systems.Interfaces;
using DivineAscension.Tests.Helpers;
using Moq;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Xunit;

namespace DivineAscension.Tests.Systems.Favor;

[ExcludeFromCodeCoverage]
public class PatrolFavorTrackerTests
{
    private readonly Mock<IPlayerProgressionDataManager> _mockPlayerProgressionDataManager;
    private readonly Mock<ILoggerWrapper> _mockLogger;
    private readonly FakeTimeService _fakeTimeService;
    private readonly Mock<IFavorSystem> _mockFavorSystem;
    private readonly Mock<IHolySiteAreaTracker> _mockAreaTracker;
    private readonly Mock<ICivilizationManager> _mockCivilizationManager;
    private readonly Mock<IHolySiteManager> _mockHolySiteManager;
    private readonly Mock<IReligionManager> _mockReligionManager;
    private readonly Mock<IPlayerMessengerService> _mockMessenger;
    private readonly PatrolFavorTracker _tracker;

    public PatrolFavorTrackerTests()
    {
        _mockPlayerProgressionDataManager = new Mock<IPlayerProgressionDataManager>();
        _mockLogger = new Mock<ILoggerWrapper>();
        _fakeTimeService = new FakeTimeService();
        _mockFavorSystem = new Mock<IFavorSystem>();
        _mockAreaTracker = new Mock<IHolySiteAreaTracker>();
        _mockCivilizationManager = new Mock<ICivilizationManager>();
        _mockHolySiteManager = new Mock<IHolySiteManager>();
        _mockReligionManager = new Mock<IReligionManager>();
        _mockMessenger = new Mock<IPlayerMessengerService>();

        _tracker = new PatrolFavorTracker(
            _mockPlayerProgressionDataManager.Object,
            _mockLogger.Object,
            _fakeTimeService,
            _mockFavorSystem.Object,
            _mockAreaTracker.Object,
            _mockCivilizationManager.Object,
            _mockHolySiteManager.Object,
            _mockReligionManager.Object,
            _mockMessenger.Object);
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

    private Mock<IServerPlayer> CreateMockPlayer(string playerUID, string playerName)
    {
        var mockPlayer = new Mock<IServerPlayer>();
        mockPlayer.Setup(p => p.PlayerUID).Returns(playerUID);
        mockPlayer.Setup(p => p.PlayerName).Returns(playerName);
        // Note: We can't mock Entity.Pos due to Moq limitations with non-virtual properties
        // Tests that need entity position will be integration tests
        return mockPlayer;
    }

    private void SetupConquestPlayer(string playerUID)
    {
        _mockPlayerProgressionDataManager.Setup(m => m.GetPlayerDeityType(playerUID))
            .Returns(DeityDomain.Conquest);
    }

    private void SetupNonConquestPlayer(string playerUID)
    {
        _mockPlayerProgressionDataManager.Setup(m => m.GetPlayerDeityType(playerUID))
            .Returns(DeityDomain.Craft);
    }

    #endregion

    #region Initialization Tests

    [Fact]
    public void Initialize_SubscribesToAreaTrackerEvents()
    {
        // Act
        _tracker.Initialize();

        // Assert - verify that event handlers were subscribed
        _mockAreaTracker.VerifyAdd(
            m => m.OnPlayerEnteredHolySite += It.IsAny<Action<IServerPlayer, HolySiteData>>(),
            Times.Once);
        _mockAreaTracker.VerifyAdd(
            m => m.OnPlayerExitedHolySite += It.IsAny<Action<IServerPlayer, HolySiteData>>(),
            Times.Once);
    }

    [Fact]
    public void Initialize_SubscribesToPlayerLeavesReligionEvent()
    {
        // Act
        _tracker.Initialize();

        // Assert
        _mockPlayerProgressionDataManager.VerifyAdd(
            m => m.OnPlayerLeavesReligion += It.IsAny<PlayerProgressionDataManager.PlayerReligionDataChangedDelegate>(),
            Times.Once);
    }

    #endregion

    #region Dispose Tests

    [Fact]
    public void Dispose_UnsubscribesFromEvents()
    {
        // Arrange
        _tracker.Initialize();

        // Act
        _tracker.Dispose();

        // Assert
        _mockAreaTracker.VerifyRemove(
            m => m.OnPlayerEnteredHolySite -= It.IsAny<Action<IServerPlayer, HolySiteData>>(),
            Times.Once);
        _mockAreaTracker.VerifyRemove(
            m => m.OnPlayerExitedHolySite -= It.IsAny<Action<IServerPlayer, HolySiteData>>(),
            Times.Once);
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

    #region DeityDomain Property Tests

    [Fact]
    public void DeityDomain_ReturnsConquest()
    {
        // Assert
        Assert.Equal(DeityDomain.Conquest, _tracker.DeityDomain);
    }

    #endregion

    #region Domain Filtering Tests

    [Fact]
    public void OnPlayerEnteredHolySite_NonConquestPlayer_DoesNotAwardFavor()
    {
        // Arrange
        var mockPlayer = CreateMockPlayer("player1", "TestPlayer");
        var site = CreateTestSite("site1", "Temple");

        SetupNonConquestPlayer("player1");
        _tracker.Initialize();

        // Act - simulate enter event via raising the mock event
        _mockAreaTracker.Raise(m => m.OnPlayerEnteredHolySite += null, mockPlayer.Object, site);

        // Assert - no favor awarded (player is not Conquest)
        _mockFavorSystem.Verify(m => m.AwardFavorForAction(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<float>(), It.IsAny<DeityDomain>()),
            Times.Never);
    }

    #endregion

    #region IsConquestDomainPlayer Tests

    [Fact]
    public void IsConquestDomainPlayer_ConquestPlayer_ReturnsTrue()
    {
        // Arrange
        SetupConquestPlayer("player1");

        // Use reflection to test internal method
        var method = typeof(PatrolFavorTracker).GetMethod("IsConquestDomainPlayer",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        // Act
        var result = (bool)method!.Invoke(_tracker, new object[] { "player1" })!;

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsConquestDomainPlayer_NonConquestPlayer_ReturnsFalse()
    {
        // Arrange
        SetupNonConquestPlayer("player1");

        // Use reflection to test internal method
        var method = typeof(PatrolFavorTracker).GetMethod("IsConquestDomainPlayer",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        // Act
        var result = (bool)method!.Invoke(_tracker, new object[] { "player1" })!;

        // Assert
        Assert.False(result);
    }

    #endregion

    #region GetCivilizationHolySites Tests

    [Fact]
    public void GetCivilizationHolySites_NotInCivilization_ReturnsOwnReligionSites()
    {
        // Arrange
        var religion = new ReligionData("rel1", "Test Religion", DeityDomain.Conquest, "Test Deity", "founder", "Founder");
        _mockReligionManager.Setup(m => m.GetPlayerReligion("player1")).Returns(religion);
        _mockCivilizationManager.Setup(m => m.GetCivilizationByPlayer("player1")).Returns((Civilization?)null);

        var sites = new List<HolySiteData>
        {
            CreateTestSite("site1", "Temple 1"),
            CreateTestSite("site2", "Temple 2")
        };
        _mockHolySiteManager.Setup(m => m.GetReligionHolySites("rel1")).Returns(sites);

        // Use reflection to test internal method
        var method = typeof(PatrolFavorTracker).GetMethod("GetCivilizationHolySites",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        // Act
        var result = (List<HolySiteData>)method!.Invoke(_tracker, new object[] { "player1" })!;

        // Assert
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void GetCivilizationHolySites_InCivilization_ReturnsCombinedSites()
    {
        // Arrange
        var religion1 = new ReligionData("rel1", "Test Religion 1", DeityDomain.Conquest, "Test Deity", "founder", "Founder");
        var religion2 = new ReligionData("rel2", "Test Religion 2", DeityDomain.Craft, "Test Deity 2", "founder2", "Founder2");

        _mockReligionManager.Setup(m => m.GetPlayerReligion("player1")).Returns(religion1);

        var civ = new Civilization("civ1", "Test Civ", "founder", "rel1");
        _mockCivilizationManager.Setup(m => m.GetCivilizationByPlayer("player1")).Returns(civ);
        _mockCivilizationManager.Setup(m => m.GetCivReligions("civ1")).Returns(new List<ReligionData> { religion1, religion2 });

        var sites1 = new List<HolySiteData> { CreateTestSite("site1", "Temple 1") };
        var sites2 = new List<HolySiteData> { CreateTestSite("site2", "Temple 2"), CreateTestSite("site3", "Temple 3") };

        _mockHolySiteManager.Setup(m => m.GetReligionHolySites("rel1")).Returns(sites1);
        _mockHolySiteManager.Setup(m => m.GetReligionHolySites("rel2")).Returns(sites2);

        // Use reflection to test internal method
        var method = typeof(PatrolFavorTracker).GetMethod("GetCivilizationHolySites",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        // Act
        var result = (List<HolySiteData>)method!.Invoke(_tracker, new object[] { "player1" })!;

        // Assert
        Assert.Equal(3, result.Count);
    }

    #endregion

    #region Combo Multiplier Tests

    [Theory]
    [InlineData(0, 1.0f)]
    [InlineData(1, 1.0f)]
    [InlineData(2, 1.15f)]
    [InlineData(3, 1.15f)]
    [InlineData(4, 1.3f)]
    [InlineData(7, 1.3f)]
    [InlineData(8, 1.5f)]
    [InlineData(14, 1.5f)]
    [InlineData(15, 1.75f)]
    [InlineData(100, 1.75f)]
    public void GetComboMultiplier_ReturnsCorrectValue(int comboCount, float expectedMultiplier)
    {
        // Use reflection to test internal method
        var method = typeof(PatrolFavorTracker).GetMethod("GetComboMultiplier",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        // Act
        var result = (float)method!.Invoke(_tracker, new object[] { comboCount })!;

        // Assert
        Assert.Equal(expectedMultiplier, result);
    }

    #endregion

    #region Combo Tier Name Tests

    [Theory]
    [InlineData(0, "")]
    [InlineData(1, "")]
    [InlineData(2, "Vigilant")]
    [InlineData(3, "Vigilant")]
    [InlineData(4, "Dedicated")]
    [InlineData(7, "Dedicated")]
    [InlineData(8, "Tireless")]
    [InlineData(14, "Tireless")]
    [InlineData(15, "Legendary")]
    [InlineData(100, "Legendary")]
    public void GetComboTierName_ReturnsCorrectName(int comboCount, string expectedName)
    {
        // Use reflection to test internal method
        var method = typeof(PatrolFavorTracker).GetMethod("GetComboTierName",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        // Act
        var result = (string)method!.Invoke(_tracker, new object[] { comboCount })!;

        // Assert
        Assert.Equal(expectedName, result);
    }

    #endregion

    #region Cooldown Tests

    [Fact]
    public void IsOnPatrolCooldown_NoCompletedPatrol_ReturnsFalse()
    {
        // Arrange
        var playerData = new PlayerProgressionData { LastPatrolCompletionTime = 0 };
        _fakeTimeService.SetElapsedMilliseconds(1000);

        // Use reflection to test internal method
        var method = typeof(PatrolFavorTracker).GetMethod("IsOnPatrolCooldown",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        // Act
        var result = (bool)method!.Invoke(_tracker, new object[] { playerData, 1000L })!;

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsOnPatrolCooldown_WithinCooldown_ReturnsTrue()
    {
        // Arrange - patrol completed 30 minutes ago (within 60-minute cooldown)
        var playerData = new PlayerProgressionData { LastPatrolCompletionTime = 30 * 60 * 1000 };
        var currentTime = 60 * 60 * 1000L; // 60 minutes total

        // Use reflection to test internal method
        var method = typeof(PatrolFavorTracker).GetMethod("IsOnPatrolCooldown",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        // Act
        var result = (bool)method!.Invoke(_tracker, new object[] { playerData, currentTime })!;

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsOnPatrolCooldown_AfterCooldown_ReturnsFalse()
    {
        // Arrange - patrol completed 90 minutes ago (after 60-minute cooldown)
        var playerData = new PlayerProgressionData { LastPatrolCompletionTime = 0 };
        var currentTime = 90 * 60 * 1000L; // 90 minutes total

        // Use reflection to test internal method
        var method = typeof(PatrolFavorTracker).GetMethod("IsOnPatrolCooldown",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        // Act
        var result = (bool)method!.Invoke(_tracker, new object[] { playerData, currentTime })!;

        // Assert
        Assert.False(result);
    }

    #endregion

    #region Combo Timeout Tests

    [Fact]
    public void ShouldResetCombo_NoCombo_ReturnsFalse()
    {
        // Arrange
        var playerData = new PlayerProgressionData { PatrolComboCount = 0 };
        var currentTime = 10 * 60 * 60 * 1000L; // 10 hours

        // Use reflection to test internal method
        var method = typeof(PatrolFavorTracker).GetMethod("ShouldResetCombo",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        // Act
        var result = (bool)method!.Invoke(_tracker, new object[] { playerData, currentTime })!;

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ShouldResetCombo_WithinTimeout_ReturnsFalse()
    {
        // Arrange - last patrol 1 hour ago (within 2-hour timeout)
        var playerData = new PlayerProgressionData
        {
            PatrolComboCount = 5,
            LastPatrolCompletionTime = 1 * 60 * 60 * 1000 // 1 hour
        };
        var currentTime = 2 * 60 * 60 * 1000L; // 2 hours total

        // Use reflection to test internal method
        var method = typeof(PatrolFavorTracker).GetMethod("ShouldResetCombo",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        // Act
        var result = (bool)method!.Invoke(_tracker, new object[] { playerData, currentTime })!;

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ShouldResetCombo_AfterTimeout_ReturnsTrue()
    {
        // Arrange - last patrol 3 hours ago (after 2-hour timeout)
        var playerData = new PlayerProgressionData
        {
            PatrolComboCount = 5,
            LastPatrolCompletionTime = 0
        };
        var currentTime = 3 * 60 * 60 * 1000L; // 3 hours

        // Use reflection to test internal method
        var method = typeof(PatrolFavorTracker).GetMethod("ShouldResetCombo",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        // Act
        var result = (bool)method!.Invoke(_tracker, new object[] { playerData, currentTime })!;

        // Assert
        Assert.True(result);
    }

    #endregion
}
