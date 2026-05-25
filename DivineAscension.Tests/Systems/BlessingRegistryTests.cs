using System.Diagnostics.CodeAnalysis;
using DivineAscension.Configuration;
using DivineAscension.Models;
using DivineAscension.Models.Enum;
using DivineAscension.Services.Interfaces;
using DivineAscension.Systems;
using DivineAscension.Tests.Helpers;
using Moq;
using Vintagestory.API.Common;

namespace DivineAscension.Tests.Systems;

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
        TestFixtures.InitializeLocalizationForTests();

        _mockAPI = TestFixtures.CreateMockCoreAPI();
        _mockLogger = new Mock<ILogger>();
        _mockAPI.Setup(a => a.Logger).Returns(_mockLogger.Object);

        // Use TestBlessingLoader with sample blessings for most tests
        _registry = new BlessingRegistry(_mockAPI.Object, TestBlessingLoader.CreateWithSampleBlessings());
    }

    #region Initialization Tests

    [Fact]
    public void Initialize_WithLoader_RegistersAllBlessings()
    {
        // Act
        _registry.Initialize();

        // Assert
        var allBlessings = _registry.GetAllBlessings();
        Assert.Equal(3, allBlessings.Count); // TestBlessingLoader.CreateWithSampleBlessings() creates 3
        _mockLogger.Verify(
            l => l.Notification(It.Is<string>(s =>
                s.Contains("Blessing Registry initialized") &&
                s.Contains("3"))),
            Times.Once()
        );
    }

    [Fact]
    public void Initialize_WithLoader_LogsLoadedFromJson()
    {
        // Act
        _registry.Initialize();

        // Assert
        _mockLogger.Verify(
            l => l.Notification(It.Is<string>(s => s.Contains("Loaded") && s.Contains("from JSON"))),
            Times.Once()
        );
    }

    [Fact]
    public void Initialize_WithFailedLoader_LogsError()
    {
        // Arrange
        var failedLoader = TestBlessingLoader.CreateFailedLoader();
        var registry = new BlessingRegistry(_mockAPI.Object, failedLoader);

        // Act
        registry.Initialize();

        // Assert - should log error about failed load
        _mockLogger.Verify(
            l => l.Error(It.Is<string>(s => s.Contains("Failed to load blessings"))),
            Times.Once()
        );
    }

    [Fact]
    public void Initialize_WithoutLoader_LogsError()
    {
        // Arrange
        var registry = new BlessingRegistry(_mockAPI.Object, null);

        // Act
        registry.Initialize();

        // Assert - should log error about no loader
        _mockLogger.Verify(
            l => l.Error(It.Is<string>(s => s.Contains("No blessing loader provided"))),
            Times.Once()
        );

        // Should have no blessings
        var allBlessings = registry.GetAllBlessings();
        Assert.Empty(allBlessings);
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
            DeityDomain.Craft,
            BlessingKind.Player);

        // Act
        _registry.RegisterBlessing(blessing);

        // Assert
        var retrieved = _registry.GetBlessing("test_blessing_1");
        Assert.NotNull(retrieved);
        Assert.Equal("Test Blessing", retrieved.Name);
        Assert.Equal(DeityDomain.Craft, retrieved.Domain);
    }

    [Fact]
    public void RegisterBlessing_WithEmptyId_LogsError()
    {
        // Arrange
        var blessing = new Blessing("", "Invalid Blessing", DeityDomain.Craft);

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
        _registry.RegisterBlessing(TestFixtures.CreateTestBlessing("khoras_1", "Khoras Blessing 1", DeityDomain.Craft));
        _registry.RegisterBlessing(TestFixtures.CreateTestBlessing("khoras_2", "Khoras Blessing 2", DeityDomain.Craft));
        _registry.RegisterBlessing(TestFixtures.CreateTestBlessing("lysa_1", "Lysa Blessing 1", DeityDomain.Wild));

        // Act
        var khorasBlessings = _registry.GetBlessingsForDeity(DeityDomain.Craft);

        // Assert
        Assert.Equal(2, khorasBlessings.Count);
        Assert.All(khorasBlessings, b => Assert.Equal(DeityDomain.Craft, b.Domain));
    }

    [Fact]
    public void GetBlessingsForDeity_WithTypeFilter_ReturnsOnlyMatchingType()
    {
        // Arrange
        _registry.RegisterBlessing(TestFixtures.CreateTestBlessing("khoras_player_1", "Player 1"));
        _registry.RegisterBlessing(TestFixtures.CreateTestBlessing("khoras_player_2", "Player 2"));
        _registry.RegisterBlessing(TestFixtures.CreateTestBlessing("khoras_religion_1", "Religion 1", DeityDomain.Craft,
            BlessingKind.Religion));

        // Act
        var playerBlessings = _registry.GetBlessingsForDeity(DeityDomain.Craft, BlessingKind.Player);

        // Assert
        Assert.Equal(2, playerBlessings.Count);
        Assert.All(playerBlessings, b => Assert.Equal(BlessingKind.Player, b.Kind));
    }

    [Fact]
    public void GetBlessingsForDeity_OrdersByFavorRankThenPrestigeRank()
    {
        // Arrange
        var blessing1 = TestFixtures.CreateTestBlessing("b1", "B1", DeityDomain.Craft);
        blessing1.RequiredFavorRank = 2;
        blessing1.RequiredPrestigeRank = 0;

        var blessing2 = TestFixtures.CreateTestBlessing("b2", "B2", DeityDomain.Craft);
        blessing2.RequiredFavorRank = 1;
        blessing2.RequiredPrestigeRank = 1;

        var blessing3 = TestFixtures.CreateTestBlessing("b3", "B3", DeityDomain.Craft);
        blessing3.RequiredFavorRank = 1;
        blessing3.RequiredPrestigeRank = 0;

        _registry.RegisterBlessing(blessing1);
        _registry.RegisterBlessing(blessing2);
        _registry.RegisterBlessing(blessing3);

        // Act
        var blessings = _registry.GetBlessingsForDeity(DeityDomain.Craft);

        // Assert
        Assert.Equal("b3", blessings[0].BlessingId); // FavorRank 1, PrestigeRank 0
        Assert.Equal("b2", blessings[1].BlessingId); // FavorRank 1, PrestigeRank 1
        Assert.Equal("b1", blessings[2].BlessingId); // FavorRank 2, PrestigeRank 0
    }

    [Fact]
    public void GetBlessingsForDeity_WithNoMatchingDeity_ReturnsEmptyList()
    {
        // Arrange
        _registry.RegisterBlessing(TestFixtures.CreateTestBlessing("craft_1", "Craft", DeityDomain.Craft));

        // Act
        var blessings = _registry.GetBlessingsForDeity(DeityDomain.Harvest);

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

    #region CanUnlockBlessing Tests - Player Blessings

    [Fact]
    public void CanUnlockBlessing_PlayerBlessing_WithNullBlessing_ReturnsFalse()
    {
        // Arrange
        var playerData = TestFixtures.CreateTestPlayerReligionData("player-uid", DeityDomain.Craft, "religion-uid");

        // Act
        var (canUnlock, reason) = _registry.CanUnlockBlessing("player-uid", FavorRank.Initiate, FavorRank.Initiate, playerData, null, null);

        // Assert
        Assert.False(canUnlock);
        Assert.Equal("Blessing not found", reason);
    }

    [Fact]
    public void CanUnlockBlessing_PlayerBlessing_WithoutReligion_ReturnsFalse()
    {
        // Arrange
        var playerData = TestFixtures.CreateTestPlayerReligionData("player-uid", DeityDomain.None, null);
        var blessing = TestFixtures.CreateTestBlessing("test", "Test", DeityDomain.Craft, BlessingKind.Player);

        // Act
        var (canUnlock, reason) = _registry.CanUnlockBlessing("player-uid", FavorRank.Initiate, FavorRank.Initiate, playerData, null, blessing);

        // Assert
        Assert.False(canUnlock);
        Assert.Equal("Not in a religion", reason);
    }

    [Fact]
    public void CanUnlockBlessing_PlayerBlessing_AlreadyUnlocked_ReturnsFalse()
    {
        // Arrange
        var playerData = TestFixtures.CreateTestPlayerReligionData("player-uid", DeityDomain.Craft, "religion-uid");
        var blessing = TestFixtures.CreateTestBlessing("test_blessing", "Test", DeityDomain.Craft, BlessingKind.Player);
        var religion = TestFixtures.CreateTestReligion();
        playerData.UnlockBlessing("test_blessing");

        // Act
        var (canUnlock, reason) = _registry.CanUnlockBlessing("player-uid", FavorRank.Initiate, FavorRank.Initiate, playerData, religion, blessing);

        // Assert
        Assert.False(canUnlock);
        Assert.Equal("Blessing already unlocked", reason);
    }

    [Fact]
    public void CanUnlockBlessing_PlayerBlessing_InsufficientFavorRank_ReturnsFalse()
    {
        // Arrange
        var playerData = TestFixtures.CreateTestPlayerReligionData("player-uid", DeityDomain.Craft, "religion-uid");
        var religion = TestFixtures.CreateTestReligion();
        var blessing = TestFixtures.CreateTestBlessing("test", "Test", DeityDomain.Craft, BlessingKind.Player);
        blessing.RequiredFavorRank = 2; // Requires Zealot

        // Act
        var (canUnlock, reason) = _registry.CanUnlockBlessing("player-uid", FavorRank.Initiate, FavorRank.Initiate, playerData, religion, blessing);

        // Assert
        Assert.False(canUnlock);
        Assert.Contains("Requires Zealot favor rank", reason);
    }

    [Fact]
    public void CanUnlockBlessing_PlayerBlessing_NonCapstoneWrongDeity_ReturnsTrue()
    {
        // Phase 3: non-capstone player blessings unlock from any of the five deity trees,
        // regardless of religion's patron domain. (Cost is 1.5x; see separate cost test.)
        var playerData = TestFixtures.CreateTestPlayerReligionData("player-uid", DeityDomain.Craft, "religion-uid");
        var blessing = TestFixtures.CreateTestBlessing("test", "Test", DeityDomain.Wild, BlessingKind.Player);
        blessing.RequiredFavorRank = 0;
        blessing.RequiresPatron = false;
        var religion = TestFixtures.CreateTestReligion("test-religion", "Test", DeityDomain.Craft, "player-uid");

        var (canUnlock, _) = _registry.CanUnlockBlessing("player-uid", FavorRank.Initiate, FavorRank.Initiate, playerData, religion, blessing);

        Assert.True(canUnlock);
    }

    [Fact]
    public void CanUnlockBlessing_PlayerBlessing_CapstoneNonPatron_ReturnsFalse()
    {
        // Capstones (RequiresPatron=true) are restricted to followers of the matching patron.
        var playerData = TestFixtures.CreateTestPlayerReligionData("player-uid", DeityDomain.Craft, "religion-uid");
        var blessing = TestFixtures.CreateTestBlessing("avatar_of_wild", "Avatar of Wild", DeityDomain.Wild, BlessingKind.Player);
        blessing.RequiredFavorRank = 0;
        blessing.RequiresPatron = true;
        var religion = TestFixtures.CreateTestReligion("test-religion", "Test", DeityDomain.Craft, "player-uid");

        var (canUnlock, reason) = _registry.CanUnlockBlessing("player-uid", FavorRank.Initiate, FavorRank.Initiate, playerData, religion, blessing);

        Assert.False(canUnlock);
        Assert.Contains("Capstone blessing requires patron deity: Wild", reason);
    }

    [Fact]
    public void CanUnlockBlessing_PlayerBlessing_CapstonePatron_ReturnsTrue()
    {
        var playerData = TestFixtures.CreateTestPlayerReligionData("player-uid", DeityDomain.Wild, "religion-uid");
        var blessing = TestFixtures.CreateTestBlessing("avatar_of_wild", "Avatar of Wild", DeityDomain.Wild, BlessingKind.Player);
        blessing.RequiredFavorRank = 0;
        blessing.RequiresPatron = true;
        var religion = TestFixtures.CreateTestReligion("test-religion", "Test", DeityDomain.Wild, "player-uid");

        var (canUnlock, _) = _registry.CanUnlockBlessing("player-uid", FavorRank.Initiate, FavorRank.Initiate, playerData, religion, blessing);

        Assert.True(canUnlock);
    }

    [Fact]
    public void CanUnlockBlessing_PlayerBlessing_NonPatronCostIs1_5x()
    {
        // Patron=Craft, blessing domain=Wild, cost=100 → adjusted=150. Player has 140 → insufficient.
        var playerData = TestFixtures.CreateTestPlayerReligionData("player-uid", DeityDomain.Craft, "religion-uid");
        playerData.AddFavor(DeityDomain.Wild, 140);

        var blessing = TestFixtures.CreateTestBlessing("test", "Test", DeityDomain.Wild, BlessingKind.Player);
        blessing.RequiredFavorRank = 0;
        blessing.Cost = 100;
        var religion = TestFixtures.CreateTestReligion("test-religion", "Test", DeityDomain.Craft, "player-uid");

        var (canUnlock, reason) = _registry.CanUnlockBlessing("player-uid", FavorRank.Initiate, FavorRank.Initiate, playerData, religion, blessing);

        Assert.False(canUnlock);
        Assert.Contains("Insufficient favor", reason);
        Assert.Contains("requires 150", reason);
    }

    [Fact]
    public void CanUnlockBlessing_PlayerBlessing_PatronCostIs1x()
    {
        // Patron=Wild, blessing domain=Wild, cost=100, player has 100 → exactly enough at 1.0x.
        var playerData = TestFixtures.CreateTestPlayerReligionData("player-uid", DeityDomain.Wild, "religion-uid", favor: 0);
        playerData.AddFavor(DeityDomain.Wild, 100);

        var blessing = TestFixtures.CreateTestBlessing("test", "Test", DeityDomain.Wild, BlessingKind.Player);
        blessing.RequiredFavorRank = 0;
        blessing.Cost = 100;
        var religion = TestFixtures.CreateTestReligion("test-religion", "Test", DeityDomain.Wild, "player-uid");

        var (canUnlock, _) = _registry.CanUnlockBlessing("player-uid", FavorRank.Initiate, FavorRank.Initiate, playerData, religion, blessing);

        Assert.True(canUnlock);
    }

    [Fact]
    public void CanUnlockBlessing_PlayerBlessing_MissingPrerequisite_ReturnsFalse()
    {
        // Arrange
        var playerData = TestFixtures.CreateTestPlayerReligionData("player-uid", DeityDomain.Craft, "religion-uid");

        var prereqBlessing = TestFixtures.CreateTestBlessing("prereq", "Prerequisite", DeityDomain.Craft);
        _registry.RegisterBlessing(prereqBlessing);

        var blessing = TestFixtures.CreateTestBlessing("test", "Test", DeityDomain.Craft, BlessingKind.Player);
        blessing.RequiredFavorRank = 0; // Set to Initiate so favor rank check passes
        blessing.PrerequisiteBlessings.Add("prereq");
        blessing.Branch = "TestBranch"; // Non-null branch = AND logic for prerequisites

        var religion = TestFixtures.CreateTestReligion("test-religion", "Test", DeityDomain.Craft, "player-uid");


        // Act
        var (canUnlock, reason) = _registry.CanUnlockBlessing("player-uid", FavorRank.Initiate, FavorRank.Initiate, playerData, religion, blessing);

        // Assert
        Assert.False(canUnlock);
        Assert.Contains("Requires prerequisite blessing: Prerequisite", reason);
    }

    [Fact]
    public void CanUnlockBlessing_PlayerBlessing_AllRequirementsMet_ReturnsTrue()
    {
        // Arrange
        var playerData = TestFixtures.CreateTestPlayerReligionData("player-uid", DeityDomain.Craft, "religion-uid");

        var blessing = TestFixtures.CreateTestBlessing("test", "Test", DeityDomain.Craft, BlessingKind.Player);
        blessing.RequiredFavorRank = 1;

        var religion = TestFixtures.CreateTestReligion("test-religion", "Test", DeityDomain.Craft, "player-uid");

        // Act
        var (canUnlock, reason) = _registry.CanUnlockBlessing("player-uid", FavorRank.Disciple, FavorRank.Disciple, playerData, religion, blessing);

        // Assert
        Assert.True(canUnlock);
        Assert.Equal("Can unlock", reason);
    }

    [Fact]
    public void CanUnlockBlessing_PlayerBlessing_InsufficientFavor_ReturnsFalse()
    {
        // Arrange - explicitly set favor to 30 (less than cost of 50)
        var playerData = TestFixtures.CreateTestPlayerReligionData("player-uid", DeityDomain.Craft, "religion-uid", favor: 30);

        var blessing = TestFixtures.CreateTestBlessing("test", "Test", DeityDomain.Craft, BlessingKind.Player);
        blessing.RequiredFavorRank = 0;
        blessing.Cost = 50;

        var religion = TestFixtures.CreateTestReligion("test-religion", "Test", DeityDomain.Craft, "player-uid");

        // Act
        var (canUnlock, reason) = _registry.CanUnlockBlessing("player-uid", FavorRank.Initiate, FavorRank.Initiate, playerData, religion, blessing);

        // Assert
        Assert.False(canUnlock);
        Assert.Contains("Insufficient favor", reason);
        Assert.Contains("requires 50", reason);
        Assert.Contains("have 30", reason);
    }

    [Fact]
    public void CanUnlockBlessing_PlayerBlessing_SufficientFavor_ReturnsTrue()
    {
        // Arrange
        var playerData = TestFixtures.CreateTestPlayerReligionData("player-uid", DeityDomain.Craft, "religion-uid");
        playerData.AddFavor(DeityDomain.Craft, 100); // Enough for cost of 50

        var blessing = TestFixtures.CreateTestBlessing("test", "Test", DeityDomain.Craft, BlessingKind.Player);
        blessing.RequiredFavorRank = 0;
        blessing.Cost = 50;

        var religion = TestFixtures.CreateTestReligion("test-religion", "Test", DeityDomain.Craft, "player-uid");

        // Act
        var (canUnlock, reason) = _registry.CanUnlockBlessing("player-uid", FavorRank.Initiate, FavorRank.Initiate, playerData, religion, blessing);

        // Assert
        Assert.True(canUnlock);
        Assert.Equal("Can unlock", reason);
    }

    [Fact]
    public void CanUnlockBlessing_PlayerBlessing_ZeroCost_ReturnsTrue()
    {
        // Arrange - even with 0 favor, a free blessing should be unlockable
        var playerData = TestFixtures.CreateTestPlayerReligionData("player-uid", DeityDomain.Craft, "religion-uid", favor: 0);

        var blessing = TestFixtures.CreateTestBlessing("test", "Test", DeityDomain.Craft, BlessingKind.Player);
        blessing.RequiredFavorRank = 0;
        blessing.Cost = 0; // Free blessing

        var religion = TestFixtures.CreateTestReligion("test-religion", "Test", DeityDomain.Craft, "player-uid");

        // Act
        var (canUnlock, reason) = _registry.CanUnlockBlessing("player-uid", FavorRank.Initiate, FavorRank.Initiate, playerData, religion, blessing);

        // Assert
        Assert.True(canUnlock);
        Assert.Equal("Can unlock", reason);
    }

    [Fact]
    public void CanUnlockBlessing_PlayerBlessing_InsufficientFavor_WithSkipCostCheck_ReturnsTrue()
    {
        // Arrange - insufficient favor, but skipCostCheck=true should allow unlock
        var playerData = TestFixtures.CreateTestPlayerReligionData("player-uid", DeityDomain.Craft, "religion-uid", favor: 30);

        var blessing = TestFixtures.CreateTestBlessing("test", "Test", DeityDomain.Craft, BlessingKind.Player);
        blessing.RequiredFavorRank = 0;
        blessing.Cost = 50; // More than available favor

        var religion = TestFixtures.CreateTestReligion("test-religion", "Test", DeityDomain.Craft, "player-uid");

        // Act - pass skipCostCheck: true
        var (canUnlock, reason) = _registry.CanUnlockBlessing("player-uid", FavorRank.Initiate, FavorRank.Initiate, playerData, religion, blessing, skipCostCheck: true);

        // Assert - should return true because cost check is skipped
        Assert.True(canUnlock);
        Assert.Equal("Can unlock", reason);
    }

    #endregion

    #region CanUnlockBlessing Tests - Unlock Cap (#423.2)

    /// <summary>
    /// Helper: build a registry with a specific GameBalanceConfig so cap tests
    /// can stay independent of the shared default-config _registry.
    /// </summary>
    private BlessingRegistry CreateRegistryWithConfig(GameBalanceConfig config)
    {
        return new BlessingRegistry(_mockAPI.Object, TestBlessingLoader.CreateWithSampleBlessings(), config);
    }

    [Fact]
    public void CanUnlockBlessing_PlayerBlessing_AtCap_ReturnsFalseWithCapReason()
    {
        // Initiate + Fledgling => 1 favor slot + 0 prestige bonus = 1 slot total.
        // Player already has 1 unlocked blessing, attempting a second.
        var config = new GameBalanceConfig();
        var registry = CreateRegistryWithConfig(config);

        var playerData = TestFixtures.CreateTestPlayerReligionData("player-uid", DeityDomain.Craft, "religion-uid");
        playerData.UnlockBlessing("existing_blessing");

        var blessing = TestFixtures.CreateTestBlessing("new_blessing", "New", DeityDomain.Craft, BlessingKind.Player);
        blessing.RequiredFavorRank = 0;
        blessing.Cost = 0;

        var religion = TestFixtures.CreateTestReligion("test-religion", "Test", DeityDomain.Craft, "player-uid");
        religion.PrestigeRank = PrestigeRank.Fledgling;

        var (canUnlock, reason) = registry.CanUnlockBlessing(
            "player-uid", FavorRank.Initiate, FavorRank.Initiate, playerData, religion, blessing);

        Assert.False(canUnlock);
        // Reason should reflect cap state (current/max) and point at unlearn (#425).
        Assert.Contains("1/1", reason);
        Assert.Contains("Unlearn", reason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void CanUnlockBlessing_PlayerBlessing_UnderCap_AllowsUnlock()
    {
        // Disciple + Fledgling => 2 favor slots + 0 prestige bonus = 2 slots.
        // Player has 1 unlocked, attempting a second (under cap).
        var config = new GameBalanceConfig();
        var registry = CreateRegistryWithConfig(config);

        var playerData = TestFixtures.CreateTestPlayerReligionData("player-uid", DeityDomain.Craft, "religion-uid");
        playerData.UnlockBlessing("existing_blessing");

        var blessing = TestFixtures.CreateTestBlessing("new_blessing", "New", DeityDomain.Craft, BlessingKind.Player);
        blessing.RequiredFavorRank = 0;
        blessing.Cost = 0;

        var religion = TestFixtures.CreateTestReligion("test-religion", "Test", DeityDomain.Craft, "player-uid");
        religion.PrestigeRank = PrestigeRank.Fledgling;

        var (canUnlock, reason) = registry.CanUnlockBlessing(
            "player-uid", FavorRank.Disciple, FavorRank.Disciple, playerData, religion, blessing);

        Assert.True(canUnlock);
        Assert.Equal("Can unlock", reason);
    }

    [Fact]
    public void CanUnlockBlessing_PlayerBlessing_PrestigeBonusSlotsCountTowardCap()
    {
        // Initiate + Renowned => 1 favor + 1 prestige bonus = 2 slots.
        // Player already has 2 unlocked → cap reached.
        var config = new GameBalanceConfig();
        var registry = CreateRegistryWithConfig(config);

        var playerData = TestFixtures.CreateTestPlayerReligionData("player-uid", DeityDomain.Craft, "religion-uid");
        playerData.UnlockBlessing("existing_a");
        playerData.UnlockBlessing("existing_b");

        var blessing = TestFixtures.CreateTestBlessing("third", "Third", DeityDomain.Craft, BlessingKind.Player);
        blessing.RequiredFavorRank = 0;
        blessing.Cost = 0;

        var religion = TestFixtures.CreateTestReligion("test-religion", "Test", DeityDomain.Craft, "player-uid");
        religion.PrestigeRank = PrestigeRank.Renowned;

        var (canUnlock, reason) = registry.CanUnlockBlessing(
            "player-uid", FavorRank.Initiate, FavorRank.Initiate, playerData, religion, blessing);

        // 2 unlocked, cap of 2 (1 favor + 1 prestige bonus) → rejected with cap reason.
        Assert.False(canUnlock);
        Assert.Contains("2/2", reason);
    }

    [Fact]
    public void CanUnlockBlessing_PlayerBlessing_NonReligionPlayer_UsesFavorOnlyCap()
    {
        // No religion → calculator treats prestige rank as null → favor slots only.
        // Disciple gives 2 slots regardless of any prestige bonus.
        var config = new GameBalanceConfig
        {
            // Make prestige bonus large so we can prove it is NOT applied without religion.
            RenownedBonusSlots = 5
        };
        var registry = CreateRegistryWithConfig(config);

        var playerData = TestFixtures.CreateTestPlayerReligionData("player-uid", DeityDomain.Craft, religionUID: null);
        playerData.UnlockBlessing("existing_a");
        playerData.UnlockBlessing("existing_b");

        var blessing = TestFixtures.CreateTestBlessing("third", "Third", DeityDomain.Craft, BlessingKind.Player);
        blessing.RequiredFavorRank = 0;
        blessing.Cost = 0;

        var (canUnlock, reason) = registry.CanUnlockBlessing(
            "player-uid", FavorRank.Disciple, FavorRank.Disciple, playerData, religionData: null, blessing);

        // 2 unlocked, cap of 2 (favor-only) → rejected with cap reason, NOT "Not in a religion".
        Assert.False(canUnlock);
        Assert.Contains("2/2", reason);
    }

    [Fact]
    public void CanUnlockBlessing_PlayerBlessing_SlotCapUsesPatronRank_NotBlessingDomainRank()
    {
        // Regression for #472: slot cap must derive from the patron-domain favor rank
        // (slotCapFavorRank), not the blessing's own domain rank (playerFavorRank).
        // Player: patron Craft = Initiate, incidental Wild = Disciple, prestige Renowned.
        // Patron rank Initiate + Renowned bonus = 2 slots. With 2 unlocked → cap reached.
        // A higher Wild rank must NOT inflate the cap to 3.
        var config = new GameBalanceConfig();
        var registry = CreateRegistryWithConfig(config);

        var playerData = TestFixtures.CreateTestPlayerReligionData("player-uid", DeityDomain.Craft, "religion-uid");
        playerData.UnlockBlessing("existing_a");
        playerData.UnlockBlessing("existing_b");

        var blessing = TestFixtures.CreateTestBlessing("third", "Third", DeityDomain.Wild, BlessingKind.Player);
        blessing.RequiredFavorRank = 0;
        blessing.Cost = 0;

        var religion = TestFixtures.CreateTestReligion("test-religion", "Test", DeityDomain.Craft, "player-uid");
        religion.PrestigeRank = PrestigeRank.Renowned;

        // playerFavorRank (Wild) = Disciple would give 3 slots if it drove the cap;
        // slotCapFavorRank (patron Craft) = Initiate gives 2.
        var (canUnlock, reason) = registry.CanUnlockBlessing(
            "player-uid", FavorRank.Disciple, FavorRank.Initiate, playerData, religion, blessing);

        Assert.False(canUnlock);
        Assert.Contains("2/2", reason);
    }

    #endregion

    #region CanUnlockBlessing Tests - Religion Blessings

    [Fact]
    public void CanUnlockBlessing_ReligionBlessing_WithoutReligion_ReturnsFalse()
    {
        // Arrange
        var playerData = TestFixtures.CreateTestPlayerReligionData("player-uid", DeityDomain.Craft, null);
        var blessing = TestFixtures.CreateTestBlessing("test", "Test", DeityDomain.Craft, BlessingKind.Religion);

        // Act
        var (canUnlock, reason) = _registry.CanUnlockBlessing("player-uid", FavorRank.Initiate, FavorRank.Initiate, playerData, null, blessing);

        // Assert
        Assert.False(canUnlock);
        Assert.Equal("Not in a religion", reason);
    }

    [Fact]
    public void CanUnlockBlessing_ReligionBlessing_AlreadyUnlocked_ReturnsFalse()
    {
        // Arrange
        var playerData = TestFixtures.CreateTestPlayerReligionData("player-uid", DeityDomain.Craft, "religion-uid");
        var religionData = TestFixtures.CreateTestReligion("religion-uid", "Test Religion", DeityDomain.Craft);
        religionData.UnlockBlessing("test_blessing");

        var blessing =
            TestFixtures.CreateTestBlessing("test_blessing", "Test", DeityDomain.Craft, BlessingKind.Religion);

        // Act
        var (canUnlock, reason) = _registry.CanUnlockBlessing("player-uid", FavorRank.Initiate, FavorRank.Initiate, playerData, religionData, blessing);

        // Assert
        Assert.False(canUnlock);
        Assert.Equal("Blessing already unlocked", reason);
    }

    [Fact]
    public void CanUnlockBlessing_ReligionBlessing_InsufficientPrestigeRank_ReturnsFalse()
    {
        // Arrange
        var playerData = TestFixtures.CreateTestPlayerReligionData("player-uid", DeityDomain.Craft, "religion-uid");
        var religionData = TestFixtures.CreateTestReligion("religion-uid", "Test Religion", DeityDomain.Craft);
        religionData.PrestigeRank = PrestigeRank.Fledgling;

        var blessing = TestFixtures.CreateTestBlessing("test", "Test", DeityDomain.Craft, BlessingKind.Religion);
        blessing.RequiredPrestigeRank = 2; // Requires Renowned

        // Act
        var (canUnlock, reason) = _registry.CanUnlockBlessing("player-uid", FavorRank.Initiate, FavorRank.Initiate, playerData, religionData, blessing);

        // Assert
        Assert.False(canUnlock);
        Assert.Contains("Religion requires Renowned prestige rank", reason);
    }

    [Fact]
    public void CanUnlockBlessing_ReligionBlessing_NonCapstoneWrongDeity_ReturnsTrue()
    {
        // Phase 3: non-capstone religion blessings unlock from any deity tree regardless of patron.
        var playerData = TestFixtures.CreateTestPlayerReligionData("player-uid", DeityDomain.Craft, "religion-uid");
        var religionData = TestFixtures.CreateTestReligion("religion-uid", "Test Religion", DeityDomain.Craft);
        var blessing = TestFixtures.CreateTestBlessing("test", "Test", DeityDomain.Wild, BlessingKind.Religion);
        blessing.RequiresPatron = false;

        var (canUnlock, _) = _registry.CanUnlockBlessing("player-uid", FavorRank.Initiate, FavorRank.Initiate, playerData, religionData, blessing);

        Assert.True(canUnlock);
    }

    [Fact]
    public void CanUnlockBlessing_ReligionBlessing_CapstoneNonPatron_ReturnsFalse()
    {
        var playerData = TestFixtures.CreateTestPlayerReligionData("player-uid", DeityDomain.Craft, "religion-uid");
        var religionData = TestFixtures.CreateTestReligion("religion-uid", "Test Religion", DeityDomain.Craft);
        var blessing = TestFixtures.CreateTestBlessing("pantheon_of_wild", "Pantheon of Wild", DeityDomain.Wild, BlessingKind.Religion);
        blessing.RequiresPatron = true;

        var (canUnlock, reason) = _registry.CanUnlockBlessing("player-uid", FavorRank.Initiate, FavorRank.Initiate, playerData, religionData, blessing);

        Assert.False(canUnlock);
        Assert.Contains("Capstone blessing requires patron deity: Wild", reason);
    }

    [Fact]
    public void CanUnlockBlessing_ReligionBlessing_CapstonePatron_ReturnsTrue()
    {
        var playerData = TestFixtures.CreateTestPlayerReligionData("player-uid", DeityDomain.Wild, "religion-uid");
        var religionData = TestFixtures.CreateTestReligion("religion-uid", "Test Religion", DeityDomain.Wild);
        var blessing = TestFixtures.CreateTestBlessing("pantheon_of_wild", "Pantheon of Wild", DeityDomain.Wild, BlessingKind.Religion);
        blessing.RequiresPatron = true;

        var (canUnlock, _) = _registry.CanUnlockBlessing("player-uid", FavorRank.Initiate, FavorRank.Initiate, playerData, religionData, blessing);

        Assert.True(canUnlock);
    }

    [Fact]
    public void CanUnlockBlessing_ReligionBlessing_NonPatronCostIs1_5x()
    {
        // Patron=Craft, blessing domain=Wild, cost=400 → adjusted=600. Religion has 500 → insufficient.
        var playerData = TestFixtures.CreateTestPlayerReligionData("player-uid", DeityDomain.Craft, "religion-uid");
        var religionData = TestFixtures.CreateTestReligion("religion-uid", "Test Religion", DeityDomain.Craft);
        religionData.AddPrestige(500);

        var blessing = TestFixtures.CreateTestBlessing("test", "Test", DeityDomain.Wild, BlessingKind.Religion);
        blessing.RequiredPrestigeRank = 0;
        blessing.Cost = 400;

        var (canUnlock, reason) = _registry.CanUnlockBlessing("player-uid", FavorRank.Initiate, FavorRank.Initiate, playerData, religionData, blessing);

        Assert.False(canUnlock);
        Assert.Contains("Insufficient prestige", reason);
        Assert.Contains("requires 600", reason);
    }

    [Fact]
    public void CanUnlockBlessing_ReligionBlessing_AllRequirementsMet_ReturnsTrue()
    {
        // Arrange
        var playerData = TestFixtures.CreateTestPlayerReligionData("player-uid", DeityDomain.Craft, "religion-uid");
        var religionData = TestFixtures.CreateTestReligion("religion-uid", "Test Religion", DeityDomain.Craft);
        religionData.PrestigeRank = PrestigeRank.Established;

        var blessing = TestFixtures.CreateTestBlessing("test", "Test", DeityDomain.Craft, BlessingKind.Religion);
        blessing.RequiredPrestigeRank = 1;

        // Act
        var (canUnlock, reason) = _registry.CanUnlockBlessing("player-uid", FavorRank.Initiate, FavorRank.Initiate, playerData, religionData, blessing);

        // Assert
        Assert.True(canUnlock);
        Assert.Equal("Can unlock", reason);
    }

    [Fact]
    public void CanUnlockBlessing_ReligionBlessing_AtSlotCap_ReturnsFalse()
    {
        // Fledgling cap is 2; inscribe two then attempt a third.
        var playerData = TestFixtures.CreateTestPlayerReligionData("player-uid", DeityDomain.Craft, "religion-uid");
        var religionData = TestFixtures.CreateTestReligion("religion-uid", "Test Religion", DeityDomain.Craft);
        religionData.PrestigeRank = PrestigeRank.Fledgling;
        religionData.UnlockBlessing("vow_a");
        religionData.UnlockBlessing("vow_b");

        var blessing = TestFixtures.CreateTestBlessing("vow_c", "Vow C", DeityDomain.Craft, BlessingKind.Religion);
        blessing.RequiredPrestigeRank = 0;
        blessing.Cost = 0;

        var (canUnlock, reason) = _registry.CanUnlockBlessing("player-uid", FavorRank.Initiate, FavorRank.Initiate, playerData, religionData, blessing);

        Assert.False(canUnlock);
        Assert.Contains("Religion blessing slots full (2/2)", reason);
    }

    [Fact]
    public void CanUnlockBlessing_ReligionBlessing_UnderSlotCap_ReturnsTrue()
    {
        // Fledgling cap is 2; one inscribed leaves room for one more.
        var playerData = TestFixtures.CreateTestPlayerReligionData("player-uid", DeityDomain.Craft, "religion-uid");
        var religionData = TestFixtures.CreateTestReligion("religion-uid", "Test Religion", DeityDomain.Craft);
        religionData.PrestigeRank = PrestigeRank.Fledgling;
        religionData.UnlockBlessing("vow_a");

        var blessing = TestFixtures.CreateTestBlessing("vow_b", "Vow B", DeityDomain.Craft, BlessingKind.Religion);
        blessing.RequiredPrestigeRank = 0;
        blessing.Cost = 0;

        var (canUnlock, _) = _registry.CanUnlockBlessing("player-uid", FavorRank.Initiate, FavorRank.Initiate, playerData, religionData, blessing);

        Assert.True(canUnlock);
    }

    [Fact]
    public void CanUnlockBlessing_ReligionBlessing_AlreadyUnlockedAtCap_ReportsAlreadyUnlocked_NotCap()
    {
        // At the cap, re-attempting an inscribed blessing must report "already unlocked", not the
        // cap message — the already-unlocked check runs first.
        var playerData = TestFixtures.CreateTestPlayerReligionData("player-uid", DeityDomain.Craft, "religion-uid");
        var religionData = TestFixtures.CreateTestReligion("religion-uid", "Test Religion", DeityDomain.Craft);
        religionData.PrestigeRank = PrestigeRank.Fledgling;
        religionData.UnlockBlessing("vow_a");
        religionData.UnlockBlessing("vow_b"); // at cap (2/2)

        var blessing = TestFixtures.CreateTestBlessing("vow_a", "Vow A", DeityDomain.Craft, BlessingKind.Religion);

        var (canUnlock, reason) = _registry.CanUnlockBlessing("player-uid", FavorRank.Initiate, FavorRank.Initiate, playerData, religionData, blessing);

        Assert.False(canUnlock);
        Assert.Equal("Blessing already unlocked", reason);
    }

    [Fact]
    public void CanUnlockBlessing_ReligionBlessing_HigherPrestigeRankRaisesCap()
    {
        // Two inscribed blocks Fledgling (cap 2) but fits Renowned (cap 4).
        var playerData = TestFixtures.CreateTestPlayerReligionData("player-uid", DeityDomain.Craft, "religion-uid");
        var religionData = TestFixtures.CreateTestReligion("religion-uid", "Test Religion", DeityDomain.Craft);
        religionData.UnlockBlessing("vow_a");
        religionData.UnlockBlessing("vow_b");

        var blessing = TestFixtures.CreateTestBlessing("vow_c", "Vow C", DeityDomain.Craft, BlessingKind.Religion);
        blessing.RequiredPrestigeRank = 0;
        blessing.Cost = 0;

        religionData.PrestigeRank = PrestigeRank.Fledgling;
        var (atFledgling, _) = _registry.CanUnlockBlessing("player-uid", FavorRank.Initiate, FavorRank.Initiate, playerData, religionData, blessing);
        Assert.False(atFledgling);

        religionData.PrestigeRank = PrestigeRank.Renowned;
        var (atRenowned, _) = _registry.CanUnlockBlessing("player-uid", FavorRank.Initiate, FavorRank.Initiate, playerData, religionData, blessing);
        Assert.True(atRenowned);
    }

    [Fact]
    public void CanUnlockBlessing_ReligionBlessing_InsufficientPrestige_ReturnsFalse()
    {
        // Arrange
        var playerData = TestFixtures.CreateTestPlayerReligionData("player-uid", DeityDomain.Craft, "religion-uid");
        var religionData = TestFixtures.CreateTestReligion("religion-uid", "Test Religion", DeityDomain.Craft);
        religionData.AddPrestige(300); // Not enough for cost of 500

        var blessing = TestFixtures.CreateTestBlessing("test", "Test", DeityDomain.Craft, BlessingKind.Religion);
        blessing.RequiredPrestigeRank = 0;
        blessing.Cost = 500;

        // Act
        var (canUnlock, reason) = _registry.CanUnlockBlessing("player-uid", FavorRank.Initiate, FavorRank.Initiate, playerData, religionData, blessing);

        // Assert
        Assert.False(canUnlock);
        Assert.Contains("Insufficient prestige", reason);
        Assert.Contains("requires 500", reason);
        Assert.Contains("have 300", reason);
    }

    [Fact]
    public void CanUnlockBlessing_ReligionBlessing_SufficientPrestige_ReturnsTrue()
    {
        // Arrange
        var playerData = TestFixtures.CreateTestPlayerReligionData("player-uid", DeityDomain.Craft, "religion-uid");
        var religionData = TestFixtures.CreateTestReligion("religion-uid", "Test Religion", DeityDomain.Craft);
        religionData.AddPrestige(1000); // Enough for cost of 500

        var blessing = TestFixtures.CreateTestBlessing("test", "Test", DeityDomain.Craft, BlessingKind.Religion);
        blessing.RequiredPrestigeRank = 0;
        blessing.Cost = 500;

        // Act
        var (canUnlock, reason) = _registry.CanUnlockBlessing("player-uid", FavorRank.Initiate, FavorRank.Initiate, playerData, religionData, blessing);

        // Assert
        Assert.True(canUnlock);
        Assert.Equal("Can unlock", reason);
    }

    [Fact]
    public void CanUnlockBlessing_ReligionBlessing_ZeroCost_ReturnsTrue()
    {
        // Arrange
        var playerData = TestFixtures.CreateTestPlayerReligionData("player-uid", DeityDomain.Craft, "religion-uid");
        var religionData = TestFixtures.CreateTestReligion("religion-uid", "Test Religion", DeityDomain.Craft);
        // No prestige added

        var blessing = TestFixtures.CreateTestBlessing("test", "Test", DeityDomain.Craft, BlessingKind.Religion);
        blessing.RequiredPrestigeRank = 0;
        blessing.Cost = 0; // Free blessing

        // Act
        var (canUnlock, reason) = _registry.CanUnlockBlessing("player-uid", FavorRank.Initiate, FavorRank.Initiate, playerData, religionData, blessing);

        // Assert
        Assert.True(canUnlock);
        Assert.Equal("Can unlock", reason);
    }

    [Fact]
    public void CanUnlockBlessing_ReligionBlessing_InsufficientPrestige_WithSkipCostCheck_ReturnsTrue()
    {
        // Arrange - insufficient prestige, but skipCostCheck=true should allow unlock
        var playerData = TestFixtures.CreateTestPlayerReligionData("player-uid", DeityDomain.Craft, "religion-uid");
        var religionData = TestFixtures.CreateTestReligion("religion-uid", "Test Religion", DeityDomain.Craft);
        religionData.AddPrestige(300); // Not enough for cost of 500

        var blessing = TestFixtures.CreateTestBlessing("test", "Test", DeityDomain.Craft, BlessingKind.Religion);
        blessing.RequiredPrestigeRank = 0;
        blessing.Cost = 500; // More than available prestige

        // Act - pass skipCostCheck: true
        var (canUnlock, reason) = _registry.CanUnlockBlessing("player-uid", FavorRank.Initiate, FavorRank.Initiate, playerData, religionData, blessing, skipCostCheck: true);

        // Assert - should return true because cost check is skipped
        Assert.True(canUnlock);
        Assert.Equal("Can unlock", reason);
    }

    #endregion

    #region CanUnlockBlessing Tests - Branch Exclusivity

    [Fact]
    public void CanUnlockBlessing_PlayerBlessing_BranchLocked_ReturnsFalse()
    {
        // Arrange
        var playerData = TestFixtures.CreateTestPlayerReligionData("player-uid", DeityDomain.Craft, "religion-uid");
        playerData.AddFavor(DeityDomain.Craft, 200);
        // Player committed to "Forge" branch, locking "Endurance"
        playerData.CommitToBranch(DeityDomain.Craft, "Forge", new[] { "Endurance" });

        var blessing = TestFixtures.CreateTestBlessing("test", "Test", DeityDomain.Craft, BlessingKind.Player);
        blessing.RequiredFavorRank = 0;
        blessing.Branch = "Endurance"; // This branch is locked

        var religion = TestFixtures.CreateTestReligion("test-religion", "Test", DeityDomain.Craft, "player-uid");

        // Act
        var (canUnlock, reason) = _registry.CanUnlockBlessing("player-uid", FavorRank.Initiate, FavorRank.Initiate, playerData, religion, blessing);

        // Assert
        Assert.False(canUnlock);
        Assert.Contains("Branch 'Endurance' is locked", reason);
        Assert.Contains("committed to 'Forge'", reason);
    }

    [Fact]
    public void CanUnlockBlessing_PlayerBlessing_SameBranchAsCommitted_ReturnsTrue()
    {
        // Arrange
        var playerData = TestFixtures.CreateTestPlayerReligionData("player-uid", DeityDomain.Craft, "religion-uid");
        playerData.AddFavor(DeityDomain.Craft, 200);
        // Player committed to "Forge" branch
        playerData.CommitToBranch(DeityDomain.Craft, "Forge", new[] { "Endurance" });

        var blessing = TestFixtures.CreateTestBlessing("test", "Test", DeityDomain.Craft, BlessingKind.Player);
        blessing.RequiredFavorRank = 0;
        blessing.Branch = "Forge"; // Same as committed - should work

        var religion = TestFixtures.CreateTestReligion("test-religion", "Test", DeityDomain.Craft, "player-uid");

        // Act
        var (canUnlock, reason) = _registry.CanUnlockBlessing("player-uid", FavorRank.Initiate, FavorRank.Initiate, playerData, religion, blessing);

        // Assert
        Assert.True(canUnlock);
        Assert.Equal("Can unlock", reason);
    }

    [Fact]
    public void CanUnlockBlessing_PlayerBlessing_NoBranchRestriction_ReturnsTrue()
    {
        // Arrange
        var playerData = TestFixtures.CreateTestPlayerReligionData("player-uid", DeityDomain.Craft, "religion-uid");
        playerData.AddFavor(DeityDomain.Craft, 200);
        // Player committed to "Forge" branch
        playerData.CommitToBranch(DeityDomain.Craft, "Forge", new[] { "Endurance" });

        var blessing = TestFixtures.CreateTestBlessing("test", "Test", DeityDomain.Craft, BlessingKind.Player);
        blessing.RequiredFavorRank = 0;
        blessing.Branch = null; // Shared blessing - no branch restriction

        var religion = TestFixtures.CreateTestReligion("test-religion", "Test", DeityDomain.Craft, "player-uid");

        // Act
        var (canUnlock, reason) = _registry.CanUnlockBlessing("player-uid", FavorRank.Initiate, FavorRank.Initiate, playerData, religion, blessing);

        // Assert
        Assert.True(canUnlock);
        Assert.Equal("Can unlock", reason);
    }

    [Fact]
    public void CanUnlockBlessing_PlayerBlessing_EmptyBranch_ReturnsTrue()
    {
        // Arrange
        var playerData = TestFixtures.CreateTestPlayerReligionData("player-uid", DeityDomain.Craft, "religion-uid");
        playerData.AddFavor(DeityDomain.Craft, 200);
        // Player committed to "Forge" branch
        playerData.CommitToBranch(DeityDomain.Craft, "Forge", new[] { "Endurance" });

        var blessing = TestFixtures.CreateTestBlessing("test", "Test", DeityDomain.Craft, BlessingKind.Player);
        blessing.RequiredFavorRank = 0;
        blessing.Branch = ""; // Empty string should be treated as shared

        var religion = TestFixtures.CreateTestReligion("test-religion", "Test", DeityDomain.Craft, "player-uid");

        // Act
        var (canUnlock, reason) = _registry.CanUnlockBlessing("player-uid", FavorRank.Initiate, FavorRank.Initiate, playerData, religion, blessing);

        // Assert
        Assert.True(canUnlock);
        Assert.Equal("Can unlock", reason);
    }

    [Fact]
    public void CanUnlockBlessing_PlayerBlessing_BranchLockedInDifferentDomain_ReturnsTrue()
    {
        // Arrange
        var playerData = TestFixtures.CreateTestPlayerReligionData("player-uid", DeityDomain.Wild, "religion-uid");
        playerData.AddFavor(DeityDomain.Craft, 200);
        // Player committed to "Forge" branch in CRAFT domain
        playerData.CommitToBranch(DeityDomain.Craft, "Forge", new[] { "Endurance" });

        var blessing = TestFixtures.CreateTestBlessing("test", "Test", DeityDomain.Wild, BlessingKind.Player);
        blessing.RequiredFavorRank = 0;
        blessing.Branch = "Endurance"; // Same name but different domain

        var religion = TestFixtures.CreateTestReligion("test-religion", "Test", DeityDomain.Wild, "player-uid");

        // Act
        var (canUnlock, reason) = _registry.CanUnlockBlessing("player-uid", FavorRank.Initiate, FavorRank.Initiate, playerData, religion, blessing);

        // Assert - Should pass because branch lock is domain-specific
        Assert.True(canUnlock);
        Assert.Equal("Can unlock", reason);
    }

    [Fact]
    public void CanUnlockBlessing_PlayerBlessing_NoPreviousCommitment_ReturnsTrue()
    {
        // Arrange
        var playerData = TestFixtures.CreateTestPlayerReligionData("player-uid", DeityDomain.Craft, "religion-uid");
        playerData.AddFavor(DeityDomain.Craft, 200);
        // No branch commitment yet

        var blessing = TestFixtures.CreateTestBlessing("test", "Test", DeityDomain.Craft, BlessingKind.Player);
        blessing.RequiredFavorRank = 0;
        blessing.Branch = "Forge";
        blessing.ExclusiveBranches = new List<string> { "Endurance" };

        var religion = TestFixtures.CreateTestReligion("test-religion", "Test", DeityDomain.Craft, "player-uid");

        // Act
        var (canUnlock, reason) = _registry.CanUnlockBlessing("player-uid", FavorRank.Initiate, FavorRank.Initiate, playerData, religion, blessing);

        // Assert - Should pass because no branch is locked yet
        Assert.True(canUnlock);
        Assert.Equal("Can unlock", reason);
    }

    #endregion

    #region AdjustedCost Tests

    [Fact]
    public void AdjustedCost_PatronDomain_ReturnsBaseCost()
    {
        var blessing = new Blessing { Cost = 100, Domain = DeityDomain.Craft };
        var religion = TestFixtures.CreateTestReligion("uid", "Test", DeityDomain.Craft, "founder");

        Assert.Equal(100, BlessingRegistry.AdjustedCost(blessing, religion));
    }

    [Fact]
    public void AdjustedCost_NonPatronDomain_AppliesOneAndAHalfMultiplier()
    {
        var blessing = new Blessing { Cost = 100, Domain = DeityDomain.Wild };
        var religion = TestFixtures.CreateTestReligion("uid", "Test", DeityDomain.Craft, "founder");

        Assert.Equal(150, BlessingRegistry.AdjustedCost(blessing, religion));
    }

    [Fact]
    public void AdjustedCost_ZeroBaseCost_ReturnsZero()
    {
        var blessing = new Blessing { Cost = 0, Domain = DeityDomain.Wild };
        var religion = TestFixtures.CreateTestReligion("uid", "Test", DeityDomain.Craft, "founder");

        Assert.Equal(0, BlessingRegistry.AdjustedCost(blessing, religion));
    }

    #endregion
}
