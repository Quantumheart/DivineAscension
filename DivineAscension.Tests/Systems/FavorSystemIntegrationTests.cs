using System.Diagnostics.CodeAnalysis;
using DivineAscension.Configuration;
using DivineAscension.Models.Enum;
using DivineAscension.Systems;
using DivineAscension.Systems.Interfaces;
using DivineAscension.Tests.Helpers;
using Moq;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace DivineAscension.Tests.Systems;

/// <summary>
///     Integration tests for FavorSystem
///     Tests PvP kill processing, death penalties, and favor calculations with fake services
/// </summary>
[ExcludeFromCodeCoverage]
public class FavorSystemIntegrationTests
{
    private readonly FakeEventService _fakeEventService;
    private readonly FakeWorldService _fakeWorldService;
    private readonly FavorSystem _favorSystem;
    private readonly Mock<IPlayerProgressionDataManager> _mockPlayerReligionDataManager;
    private readonly Mock<IReligionPrestigeManager> _mockPrestigeManager;
    private readonly Mock<IReligionManager> _mockReligionManager;

    public FavorSystemIntegrationTests()
    {
        var mockLogger = new Mock<ILogger>();
        _fakeEventService = new FakeEventService();
        _fakeWorldService = new FakeWorldService();
        _mockPlayerReligionDataManager = TestFixtures.CreateMockPlayerProgressionDataManager();
        _mockReligionManager = TestFixtures.CreateMockReligionManager();
        _mockPrestigeManager = TestFixtures.CreateMockReligionPrestigeManager();

        var mockActivityLogManager = new Mock<IActivityLogManager>();
        var testConfig = new GameBalanceConfig(); // Uses default values
        _favorSystem = new FavorSystem(
            mockLogger.Object,
            _fakeEventService,
            _fakeWorldService,
            _mockPlayerReligionDataManager.Object,
            _mockReligionManager.Object,
            _mockPrestigeManager.Object,
            mockActivityLogManager.Object,
            testConfig
        );
    }

    #region PvP Kill Processing Tests

    [Fact]
    public void ProcessPvPKill_AttackerWithoutDeity_AwardsNoFavor()
    {
        // Arrange
        var attackerData = TestFixtures.CreateTestPlayerReligionData(
            "attacker-uid",
            DeityDomain.None, // No deity
            null);

        var victimData = TestFixtures.CreateTestPlayerReligionData(
            "victim-uid",
            DeityDomain.Craft,
            "religion-1");

        var mockAttacker = TestFixtures.CreateMockServerPlayer("attacker-uid", "Attacker");
        var mockVictim = TestFixtures.CreateMockServerPlayer("victim-uid", "Victim");

        _mockPlayerReligionDataManager
            .Setup(m => m.GetOrCreatePlayerData("attacker-uid"))
            .Returns(attackerData);

        _mockPlayerReligionDataManager
            .Setup(m => m.GetOrCreatePlayerData("victim-uid"))
            .Returns(victimData);

        // Act
        _favorSystem.ProcessPvPKill(mockAttacker.Object, mockVictim.Object);

        // Assert - Should not award any favor
        _mockPlayerReligionDataManager.Verify(
            m => m.AddFavor(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>()),
            Times.Never()
        );
    }

    #endregion

    #region Favor Calculation Tests

    [Fact]
    public void CalculateFavorReward_WithNoVictimDeity_ReturnsBaseFavor()
    {
        // Arrange & Act
        var reward = _favorSystem.CalculateFavorReward(DeityDomain.Craft, DeityDomain.None);

        // Assert
        Assert.Equal(10, reward); // BASE_KILL_FAVOR
    }

    #endregion

    #region Initialization Tests

    [Fact]
    public void Initialize_SubscribesToPlayerDeathEvent()
    {
        // Arrange
        var initialCount = _fakeEventService.PlayerDeathCallbackCount;

        // Act
        _favorSystem.Initialize();

        // Assert
        Assert.Equal(initialCount + 1, _fakeEventService.PlayerDeathCallbackCount);
    }

    #endregion

    #region Death Penalty Tests

    [Fact]
    public void ProcessDeathPenalty_RemovesCorrectFavorAmount()
    {
        // Arrange
        var playerData = TestFixtures.CreateTestPlayerReligionData(
            "player-uid",
            DeityDomain.Craft,
            "religion-1",
            favor: 10);

        var mockPlayer = TestFixtures.CreateMockServerPlayer("player-uid", "TestPlayer");

        _mockPlayerReligionDataManager
            .Setup(m => m.GetOrCreatePlayerData("player-uid"))
            .Returns(playerData);
        _mockReligionManager.Setup(d => d.GetPlayerActiveDeityDomain("player-uid")).Returns(DeityDomain.Craft);

        // Act
        _favorSystem.ProcessDeathPenalty(mockPlayer.Object);

        // Assert - Should remove 50 favor (DEATH_PENALTY_FAVOR)
        _mockPlayerReligionDataManager.Verify(
            m => m.RemoveFavor("player-uid", 10, "Death penalty"),
            Times.Once()
        );
    }

    [Fact]
    public void ProcessDeathPenalty_WithZeroFavor_RemovesNothing()
    {
        // Arrange
        var playerData = TestFixtures.CreateTestPlayerReligionData(
            "player-uid",
            DeityDomain.Craft,
            "religion-1",
            favor: 0);

        var mockPlayer = TestFixtures.CreateMockServerPlayer("player-uid", "TestPlayer");

        _mockPlayerReligionDataManager
            .Setup(m => m.GetOrCreatePlayerData("player-uid"))
            .Returns(playerData);

        // Act
        _favorSystem.ProcessDeathPenalty(mockPlayer.Object);

        // Assert - Should not call RemoveFavor when favor is 0
        _mockPlayerReligionDataManager.Verify(
            m => m.RemoveFavor(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>()),
            Times.Never()
        );
    }

    [Fact]
    public void ProcessDeathPenalty_SendsNotificationToPlayer()
    {
        // Arrange
        var playerData = TestFixtures.CreateTestPlayerReligionData(
            "player-uid",
            DeityDomain.Craft,
            "religion-1",
            favor: 10);

        var mockPlayer = TestFixtures.CreateMockServerPlayer("player-uid", "TestPlayer");

        _mockPlayerReligionDataManager
            .Setup(m => m.GetOrCreatePlayerData("player-uid"))
            .Returns(playerData);
        _mockReligionManager.Setup(d => d.GetPlayerActiveDeityDomain("player-uid")).Returns(DeityDomain.Craft);

        // Act
        _favorSystem.ProcessDeathPenalty(mockPlayer.Object);

        // Assert
        mockPlayer.Verify(
            p => p.SendMessage(
                It.Is<int>(g => g == GlobalConstants.GeneralChatGroup),
                It.Is<string>(s => s.Contains("lost") && s.Contains("favor")),
                It.Is<EnumChatType>(t => t == EnumChatType.Notification),
                It.IsAny<string>()),
            Times.Once
        );
    }

    #endregion

    #region Award Favor For Action Tests

    [Fact]
    public void AwardFavorForAction_WithValidPlayer_AwardsFavor()
    {
        // Arrange
        var playerData = TestFixtures.CreateTestPlayerReligionData(
            "player-uid",
            DeityDomain.Craft,
            "religion-1");

        var religion = TestFixtures.CreateTestReligion(
            "religion-1");

        var mockPlayer = TestFixtures.CreateMockServerPlayer("player-uid", "TestPlayer");

        _mockPlayerReligionDataManager
            .Setup(m => m.GetOrCreatePlayerData("player-uid"))
            .Returns(playerData);
        _mockReligionManager.Setup(d => d.GetPlayerActiveDeityDomain("player-uid")).Returns(DeityDomain.Craft);
        _mockReligionManager.Setup(d => d.GetPlayerReligion("player-uid")).Returns(religion);

        // Act
        _favorSystem.AwardFavorForAction(mockPlayer.Object, "test action", 15);

        // Assert
        _mockPlayerReligionDataManager.Verify(
            m => m.AddFractionalFavor("player-uid", 15, "test action"),
            Times.Once()
        );
    }

    [Fact]
    public void AwardFavorForAction_WithoutDeity_DoesNotAwardFavor()
    {
        // Arrange
        var playerData = TestFixtures.CreateTestPlayerReligionData(
            "player-uid",
            DeityDomain.None,
            null);

        var mockPlayer = TestFixtures.CreateMockServerPlayer("player-uid", "TestPlayer");

        _mockPlayerReligionDataManager
            .Setup(m => m.GetOrCreatePlayerData("player-uid"))
            .Returns(playerData);

        // Act
        _favorSystem.AwardFavorForAction(mockPlayer.Object, "test action", 15);

        // Assert
        _mockPlayerReligionDataManager.Verify(
            m => m.AddFavor(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>()),
            Times.Never()
        );
    }

    #endregion
}