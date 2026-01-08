using System.Diagnostics.CodeAnalysis;
using DivineAscension.Data;
using DivineAscension.Models.Enum;
using DivineAscension.Systems;
using DivineAscension.Systems.Interfaces;
using Moq;
using Vintagestory.API.Common;

namespace DivineAscension.Tests.Systems;

/// <summary>
///     Unit tests for PlayerDataMigration
///     Tests migration from v2 (PlayerReligionData) to v3 (PlayerProgressionData)
/// </summary>
[ExcludeFromCodeCoverage]
public class PlayerDataMigrationTests
{
    private readonly Mock<ILogger> _mockLogger;
    private readonly Mock<IReligionManager> _mockReligionManager;

    public PlayerDataMigrationTests()
    {
        _mockReligionManager = new Mock<IReligionManager>();
        _mockLogger = new Mock<ILogger>();
    }

    #region GetMigrationPath Tests

    [Fact]
    public void GetMigrationPath_ReturnsFormattedString()
    {
        // Act
        var path = PlayerDataMigration.GetMigrationPath(2, 3);

        // Assert
        Assert.Equal("v2 → v3", path);
    }

    #endregion

    #region NeedsMigration Tests

    [Fact]
    public void NeedsMigration_WithVersion2_ReturnsTrue()
    {
        // Act
        var needsMigration = PlayerDataMigration.NeedsMigration(2);

        // Assert
        Assert.True(needsMigration);
    }

    [Fact]
    public void NeedsMigration_WithVersion1_ReturnsTrue()
    {
        // Act
        var needsMigration = PlayerDataMigration.NeedsMigration(1);

        // Assert
        Assert.True(needsMigration);
    }

    [Fact]
    public void NeedsMigration_WithVersion3_ReturnsFalse()
    {
        // Act
        var needsMigration = PlayerDataMigration.NeedsMigration(3);

        // Assert
        Assert.False(needsMigration);
    }

    [Fact]
    public void NeedsMigration_WithVersion4_ReturnsFalse()
    {
        // Act
        var needsMigration = PlayerDataMigration.NeedsMigration(4);

        // Assert
        Assert.False(needsMigration);
    }

    #endregion

    #region MigrateV2ToV3 Tests

    [Fact]
    public void MigrateV2ToV3_CopiesBasicFields()
    {
        // Arrange
        var oldData = new PlayerReligionData("player-123")
        {
            Favor = 100,
            TotalFavorEarned = 2500,
            AccumulatedFractionalFavor = 0.75f,
            DataVersion = 2
        };

        _mockReligionManager.Setup(m => m.GetPlayerReligionId("player-123")).Returns((string?)null);

        // Act
        var newData = PlayerDataMigration.MigrateV2ToV3(oldData, _mockReligionManager.Object, _mockLogger.Object);

        // Assert
        Assert.Equal("player-123", newData.Id);
        Assert.Equal(100, newData.Favor);
        Assert.Equal(2500, newData.TotalFavorEarned);
        Assert.Equal(0.75f, newData.AccumulatedFractionalFavor);
        Assert.Equal(3, newData.DataVersion);
    }

    [Fact]
    public void MigrateV2ToV3_ConvertsBlessingDictionaryToHashSet()
    {
        // Arrange
        var oldData = new PlayerReligionData("player-123")
        {
            UnlockedBlessings = new Dictionary<string, bool>
            {
                { "blessing1", true },
                { "blessing2", true },
                { "blessing3", false }, // Should not be included
                { "blessing4", true }
            }
        };

        _mockReligionManager.Setup(m => m.GetPlayerReligionId("player-123")).Returns((string?)null);

        // Act
        var newData = PlayerDataMigration.MigrateV2ToV3(oldData, _mockReligionManager.Object, _mockLogger.Object);

        // Assert
        Assert.Equal(3, newData.UnlockedBlessings.Count);
        Assert.Contains("blessing1", newData.UnlockedBlessings);
        Assert.Contains("blessing2", newData.UnlockedBlessings);
        Assert.Contains("blessing4", newData.UnlockedBlessings);
        Assert.DoesNotContain("blessing3", newData.UnlockedBlessings);
    }

    [Fact]
    public void MigrateV2ToV3_WithEmptyBlessings_CreatesEmptyHashSet()
    {
        // Arrange
        var oldData = new PlayerReligionData("player-123")
        {
            UnlockedBlessings = new Dictionary<string, bool>()
        };

        _mockReligionManager.Setup(m => m.GetPlayerReligionId("player-123")).Returns((string?)null);

        // Act
        var newData = PlayerDataMigration.MigrateV2ToV3(oldData, _mockReligionManager.Object, _mockLogger.Object);

        // Assert
        Assert.Empty(newData.UnlockedBlessings);
    }

    [Fact]
    public void MigrateV2ToV3_ValidatesReligionMembership_WhenMatching()
    {
        // Arrange
        var oldData = new PlayerReligionData("player-123")
        {
            ReligionUID = "religion-456"
        };

        _mockReligionManager.Setup(m => m.GetPlayerReligionId("player-123")).Returns("religion-456");

        // Act
        var newData = PlayerDataMigration.MigrateV2ToV3(oldData, _mockReligionManager.Object, _mockLogger.Object);

        // Assert
        Assert.NotNull(newData);
        _mockLogger.Verify(
            l => l.Debug(It.Is<string>(s =>
                s.Contains("religion validated") && s.Contains("religion-456"))),
            Times.Once);
    }

    [Fact]
    public void MigrateV2ToV3_LogsWarning_WhenReligionMismatched()
    {
        // Arrange
        var oldData = new PlayerReligionData("player-123")
        {
            ReligionUID = "religion-wrong"
        };

        _mockReligionManager.Setup(m => m.GetPlayerReligionId("player-123")).Returns("religion-correct");

        // Act
        var newData = PlayerDataMigration.MigrateV2ToV3(oldData, _mockReligionManager.Object, _mockLogger.Object);

        // Assert
        Assert.NotNull(newData);
        _mockLogger.Verify(
            l => l.Warning(It.Is<string>(s =>
                s.Contains("mismatched religion") &&
                s.Contains("religion-wrong") &&
                s.Contains("religion-correct"))),
            Times.Once);
    }

    [Fact]
    public void MigrateV2ToV3_LogsWarning_WhenReligionNotFoundInManager()
    {
        // Arrange
        var oldData = new PlayerReligionData("player-123")
        {
            ReligionUID = "religion-gone"
        };

        _mockReligionManager.Setup(m => m.GetPlayerReligionId("player-123")).Returns((string?)null);

        // Act
        var newData = PlayerDataMigration.MigrateV2ToV3(oldData, _mockReligionManager.Object, _mockLogger.Object);

        // Assert
        Assert.NotNull(newData);
        _mockLogger.Verify(
            l => l.Warning(It.Is<string>(s =>
                s.Contains("had religion") &&
                s.Contains("religion-gone") &&
                s.Contains("not a member"))),
            Times.Once);
    }

    [Fact]
    public void MigrateV2ToV3_ComputesFavorRankCorrectly()
    {
        // Arrange - Avatar rank (10000+)
        var oldData = new PlayerReligionData("player-123")
        {
            TotalFavorEarned = 15000,
            FavorRank = FavorRank.Disciple // Old stored rank (should be ignored)
        };

        _mockReligionManager.Setup(m => m.GetPlayerReligionId("player-123")).Returns((string?)null);

        // Act
        var newData = PlayerDataMigration.MigrateV2ToV3(oldData, _mockReligionManager.Object, _mockLogger.Object);

        // Assert - FavorRank should be computed, not copied
        Assert.Equal(FavorRank.Avatar, newData.FavorRank);
    }

    [Fact]
    public void MigrateV2ToV3_LogsNotificationWithDetails()
    {
        // Arrange
        var oldData = new PlayerReligionData("player-123")
        {
            Favor = 50,
            TotalFavorEarned = 750,
            UnlockedBlessings = new Dictionary<string, bool>
            {
                { "blessing1", true },
                { "blessing2", true }
            }
        };

        _mockReligionManager.Setup(m => m.GetPlayerReligionId("player-123")).Returns((string?)null);

        // Act
        var newData = PlayerDataMigration.MigrateV2ToV3(oldData, _mockReligionManager.Object, _mockLogger.Object);

        // Assert
        _mockLogger.Verify(
            l => l.Notification(It.Is<string>(s =>
                s.Contains("Migrated player player-123") &&
                s.Contains("v2 to v3") &&
                s.Contains("Favor=50") &&
                s.Contains("TotalFavor=750") &&
                s.Contains("Blessings=2") &&
                s.Contains($"Rank={FavorRank.Disciple}"))), // 750 = Disciple
            Times.Once);
    }

    [Fact]
    public void MigrateV2ToV3_PreservesAllBlessingData()
    {
        // Arrange
        var oldData = new PlayerReligionData("player-123")
        {
            UnlockedBlessings = new Dictionary<string, bool>
            {
                { "khoras_strength_1", true },
                { "khoras_strength_2", true },
                { "khoras_armor_1", false },
                { "lysa_healing_1", true },
                { "lysa_healing_2", false },
                { "lysa_speed_1", true }
            }
        };

        _mockReligionManager.Setup(m => m.GetPlayerReligionId("player-123")).Returns((string?)null);

        // Act
        var newData = PlayerDataMigration.MigrateV2ToV3(oldData, _mockReligionManager.Object, _mockLogger.Object);

        // Assert - Only true values should be migrated
        Assert.Equal(4, newData.UnlockedBlessings.Count);
        Assert.Contains("khoras_strength_1", newData.UnlockedBlessings);
        Assert.Contains("khoras_strength_2", newData.UnlockedBlessings);
        Assert.Contains("lysa_healing_1", newData.UnlockedBlessings);
        Assert.Contains("lysa_speed_1", newData.UnlockedBlessings);
        Assert.DoesNotContain("khoras_armor_1", newData.UnlockedBlessings);
        Assert.DoesNotContain("lysa_healing_2", newData.UnlockedBlessings);
    }

    [Fact]
    public void MigrateV2ToV3_RemovesDeprecatedFields()
    {
        // Arrange
        var oldData = new PlayerReligionData("player-123")
        {
            ReligionUID = "religion-456",
            ActiveDeity = DeityType.Khoras,
            FavorRank = FavorRank.Champion,
            LastReligionSwitch = DateTime.UtcNow.AddDays(-10),
            KillCount = 5
        };

        _mockReligionManager.Setup(m => m.GetPlayerReligionId("player-123")).Returns("religion-456");

        // Act
        var newData = PlayerDataMigration.MigrateV2ToV3(oldData, _mockReligionManager.Object, _mockLogger.Object);

        // Assert - Verify new data doesn't have these fields (they're not in the model)
        // Just verify successful migration
        Assert.NotNull(newData);
        Assert.Equal(3, newData.DataVersion);
    }

    #endregion
}