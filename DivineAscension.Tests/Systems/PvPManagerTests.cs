using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using DivineAscension.Data;
using DivineAscension.Models;
using DivineAscension.Models.Enum;
using DivineAscension.Systems;
using DivineAscension.Systems.Interfaces;
using DivineAscension.Tests.Helpers;
using Moq;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

namespace DivineAscension.Tests.Systems;

/// <summary>
///     Unit tests for PvPManager
///     Tests PvP kill processing, favor/prestige rewards, and death penalties
/// </summary>
[ExcludeFromCodeCoverage]
public class PvPManagerTests
{
    private readonly Mock<ICoreServerAPI> _mockAPI;
    private readonly Mock<ICivilizationManager> _mockCivilizationManager;
    private readonly Mock<IDeityRegistry> _mockDeityRegistry;
    private readonly Mock<IDiplomacyManager> _mockDiplomacyManager;
    private readonly Mock<IPlayerProgressionDataManager> _mockPlayerReligionDataManager;
    private readonly Mock<IReligionPrestigeManager> _mockPrestigeManager;
    private readonly Mock<IReligionManager> _mockReligionManager;
    private readonly PvPManager _pvpManager;

    public PvPManagerTests()
    {
        _mockAPI = TestFixtures.CreateMockServerAPI();
        _mockPlayerReligionDataManager = TestFixtures.CreateMockPlayerProgressionDataManager();
        _mockReligionManager = new Mock<IReligionManager>();
        _mockPrestigeManager = new Mock<IReligionPrestigeManager>();
        _mockDeityRegistry = new Mock<IDeityRegistry>();
        _mockCivilizationManager = new Mock<ICivilizationManager>();
        _mockDiplomacyManager = new Mock<IDiplomacyManager>();

        _pvpManager = new PvPManager(
            _mockAPI.Object,
            _mockPlayerReligionDataManager.Object,
            _mockReligionManager.Object,
            _mockPrestigeManager.Object,
            _mockDeityRegistry.Object,
            _mockCivilizationManager.Object,
            _mockDiplomacyManager.Object
        );
    }

    #region Initialization Tests

    [Fact]
    public void Initialize_DoesNotThrowException()
    {
        // Act & Assert - Should not throw
        var exception = Record.Exception(() => _pvpManager.Initialize());
        Assert.Null(exception);
    }

    [Fact]
    public void Initialize_LogsNotification()
    {
        // Arrange
        var mockLogger = new Mock<ILogger>();
        _mockAPI.Setup(a => a.Logger).Returns(mockLogger.Object);

        // Act
        _pvpManager.Initialize();

        // Assert
        mockLogger.Verify(
            l => l.Notification(It.Is<string>(s => s.Contains("Initializing") && s.Contains("PvP"))),
            Times.Once()
        );
    }

    #endregion

    #region AwardRewardsForAction Tests

    [Fact]
    public void AwardRewardsForAction_WithValidPlayer_AwardsFavorAndPrestige()
    {
        // Arrange
        var playerData = TestFixtures.CreateTestPlayerReligionData("player-uid", DeityType.Khoras, "religion-uid");

        _mockPlayerReligionDataManager
            .Setup(m => m.GetOrCreatePlayerData("player-uid"))
            .Returns(playerData);
        _mockPlayerReligionDataManager.Setup(m => m.HasReligion(It.IsAny<string>())).Returns(true);
        _mockPlayerReligionDataManager.Setup(m => m.GetPlayerDeityType(It.IsAny<string>())).Returns(DeityType.Khoras);
        _mockReligionManager.Setup(m => m.GetPlayerReligion("player-uid")).Returns(TestFixtures.CreateTestReligion());

        var mockPlayer = new Mock<IServerPlayer>();
        mockPlayer.Setup(p => p.PlayerUID).Returns("player-uid");
        mockPlayer.Setup(p => p.PlayerName).Returns("TestPlayer");

        // Act
        _pvpManager.AwardRewardsForAction(mockPlayer.Object, "test action", 10, 15);

        // Assert - Should award favor
        _mockPlayerReligionDataManager.Verify(
            m => m.AddFavor("player-uid", 10, "test action"),
            Times.Once()
        );

        // Assert - Should award prestige
        _mockPrestigeManager.Verify(
            m => m.AddPrestige("test-religion-uid", 15, It.Is<string>(s => s.Contains("test action"))),
            Times.Once()
        );

        // Assert - Should send message
        mockPlayer.Verify(
            p => p.SendMessage(
                It.Is<int>(g => g == GlobalConstants.GeneralChatGroup),
                It.Is<string>(s => s.Contains("10 favor") && s.Contains("15 prestige")),
                It.Is<EnumChatType>(t => t == EnumChatType.Notification),
                It.IsAny<string>()
            ),
            Times.Once()
        );
    }

    [Fact]
    public void AwardRewardsForAction_WithoutDeity_DoesNotAwardRewards()
    {
        // Arrange
        var playerData = TestFixtures.CreateTestPlayerReligionData("player-uid", DeityType.None, null);

        _mockPlayerReligionDataManager
            .Setup(m => m.GetOrCreatePlayerData("player-uid"))
            .Returns(playerData);

        var mockPlayer = new Mock<IServerPlayer>();
        mockPlayer.Setup(p => p.PlayerUID).Returns("player-uid");

        // Act
        _pvpManager.AwardRewardsForAction(mockPlayer.Object, "test action", 10, 15);

        // Assert - Should not award favor
        _mockPlayerReligionDataManager.Verify(
            m => m.AddFavor(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>()),
            Times.Never()
        );

        // Assert - Should not award prestige
        _mockPrestigeManager.Verify(
            m => m.AddPrestige(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>()),
            Times.Never()
        );
    }

    [Fact]
    public void AwardRewardsForAction_WithoutReligion_DoesNotAwardRewards()
    {
        // Arrange
        var playerData = TestFixtures.CreateTestPlayerReligionData("player-uid", DeityType.Khoras, null);

        _mockPlayerReligionDataManager
            .Setup(m => m.GetOrCreatePlayerData("player-uid"))
            .Returns(playerData);

        var mockPlayer = new Mock<IServerPlayer>();
        mockPlayer.Setup(p => p.PlayerUID).Returns("player-uid");

        // Act
        _pvpManager.AwardRewardsForAction(mockPlayer.Object, "test action", 10, 15);

        // Assert - Should not award anything
        _mockPlayerReligionDataManager.Verify(
            m => m.AddFavor(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>()),
            Times.Never()
        );

        _mockPrestigeManager.Verify(
            m => m.AddPrestige(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>()),
            Times.Never()
        );
    }

    [Fact]
    public void AwardRewardsForAction_WithZeroFavor_DoesNotAwardFavor()
    {
        // Arrange
        var playerData = TestFixtures.CreateTestPlayerReligionData("player-uid", DeityType.Khoras, "religion-uid");

        _mockPlayerReligionDataManager
            .Setup(m => m.GetOrCreatePlayerData("player-uid"))
            .Returns(playerData);
        _mockPlayerReligionDataManager.Setup(m => m.HasReligion(It.IsAny<string>())).Returns(true);
        _mockPlayerReligionDataManager.Setup(m => m.GetPlayerDeityType(It.IsAny<string>())).Returns(DeityType.Khoras);
        _mockReligionManager.Setup(m => m.GetPlayerReligion("player-uid")).Returns(TestFixtures.CreateTestReligion());


        var mockPlayer = new Mock<IServerPlayer>();
        mockPlayer.Setup(p => p.PlayerUID).Returns("player-uid");
        mockPlayer.Setup(p => p.PlayerName).Returns("TestPlayer");

        // Act - Zero favor, but 15 prestige
        _pvpManager.AwardRewardsForAction(mockPlayer.Object, "test action", 0, 15);

        // Assert - Should not award favor
        _mockPlayerReligionDataManager.Verify(
            m => m.AddFavor(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>()),
            Times.Never()
        );

        // Assert - Should still award prestige
        _mockPrestigeManager.Verify(
            m => m.AddPrestige("test-religion-uid", 15, It.IsAny<string>()),
            Times.Once()
        );
    }

    [Fact]
    public void AwardRewardsForAction_WithZeroPrestige_DoesNotAwardPrestige()
    {
        // Arrange
        var playerData = TestFixtures.CreateTestPlayerReligionData("player-uid", DeityType.Khoras, "religion-uid");

        // _mockPlayerReligionDataManager
        // .Setup(m => m.GetOrCreatePlayerData("player-uid"))
        // .Returns(playerData);
        _mockPlayerReligionDataManager.Setup(m => m.HasReligion(It.IsAny<string>())).Returns(true);
        _mockPlayerReligionDataManager.Setup(m => m.GetPlayerDeityType(It.IsAny<string>())).Returns(DeityType.Khoras);
        _mockReligionManager.Setup(m => m.GetPlayerReligion("player-uid")).Returns(TestFixtures.CreateTestReligion());


        var mockPlayer = new Mock<IServerPlayer>();
        mockPlayer.Setup(p => p.PlayerUID).Returns("player-uid");
        mockPlayer.Setup(p => p.PlayerName).Returns("TestPlayer");

        // Act - 10 favor, but zero prestige
        _pvpManager.AwardRewardsForAction(mockPlayer.Object, "test action", 10, 0);

        // Assert - Should award favor
        _mockPlayerReligionDataManager.Verify(
            m => m.AddFavor("player-uid", 10, "test action"),
            Times.Once()
        );

        // Assert - Should not award prestige
        _mockPrestigeManager.Verify(
            m => m.AddPrestige(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>()),
            Times.Never()
        );
    }

    #endregion

    #region CalculateFavorReward Tests

    [Fact]
    public void CalculateFavorReward_WithNoVictimDeity_ReturnsBaseFavor()
    {
        // This tests the private method indirectly through ProcessPvPKill
        // Arrange
        var attackerData = TestFixtures.CreateTestPlayerReligionData("attacker-uid", DeityType.Khoras, "religion-uid");
        var victimData = TestFixtures.CreateTestPlayerReligionData("victim-uid", DeityType.None, null);
        var religion = TestFixtures.CreateTestReligion("religion-uid");

        _mockPlayerReligionDataManager
            .Setup(m => m.GetOrCreatePlayerData("attacker-uid"))
            .Returns(attackerData);

        _mockPlayerReligionDataManager
            .Setup(m => m.GetOrCreatePlayerData("victim-uid"))
            .Returns(victimData);

        _mockReligionManager
            .Setup(m => m.GetReligion("religion-uid"))
            .Returns(religion);

        _mockReligionManager
            .Setup(m => m.GetPlayerActiveDeity("attacker-uid"))
            .Returns(DeityType.Khoras);

        _mockReligionManager
            .Setup(m => m.GetPlayerReligion("attacker-uid"))
            .Returns(religion);

        _mockReligionManager
            .Setup(m => m.GetPlayerActiveDeity("victim-uid"))
            .Returns(DeityType.None);

        _mockReligionManager
            .Setup(m => m.GetPlayerReligion("victim-uid"))
            .Returns((ReligionData?)null);

        var mockAttacker = new Mock<IServerPlayer>();
        mockAttacker.Setup(p => p.PlayerUID).Returns("attacker-uid");
        mockAttacker.Setup(p => p.PlayerName).Returns("Attacker");

        var mockVictim = new Mock<IServerPlayer>();
        mockVictim.Setup(p => p.PlayerUID).Returns("victim-uid");
        mockVictim.Setup(p => p.PlayerName).Returns("Victim");

        var khoras = new Deity(DeityType.Khoras, "Khoras", "War");
        _mockDeityRegistry.Setup(r => r.GetDeity(DeityType.Khoras)).Returns(khoras);

        // Act
        _pvpManager.ProcessPvPKill(mockAttacker.Object, mockVictim.Object);

        // Assert - Base favor should be 10
        _mockPlayerReligionDataManager.Verify(
            m => m.AddFavor("attacker-uid", 10, It.IsAny<string>()),
            Times.Once()
        );
    }

    [Fact]
    public void CalculateFavorReward_WithSameDeity_ReturnsFullFavor()
    {
        // Arrange
        // Arrange
        var attackerReligion = TestFixtures.CreateTestReligion("attacker-religion-uid");
        var victimReligion = TestFixtures.CreateTestReligion("victim-religion-uid");

        _mockReligionManager
            .Setup(m => m.GetPlayerActiveDeity("attacker-uid"))
            .Returns(DeityType.Khoras);

        _mockReligionManager
            .Setup(m => m.GetPlayerActiveDeity("victim-uid"))
            .Returns(DeityType.Lysa);

        _mockReligionManager
            .Setup(m => m.GetPlayerReligion("attacker-uid"))
            .Returns(attackerReligion);
        _mockReligionManager
            .Setup(m => m.GetPlayerReligion("victim-uid"))
            .Returns(victimReligion);
        var mockAttacker = new Mock<IServerPlayer>();
        mockAttacker.Setup(p => p.PlayerUID).Returns("attacker-uid");
        mockAttacker.Setup(p => p.PlayerName).Returns("Attacker");

        var mockVictim = new Mock<IServerPlayer>();
        mockVictim.Setup(p => p.PlayerUID).Returns("victim-uid");
        mockVictim.Setup(p => p.PlayerName).Returns("Victim");

        var khoras = new Deity(DeityType.Khoras, "Khoras", "War");
        _mockDeityRegistry.Setup(r => r.GetDeity(DeityType.Khoras)).Returns(khoras);

        // Use reflection to call private method
        var method = typeof(PvPManager).GetMethod("ProcessPvPKill",
            BindingFlags.NonPublic | BindingFlags.Instance);

        // Act
        method?.Invoke(_pvpManager, new object[] { mockAttacker.Object, mockVictim.Object });

        // Assert - Full favor (no same-deity penalty)
        _mockPlayerReligionDataManager.Verify(
            m => m.AddFavor("attacker-uid", 10, It.IsAny<string>()),
            Times.Once()
        );
    }

    #endregion

    #region CalculatePrestigeReward Tests

    [Fact]
    public void CalculatePrestigeReward_WithNoVictimDeity_ReturnsBasePrestige()
    {
        // Arrange
        // Arrange
        var attackerReligion = TestFixtures.CreateTestReligion("attacker-religion-uid");
        var victimReligion = TestFixtures.CreateTestReligion("victim-religion-uid");

        _mockReligionManager
            .Setup(m => m.GetPlayerActiveDeity("attacker-uid"))
            .Returns(DeityType.Khoras);

        _mockReligionManager
            .Setup(m => m.GetPlayerActiveDeity("victim-uid"))
            .Returns(DeityType.Lysa);

        _mockReligionManager
            .Setup(m => m.GetPlayerReligion("attacker-uid"))
            .Returns(attackerReligion);
        _mockReligionManager
            .Setup(m => m.GetPlayerReligion("victim-uid"))
            .Returns(victimReligion);

        var mockAttacker = new Mock<IServerPlayer>();
        mockAttacker.Setup(p => p.PlayerUID).Returns("attacker-uid");
        mockAttacker.Setup(p => p.PlayerName).Returns("Attacker");

        var mockVictim = new Mock<IServerPlayer>();
        mockVictim.Setup(p => p.PlayerUID).Returns("victim-uid");
        mockVictim.Setup(p => p.PlayerName).Returns("Victim");

        var khoras = new Deity(DeityType.Khoras, "Khoras", "War");
        _mockDeityRegistry.Setup(r => r.GetDeity(DeityType.Khoras)).Returns(khoras);

        // Act
        _pvpManager.ProcessPvPKill(mockAttacker.Object, mockVictim.Object);

        // Assert - Base prestige should be 15
        _mockPrestigeManager.Verify(
            m => m.AddPrestige("attacker-religion-uid", 15, It.IsAny<string>()),
            Times.Once()
        );
    }

    [Fact]
    public void CalculatePrestigeReward_WithSameDeity_ReturnsFullPrestige()
    {
        // Arrange
        var attackerReligion = TestFixtures.CreateTestReligion("attacker-religion-uid");
        var victimReligion = TestFixtures.CreateTestReligion("victim-religion-uid");

        _mockReligionManager
            .Setup(m => m.GetPlayerActiveDeity("attacker-uid"))
            .Returns(DeityType.Khoras);

        _mockReligionManager
            .Setup(m => m.GetPlayerActiveDeity("victim-uid"))
            .Returns(DeityType.Lysa);

        _mockReligionManager
            .Setup(m => m.GetPlayerReligion("attacker-uid"))
            .Returns(attackerReligion);
        _mockReligionManager
            .Setup(m => m.GetPlayerReligion("victim-uid"))
            .Returns(victimReligion);

        var mockAttacker = new Mock<IServerPlayer>();
        mockAttacker.Setup(p => p.PlayerUID).Returns("attacker-uid");
        mockAttacker.Setup(p => p.PlayerName).Returns("Attacker");

        var mockVictim = new Mock<IServerPlayer>();
        mockVictim.Setup(p => p.PlayerUID).Returns("victim-uid");
        mockVictim.Setup(p => p.PlayerName).Returns("Victim");

        var khoras = new Deity(DeityType.Khoras, "Khoras", "War");
        _mockDeityRegistry.Setup(r => r.GetDeity(DeityType.Khoras)).Returns(khoras);

        // Act
        _pvpManager.ProcessPvPKill(mockAttacker.Object, mockVictim.Object);

        // Assert - Full prestige (no same-deity penalty)
        _mockPrestigeManager.Verify(
            m => m.AddPrestige("attacker-religion-uid", 15, It.IsAny<string>()),
            Times.Once()
        );
    }

    #endregion

    #region ProcessPvPKill Tests

    [Fact]
    public void ProcessPvPKill_WithoutReligion_SendsJoinReligionMessage()
    {
        // Arrange
        var attackerData = TestFixtures.CreateTestPlayerReligionData("attacker-uid", DeityType.None, null);
        var victimData = TestFixtures.CreateTestPlayerReligionData("victim-uid", DeityType.Khoras, null);

        _mockPlayerReligionDataManager
            .Setup(m => m.GetOrCreatePlayerData("attacker-uid"))
            .Returns(attackerData);

        _mockPlayerReligionDataManager
            .Setup(m => m.GetOrCreatePlayerData("victim-uid"))
            .Returns(victimData);

        var mockAttacker = new Mock<IServerPlayer>();
        mockAttacker.Setup(p => p.PlayerUID).Returns("attacker-uid");
        mockAttacker.Setup(p => p.PlayerName).Returns("Attacker");

        var mockVictim = new Mock<IServerPlayer>();
        mockVictim.Setup(p => p.PlayerUID).Returns("victim-uid");
        mockVictim.Setup(p => p.PlayerName).Returns("Victim");

        // Use reflection to call private method
        var method = typeof(PvPManager).GetMethod("ProcessPvPKill",
            BindingFlags.NonPublic | BindingFlags.Instance);

        // Act
        method?.Invoke(_pvpManager, new object[] { mockAttacker.Object, mockVictim.Object });

        // Assert
        mockAttacker.Verify(
            p => p.SendMessage(
                It.Is<int>(g => g == GlobalConstants.GeneralChatGroup),
                It.Is<string>(s => s.Contains("Join a religion")),
                It.Is<EnumChatType>(t => t == EnumChatType.Notification),
                It.IsAny<string>()
            ),
            Times.Once()
        );

        // Should not award any rewards
        _mockPlayerReligionDataManager.Verify(
            m => m.AddFavor(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>()),
            Times.Never()
        );
    }

    [Fact]
    public void ProcessPvPKill_WithInvalidReligion_AwardsAttackerRewards()
    {
        // Arrange - Victim has no religion, but attacker should still get rewards
        var attackerReligion = TestFixtures.CreateTestReligion("attacker-religion-uid");

        _mockReligionManager
            .Setup(m => m.GetPlayerActiveDeity("attacker-uid"))
            .Returns(DeityType.Khoras);

        _mockReligionManager
            .Setup(m => m.GetPlayerActiveDeity("victim-uid"))
            .Returns(DeityType.Lysa);

        _mockReligionManager
            .Setup(m => m.GetPlayerReligion("attacker-uid"))
            .Returns(attackerReligion);
        _mockReligionManager
            .Setup(m => m.GetPlayerReligion("victim-uid"))
            .Returns((ReligionData?)null);

        var mockAttacker = new Mock<IServerPlayer>();
        mockAttacker.Setup(p => p.PlayerUID).Returns("attacker-uid");
        mockAttacker.Setup(p => p.PlayerName).Returns("Attacker");

        var mockVictim = new Mock<IServerPlayer>();
        mockVictim.Setup(p => p.PlayerUID).Returns("victim-uid");
        mockVictim.Setup(p => p.PlayerName).Returns("Victim");

        var khoras = new Deity(DeityType.Khoras, "Khoras", "War");
        _mockDeityRegistry.Setup(r => r.GetDeity(DeityType.Khoras)).Returns(khoras);

        // Act
        _pvpManager.ProcessPvPKill(mockAttacker.Object, mockVictim.Object);

        // Assert - Attacker should get rewards even if victim has no religion
        _mockPlayerReligionDataManager.Verify(
            m => m.AddFavor("attacker-uid", 10, It.IsAny<string>()),
            Times.Once()
        );

        _mockPrestigeManager.Verify(
            m => m.AddPrestige("attacker-religion-uid", 15, It.IsAny<string>()),
            Times.Once()
        );
    }

    [Fact]
    public void ProcessPvPKill_SendsNotificationToVictim()
    {
        // Arrange
        var attackerReligion = TestFixtures.CreateTestReligion("attacker-religion-uid");
        var victimReligion = TestFixtures.CreateTestReligion("victim-religion-uid");

        _mockReligionManager
            .Setup(m => m.GetPlayerActiveDeity("attacker-uid"))
            .Returns(DeityType.Khoras);

        _mockReligionManager
            .Setup(m => m.GetPlayerActiveDeity("victim-uid"))
            .Returns(DeityType.Lysa);

        _mockReligionManager
            .Setup(m => m.GetPlayerReligion("attacker-uid"))
            .Returns(attackerReligion);
        _mockReligionManager
            .Setup(m => m.GetPlayerReligion("victim-uid"))
            .Returns(victimReligion);

        var mockAttacker = new Mock<IServerPlayer>();
        mockAttacker.Setup(p => p.PlayerUID).Returns("attacker-uid");
        mockAttacker.Setup(p => p.PlayerName).Returns("Attacker");

        var mockVictim = new Mock<IServerPlayer>();
        mockVictim.Setup(p => p.PlayerUID).Returns("victim-uid");
        mockVictim.Setup(p => p.PlayerName).Returns("Victim");

        var khoras = new Deity(DeityType.Khoras, "Khoras", "War");
        var lysa = new Deity(DeityType.Lysa, "Lysa", "Hunt");
        _mockDeityRegistry.Setup(r => r.GetDeity(DeityType.Khoras)).Returns(khoras);
        _mockDeityRegistry.Setup(r => r.GetDeity(DeityType.Lysa)).Returns(lysa);

        // Act
        _pvpManager.ProcessPvPKill(mockAttacker.Object, mockVictim.Object);

        // Assert - Victim should receive notification
        mockVictim.Verify(
            p => p.SendMessage(
                It.Is<int>(g => g == GlobalConstants.GeneralChatGroup),
                It.Is<string>(s => s.Contains("Lysa") && s.Contains("displeased")),
                It.Is<EnumChatType>(t => t == EnumChatType.Notification),
                It.IsAny<string>()
            ),
            Times.Once()
        );
    }

    #endregion

    #region ProcessDeathPenalty Tests

    [Fact]
    public void ProcessDeathPenalty_WithSufficientFavor_RemovesPenalty()
    {
        // Arrange
        var playerData = TestFixtures.CreateTestPlayerReligionData("player-uid", DeityType.Khoras, "religion-uid");
        playerData.Favor = 100;

        _mockPlayerReligionDataManager
            .Setup(m => m.GetOrCreatePlayerData("player-uid"))
            .Returns(playerData);

        _mockPlayerReligionDataManager
            .Setup(m => m.GetPlayerDeityType("player-uid"))
            .Returns(DeityType.Khoras);

        _mockReligionManager
            .Setup(m => m.GetPlayerReligion("player-uid"))
            .Returns(TestFixtures.CreateTestReligion("religion-uid"));

        var mockPlayer = new Mock<IServerPlayer>();
        mockPlayer.Setup(p => p.PlayerUID).Returns("player-uid");

        // Act
        _pvpManager.ProcessDeathPenalty(mockPlayer.Object);

        // Assert - Should have removed 50 favor (100 - 50 = 50)
        Assert.Equal(50, playerData.Favor);

        // Should send notification
        mockPlayer.Verify(
            p => p.SendMessage(
                It.Is<int>(g => g == GlobalConstants.GeneralChatGroup),
                It.Is<string>(s => s.Contains("50 favor") && s.Contains("death")),
                It.Is<EnumChatType>(t => t == EnumChatType.Notification),
                It.IsAny<string>()
            ),
            Times.Once()
        );
    }

    [Fact]
    public void ProcessDeathPenalty_WithInsufficientFavor_RemovesOnlyAvailable()
    {
        // Arrange
        var playerData = TestFixtures.CreateTestPlayerReligionData("player-uid", DeityType.Khoras, "religion-uid");
        playerData.Favor = 30;

        _mockPlayerReligionDataManager
            .Setup(m => m.GetOrCreatePlayerData("player-uid"))
            .Returns(playerData);

        _mockPlayerReligionDataManager
            .Setup(m => m.GetPlayerDeityType("player-uid"))
            .Returns(DeityType.Khoras);

        _mockReligionManager
            .Setup(m => m.GetPlayerReligion("player-uid"))
            .Returns(TestFixtures.CreateTestReligion("religion-uid"));

        var mockPlayer = new Mock<IServerPlayer>();
        mockPlayer.Setup(p => p.PlayerUID).Returns("player-uid");

        // Act
        _pvpManager.ProcessDeathPenalty(mockPlayer.Object);

        // Assert - Should have removed only 30 favor (all available, since penalty is 50 but only 30 available)
        Assert.Equal(0, playerData.Favor);

        // Should send notification with 30 favor
        mockPlayer.Verify(
            p => p.SendMessage(
                It.Is<int>(g => g == GlobalConstants.GeneralChatGroup),
                It.Is<string>(s => s.Contains("30 favor")),
                It.Is<EnumChatType>(t => t == EnumChatType.Notification),
                It.IsAny<string>()
            ),
            Times.Once()
        );
    }

    [Fact]
    public void ProcessDeathPenalty_WithZeroFavor_DoesNotRemoveOrNotify()
    {
        // Arrange
        var playerData = TestFixtures.CreateTestPlayerReligionData("player-uid", DeityType.Khoras, "religion-uid");
        playerData.Favor = 0;

        _mockPlayerReligionDataManager
            .Setup(m => m.GetOrCreatePlayerData("player-uid"))
            .Returns(playerData);

        var mockPlayer = new Mock<IServerPlayer>();
        mockPlayer.Setup(p => p.PlayerUID).Returns("player-uid");

        _pvpManager.ProcessDeathPenalty(mockPlayer.Object);

        // Assert - Should not send any notification
        mockPlayer.Verify(
            p => p.SendMessage(
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<EnumChatType>(),
                It.IsAny<string>()
            ),
            Times.Never()
        );
    }

    [Fact]
    public void ProcessDeathPenalty_WithoutDeity_DoesNothing()
    {
        // Arrange
        var playerData = TestFixtures.CreateTestPlayerReligionData("player-uid", DeityType.None, null);
        playerData.Favor = 10;

        _mockPlayerReligionDataManager
            .Setup(m => m.GetOrCreatePlayerData("player-uid"))
            .Returns(playerData);

        var mockPlayer = new Mock<IServerPlayer>();
        mockPlayer.Setup(p => p.PlayerUID).Returns("player-uid");

        _pvpManager.ProcessDeathPenalty(mockPlayer.Object);

        // Assert - Favor should remain unchanged
        Assert.Equal(10, playerData.Favor);

        // Should not send notification
        mockPlayer.Verify(
            p => p.SendMessage(
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<EnumChatType>(),
                It.IsAny<string>()
            ),
            Times.Never()
        );
    }

    [Fact]
    public void ProcessDeathPenalty_WithoutReligion_DoesNothing()
    {
        // Arrange
        var playerData = TestFixtures.CreateTestPlayerReligionData("player-uid", DeityType.Khoras, null);
        playerData.Favor = 10;

        _mockPlayerReligionDataManager
            .Setup(m => m.GetOrCreatePlayerData("player-uid"))
            .Returns(playerData);

        var mockPlayer = new Mock<IServerPlayer>();
        mockPlayer.Setup(p => p.PlayerUID).Returns("player-uid");

        _pvpManager.ProcessDeathPenalty(mockPlayer.Object);

        // Assert - Favor should remain unchanged
        Assert.Equal(10, playerData.Favor);

        // Should not send notification
        mockPlayer.Verify(
            p => p.SendMessage(
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<EnumChatType>(),
                It.IsAny<string>()
            ),
            Times.Never()
        );
    }

    #endregion
}