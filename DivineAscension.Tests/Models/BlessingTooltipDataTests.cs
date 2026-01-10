using System.Diagnostics.CodeAnalysis;
using DivineAscension.Models;
using DivineAscension.Models.Enum;
using DivineAscension.Tests.Helpers;

namespace DivineAscension.Tests.Models;

/// <summary>
///     Unit tests for BlessingTooltipData model
/// </summary>
[ExcludeFromCodeCoverage]
public class BlessingTooltipDataTests
{
    public BlessingTooltipDataTests()
    {
        TestFixtures.InitializeLocalizationForTests();
    }

    #region Stat Formatting Tests

    [Theory]
    [InlineData("walkspeed", 0.15f, "+15% Movement Speed")]
    [InlineData("meleeWeaponsDamage", 0.20f, "+20% Melee Damage")]
    [InlineData("rangedWeaponsDamage", -0.10f, "-10% Ranged Damage")]
    [InlineData("maxhealthExtraPoints", 5f, "+5 Max Health")]
    [InlineData("armor", 3f, "+3 Armor")]
    public void FromBlessingAndState_StatModifiers_FormatCorrectly(string statKey, float value, string expectedContains)
    {
        // Arrange
        var blessing = TestFixtures.CreateTestBlessing("test-blessing", "Test");
        blessing.StatModifiers.Clear(); // Clear default modifiers from TestFixtures
        blessing.StatModifiers[statKey] = value;

        var state = new BlessingNodeState(blessing);

        // Act
        var tooltip = BlessingTooltipData.FromBlessingAndState(blessing, state);

        // Assert
        Assert.Single(tooltip.FormattedStats);
        Assert.Contains(expectedContains, tooltip.FormattedStats[0]);
    }

    #endregion

    #region Constructor and Property Tests

    [Fact]
    public void Constructor_InitializesWithDefaultValues()
    {
        // Act
        var tooltip = new BlessingTooltipData();

        // Assert
        Assert.Equal(string.Empty, tooltip.Name);
        Assert.Equal(string.Empty, tooltip.Description);
        Assert.Equal(BlessingCategory.Combat, tooltip.Category); // Default enum value
        Assert.Equal(BlessingKind.Player, tooltip.Kind); // Default enum value
        Assert.Equal(0, tooltip.Tier);
        Assert.Equal(string.Empty, tooltip.RequiredFavorRank);
        Assert.Equal(string.Empty, tooltip.RequiredPrestigeRank);
        Assert.Empty(tooltip.PrerequisiteNames);
        Assert.Empty(tooltip.FormattedStats);
        Assert.Empty(tooltip.SpecialEffectDescriptions);
        Assert.False(tooltip.IsUnlocked);
        Assert.False(tooltip.CanUnlock);
        Assert.Equal(string.Empty, tooltip.UnlockBlockReason);
    }

    [Fact]
    public void Properties_CanBeSetAndRetrieved()
    {
        // Arrange
        var tooltip = new BlessingTooltipData();

        // Act
        tooltip.Name = "Test Blessing";
        tooltip.Description = "Test Description";
        tooltip.Category = BlessingCategory.Defense;
        tooltip.Kind = BlessingKind.Religion;
        tooltip.Tier = 2;
        tooltip.RequiredFavorRank = "Zealot";
        tooltip.RequiredPrestigeRank = "Renowned";
        tooltip.PrerequisiteNames = new List<string> { "Prereq1", "Prereq2" };
        tooltip.FormattedStats = new List<string> { "+10% Melee Damage" };
        tooltip.SpecialEffectDescriptions = new List<string> { "Special Effect 1" };
        tooltip.IsUnlocked = true;
        tooltip.CanUnlock = true;
        tooltip.UnlockBlockReason = "Test Reason";

        // Assert
        Assert.Equal("Test Blessing", tooltip.Name);
        Assert.Equal("Test Description", tooltip.Description);
        Assert.Equal(BlessingCategory.Defense, tooltip.Category);
        Assert.Equal(BlessingKind.Religion, tooltip.Kind);
        Assert.Equal(2, tooltip.Tier);
        Assert.Equal("Zealot", tooltip.RequiredFavorRank);
        Assert.Equal("Renowned", tooltip.RequiredPrestigeRank);
        Assert.Equal(2, tooltip.PrerequisiteNames.Count);
        Assert.Single(tooltip.FormattedStats);
        Assert.Single(tooltip.SpecialEffectDescriptions);
        Assert.True(tooltip.IsUnlocked);
        Assert.True(tooltip.CanUnlock);
        Assert.Equal("Test Reason", tooltip.UnlockBlockReason);
    }

    #endregion

    #region FromBlessingAndState Tests

    [Fact]
    public void FromBlessingAndState_CreatesBasicTooltipData()
    {
        // Arrange
        var blessing = TestFixtures.CreateTestBlessing("test-blessing", "Test Blessing");
        blessing.Description = "Test Description";
        blessing.Category = BlessingCategory.Combat;
        blessing.Kind = BlessingKind.Player;
        blessing.RequiredFavorRank = 2; // Zealot

        var state = new BlessingNodeState(blessing)
        {
            Tier = 3,
            IsUnlocked = false,
            CanUnlock = true
        };

        // Act
        var tooltip = BlessingTooltipData.FromBlessingAndState(blessing, state);

        // Assert
        Assert.Equal("Test Blessing", tooltip.Name);
        Assert.Equal("Test Description", tooltip.Description);
        Assert.Equal(BlessingCategory.Combat, tooltip.Category);
        Assert.Equal(BlessingKind.Player, tooltip.Kind);
        Assert.Equal(3, tooltip.Tier);
        Assert.Equal("Zealot", tooltip.RequiredFavorRank);
        Assert.False(tooltip.IsUnlocked);
        Assert.True(tooltip.CanUnlock);
    }

    [Fact]
    public void FromBlessingAndState_ForPlayerBlessing_SetsFavorRank()
    {
        // Arrange
        var blessing = TestFixtures.CreateTestBlessing("player-blessing", "Player Blessing");
        blessing.Kind = BlessingKind.Player;
        blessing.RequiredFavorRank = 3; // Champion

        var state = new BlessingNodeState(blessing);

        // Act
        var tooltip = BlessingTooltipData.FromBlessingAndState(blessing, state);

        // Assert
        Assert.Equal("Champion", tooltip.RequiredFavorRank);
        Assert.Equal(string.Empty, tooltip.RequiredPrestigeRank);
    }

    [Fact]
    public void FromBlessingAndState_ForReligionBlessing_SetsPrestigeRank()
    {
        // Arrange
        var blessing = TestFixtures.CreateTestBlessing("religion-blessing", "Religion Blessing");
        blessing.Kind = BlessingKind.Religion;
        blessing.RequiredPrestigeRank = 2; // Renowned

        var state = new BlessingNodeState(blessing);

        // Act
        var tooltip = BlessingTooltipData.FromBlessingAndState(blessing, state);

        // Assert
        Assert.Equal("Renowned", tooltip.RequiredPrestigeRank);
        Assert.Equal(string.Empty, tooltip.RequiredFavorRank);
    }

    [Fact]
    public void FromBlessingAndState_WithStatModifiers_FormatsCorrectly()
    {
        // Arrange
        var blessing = TestFixtures.CreateTestBlessing("stat-blessing", "Stat Blessing");
        blessing.StatModifiers["walkspeed"] = 0.15f;
        blessing.StatModifiers["meleeWeaponsDamage"] = 0.20f;

        var state = new BlessingNodeState(blessing);

        // Act
        var tooltip = BlessingTooltipData.FromBlessingAndState(blessing, state);

        // Assert
        Assert.Equal(2, tooltip.FormattedStats.Count);
        Assert.Contains(tooltip.FormattedStats, s => s.Contains("15") && s.Contains("Movement Speed"));
        Assert.Contains(tooltip.FormattedStats, s => s.Contains("20") && s.Contains("Melee Damage"));
    }

    [Fact]
    public void FromBlessingAndState_WithSpecialEffects_AddsToTooltip()
    {
        // Arrange
        var blessing = TestFixtures.CreateTestBlessing("special-blessing", "Special Blessing");
        blessing.SpecialEffects.Add("Special Effect 1");
        blessing.SpecialEffects.Add("Special Effect 2");

        var state = new BlessingNodeState(blessing);

        // Act
        var tooltip = BlessingTooltipData.FromBlessingAndState(blessing, state);

        // Assert
        Assert.Equal(2, tooltip.SpecialEffectDescriptions.Count);
        Assert.Contains("Special Effect 1", tooltip.SpecialEffectDescriptions);
        Assert.Contains("Special Effect 2", tooltip.SpecialEffectDescriptions);
    }

    [Fact]
    public void FromBlessingAndState_WithPrerequisites_AddsPrerequisiteNames()
    {
        // Arrange
        var prereq1 = TestFixtures.CreateTestBlessing("prereq1", "Prerequisite 1");
        var prereq2 = TestFixtures.CreateTestBlessing("prereq2", "Prerequisite 2");

        var blessing = TestFixtures.CreateTestBlessing("main-blessing", "Main Blessing");
        blessing.PrerequisiteBlessings.Add("prereq1");
        blessing.PrerequisiteBlessings.Add("prereq2");

        var registry = new Dictionary<string, Blessing>
        {
            { "prereq1", prereq1 },
            { "prereq2", prereq2 }
        };

        var state = new BlessingNodeState(blessing);

        // Act
        var tooltip = BlessingTooltipData.FromBlessingAndState(blessing, state, registry);

        // Assert
        Assert.Equal(2, tooltip.PrerequisiteNames.Count);
        Assert.Contains("Prerequisite 1", tooltip.PrerequisiteNames);
        Assert.Contains("Prerequisite 2", tooltip.PrerequisiteNames);
    }

    [Fact]
    public void FromBlessingAndState_WithMissingPrerequisite_SkipsIt()
    {
        // Arrange
        var prereq1 = TestFixtures.CreateTestBlessing("prereq1", "Prerequisite 1");

        var blessing = TestFixtures.CreateTestBlessing("main-blessing", "Main Blessing");
        blessing.PrerequisiteBlessings.Add("prereq1");
        blessing.PrerequisiteBlessings.Add("prereq2"); // Not in registry

        var registry = new Dictionary<string, Blessing>
        {
            { "prereq1", prereq1 }
        };

        var state = new BlessingNodeState(blessing);

        // Act
        var tooltip = BlessingTooltipData.FromBlessingAndState(blessing, state, registry);

        // Assert
        Assert.Single(tooltip.PrerequisiteNames);
        Assert.Contains("Prerequisite 1", tooltip.PrerequisiteNames);
    }

    [Fact]
    public void FromBlessingAndState_LockedPlayerBlessingWithoutPrereqs_SetsUnlockBlockReason()
    {
        // Arrange
        var blessing = TestFixtures.CreateTestBlessing("locked-blessing", "Locked Blessing");
        blessing.Kind = BlessingKind.Player;
        blessing.RequiredFavorRank = 2; // Zealot

        var state = new BlessingNodeState(blessing)
        {
            IsUnlocked = false,
            CanUnlock = false
        };

        // Act
        var tooltip = BlessingTooltipData.FromBlessingAndState(blessing, state);

        // Assert
        Assert.Equal("Requires Zealot rank", tooltip.UnlockBlockReason);
    }

    [Fact]
    public void FromBlessingAndState_LockedReligionBlessingWithoutPrereqs_SetsUnlockBlockReason()
    {
        // Arrange
        var blessing = TestFixtures.CreateTestBlessing("locked-blessing", "Locked Blessing");
        blessing.Kind = BlessingKind.Religion;
        blessing.RequiredPrestigeRank = 3; // Legendary

        var state = new BlessingNodeState(blessing)
        {
            IsUnlocked = false,
            CanUnlock = false
        };

        // Act
        var tooltip = BlessingTooltipData.FromBlessingAndState(blessing, state);

        // Assert
        Assert.Equal("Requires religion Legendary rank", tooltip.UnlockBlockReason);
    }

    [Fact]
    public void FromBlessingAndState_LockedWithPrereqs_DoesNotSetUnlockBlockReason()
    {
        // Arrange
        var blessing = TestFixtures.CreateTestBlessing("locked-blessing", "Locked Blessing");
        blessing.Kind = BlessingKind.Player;
        blessing.RequiredFavorRank = 2;
        blessing.PrerequisiteBlessings.Add("prereq1");

        var state = new BlessingNodeState(blessing)
        {
            IsUnlocked = false,
            CanUnlock = false
        };

        // Act
        var tooltip = BlessingTooltipData.FromBlessingAndState(blessing, state);

        // Assert
        Assert.Equal(string.Empty, tooltip.UnlockBlockReason);
    }

    [Fact]
    public void FromBlessingAndState_UnlockedBlessing_NoUnlockBlockReason()
    {
        // Arrange
        var blessing = TestFixtures.CreateTestBlessing("unlocked-blessing", "Unlocked Blessing");
        var state = new BlessingNodeState(blessing)
        {
            IsUnlocked = true,
            CanUnlock = false
        };

        // Act
        var tooltip = BlessingTooltipData.FromBlessingAndState(blessing, state);

        // Assert
        Assert.Equal(string.Empty, tooltip.UnlockBlockReason);
    }

    [Fact]
    public void FromBlessingAndState_UnlockableBlessing_NoUnlockBlockReason()
    {
        // Arrange
        var blessing = TestFixtures.CreateTestBlessing("unlockable-blessing", "Unlockable Blessing");
        var state = new BlessingNodeState(blessing)
        {
            IsUnlocked = false,
            CanUnlock = true
        };

        // Act
        var tooltip = BlessingTooltipData.FromBlessingAndState(blessing, state);

        // Assert
        Assert.Equal(string.Empty, tooltip.UnlockBlockReason);
    }

    #endregion

    #region Rank Name Tests

    [Theory]
    [InlineData(0, "Initiate")]
    [InlineData(1, "Disciple")]
    [InlineData(2, "Zealot")]
    [InlineData(3, "Champion")]
    [InlineData(4, "Avatar")]
    [InlineData(5, "Rank 5")]
    [InlineData(99, "Rank 99")]
    public void FromBlessingAndState_FavorRanks_FormatsCorrectly(int rank, string expected)
    {
        // Arrange
        var blessing = TestFixtures.CreateTestBlessing("test-blessing", "Test");
        blessing.Kind = BlessingKind.Player;
        blessing.RequiredFavorRank = rank;

        var state = new BlessingNodeState(blessing);

        // Act
        var tooltip = BlessingTooltipData.FromBlessingAndState(blessing, state);

        // Assert
        Assert.Equal(expected, tooltip.RequiredFavorRank);
    }

    [Theory]
    [InlineData(0, "Fledgling")]
    [InlineData(1, "Established")]
    [InlineData(2, "Renowned")]
    [InlineData(3, "Legendary")]
    [InlineData(4, "Mythic")]
    [InlineData(5, "Rank 5")]
    [InlineData(99, "Rank 99")]
    public void FromBlessingAndState_PrestigeRanks_FormatsCorrectly(int rank, string expected)
    {
        // Arrange
        var blessing = TestFixtures.CreateTestBlessing("test-blessing", "Test");
        blessing.Kind = BlessingKind.Religion;
        blessing.RequiredPrestigeRank = rank;

        var state = new BlessingNodeState(blessing);

        // Act
        var tooltip = BlessingTooltipData.FromBlessingAndState(blessing, state);

        // Assert
        Assert.Equal(expected, tooltip.RequiredPrestigeRank);
    }

    #endregion
}