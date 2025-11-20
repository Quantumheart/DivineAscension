using System.Diagnostics.CodeAnalysis;
using Moq;
using PantheonWars.Data;
using PantheonWars.Models;
using PantheonWars.Models.Enum;
using PantheonWars.Systems;
using PantheonWars.Systems.Interfaces;
using PantheonWars.Tests.Helpers;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

namespace PantheonWars.Tests.Systems;

/// <summary>
///     Unit tests for ReligionPrestigeManager
///     Tests prestige tracking, rank progression, and blessing unlocks
/// </summary>
[ExcludeFromCodeCoverage]
public class ReligionPrestigeManagerTests
{
    private readonly Mock<ICoreServerAPI> _mockAPI;
    private readonly Mock<ILogger> _mockLogger;
    private readonly Mock<IReligionManager> _mockReligionManager;
    private readonly ReligionPrestigeManager _prestigeManager;
    private readonly ReligionData _testReligion;

    public ReligionPrestigeManagerTests()
    {
        _mockAPI = TestFixtures.CreateMockServerAPI();
        _mockLogger = new Mock<ILogger>();
        _mockAPI.Setup(a => a.Logger).Returns(_mockLogger.Object);

        _mockReligionManager = new Mock<IReligionManager>();

        _testReligion = TestFixtures.CreateTestReligion(
            "test-religion-uid",
            "Test Religion",
            DeityType.Aethra,
            "founder-uid");

        _mockReligionManager
            .Setup(m => m.GetReligion("test-religion-uid"))
            .Returns(_testReligion);

        _prestigeManager = new ReligionPrestigeManager(_mockAPI.Object, _mockReligionManager.Object);
    }

    #region Initialization Tests

    [Fact]
    public void Initialize_LogsNotification()
    {
        // Act
        _prestigeManager.Initialize();

        // Assert
        _mockLogger.Verify(
            l => l.Notification(It.Is<string>(s => s.Contains("Initializing") && s.Contains("Religion Prestige"))),
            Times.Once()
        );
    }

    [Fact]
    public void SetBlessingSystems_SetsReferences()
    {
        // Arrange
        var mockBlessingRegistry = new Mock<BlessingRegistry>(_mockAPI.Object);
        var mockBlessingEffectSystem = new Mock<BlessingEffectSystem>(
            _mockAPI.Object,
            mockBlessingRegistry.Object,
            new Mock<IPlayerReligionDataManager>().Object,
            _mockReligionManager.Object);

        // Act
        _prestigeManager.SetBlessingSystems(mockBlessingRegistry.Object, mockBlessingEffectSystem.Object);
        _prestigeManager.Initialize();

        // Assert - Should not throw and should complete initialization
        _mockLogger.Verify(
            l => l.Notification(It.Is<string>(s => s.Contains("initialized"))),
            Times.Once()
        );
    }

    #endregion

    #region AddPrestige Tests

    [Fact]
    public void AddPrestige_IncreasesPrestigeAndTotalPrestige()
    {
        // Arrange
        var initialPrestige = _testReligion.Prestige;
        var initialTotal = _testReligion.TotalPrestige;

        // Act
        _prestigeManager.AddPrestige("test-religion-uid", 100, "Test reason");

        // Assert
        Assert.Equal(initialPrestige + 100, _testReligion.Prestige);
        Assert.Equal(initialTotal + 100, _testReligion.TotalPrestige);
    }

    [Fact]
    public void AddPrestige_WithReason_LogsDebugMessage()
    {
        // Act
        _prestigeManager.AddPrestige("test-religion-uid", 50, "PvP victory");

        // Assert
        _mockLogger.Verify(
            l => l.Debug(It.Is<string>(s =>
                s.Contains("Test Religion") &&
                s.Contains("50") &&
                s.Contains("prestige") &&
                s.Contains("PvP victory"))),
            Times.Once()
        );
    }

    [Fact]
    public void AddPrestige_WithNonExistentReligion_LogsError()
    {
        // Arrange
        _mockReligionManager.Setup(m => m.GetReligion("invalid-uid")).Returns((ReligionData)null!);

        // Act
        _prestigeManager.AddPrestige("invalid-uid", 100);

        // Assert
        _mockLogger.Verify(
            l => l.Error(It.Is<string>(s => s.Contains("Cannot add prestige"))),
            Times.Once()
        );
    }

    [Fact]
    public void AddPrestige_ThatCausesRankUp_SendsNotification()
    {
        // Arrange
        _testReligion.TotalPrestige = 450;
        _testReligion.PrestigeRank = PrestigeRank.Fledgling;

        var mockWorld = new Mock<IServerWorldAccessor>();
        _mockAPI.Setup(a => a.World).Returns(mockWorld.Object);

        // Act - Add 100 prestige to reach 550 (Established threshold is 500)
        _prestigeManager.AddPrestige("test-religion-uid", 100);

        // Assert
        Assert.Equal(PrestigeRank.Established, _testReligion.PrestigeRank);
    }

    #endregion

    #region UpdatePrestigeRank Tests

    [Fact]
    public void UpdatePrestigeRank_At500TotalPrestige_RanksToEstablished()
    {
        // Arrange
        _testReligion.TotalPrestige = 500;
        _testReligion.PrestigeRank = PrestigeRank.Fledgling;

        // Act
        _prestigeManager.UpdatePrestigeRank("test-religion-uid");

        // Assert
        Assert.Equal(PrestigeRank.Established, _testReligion.PrestigeRank);
    }

    [Fact]
    public void UpdatePrestigeRank_At2000TotalPrestige_RanksToRenowned()
    {
        // Arrange
        _testReligion.TotalPrestige = 2000;
        _testReligion.PrestigeRank = PrestigeRank.Fledgling;

        // Act
        _prestigeManager.UpdatePrestigeRank("test-religion-uid");

        // Assert
        Assert.Equal(PrestigeRank.Renowned, _testReligion.PrestigeRank);
    }

    [Fact]
    public void UpdatePrestigeRank_At5000TotalPrestige_RanksToLegendary()
    {
        // Arrange
        _testReligion.TotalPrestige = 5000;
        _testReligion.PrestigeRank = PrestigeRank.Fledgling;

        // Act
        _prestigeManager.UpdatePrestigeRank("test-religion-uid");

        // Assert
        Assert.Equal(PrestigeRank.Legendary, _testReligion.PrestigeRank);
    }

    [Fact]
    public void UpdatePrestigeRank_At10000TotalPrestige_RanksToMythic()
    {
        // Arrange
        _testReligion.TotalPrestige = 10000;
        _testReligion.PrestigeRank = PrestigeRank.Fledgling;

        // Act
        _prestigeManager.UpdatePrestigeRank("test-religion-uid");

        // Assert
        Assert.Equal(PrestigeRank.Mythic, _testReligion.PrestigeRank);
    }

    [Fact]
    public void UpdatePrestigeRank_WithNoChange_DoesNotLogNotification()
    {
        // Arrange
        _testReligion.TotalPrestige = 100;
        _testReligion.PrestigeRank = PrestigeRank.Fledgling;
        _mockLogger.Reset();

        // Act
        _prestigeManager.UpdatePrestigeRank("test-religion-uid");

        // Assert - Should not log rank change notification
        _mockLogger.Verify(
            l => l.Notification(It.Is<string>(s => s.Contains("rank changed"))),
            Times.Never()
        );
    }

    [Fact]
    public void UpdatePrestigeRank_WithRankChange_LogsNotification()
    {
        // Arrange
        _testReligion.TotalPrestige = 600;
        _testReligion.PrestigeRank = PrestigeRank.Fledgling;

        // Act
        _prestigeManager.UpdatePrestigeRank("test-religion-uid");

        // Assert
        _mockLogger.Verify(
            l => l.Notification(It.Is<string>(s =>
                s.Contains("Test Religion") &&
                s.Contains("rank changed") &&
                s.Contains("Fledgling") &&
                s.Contains("Established"))),
            Times.Once()
        );
    }

    [Fact]
    public void UpdatePrestigeRank_WithInvalidReligion_LogsError()
    {
        // Arrange
        _mockReligionManager.Setup(m => m.GetReligion("invalid-uid")).Returns((ReligionData)null!);

        // Act
        _prestigeManager.UpdatePrestigeRank("invalid-uid");

        // Assert
        _mockLogger.Verify(
            l => l.Error(It.Is<string>(s => s.Contains("Cannot update prestige rank"))),
            Times.Once()
        );
    }

    #endregion

    #region UnlockReligionBlessing Tests

    [Fact]
    public void UnlockReligionBlessing_WithValidBlessing_ReturnsTrue()
    {
        // Arrange
        var blessingId = "khoras_blessing_1";

        // Act
        var result = _prestigeManager.UnlockReligionBlessing("test-religion-uid", blessingId);

        // Assert
        Assert.True(result);
        Assert.True(_testReligion.UnlockedBlessings[blessingId]);
    }

    [Fact]
    public void UnlockReligionBlessing_AlreadyUnlocked_ReturnsFalse()
    {
        // Arrange
        var blessingId = "khoras_blessing_1";
        _testReligion.UnlockedBlessings[blessingId] = true;

        // Act
        var result = _prestigeManager.UnlockReligionBlessing("test-religion-uid", blessingId);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void UnlockReligionBlessing_WithInvalidReligion_ReturnsFalse()
    {
        // Arrange
        _mockReligionManager.Setup(m => m.GetReligion("invalid-uid")).Returns((ReligionData)null!);

        // Act
        var result = _prestigeManager.UnlockReligionBlessing("invalid-uid", "blessing_id");

        // Assert
        Assert.False(result);
        _mockLogger.Verify(
            l => l.Error(It.Is<string>(s => s.Contains("Cannot unlock blessing"))),
            Times.Once()
        );
    }

    [Fact]
    public void UnlockReligionBlessing_LogsNotification()
    {
        // Arrange
        var blessingId = "khoras_blessing_test";

        // Act
        _prestigeManager.UnlockReligionBlessing("test-religion-uid", blessingId);

        // Assert
        _mockLogger.Verify(
            l => l.Notification(It.Is<string>(s =>
                s.Contains("Test Religion") &&
                s.Contains("unlocked blessing") &&
                s.Contains(blessingId))),
            Times.Once()
        );
    }

    #endregion

    #region GetActiveReligionBlessings Tests

    [Fact]
    public void GetActiveReligionBlessings_WithNoUnlockedBlessings_ReturnsEmptyList()
    {
        // Act
        var blessings = _prestigeManager.GetActiveReligionBlessings("test-religion-uid");

        // Assert
        Assert.Empty(blessings);
    }

    [Fact]
    public void GetActiveReligionBlessings_WithUnlockedBlessings_ReturnsCorrectList()
    {
        // Arrange
        _testReligion.UnlockedBlessings["blessing1"] = true;
        _testReligion.UnlockedBlessings["blessing2"] = true;
        _testReligion.UnlockedBlessings["blessing3"] = false; // Not unlocked

        // Act
        var blessings = _prestigeManager.GetActiveReligionBlessings("test-religion-uid");

        // Assert
        Assert.Equal(2, blessings.Count);
        Assert.Contains("blessing1", blessings);
        Assert.Contains("blessing2", blessings);
        Assert.DoesNotContain("blessing3", blessings);
    }

    [Fact]
    public void GetActiveReligionBlessings_WithInvalidReligion_ReturnsEmptyList()
    {
        // Arrange
        _mockReligionManager.Setup(m => m.GetReligion("invalid-uid")).Returns((ReligionData)null!);

        // Act
        var blessings = _prestigeManager.GetActiveReligionBlessings("invalid-uid");

        // Assert
        Assert.Empty(blessings);
    }

    #endregion

    #region GetPrestigeProgress Tests

    [Fact]
    public void GetPrestigeProgress_AtFledglingRank_ReturnsCorrectProgress()
    {
        // Arrange
        _testReligion.TotalPrestige = 250;
        _testReligion.PrestigeRank = PrestigeRank.Fledgling;

        // Act
        var (current, nextThreshold, nextRank) = _prestigeManager.GetPrestigeProgress("test-religion-uid");

        // Assert
        Assert.Equal(250, current);
        Assert.Equal(500, nextThreshold); // ESTABLISHED_THRESHOLD
        Assert.Equal(PrestigeRank.Established, nextRank);
    }

    [Fact]
    public void GetPrestigeProgress_AtEstablishedRank_ReturnsCorrectProgress()
    {
        // Arrange
        _testReligion.TotalPrestige = 1000;
        _testReligion.PrestigeRank = PrestigeRank.Established;

        // Act
        var (current, nextThreshold, nextRank) = _prestigeManager.GetPrestigeProgress("test-religion-uid");

        // Assert
        Assert.Equal(1000, current);
        Assert.Equal(2000, nextThreshold); // RENOWNED_THRESHOLD
        Assert.Equal(PrestigeRank.Renowned, nextRank);
    }

    [Fact]
    public void GetPrestigeProgress_AtRenownedRank_ReturnsCorrectProgress()
    {
        // Arrange
        _testReligion.TotalPrestige = 3000;
        _testReligion.PrestigeRank = PrestigeRank.Renowned;

        // Act
        var (current, nextThreshold, nextRank) = _prestigeManager.GetPrestigeProgress("test-religion-uid");

        // Assert
        Assert.Equal(3000, current);
        Assert.Equal(5000, nextThreshold); // LEGENDARY_THRESHOLD
        Assert.Equal(PrestigeRank.Legendary, nextRank);
    }

    [Fact]
    public void GetPrestigeProgress_AtLegendaryRank_ReturnsCorrectProgress()
    {
        // Arrange
        _testReligion.TotalPrestige = 7000;
        _testReligion.PrestigeRank = PrestigeRank.Legendary;

        // Act
        var (current, nextThreshold, nextRank) = _prestigeManager.GetPrestigeProgress("test-religion-uid");

        // Assert
        Assert.Equal(7000, current);
        Assert.Equal(10000, nextThreshold); // MYTHIC_THRESHOLD
        Assert.Equal(PrestigeRank.Mythic, nextRank);
    }

    [Fact]
    public void GetPrestigeProgress_AtMythicRank_ReturnsMaxRank()
    {
        // Arrange
        _testReligion.TotalPrestige = 15000;
        _testReligion.PrestigeRank = PrestigeRank.Mythic;

        // Act
        var (current, nextThreshold, nextRank) = _prestigeManager.GetPrestigeProgress("test-religion-uid");

        // Assert
        Assert.Equal(15000, current);
        Assert.Equal(10000, nextThreshold); // Max rank, no higher threshold
        Assert.Equal(PrestigeRank.Mythic, nextRank); // Still Mythic
    }

    [Fact]
    public void GetPrestigeProgress_WithInvalidReligion_ReturnsDefaults()
    {
        // Arrange
        _mockReligionManager.Setup(m => m.GetReligion("invalid-uid")).Returns((ReligionData)null!);

        // Act
        var (current, nextThreshold, nextRank) = _prestigeManager.GetPrestigeProgress("invalid-uid");

        // Assert
        Assert.Equal(0, current);
        Assert.Equal(0, nextThreshold);
        Assert.Equal(PrestigeRank.Fledgling, nextRank);
    }

    #endregion

    #region Notification Tests

    [Fact]
    public void AddPrestige_ThatCausesRankUp_NotifiesMembers()
    {
        // Arrange
        _testReligion.TotalPrestige = 450;
        _testReligion.PrestigeRank = PrestigeRank.Fledgling;
        _testReligion.AddMember("member-1");
        _testReligion.AddMember("member-2");

        var mockWorld = new Mock<IServerWorldAccessor>();
        var mockPlayer1 = new Mock<IServerPlayer>();
        var mockPlayer2 = new Mock<IServerPlayer>();

        mockWorld.Setup(w => w.PlayerByUid("member-1")).Returns(mockPlayer1.Object);
        mockWorld.Setup(w => w.PlayerByUid("member-2")).Returns(mockPlayer2.Object);
        _mockAPI.Setup(a => a.World).Returns(mockWorld.Object);

        // Act - Add prestige to trigger rank-up
        _prestigeManager.AddPrestige("test-religion-uid", 100);

        // Assert - Both members should receive notifications
        mockPlayer1.Verify(
            p => p.SendMessage(
                It.Is<int>(g => g == GlobalConstants.GeneralChatGroup),
                It.Is<string>(s => s.Contains("ascended") && s.Contains("Established")),
                It.Is<EnumChatType>(t => t == EnumChatType.Notification),
                It.IsAny<string>()
            ),
            Times.Once()
        );

        mockPlayer2.Verify(
            p => p.SendMessage(
                It.Is<int>(g => g == GlobalConstants.GeneralChatGroup),
                It.Is<string>(s => s.Contains("ascended") && s.Contains("Established")),
                It.Is<EnumChatType>(t => t == EnumChatType.Notification),
                It.IsAny<string>()
            ),
            Times.Once()
        );
    }

    [Fact]
    public void AddPrestige_WithoutRankUp_DoesNotNotifyMembers()
    {
        // Arrange
        _testReligion.TotalPrestige = 100;
        _testReligion.PrestigeRank = PrestigeRank.Fledgling;
        _testReligion.AddMember("member-1");

        var mockWorld = new Mock<IServerWorldAccessor>();
        var mockPlayer = new Mock<IServerPlayer>();

        mockWorld.Setup(w => w.PlayerByUid("member-1")).Returns(mockPlayer.Object);
        _mockAPI.Setup(a => a.World).Returns(mockWorld.Object);

        // Act - Add prestige but don't rank up
        _prestigeManager.AddPrestige("test-religion-uid", 50);

        // Assert - No notification should be sent
        mockPlayer.Verify(
            p => p.SendMessage(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<EnumChatType>(), It.IsAny<string>()),
            Times.Never()
        );
    }

    [Fact]
    public void AddPrestige_WithNullPlayer_DoesNotCrash()
    {
        // Arrange
        _testReligion.TotalPrestige = 450;
        _testReligion.PrestigeRank = PrestigeRank.Fledgling;
        _testReligion.AddMember("offline-player");

        var mockWorld = new Mock<IServerWorldAccessor>();
        mockWorld.Setup(w => w.PlayerByUid("offline-player")).Returns((IServerPlayer)null!);
        _mockAPI.Setup(a => a.World).Returns(mockWorld.Object);

        // Act & Assert - Should not throw
        _prestigeManager.AddPrestige("test-religion-uid", 100);
        Assert.Equal(PrestigeRank.Established, _testReligion.PrestigeRank);
    }

    #endregion

    #region Blessing Unlock Notification Tests

    [Fact]
    public void UpdatePrestigeRank_WithNewBlessingsAvailable_NotifiesMembers()
    {
        // Arrange
        var mockBlessingRegistry = new Mock<IBlessingRegistry>();
        var mockBlessingEffectSystem = new Mock<IBlessingEffectSystem>();

        var blessing = new Blessing
        {
            BlessingId = "test_blessing",
            Name = "Test Blessing",
            Deity = DeityType.Aethra,
            Kind = BlessingKind.Religion,
            RequiredPrestigeRank = 1, // Established
            PrerequisiteBlessings = new List<string>()
        };

        mockBlessingRegistry
            .Setup(r => r.GetBlessingsForDeity(DeityType.Aethra, BlessingKind.Religion))
            .Returns(new List<Blessing> { blessing });

        _prestigeManager.SetBlessingSystems(mockBlessingRegistry.Object, mockBlessingEffectSystem.Object);

        _testReligion.TotalPrestige = 500;
        _testReligion.PrestigeRank = PrestigeRank.Fledgling;
        _testReligion.AddMember("member-1");

        var mockWorld = new Mock<IServerWorldAccessor>();
        var mockPlayer = new Mock<IServerPlayer>();
        mockWorld.Setup(w => w.PlayerByUid("member-1")).Returns(mockPlayer.Object);
        _mockAPI.Setup(a => a.World).Returns(mockWorld.Object);

        // Act
        _prestigeManager.UpdatePrestigeRank("test-religion-uid");

        // Assert - Player should be notified about new blessings
        mockPlayer.Verify(
            p => p.SendMessage(
                It.Is<int>(g => g == GlobalConstants.GeneralChatGroup),
                It.Is<string>(s => s.Contains("New blessings available") && s.Contains("Test Blessing")),
                It.Is<EnumChatType>(t => t == EnumChatType.Notification),
                It.IsAny<string>()
            ),
            Times.Once()
        );
    }

    [Fact]
    public void UpdatePrestigeRank_WithoutBlessingRegistry_SkipsUnlockCheck()
    {
        // Arrange - Don't set blessing systems
        _testReligion.TotalPrestige = 500;
        _testReligion.PrestigeRank = PrestigeRank.Fledgling;

        // Act
        _prestigeManager.UpdatePrestigeRank("test-religion-uid");

        // Assert - Should log debug message about skipping
        _mockLogger.Verify(
            l => l.Debug(It.Is<string>(s => s.Contains("not yet initialized"))),
            Times.Once()
        );
    }

    [Fact]
    public void UpdatePrestigeRank_WithNoNewBlessings_LogsDebug()
    {
        // Arrange
        var mockBlessingRegistry = new Mock<IBlessingRegistry>();
        var mockBlessingEffectSystem = new Mock<IBlessingEffectSystem>();

        // Return empty blessing list
        mockBlessingRegistry
            .Setup(r => r.GetBlessingsForDeity(DeityType.Aethra, BlessingKind.Religion))
            .Returns(new List<Blessing>());

        _prestigeManager.SetBlessingSystems(mockBlessingRegistry.Object, mockBlessingEffectSystem.Object);

        _testReligion.TotalPrestige = 500;
        _testReligion.PrestigeRank = PrestigeRank.Fledgling;

        // Act
        _prestigeManager.UpdatePrestigeRank("test-religion-uid");

        // Assert
        _mockLogger.Verify(
            l => l.Debug(It.Is<string>(s => s.Contains("No new blessings available"))),
            Times.Once()
        );
    }

    [Fact]
    public void UpdatePrestigeRank_WithPrerequisiteNotMet_DoesNotUnlockBlessing()
    {
        // Arrange
        var mockBlessingRegistry = new Mock<IBlessingRegistry>();
        var mockBlessingEffectSystem = new Mock<IBlessingEffectSystem>();

        var blessing = new Blessing
        {
            BlessingId = "test_blessing_2",
            Name = "Test Blessing 2",
            Deity = DeityType.Aethra,
            Kind = BlessingKind.Religion,
            RequiredPrestigeRank = 1,
            PrerequisiteBlessings = new List<string> { "prerequisite_blessing" } // Has prerequisite
        };

        mockBlessingRegistry
            .Setup(r => r.GetBlessingsForDeity(DeityType.Aethra, BlessingKind.Religion))
            .Returns(new List<Blessing> { blessing });

        _prestigeManager.SetBlessingSystems(mockBlessingRegistry.Object, mockBlessingEffectSystem.Object);

        _testReligion.TotalPrestige = 500;
        _testReligion.PrestigeRank = PrestigeRank.Fledgling;

        // Act
        _prestigeManager.UpdatePrestigeRank("test-religion-uid");

        // Assert - Should not notify about this blessing since prerequisite isn't met
        _mockLogger.Verify(
            l => l.Debug(It.Is<string>(s => s.Contains("No new blessings available"))),
            Times.Once()
        );
    }

    #endregion

    #region Blessing Effect Refresh Tests

    [Fact]
    public void UnlockReligionBlessing_TriggersEffectRefresh()
    {
        // Arrange
        var mockBlessingRegistry = new Mock<IBlessingRegistry>();
        var mockBlessingEffectSystem = new Mock<IBlessingEffectSystem>();

        _prestigeManager.SetBlessingSystems(mockBlessingRegistry.Object, mockBlessingEffectSystem.Object);

        // Act
        _prestigeManager.UnlockReligionBlessing("test-religion-uid", "new_blessing");

        // Assert
        mockBlessingEffectSystem.Verify(
            s => s.RefreshReligionBlessings("test-religion-uid"),
            Times.Once()
        );
    }

    [Fact]
    public void UnlockReligionBlessing_WithoutBlessingEffectSystem_LogsDebug()
    {
        // Arrange - Don't set blessing systems

        // Act
        _prestigeManager.UnlockReligionBlessing("test-religion-uid", "new_blessing");

        // Assert
        _mockLogger.Verify(
            l => l.Debug(It.Is<string>(s => s.Contains("Blessing effect system not yet initialized"))),
            Times.Once()
        );
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void FullProgressionPath_FromFledglingToMythic_WorksCorrectly()
    {
        // Arrange
        _testReligion.TotalPrestige = 0;
        _testReligion.PrestigeRank = PrestigeRank.Fledgling;

        // Act & Assert - Progress through all ranks
        _prestigeManager.AddPrestige("test-religion-uid", 500); // -> Established
        Assert.Equal(PrestigeRank.Established, _testReligion.PrestigeRank);

        _prestigeManager.AddPrestige("test-religion-uid", 1500); // -> Renowned
        Assert.Equal(PrestigeRank.Renowned, _testReligion.PrestigeRank);

        _prestigeManager.AddPrestige("test-religion-uid", 3000); // -> Legendary
        Assert.Equal(PrestigeRank.Legendary, _testReligion.PrestigeRank);

        _prestigeManager.AddPrestige("test-religion-uid", 5000); // -> Mythic
        Assert.Equal(PrestigeRank.Mythic, _testReligion.PrestigeRank);

        Assert.Equal(10000, _testReligion.TotalPrestige);
    }

    #endregion
}
