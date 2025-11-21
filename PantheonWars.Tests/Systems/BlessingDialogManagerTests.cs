using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using PantheonWars.GUI;
using PantheonWars.Models;
using PantheonWars.Models.Enum;
using PantheonWars.Tests.Helpers;
using Xunit;

namespace PantheonWars.Tests.Systems;

[ExcludeFromCodeCoverage]
public class BlessingDialogManagerTests
{
    [Fact]
    public void TestPropertyInitialization()
    {
        var manager = new BlessingDialogManager(null!);
        Assert.Null(manager.CurrentReligionUID);
        Assert.Equal(DeityType.None, manager.CurrentDeity);
        Assert.Null(manager.CurrentReligionName);
        Assert.Null(manager.SelectedBlessingId);
        Assert.Null(manager.HoveringBlessingId);
        Assert.Equal(0f, manager.PlayerTreeScrollX);
        Assert.Equal(0f, manager.PlayerTreeScrollY);
        Assert.Equal(0f, manager.ReligionTreeScrollX);
        Assert.Equal(0f, manager.ReligionTreeScrollY);
        Assert.False(manager.IsDataLoaded);
    }

    [Fact]
    public void TestInitializeMethod()
    {
        var manager = new BlessingDialogManager(null!);
        manager.Initialize("religion123", DeityType.Aethra, "God of Warriors");
        Assert.Equal("religion123", manager.CurrentReligionUID);
        Assert.Equal(DeityType.Aethra, manager.CurrentDeity);
        Assert.Equal("God of Warriors", manager.CurrentReligionName);
        Assert.True(manager.IsDataLoaded);
        Assert.Null(manager.SelectedBlessingId);
        Assert.Null(manager.HoveringBlessingId);
        Assert.Equal(0f, manager.PlayerTreeScrollX);
        Assert.Equal(0f, manager.PlayerTreeScrollY);
        Assert.Equal(0f, manager.ReligionTreeScrollX);
        Assert.Equal(0f, manager.ReligionTreeScrollY);
    }

    [Fact]
    public void TestResetMethod()
    {
        var manager = new BlessingDialogManager(null!);
        manager.Initialize("religion123", DeityType.Aethra, "God of Warriors");
        manager.Reset();
        Assert.Null(manager.CurrentReligionUID);
        Assert.Equal(DeityType.None, manager.CurrentDeity);
        Assert.Null(manager.CurrentReligionName);
        Assert.Null(manager.SelectedBlessingId);
        Assert.Null(manager.HoveringBlessingId);
        Assert.Equal(0f, manager.PlayerTreeScrollX);
        Assert.Equal(0f, manager.PlayerTreeScrollY);
        Assert.Equal(0f, manager.ReligionTreeScrollX);
        Assert.Equal(0f, manager.ReligionTreeScrollY);
        Assert.False(manager.IsDataLoaded);
    }

    [Fact]
    public void TestSelectBlessing()
    {
        var manager = new BlessingDialogManager(null!);
        manager.SelectBlessing("blessing456");
        Assert.Equal("blessing456", manager.SelectedBlessingId);
    }

    [Fact]
    public void TestClearSelection()
    {
        var manager = new BlessingDialogManager(null!);
        manager.SelectBlessing("blessing456");
        manager.ClearSelection();
        Assert.Null(manager.SelectedBlessingId);
    }

    [Fact]
    public void TestHasReligion()
    {
        var manager = new BlessingDialogManager(null!);
        Assert.False(manager.HasReligion());

        manager.Initialize("religion123", DeityType.Aethra, "God of Warriors");
        Assert.True(manager.HasReligion());

        manager.Reset();
        Assert.False(manager.HasReligion());
    }

    #region LoadBlessingStates Tests

    [Fact]
    public void LoadBlessingStates_WithEmptyLists_ClearsStates()
    {
        // Arrange
        var manager = new BlessingDialogManager(null!);

        // Act
        manager.LoadBlessingStates(new List<Blessing>(), new List<Blessing>());

        // Assert
        Assert.Empty(manager.PlayerBlessingStates);
        Assert.Empty(manager.ReligionBlessingStates);
    }

    [Fact]
    public void LoadBlessingStates_WithPlayerBlessings_CreatesStates()
    {
        // Arrange
        var manager = new BlessingDialogManager(null!);
        var playerBlessings = new List<Blessing>
        {
            TestFixtures.CreateTestBlessing("player1", "Player Blessing 1"),
            TestFixtures.CreateTestBlessing("player2", "Player Blessing 2")
        };

        // Act
        manager.LoadBlessingStates(playerBlessings, new List<Blessing>());

        // Assert
        Assert.Equal(2, manager.PlayerBlessingStates.Count);
        Assert.True(manager.PlayerBlessingStates.ContainsKey("player1"));
        Assert.True(manager.PlayerBlessingStates.ContainsKey("player2"));
        Assert.Empty(manager.ReligionBlessingStates);
    }

    [Fact]
    public void LoadBlessingStates_WithReligionBlessings_CreatesStates()
    {
        // Arrange
        var manager = new BlessingDialogManager(null!);
        var religionBlessings = new List<Blessing>
        {
            TestFixtures.CreateTestBlessing("religion1", "Religion Blessing 1"),
            TestFixtures.CreateTestBlessing("religion2", "Religion Blessing 2")
        };

        // Act
        manager.LoadBlessingStates(new List<Blessing>(), religionBlessings);

        // Assert
        Assert.Empty(manager.PlayerBlessingStates);
        Assert.Equal(2, manager.ReligionBlessingStates.Count);
        Assert.True(manager.ReligionBlessingStates.ContainsKey("religion1"));
        Assert.True(manager.ReligionBlessingStates.ContainsKey("religion2"));
    }

    [Fact]
    public void LoadBlessingStates_WithBothTypes_CreatesBothStates()
    {
        // Arrange
        var manager = new BlessingDialogManager(null!);
        var playerBlessings = new List<Blessing>
        {
            TestFixtures.CreateTestBlessing("player1", "Player Blessing 1")
        };
        var religionBlessings = new List<Blessing>
        {
            TestFixtures.CreateTestBlessing("religion1", "Religion Blessing 1")
        };

        // Act
        manager.LoadBlessingStates(playerBlessings, religionBlessings);

        // Assert
        Assert.Single(manager.PlayerBlessingStates);
        Assert.Single(manager.ReligionBlessingStates);
    }

    [Fact]
    public void LoadBlessingStates_CalledTwice_ReplacesExistingStates()
    {
        // Arrange
        var manager = new BlessingDialogManager(null!);
        var firstBlessings = new List<Blessing>
        {
            TestFixtures.CreateTestBlessing("old1", "Old Blessing 1")
        };
        var secondBlessings = new List<Blessing>
        {
            TestFixtures.CreateTestBlessing("new1", "New Blessing 1"),
            TestFixtures.CreateTestBlessing("new2", "New Blessing 2")
        };

        // Act
        manager.LoadBlessingStates(firstBlessings, new List<Blessing>());
        manager.LoadBlessingStates(secondBlessings, new List<Blessing>());

        // Assert
        Assert.Equal(2, manager.PlayerBlessingStates.Count);
        Assert.False(manager.PlayerBlessingStates.ContainsKey("old1"));
        Assert.True(manager.PlayerBlessingStates.ContainsKey("new1"));
        Assert.True(manager.PlayerBlessingStates.ContainsKey("new2"));
    }

    #endregion

    #region GetBlessingState Tests

    [Fact]
    public void GetBlessingState_WithPlayerBlessing_ReturnsState()
    {
        // Arrange
        var manager = new BlessingDialogManager(null!);
        var blessing = TestFixtures.CreateTestBlessing("player1", "Player Blessing");
        manager.LoadBlessingStates(new List<Blessing> { blessing }, new List<Blessing>());

        // Act
        var state = manager.GetBlessingState("player1");

        // Assert
        Assert.NotNull(state);
        Assert.Equal("player1", state.Blessing.BlessingId);
    }

    [Fact]
    public void GetBlessingState_WithReligionBlessing_ReturnsState()
    {
        // Arrange
        var manager = new BlessingDialogManager(null!);
        var blessing = TestFixtures.CreateTestBlessing("religion1", "Religion Blessing");
        manager.LoadBlessingStates(new List<Blessing>(), new List<Blessing> { blessing });

        // Act
        var state = manager.GetBlessingState("religion1");

        // Assert
        Assert.NotNull(state);
        Assert.Equal("religion1", state.Blessing.BlessingId);
    }

    [Fact]
    public void GetBlessingState_WithNonExistentBlessing_ReturnsNull()
    {
        // Arrange
        var manager = new BlessingDialogManager(null!);

        // Act
        var state = manager.GetBlessingState("nonexistent");

        // Assert
        Assert.Null(state);
    }

    [Fact]
    public void GetBlessingState_PrefersPlayerBlessingOverReligion()
    {
        // Arrange
        var manager = new BlessingDialogManager(null!);
        var playerBlessing = TestFixtures.CreateTestBlessing("shared-id", "Player Blessing");
        var religionBlessing = TestFixtures.CreateTestBlessing("shared-id", "Religion Blessing");
        manager.LoadBlessingStates(
            new List<Blessing> { playerBlessing },
            new List<Blessing> { religionBlessing }
        );

        // Act
        var state = manager.GetBlessingState("shared-id");

        // Assert
        Assert.NotNull(state);
        Assert.Equal("Player Blessing", state.Blessing.Name);
    }

    #endregion

    #region GetSelectedBlessingState Tests

    [Fact]
    public void GetSelectedBlessingState_WithNoSelection_ReturnsNull()
    {
        // Arrange
        var manager = new BlessingDialogManager(null!);

        // Act
        var state = manager.GetSelectedBlessingState();

        // Assert
        Assert.Null(state);
    }

    [Fact]
    public void GetSelectedBlessingState_WithSelection_ReturnsState()
    {
        // Arrange
        var manager = new BlessingDialogManager(null!);
        var blessing = TestFixtures.CreateTestBlessing("blessing1", "Blessing 1");
        manager.LoadBlessingStates(new List<Blessing> { blessing }, new List<Blessing>());
        manager.SelectBlessing("blessing1");

        // Act
        var state = manager.GetSelectedBlessingState();

        // Assert
        Assert.NotNull(state);
        Assert.Equal("blessing1", state.Blessing.BlessingId);
    }

    [Fact]
    public void GetSelectedBlessingState_WithInvalidSelection_ReturnsNull()
    {
        // Arrange
        var manager = new BlessingDialogManager(null!);
        manager.SelectBlessing("nonexistent");

        // Act
        var state = manager.GetSelectedBlessingState();

        // Assert
        Assert.Null(state);
    }

    #endregion

    #region SetBlessingUnlocked Tests

    [Fact]
    public void SetBlessingUnlocked_WithExistingBlessing_UpdatesState()
    {
        // Arrange
        var manager = new BlessingDialogManager(null!);
        var blessing = TestFixtures.CreateTestBlessing("blessing1", "Blessing 1");
        manager.LoadBlessingStates(new List<Blessing> { blessing }, new List<Blessing>());

        // Act
        manager.SetBlessingUnlocked("blessing1", true);

        // Assert
        var state = manager.GetBlessingState("blessing1");
        Assert.NotNull(state);
        Assert.True(state.IsUnlocked);
    }

    [Fact]
    public void SetBlessingUnlocked_UpdatesVisualState()
    {
        // Arrange
        var manager = new BlessingDialogManager(null!);
        var blessing = TestFixtures.CreateTestBlessing("blessing1", "Blessing 1");
        manager.LoadBlessingStates(new List<Blessing> { blessing }, new List<Blessing>());

        // Act
        manager.SetBlessingUnlocked("blessing1", true);

        // Assert
        var state = manager.GetBlessingState("blessing1");
        Assert.NotNull(state);
        Assert.Equal(BlessingNodeVisualState.Unlocked, state.VisualState);
    }

    [Fact]
    public void SetBlessingUnlocked_WithNonExistentBlessing_DoesNotThrow()
    {
        // Arrange
        var manager = new BlessingDialogManager(null!);

        // Act & Assert - Should not throw
        manager.SetBlessingUnlocked("nonexistent", true);
    }

    #endregion

    #region RefreshAllBlessingStates Tests

    [Fact]
    public void RefreshAllBlessingStates_UpdatesPlayerBlessingStates()
    {
        // Arrange
        var manager = new BlessingDialogManager(null!);
        var blessing = TestFixtures.CreateTestBlessing("player1", "Player Blessing");
        blessing.Kind = BlessingKind.Player;
        blessing.RequiredFavorRank = 0; // No rank requirement
        manager.LoadBlessingStates(new List<Blessing> { blessing }, new List<Blessing>());
        manager.Initialize("religion1", DeityType.Aethra, "Test Religion", favorRank: 1);

        // Act
        manager.RefreshAllBlessingStates();

        // Assert
        var state = manager.GetBlessingState("player1");
        Assert.NotNull(state);
        Assert.True(state.CanUnlock);
        Assert.Equal(BlessingNodeVisualState.Unlockable, state.VisualState);
    }

    [Fact]
    public void RefreshAllBlessingStates_UpdatesReligionBlessingStates()
    {
        // Arrange
        var manager = new BlessingDialogManager(null!);
        var blessing = TestFixtures.CreateTestBlessing("religion1", "Religion Blessing");
        blessing.Kind = BlessingKind.Religion;
        blessing.RequiredPrestigeRank = 0; // No rank requirement
        manager.LoadBlessingStates(new List<Blessing>(), new List<Blessing> { blessing });
        manager.Initialize("religion1", DeityType.Aethra, "Test Religion", prestigeRank: 1);

        // Act
        manager.RefreshAllBlessingStates();

        // Assert
        var state = manager.GetBlessingState("religion1");
        Assert.NotNull(state);
        Assert.True(state.CanUnlock);
        Assert.Equal(BlessingNodeVisualState.Unlockable, state.VisualState);
    }

    [Fact]
    public void RefreshAllBlessingStates_WithLockedBlessing_MarksAsNotUnlockable()
    {
        // Arrange
        var manager = new BlessingDialogManager(null!);
        var blessing = TestFixtures.CreateTestBlessing("player1", "Player Blessing");
        blessing.Kind = BlessingKind.Player;
        blessing.RequiredFavorRank = 5; // High rank requirement
        manager.LoadBlessingStates(new List<Blessing> { blessing }, new List<Blessing>());
        manager.Initialize("religion1", DeityType.Aethra, "Test Religion", favorRank: 1);

        // Act
        manager.RefreshAllBlessingStates();

        // Assert
        var state = manager.GetBlessingState("player1");
        Assert.NotNull(state);
        Assert.False(state.CanUnlock);
        Assert.Equal(BlessingNodeVisualState.Locked, state.VisualState);
    }

    #endregion

    #region GetReligionPrestigeProgress Tests

    [Fact]
    public void GetReligionPrestigeProgress_ReturnsCorrectData()
    {
        // Arrange
        var manager = new BlessingDialogManager(null!);
        manager.Initialize("religion1", DeityType.Aethra, "Test Religion", prestigeRank: 1);
        manager.CurrentPrestige = 1200;

        // Act
        var progress = manager.GetReligionPrestigeProgress();

        // Assert
        Assert.Equal(1200, progress.CurrentPrestige);
        Assert.Equal(1500, progress.RequiredPrestige); // Rank 1 → 2 requires 1500
        Assert.Equal(1, progress.CurrentRank);
        Assert.Equal(2, progress.NextRank);
        Assert.False(progress.IsMaxRank);
    }

    [Fact]
    public void GetReligionPrestigeProgress_AtMaxRank_ReturnsMaxRankTrue()
    {
        // Arrange
        var manager = new BlessingDialogManager(null!);
        manager.Initialize("religion1", DeityType.Aethra, "Test Religion", prestigeRank: 4);
        manager.CurrentPrestige = 15000;

        // Act
        var progress = manager.GetReligionPrestigeProgress();

        // Assert
        Assert.True(progress.IsMaxRank);
        Assert.Equal(4, progress.CurrentRank);
    }

    #endregion

    #region CanUnlockBlessing Tests (via RefreshAllBlessingStates)

    [Fact]
    public void CanUnlockBlessing_AlreadyUnlocked_ReturnsFalse()
    {
        // Arrange
        var manager = new BlessingDialogManager(null!);
        var blessing = TestFixtures.CreateTestBlessing("player1", "Player Blessing");
        blessing.Kind = BlessingKind.Player;
        blessing.RequiredFavorRank = 0;
        manager.LoadBlessingStates(new List<Blessing> { blessing }, new List<Blessing>());
        manager.Initialize("religion1", DeityType.Aethra, "Test Religion", favorRank: 1);
        manager.SetBlessingUnlocked("player1", true);

        // Act
        manager.RefreshAllBlessingStates();

        // Assert
        var state = manager.GetBlessingState("player1");
        Assert.NotNull(state);
        Assert.False(state.CanUnlock); // Already unlocked
    }

    [Fact]
    public void CanUnlockBlessing_MissingPrerequisite_ReturnsFalse()
    {
        // Arrange
        var manager = new BlessingDialogManager(null!);
        var prereq = TestFixtures.CreateTestBlessing("prereq1", "Prerequisite");
        var blessing = TestFixtures.CreateTestBlessing("player1", "Player Blessing");
        blessing.Kind = BlessingKind.Player;
        blessing.RequiredFavorRank = 0;
        blessing.PrerequisiteBlessings.Add("prereq1");

        manager.LoadBlessingStates(new List<Blessing> { prereq, blessing }, new List<Blessing>());
        manager.Initialize("religion1", DeityType.Aethra, "Test Religion", favorRank: 5);

        // Act
        manager.RefreshAllBlessingStates();

        // Assert
        var state = manager.GetBlessingState("player1");
        Assert.NotNull(state);
        Assert.False(state.CanUnlock); // Prerequisite not unlocked
    }

    [Fact]
    public void CanUnlockBlessing_WithUnlockedPrerequisite_ReturnsTrue()
    {
        // Arrange
        var manager = new BlessingDialogManager(null!);
        var prereq = TestFixtures.CreateTestBlessing("prereq1", "Prerequisite");
        var blessing = TestFixtures.CreateTestBlessing("player1", "Player Blessing");
        blessing.Kind = BlessingKind.Player;
        blessing.RequiredFavorRank = 0;
        blessing.PrerequisiteBlessings.Add("prereq1");

        manager.LoadBlessingStates(new List<Blessing> { prereq, blessing }, new List<Blessing>());
        manager.Initialize("religion1", DeityType.Aethra, "Test Religion", favorRank: 5);
        manager.SetBlessingUnlocked("prereq1", true);

        // Act
        manager.RefreshAllBlessingStates();

        // Assert
        var state = manager.GetBlessingState("player1");
        Assert.NotNull(state);
        Assert.True(state.CanUnlock); // Prerequisite unlocked
    }

    [Fact]
    public void CanUnlockBlessing_PlayerBlessing_ChecksFavorRank()
    {
        // Arrange
        var manager = new BlessingDialogManager(null!);
        var blessing = TestFixtures.CreateTestBlessing("player1", "Player Blessing");
        blessing.Kind = BlessingKind.Player;
        blessing.RequiredFavorRank = 3;
        manager.LoadBlessingStates(new List<Blessing> { blessing }, new List<Blessing>());
        manager.Initialize("religion1", DeityType.Aethra, "Test Religion", favorRank: 2);

        // Act
        manager.RefreshAllBlessingStates();

        // Assert
        var state = manager.GetBlessingState("player1");
        Assert.NotNull(state);
        Assert.False(state.CanUnlock); // Favor rank too low (2 < 3)
    }

    [Fact]
    public void CanUnlockBlessing_ReligionBlessing_ChecksPrestigeRank()
    {
        // Arrange
        var manager = new BlessingDialogManager(null!);
        var blessing = TestFixtures.CreateTestBlessing("religion1", "Religion Blessing");
        blessing.Kind = BlessingKind.Religion;
        blessing.RequiredPrestigeRank = 3;
        manager.LoadBlessingStates(new List<Blessing>(), new List<Blessing> { blessing });
        manager.Initialize("religion1", DeityType.Aethra, "Test Religion", prestigeRank: 2);

        // Act
        manager.RefreshAllBlessingStates();

        // Assert
        var state = manager.GetBlessingState("religion1");
        Assert.NotNull(state);
        Assert.False(state.CanUnlock); // Prestige rank too low (2 < 3)
    }

    #endregion
}