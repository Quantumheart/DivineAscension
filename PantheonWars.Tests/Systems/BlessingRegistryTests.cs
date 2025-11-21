using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Moq;
using PantheonWars.Data;
using PantheonWars.Models;
using PantheonWars.Models.Enum;
using PantheonWars.Systems;
using PantheonWars.Tests.Helpers;
using Vintagestory.API.Common;
using Xunit;

namespace PantheonWars.Tests.Systems;

/// <summary>
///     Unit tests for BlessingRegistry
///     Tests blessing registration, retrieval, and validation
/// </summary>
[ExcludeFromCodeCoverage]
public class BlessingRegistryTests
{
    private readonly Mock<ICoreAPI> _mockAPI;
    private readonly Mock<ILogger> _mockLogger;
    private readonly BlessingRegistry _registry;

    public BlessingRegistryTests()
    {
        _mockAPI = TestFixtures.CreateMockCoreAPI();
        _mockLogger = new Mock<ILogger>();
        _mockAPI.Setup(a => a.Logger).Returns(_mockLogger.Object);

        _registry = new BlessingRegistry(_mockAPI.Object);
    }

    #region Initialization Tests

    [Fact]
    public void Initialize_RegistersAllBlessings()
    {
        // Act
        _registry.Initialize();

        // Assert
        var allBlessings = _registry.GetAllBlessings();
        Assert.NotEmpty(allBlessings);
        _mockLogger.Verify(
            l => l.Notification(It.Is<string>(s =>
                s.Contains("Blessing Registry initialized") &&
                s.Contains($"{allBlessings.Count}"))),
            Times.Once()
        );
    }

    [Fact]
    public void Initialize_LogsNotificationMessage()
    {
        // Act
        _registry.Initialize();

        // Assert
        _mockLogger.Verify(
            l => l.Notification(It.Is<string>(s => s.Contains("Initializing Blessing Registry"))),
            Times.Once()
        );
    }

    #endregion

    #region RegisterBlessing Tests

    [Fact]
    public void RegisterBlessing_WithValidBlessing_RegistersSuccessfully()
    {
        // Arrange
        var blessing = TestFixtures.CreateTestBlessing(
            "test_blessing_1",
            "Test Blessing",
            DeityType.Aethra);

        // Act
        _registry.RegisterBlessing(blessing);

        // Assert
        var retrieved = _registry.GetBlessing("test_blessing_1");
        Assert.NotNull(retrieved);
        Assert.Equal("Test Blessing", retrieved.Name);
        Assert.Equal(DeityType.Aethra, retrieved.Deity);
    }

    [Fact]
    public void RegisterBlessing_WithEmptyId_LogsError()
    {
        // Arrange
        var blessing = new Blessing("", "Invalid Blessing", DeityType.Aethra);

        // Act
        _registry.RegisterBlessing(blessing);

        // Assert
        _mockLogger.Verify(
            l => l.Error(It.Is<string>(s => s.Contains("Cannot register blessing with empty BlessingId"))),
            Times.Once()
        );
    }

    [Fact]
    public void RegisterBlessing_WithDuplicateId_LogsWarningAndOverwrites()
    {
        // Arrange
        var blessing1 = TestFixtures.CreateTestBlessing("duplicate_id", "First Blessing");
        var blessing2 = TestFixtures.CreateTestBlessing("duplicate_id", "Second Blessing");

        // Act
        _registry.RegisterBlessing(blessing1);
        _registry.RegisterBlessing(blessing2);

        // Assert
        _mockLogger.Verify(
            l => l.Warning(It.Is<string>(s => s.Contains("already registered") && s.Contains("Overwriting"))),
            Times.Once()
        );

        var retrieved = _registry.GetBlessing("duplicate_id");
        Assert.Equal("Second Blessing", retrieved?.Name);
    }

    [Fact]
    public void RegisterBlessing_LogsDebugMessage()
    {
        // Arrange
        var blessing = TestFixtures.CreateTestBlessing("debug_test", "Debug Blessing");

        // Act
        _registry.RegisterBlessing(blessing);

        // Assert
        _mockLogger.Verify(
            l => l.Debug(It.Is<string>(s =>
                s.Contains("Registered blessing") &&
                s.Contains("debug_test") &&
                s.Contains("Debug Blessing"))),
            Times.Once()
        );
    }

    #endregion

    #region GetBlessing Tests

    [Fact]
    public void GetBlessing_WithValidId_ReturnsBlessing()
    {
        // Arrange
        var blessing = TestFixtures.CreateTestBlessing("valid_id", "Valid Blessing");
        _registry.RegisterBlessing(blessing);

        // Act
        var result = _registry.GetBlessing("valid_id");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Valid Blessing", result.Name);
    }

    [Fact]
    public void GetBlessing_WithInvalidId_ReturnsNull()
    {
        // Act
        var result = _registry.GetBlessing("nonexistent_id");

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region GetBlessingsForDeity Tests

    [Fact]
    public void GetBlessingsForDeity_ReturnsOnlyBlessingsForThatDeity()
    {
        // Arrange
        _registry.RegisterBlessing(TestFixtures.CreateTestBlessing("khoras_1", "Khoras Blessing 1", DeityType.Aethra));
        _registry.RegisterBlessing(TestFixtures.CreateTestBlessing("khoras_2", "Khoras Blessing 2", DeityType.Aethra));
        _registry.RegisterBlessing(TestFixtures.CreateTestBlessing("lysa_1", "Lysa Blessing 1", DeityType.Gaia));

        // Act
        var khorasBlessings = _registry.GetBlessingsForDeity(DeityType.Aethra);

        // Assert
        Assert.Equal(2, khorasBlessings.Count);
        Assert.All(khorasBlessings, b => Assert.Equal(DeityType.Aethra, b.Deity));
    }

    [Fact]
    public void GetBlessingsForDeity_WithTypeFilter_ReturnsOnlyMatchingType()
    {
        // Arrange - In religion-only system, all blessings are religion-level
        _registry.RegisterBlessing(TestFixtures.CreateTestBlessing("khoras_player_1", "Player 1", DeityType.Aethra));
        _registry.RegisterBlessing(TestFixtures.CreateTestBlessing("khoras_player_2", "Player 2", DeityType.Aethra));
        _registry.RegisterBlessing(TestFixtures.CreateTestBlessing("khoras_religion_1", "Religion 1", DeityType.Aethra));

        // Act
        var blessings = _registry.GetBlessingsForDeity(DeityType.Aethra);

        // Assert - All blessings returned (no type filtering in religion-only system)
        Assert.Equal(3, blessings.Count);
    }

    [Fact]
    public void GetBlessingsForDeity_OrdersByPrestigeRankThenName()
    {
        // Arrange - Religion-only system orders by PrestigeRank then Name
        var blessing1 = TestFixtures.CreateTestBlessing("b1", "B1", DeityType.Aethra);
        blessing1.RequiredPrestigeRank = 0;

        var blessing2 = TestFixtures.CreateTestBlessing("b2", "B2", DeityType.Aethra);
        blessing2.RequiredPrestigeRank = 1;

        var blessing3 = TestFixtures.CreateTestBlessing("b3", "B3", DeityType.Aethra);
        blessing3.RequiredPrestigeRank = 0;

        _registry.RegisterBlessing(blessing1);
        _registry.RegisterBlessing(blessing2);
        _registry.RegisterBlessing(blessing3);

        // Act
        var blessings = _registry.GetBlessingsForDeity(DeityType.Aethra);

        // Assert - Ordered by PrestigeRank (0, 0, 1), then by Name (B1, B3, B2)
        Assert.Equal("b1", blessings[0].BlessingId); // PrestigeRank 0, Name B1
        Assert.Equal("b3", blessings[1].BlessingId); // PrestigeRank 0, Name B3
        Assert.Equal("b2", blessings[2].BlessingId); // PrestigeRank 1, Name B2
    }

    [Fact]
    public void GetBlessingsForDeity_WithNoMatchingDeity_ReturnsEmptyList()
    {
        // Arrange
        _registry.RegisterBlessing(TestFixtures.CreateTestBlessing("khoras_1", "Khoras", DeityType.Aethra));

        // Act
        var blessings = _registry.GetBlessingsForDeity(DeityType.Morthen);

        // Assert
        Assert.Empty(blessings);
    }

    #endregion

    #region GetAllBlessings Tests

    [Fact]
    public void GetAllBlessings_ReturnsAllRegisteredBlessings()
    {
        // Arrange
        _registry.RegisterBlessing(TestFixtures.CreateTestBlessing("b1", "Blessing 1"));
        _registry.RegisterBlessing(TestFixtures.CreateTestBlessing("b2", "Blessing 2"));
        _registry.RegisterBlessing(TestFixtures.CreateTestBlessing("b3", "Blessing 3"));

        // Act
        var allBlessings = _registry.GetAllBlessings();

        // Assert
        Assert.Equal(3, allBlessings.Count);
    }

    [Fact]
    public void GetAllBlessings_WhenEmpty_ReturnsEmptyList()
    {
        // Act
        var allBlessings = _registry.GetAllBlessings();

        // Assert
        Assert.Empty(allBlessings);
    }

    #endregion

    #region CanUnlockBlessing Tests - Religion Blessings

    [Fact]
    public void CanUnlockBlessing_ReligionBlessing_WithoutReligion_ReturnsFalse()
    {
        // Arrange
        var playerData = TestFixtures.CreateTestPlayerReligionData("player-uid", DeityType.Aethra, null);
        var blessing = TestFixtures.CreateTestBlessing("test", "Test", DeityType.Aethra);

        // Act
        var (canUnlock, reason) = _registry.CanUnlockBlessing(playerData, null, blessing);

        // Assert
        Assert.False(canUnlock);
        Assert.Equal("Not in a religion", reason);
    }

    [Fact]
    public void CanUnlockBlessing_ReligionBlessing_AlreadyUnlocked_ReturnsFalse()
    {
        // Arrange
        var playerData = TestFixtures.CreateTestPlayerReligionData("player-uid", DeityType.Aethra, "religion-uid");
        var religionData = TestFixtures.CreateTestReligion("religion-uid", "Test Religion", DeityType.Aethra);
        religionData.UnlockedBlessings["test_blessing"] = true;

        var blessing = TestFixtures.CreateTestBlessing("test_blessing", "Test", DeityType.Aethra);

        // Act
        var (canUnlock, reason) = _registry.CanUnlockBlessing(playerData, religionData, blessing);

        // Assert
        Assert.False(canUnlock);
        Assert.Equal("Blessing already unlocked", reason);
    }

    [Fact]
    public void CanUnlockBlessing_ReligionBlessing_InsufficientPrestigeRank_ReturnsFalse()
    {
        // Arrange
        var playerData = TestFixtures.CreateTestPlayerReligionData("player-uid", DeityType.Aethra, "religion-uid");
        var religionData = TestFixtures.CreateTestReligion("religion-uid", "Test Religion", DeityType.Aethra);
        religionData.PrestigeRank = PrestigeRank.Fledgling;

        var blessing = TestFixtures.CreateTestBlessing("test", "Test", DeityType.Aethra);
        blessing.RequiredPrestigeRank = 2; // Requires Renowned

        // Act
        var (canUnlock, reason) = _registry.CanUnlockBlessing(playerData, religionData, blessing);

        // Assert
        Assert.False(canUnlock);
        Assert.Contains("Religion requires Renowned prestige rank", reason);
    }

    [Fact]
    public void CanUnlockBlessing_ReligionBlessing_WrongDeity_ReturnsFalse()
    {
        // Arrange
        var playerData = TestFixtures.CreateTestPlayerReligionData("player-uid", DeityType.Aethra, "religion-uid");
        var religionData = TestFixtures.CreateTestReligion("religion-uid", "Test Religion", DeityType.Aethra);
        var blessing = TestFixtures.CreateTestBlessing("test", "Test", DeityType.Gaia);

        // Act
        var (canUnlock, reason) = _registry.CanUnlockBlessing(playerData, religionData, blessing);

        // Assert
        Assert.False(canUnlock);
        Assert.Contains("Religion deity mismatch", reason);
    }

    [Fact]
    public void CanUnlockBlessing_ReligionBlessing_AllRequirementsMet_ReturnsTrue()
    {
        // Arrange
        var playerData = TestFixtures.CreateTestPlayerReligionData("player-uid", DeityType.Aethra, "religion-uid");
        var religionData = TestFixtures.CreateTestReligion("religion-uid", "Test Religion", DeityType.Aethra);
        religionData.PrestigeRank = PrestigeRank.Established;

        var blessing = TestFixtures.CreateTestBlessing("test", "Test", DeityType.Aethra);
        blessing.RequiredPrestigeRank = 1;

        // Act
        var (canUnlock, reason) = _registry.CanUnlockBlessing(playerData, religionData, blessing);

        // Assert
        Assert.True(canUnlock);
        Assert.Equal("Can unlock", reason);
    }

    #endregion
}
