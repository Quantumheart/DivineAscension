using System.Diagnostics.CodeAnalysis;
using DivineAscension.Data;
using DivineAscension.Models;
using DivineAscension.Models.Enum;
using DivineAscension.Services;
using DivineAscension.Services.Interfaces;
using DivineAscension.Systems;
using DivineAscension.Systems.Interfaces;
using Moq;

namespace DivineAscension.Tests.Systems;

/// <summary>
///     Unit tests for CivilizationMilestoneManager
///     Tests milestone detection, progression, and bonus computation
/// </summary>
[ExcludeFromCodeCoverage]
public class CivilizationMilestoneManagerTests
{
    private readonly CivilizationMilestoneManager _milestoneManager;
    private readonly Mock<ILoggerWrapper> _mockLogger;
    private readonly Mock<ICivilizationManager> _mockCivilizationManager;
    private readonly Mock<IReligionManager> _mockReligionManager;
    private readonly Mock<IHolySiteManager> _mockHolySiteManager;
    private readonly Mock<IReligionPrestigeManager> _mockPrestigeManager;
    private readonly Mock<IMilestoneDefinitionLoader> _mockMilestoneLoader;
    private readonly Mock<IRitualProgressManager> _mockRitualProgressManager;
    private readonly Mock<IPvPManager> _mockPvPManager;

    public CivilizationMilestoneManagerTests()
    {
        _mockLogger = new Mock<ILoggerWrapper>();
        _mockCivilizationManager = new Mock<ICivilizationManager>();
        _mockReligionManager = new Mock<IReligionManager>();
        _mockHolySiteManager = new Mock<IHolySiteManager>();
        _mockPrestigeManager = new Mock<IReligionPrestigeManager>();
        _mockMilestoneLoader = new Mock<IMilestoneDefinitionLoader>();
        _mockRitualProgressManager = new Mock<IRitualProgressManager>();
        _mockPvPManager = new Mock<IPvPManager>();

        _milestoneManager = new CivilizationMilestoneManager(
            _mockLogger.Object,
            _mockCivilizationManager.Object,
            _mockReligionManager.Object,
            _mockHolySiteManager.Object,
            _mockPrestigeManager.Object,
            _mockMilestoneLoader.Object);
    }

    #region Initialization Tests

    [Fact]
    public void Initialize_LogsNotification()
    {
        // Act
        _milestoneManager.Initialize();

        // Assert
        _mockLogger.Verify(
            l => l.Notification(It.Is<string>(s => s.Contains("Initializing") && s.Contains("Milestone Manager"))),
            Times.Once());
    }

    [Fact]
    public void SetRitualProgressManager_SubscribesToEvents()
    {
        // Act
        _milestoneManager.SetRitualProgressManager(_mockRitualProgressManager.Object);

        // Assert - verify the subscription happened (can't directly verify events, but can verify no exception)
        _mockLogger.Verify(
            l => l.Debug(It.Is<string>(s => s.Contains("Subscribed to ritual completion events"))),
            Times.Once());
    }

    [Fact]
    public void SetPvPManager_SubscribesToEvents()
    {
        // Act
        _milestoneManager.SetPvPManager(_mockPvPManager.Object);

        // Assert
        _mockLogger.Verify(
            l => l.Debug(It.Is<string>(s => s.Contains("Subscribed to war kill events"))),
            Times.Once());
    }

    #endregion

    #region IsMilestoneCompleted Tests

    [Fact]
    public void IsMilestoneCompleted_CompletedMilestone_ReturnsTrue()
    {
        // Arrange
        var civId = "test-civ-1";
        var milestoneId = "first_alliance";
        var civ = CreateTestCivilization(civId);
        civ.CompletedMilestones.Add(milestoneId);

        _mockCivilizationManager.Setup(c => c.GetCivilization(civId)).Returns(civ);

        // Act
        var result = _milestoneManager.IsMilestoneCompleted(civId, milestoneId);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsMilestoneCompleted_NotCompletedMilestone_ReturnsFalse()
    {
        // Arrange
        var civId = "test-civ-1";
        var milestoneId = "first_alliance";
        var civ = CreateTestCivilization(civId);

        _mockCivilizationManager.Setup(c => c.GetCivilization(civId)).Returns(civ);

        // Act
        var result = _milestoneManager.IsMilestoneCompleted(civId, milestoneId);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsMilestoneCompleted_NonExistentCiv_ReturnsFalse()
    {
        // Arrange
        _mockCivilizationManager.Setup(c => c.GetCivilization(It.IsAny<string>())).Returns((Civilization?)null);

        // Act
        var result = _milestoneManager.IsMilestoneCompleted("invalid-civ", "first_alliance");

        // Assert
        Assert.False(result);
    }

    #endregion

    #region GetCivilizationRank Tests

    [Fact]
    public void GetCivilizationRank_ValidCiv_ReturnsRank()
    {
        // Arrange
        var civId = "test-civ-1";
        var civ = CreateTestCivilization(civId);
        civ.Rank = CivilizationRank.Hegemonic;

        _mockCivilizationManager.Setup(c => c.GetCivilization(civId)).Returns(civ);

        // Act
        var result = _milestoneManager.GetCivilizationRank(civId);

        // Assert
        Assert.Equal(3, result);
    }

    [Fact]
    public void GetCivilizationRank_NonExistentCiv_ReturnsZero()
    {
        // Arrange
        _mockCivilizationManager.Setup(c => c.GetCivilization(It.IsAny<string>())).Returns((Civilization?)null);

        // Act
        var result = _milestoneManager.GetCivilizationRank("invalid-civ");

        // Assert
        Assert.Equal(0, result);
    }

    #endregion

    #region GetCompletedMilestones Tests

    [Fact]
    public void GetCompletedMilestones_ReturnsSetCopy()
    {
        // Arrange
        var civId = "test-civ-1";
        var civ = CreateTestCivilization(civId);
        civ.CompletedMilestones.Add("milestone1");
        civ.CompletedMilestones.Add("milestone2");

        _mockCivilizationManager.Setup(c => c.GetCivilization(civId)).Returns(civ);

        // Act
        var result = _milestoneManager.GetCompletedMilestones(civId);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains("milestone1", result);
        Assert.Contains("milestone2", result);
    }

    [Fact]
    public void GetCompletedMilestones_NonExistentCiv_ReturnsEmptySet()
    {
        // Arrange
        _mockCivilizationManager.Setup(c => c.GetCivilization(It.IsAny<string>())).Returns((Civilization?)null);

        // Act
        var result = _milestoneManager.GetCompletedMilestones("invalid-civ");

        // Assert
        Assert.Empty(result);
    }

    #endregion

    #region GetActiveBonuses Tests

    [Fact]
    public void GetActiveBonuses_NoCompletedMilestones_ReturnsDefaultBonuses()
    {
        // Arrange
        var civId = "test-civ-1";
        var civ = CreateTestCivilization(civId);

        _mockCivilizationManager.Setup(c => c.GetCivilization(civId)).Returns(civ);

        // Act
        var result = _milestoneManager.GetActiveBonuses(civId);

        // Assert
        Assert.Equal(1.0f, result.PrestigeMultiplier);
        Assert.Equal(1.0f, result.FavorMultiplier);
        Assert.Equal(1.0f, result.ConquestMultiplier);
        Assert.Equal(0, result.BonusHolySiteSlots);
    }

    [Fact]
    public void GetActiveBonuses_WithMilestones_ComputesBonusesCorrectly()
    {
        // Arrange
        var civId = "test-civ-1";
        var civ = CreateTestCivilization(civId);
        civ.CompletedMilestones.Add("prestige_boost");

        var milestone = CreateTestMilestone("prestige_boost", MilestoneBenefitType.PrestigeMultiplier, 0.1f);

        _mockCivilizationManager.Setup(c => c.GetCivilization(civId)).Returns(civ);
        _mockMilestoneLoader.Setup(m => m.GetMilestone("prestige_boost")).Returns(milestone);

        // Act
        var result = _milestoneManager.GetActiveBonuses(civId);

        // Assert
        Assert.Equal(1.1f, result.PrestigeMultiplier);
    }

    [Fact]
    public void GetActiveBonuses_MultipleMilestones_StacksAdditively()
    {
        // Arrange
        var civId = "test-civ-1";
        var civ = CreateTestCivilization(civId);
        civ.CompletedMilestones.Add("milestone1");
        civ.CompletedMilestones.Add("milestone2");

        var milestone1 = CreateTestMilestone("milestone1", MilestoneBenefitType.PrestigeMultiplier, 0.1f);
        var milestone2 = CreateTestMilestone("milestone2", MilestoneBenefitType.PrestigeMultiplier, 0.15f);

        _mockCivilizationManager.Setup(c => c.GetCivilization(civId)).Returns(civ);
        _mockMilestoneLoader.Setup(m => m.GetMilestone("milestone1")).Returns(milestone1);
        _mockMilestoneLoader.Setup(m => m.GetMilestone("milestone2")).Returns(milestone2);

        // Act
        var result = _milestoneManager.GetActiveBonuses(civId);

        // Assert - 1.0 + 0.1 + 0.15 = 1.25
        Assert.Equal(1.25f, result.PrestigeMultiplier);
    }

    [Fact]
    public void GetActiveBonuses_CachesBonuses()
    {
        // Arrange
        var civId = "test-civ-1";
        var civ = CreateTestCivilization(civId);

        _mockCivilizationManager.Setup(c => c.GetCivilization(civId)).Returns(civ);

        // Act - call twice
        _milestoneManager.GetActiveBonuses(civId);
        _milestoneManager.GetActiveBonuses(civId);

        // Assert - GetCivilization should only be called once due to caching
        _mockCivilizationManager.Verify(c => c.GetCivilization(civId), Times.Once());
    }

    #endregion

    #region CheckMilestones Tests

    [Fact]
    public void CheckMilestones_TriggerMet_UnlocksMilestone()
    {
        // Arrange
        var civId = "test-civ-1";
        var civ = CreateTestCivilization(civId);
        civ.AddReligion("religion-2");

        var milestone = new MilestoneDefinition(
            "first_alliance",
            "First Alliance",
            "Form your first alliance",
            MilestoneType.Major,
            new MilestoneTrigger(MilestoneTriggerType.ReligionCount, 2),
            1,
            500);

        _mockCivilizationManager.Setup(c => c.GetCivilization(civId)).Returns(civ);
        _mockMilestoneLoader.Setup(m => m.GetAllMilestones()).Returns(new List<MilestoneDefinition> { milestone });

        var unlocked = false;
        _milestoneManager.OnMilestoneUnlocked += (_, _) => unlocked = true;

        // Act
        _milestoneManager.CheckMilestones(civId);

        // Assert
        Assert.True(unlocked);
        Assert.Contains("first_alliance", civ.CompletedMilestones);
        Assert.Equal(CivilizationRank.Rising, civ.Rank);
    }

    [Fact]
    public void CheckMilestones_TriggerNotMet_DoesNotUnlock()
    {
        // Arrange
        var civId = "test-civ-1";
        var civ = CreateTestCivilization(civId);

        var milestone = new MilestoneDefinition(
            "first_alliance",
            "First Alliance",
            "Form your first alliance",
            MilestoneType.Major,
            new MilestoneTrigger(MilestoneTriggerType.ReligionCount, 2),
            1,
            0);

        _mockCivilizationManager.Setup(c => c.GetCivilization(civId)).Returns(civ);
        _mockMilestoneLoader.Setup(m => m.GetAllMilestones()).Returns(new List<MilestoneDefinition> { milestone });

        // Act
        _milestoneManager.CheckMilestones(civId);

        // Assert
        Assert.Empty(civ.CompletedMilestones);
        Assert.Equal(CivilizationRank.Nascent, civ.Rank);
    }

    [Fact]
    public void CheckMilestones_AlreadyCompleted_SkipsMilestone()
    {
        // Arrange
        var civId = "test-civ-1";
        var civ = CreateTestCivilization(civId);
        civ.AddReligion("religion-2");
        civ.CompletedMilestones.Add("first_alliance");
        civ.Rank = CivilizationRank.Rising;

        var milestone = new MilestoneDefinition(
            "first_alliance",
            "First Alliance",
            "Form your first alliance",
            MilestoneType.Major,
            new MilestoneTrigger(MilestoneTriggerType.ReligionCount, 2),
            1,
            0);

        _mockCivilizationManager.Setup(c => c.GetCivilization(civId)).Returns(civ);
        _mockMilestoneLoader.Setup(m => m.GetAllMilestones()).Returns(new List<MilestoneDefinition> { milestone });

        // Act
        _milestoneManager.CheckMilestones(civId);

        // Assert - Rank should still be Rising, not Dominant
        Assert.Equal(CivilizationRank.Rising, civ.Rank);
    }

    [Fact]
    public void CheckMilestones_MajorMilestone_AwardsPrestige()
    {
        // Arrange
        var civId = "test-civ-1";
        var civ = CreateTestCivilization(civId);
        civ.AddReligion("religion-2");

        var milestone = new MilestoneDefinition(
            "first_alliance",
            "First Alliance",
            "Form your first alliance",
            MilestoneType.Major,
            new MilestoneTrigger(MilestoneTriggerType.ReligionCount, 2),
            1,
            500);

        _mockCivilizationManager.Setup(c => c.GetCivilization(civId)).Returns(civ);
        _mockMilestoneLoader.Setup(m => m.GetAllMilestones()).Returns(new List<MilestoneDefinition> { milestone });

        // Act
        _milestoneManager.CheckMilestones(civId);

        // Assert
        _mockPrestigeManager.Verify(p => p.AddPrestige("founder-religion", 500, It.IsAny<string>()), Times.Once());
    }

    #endregion

    #region RecordWarKill Tests

    [Fact]
    public void RecordWarKill_IncrementsCount()
    {
        // Arrange
        var civId = "test-civ-1";
        var civ = CreateTestCivilization(civId);
        civ.WarKillCount = 5;

        _mockCivilizationManager.Setup(c => c.GetCivilization(civId)).Returns(civ);
        _mockMilestoneLoader.Setup(m => m.GetAllMilestones()).Returns(new List<MilestoneDefinition>());

        // Act
        _milestoneManager.RecordWarKill(civId);

        // Assert
        Assert.Equal(6, civ.WarKillCount);
    }

    [Fact]
    public void RecordWarKill_TriggersCheckMilestones()
    {
        // Arrange
        var civId = "test-civ-1";
        var civ = CreateTestCivilization(civId);
        civ.WarKillCount = 49; // One more kill will trigger the 50 threshold

        var milestone = new MilestoneDefinition(
            "war_heroes",
            "War Heroes",
            "Achieve 50 war kills",
            MilestoneType.Major,
            new MilestoneTrigger(MilestoneTriggerType.WarKillCount, 50),
            1,
            0);

        _mockCivilizationManager.Setup(c => c.GetCivilization(civId)).Returns(civ);
        _mockMilestoneLoader.Setup(m => m.GetAllMilestones()).Returns(new List<MilestoneDefinition> { milestone });

        // Act
        _milestoneManager.RecordWarKill(civId);

        // Assert
        Assert.Contains("war_heroes", civ.CompletedMilestones);
    }

    #endregion

    #region GetMilestoneProgress Tests

    [Fact]
    public void GetMilestoneProgress_ValidMilestone_ReturnsProgress()
    {
        // Arrange
        var civId = "test-civ-1";
        var civ = CreateTestCivilization(civId);

        var milestone = new MilestoneDefinition(
            "first_alliance",
            "First Alliance",
            "Form your first alliance",
            MilestoneType.Major,
            new MilestoneTrigger(MilestoneTriggerType.ReligionCount, 2),
            1,
            0);

        _mockCivilizationManager.Setup(c => c.GetCivilization(civId)).Returns(civ);
        _mockMilestoneLoader.Setup(m => m.GetMilestone("first_alliance")).Returns(milestone);

        // Act
        var result = _milestoneManager.GetMilestoneProgress(civId, "first_alliance");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("first_alliance", result.MilestoneId);
        Assert.Equal("First Alliance", result.MilestoneName);
        Assert.Equal(1, result.CurrentValue); // One founder religion
        Assert.Equal(2, result.TargetValue);
        Assert.False(result.IsCompleted);
    }

    [Fact]
    public void GetMilestoneProgress_NonExistentMilestone_ReturnsNull()
    {
        // Arrange
        _mockMilestoneLoader.Setup(m => m.GetMilestone(It.IsAny<string>())).Returns((MilestoneDefinition?)null);

        // Act
        var result = _milestoneManager.GetMilestoneProgress("test-civ", "invalid-milestone");

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region Event Handler Tests

    [Fact]
    public void HandleReligionAdded_TriggersCheckMilestones()
    {
        // Arrange
        var civId = "test-civ-1";
        var civ = CreateTestCivilization(civId);
        civ.AddReligion("religion-2");

        var milestone = new MilestoneDefinition(
            "first_alliance",
            "First Alliance",
            "Form your first alliance",
            MilestoneType.Major,
            new MilestoneTrigger(MilestoneTriggerType.ReligionCount, 2),
            1,
            0);

        _mockCivilizationManager.Setup(c => c.GetCivilization(civId)).Returns(civ);
        _mockMilestoneLoader.Setup(m => m.GetAllMilestones()).Returns(new List<MilestoneDefinition> { milestone });

        _milestoneManager.Initialize();

        // Act - simulate the event
        _mockCivilizationManager.Raise(c => c.OnReligionAdded += null, civId, "religion-2");

        // Assert
        Assert.Contains("first_alliance", civ.CompletedMilestones);
    }

    [Fact]
    public void HandleCivilizationDisbanded_InvalidatesCache()
    {
        // Arrange
        var civId = "test-civ-1";
        var civ = CreateTestCivilization(civId);

        _mockCivilizationManager.Setup(c => c.GetCivilization(civId)).Returns(civ);

        _milestoneManager.Initialize();

        // First call to populate cache
        _milestoneManager.GetActiveBonuses(civId);

        // Act - simulate disband
        _mockCivilizationManager.Raise(c => c.OnCivilizationDisbanded += null, civId);

        // Call again - should fetch fresh data
        _milestoneManager.GetActiveBonuses(civId);

        // Assert - GetCivilization should be called twice (cache was invalidated)
        _mockCivilizationManager.Verify(c => c.GetCivilization(civId), Times.Exactly(2));
    }

    #endregion

    #region Dispose Tests

    [Fact]
    public void Dispose_ClearsCache()
    {
        // Arrange
        var civId = "test-civ-1";
        var civ = CreateTestCivilization(civId);

        _mockCivilizationManager.Setup(c => c.GetCivilization(civId)).Returns(civ);

        // Populate cache
        _milestoneManager.GetActiveBonuses(civId);

        // Act
        _milestoneManager.Dispose();

        // Call after dispose - should fetch fresh data since cache is cleared
        _mockCivilizationManager.Invocations.Clear();
        _milestoneManager.GetActiveBonuses(civId);

        // Assert
        _mockCivilizationManager.Verify(c => c.GetCivilization(civId), Times.Once());
    }

    #endregion

    #region Helper Methods

    private static Civilization CreateTestCivilization(string civId)
    {
        return new Civilization(civId, "Test Civilization", "founder-uid", "founder-religion")
        {
            Rank = 0,
            WarKillCount = 0,
            MemberCount = 1
        };
    }

    private static MilestoneDefinition CreateTestMilestone(
        string id,
        MilestoneBenefitType benefitType,
        float amount)
    {
        return new MilestoneDefinition(
            id,
            $"Test Milestone {id}",
            "Test description",
            MilestoneType.Minor,
            new MilestoneTrigger(MilestoneTriggerType.ReligionCount, 1),
            0,
            0,
            new MilestoneBenefit(benefitType, amount)
        );
    }

    #endregion
}
