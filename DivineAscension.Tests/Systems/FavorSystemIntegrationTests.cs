using System.Diagnostics.CodeAnalysis;
using DivineAscension.Models.Enum;
using DivineAscension.Systems;
using DivineAscension.Systems.Interfaces;
using DivineAscension.Tests.Helpers;
using Moq;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace DivineAscension.Tests.Systems;

/// <summary>
///     Integration tests for FavorSystem
///     Tests PvP kill processing, death penalties, and favor calculations with mocked dependencies
/// </summary>
[ExcludeFromCodeCoverage]
public class FavorSystemIntegrationTests
{
    private readonly FavorSystem _favorSystem;
    private readonly Mock<ICoreServerAPI> _mockAPI;
    private readonly Mock<IPlayerProgressionDataManager> _mockPlayerReligionDataManager;
    private readonly Mock<IReligionPrestigeManager> _mockPrestigeManager;
    private readonly Mock<IReligionManager> _mockReligionManager;

    public FavorSystemIntegrationTests()
    {
        _mockAPI = TestFixtures.CreateMockServerAPI();
        _mockPlayerReligionDataManager = TestFixtures.CreateMockPlayerProgressionDataManager();
        _mockReligionManager = TestFixtures.CreateMockReligionManager();
        _mockPrestigeManager = TestFixtures.CreateMockReligionPrestigeManager();

        _favorSystem = new FavorSystem(
            _mockAPI.Object,
            _mockPlayerReligionDataManager.Object,
            _mockReligionManager.Object,
            _mockPrestigeManager.Object
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
        var mockEventAPI = new Mock<IServerEventAPI>();
        _mockAPI.Setup(a => a.Event).Returns(mockEventAPI.Object);

        // Act
        _favorSystem.Initialize();

        // Assert
        mockEventAPI.VerifyAdd(e => e.PlayerDeath += It.IsAny<PlayerDeathDelegate>(), Times.Once());
    }

    [Fact]
    public void Initialize_RegistersGameTickListener()
    {
        // Arrange
        var mockEventAPI = new Mock<IServerEventAPI>();
        _mockAPI.Setup(a => a.Event).Returns(mockEventAPI.Object);

        // Act
        _favorSystem.Initialize();

        // Assert
        mockEventAPI.Verify(
            e => e.RegisterGameTickListener(It.IsAny<Action<float>>(), It.Is<int>(i => i == 1000), 0),
            Times.Once()
        );
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
                It.IsAny<int>(),
                It.Is<string>(s => s.Contains("lost") && s.Contains("favor")),
                EnumChatType.Notification,
                It.IsAny<string>()),
            Times.Once()
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
            m => m.AddFavor("player-uid", 15, "test action"),
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