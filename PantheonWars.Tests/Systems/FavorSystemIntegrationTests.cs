using System.Diagnostics.CodeAnalysis;
using Moq;
using PantheonWars.Data;
using PantheonWars.Models.Enum;
using PantheonWars.Systems;
using PantheonWars.Systems.Interfaces;
using PantheonWars.Tests.Helpers;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Xunit;

namespace PantheonWars.Tests.Systems;

/// <summary>
///     Integration tests for FavorSystem
///     Tests PvP kill processing, death penalties, and favor calculations with mocked dependencies
/// </summary>
[ExcludeFromCodeCoverage]
public class FavorSystemIntegrationTests
{
    private readonly Mock<ICoreServerAPI> _mockAPI;
    private readonly Mock<IDeityRegistry> _mockDeityRegistry;
    private readonly Mock<IPlayerReligionDataManager> _mockPlayerReligionDataManager;
    private readonly Mock<IReligionManager> _mockReligionManager;
    private readonly Mock<IReligionPrestigeManager> _mockPrestigeManager;
    private readonly FavorSystem _favorSystem;

    public FavorSystemIntegrationTests()
    {
        _mockAPI = TestFixtures.CreateMockServerAPI();
        _mockDeityRegistry = TestFixtures.CreateMockDeityRegistry();
        _mockPlayerReligionDataManager = TestFixtures.CreateMockPlayerReligionDataManager();
        _mockReligionManager = TestFixtures.CreateMockReligionManager();
        _mockPrestigeManager = TestFixtures.CreateMockReligionPrestigeManager();

        _favorSystem = new FavorSystem(
            _mockAPI.Object,
            _mockPlayerReligionDataManager.Object,
            _mockDeityRegistry.Object,
            _mockReligionManager.Object,
            _mockPrestigeManager.Object
        );
    }

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

    #region PvP Kill Processing Tests

    [Fact]
    public void ProcessPvPKill_AttackerWithoutDeity_AwardsNoFavor()
    {
        // Arrange
        var attackerData = TestFixtures.CreateTestPlayerReligionData(
            "attacker-uid",
            DeityType.None, // No deity
            null);

        var victimData = TestFixtures.CreateTestPlayerReligionData(
            "victim-uid",
            DeityType.Khoras,
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

    [Fact]
    public void ProcessPvPKill_IncrementsKillCount()
    {
        // Arrange
        var attackerData = TestFixtures.CreateTestPlayerReligionData(
            "attacker-uid",
            DeityType.Khoras,
            "religion-1");
        attackerData.KillCount = 5;

        var victimData = TestFixtures.CreateTestPlayerReligionData(
            "victim-uid",
            DeityType.Aethra,
            "religion-2");

        var mockAttacker = TestFixtures.CreateMockServerPlayer("attacker-uid", "Attacker");
        var mockVictim = TestFixtures.CreateMockServerPlayer("victim-uid", "Victim");

        _mockPlayerReligionDataManager
            .Setup(m => m.GetOrCreatePlayerData("attacker-uid"))
            .Returns(attackerData);

        _mockPlayerReligionDataManager
            .Setup(m => m.GetOrCreatePlayerData("victim-uid"))
            .Returns(victimData);

        _mockDeityRegistry
            .Setup(r => r.GetFavorMultiplier(DeityType.Khoras, DeityType.Aethra))
            .Returns(2.0f);

        // Act
        _favorSystem.ProcessPvPKill(mockAttacker.Object, mockVictim.Object);

        // Assert
        Assert.Equal(6, attackerData.KillCount);
    }

    #endregion

    #region Death Penalty Tests

    [Fact]
    public void ProcessDeathPenalty_RemovesCorrectFavorAmount()
    {
        // Arrange
        var playerData = TestFixtures.CreateTestPlayerReligionData(
            "player-uid",
            DeityType.Khoras,
            "religion-1",
            favor: 10);

        var mockPlayer = TestFixtures.CreateMockServerPlayer("player-uid", "TestPlayer");

        _mockPlayerReligionDataManager
            .Setup(m => m.GetOrCreatePlayerData("player-uid"))
            .Returns(playerData);

        // Act
        _favorSystem.ProcessDeathPenalty(mockPlayer.Object);

        // Assert - Should remove 5 favor (DEATH_PENALTY_FAVOR)
        _mockPlayerReligionDataManager.Verify(
            m => m.RemoveFavor("player-uid", 5, "Death penalty"),
            Times.Once()
        );
    }

    [Fact]
    public void ProcessDeathPenalty_WithZeroFavor_RemovesNothing()
    {
        // Arrange
        var playerData = TestFixtures.CreateTestPlayerReligionData(
            "player-uid",
            DeityType.Khoras,
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
            DeityType.Khoras,
            "religion-1",
            favor: 10);

        var mockPlayer = TestFixtures.CreateMockServerPlayer("player-uid", "TestPlayer");

        _mockPlayerReligionDataManager
            .Setup(m => m.GetOrCreatePlayerData("player-uid"))
            .Returns(playerData);

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

    #region Favor Calculation Tests

    [Fact]
    public void CalculateFavorReward_WithNoVictimDeity_ReturnsBaseFavor()
    {
        // Arrange & Act
        var reward = _favorSystem.CalculateFavorReward(DeityType.Khoras, DeityType.None);

        // Assert
        Assert.Equal(10, reward); // BASE_KILL_FAVOR
    }

    [Fact]
    public void CalculateFavorReward_UsesDeityMultiplier_Correctly()
    {
        // Arrange
        _mockDeityRegistry
            .Setup(r => r.GetFavorMultiplier(DeityType.Khoras, DeityType.Aethra))
            .Returns(2.0f);

        // Act
        var reward = _favorSystem.CalculateFavorReward(DeityType.Khoras, DeityType.Aethra);

        // Assert
        Assert.Equal(20, reward); // BASE_KILL_FAVOR * 2.0
    }

    #endregion

    #region Award Favor For Action Tests

    [Fact]
    public void AwardFavorForAction_WithValidPlayer_AwardsFavor()
    {
        // Arrange
        var playerData = TestFixtures.CreateTestPlayerReligionData(
            "player-uid",
            DeityType.Khoras,
            "religion-1");

        var mockPlayer = TestFixtures.CreateMockServerPlayer("player-uid", "TestPlayer");

        _mockPlayerReligionDataManager
            .Setup(m => m.GetOrCreatePlayerData("player-uid"))
            .Returns(playerData);

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
            DeityType.None,
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
