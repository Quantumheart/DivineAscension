using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using PantheonWars.GUI;
using PantheonWars.Models;
using PantheonWars.Models.Enum;
using PantheonWars.Tests.Helpers;
using Xunit;

namespace PantheonWars.Tests.Systems;

[ExcludeFromCodeCoverage]
public class GuiDialogManagerTests
{
    [Fact]
    public void TestPropertyInitialization()
    {
        var manager = new GuiDialogManager(null!);
        Assert.Null(manager.ReligionStateManager.CurrentReligionUID);
        Assert.Equal(DeityType.None, manager.ReligionStateManager.CurrentDeity);
        Assert.Null(manager.ReligionStateManager.CurrentReligionName);
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
        var manager = new GuiDialogManager(null!);
        manager.Initialize("religion123", DeityType.Khoras, "God of Warriors");
        Assert.Equal("religion123", manager.ReligionStateManager.CurrentReligionUID);
        Assert.Equal(DeityType.Khoras, manager.ReligionStateManager.CurrentDeity);
        Assert.Equal("God of Warriors", manager.ReligionStateManager.CurrentReligionName);
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
        var manager = new GuiDialogManager(null!);
        manager.Initialize("religion123", DeityType.Khoras, "God of Warriors");
        manager.Reset();
        Assert.Null(manager.ReligionStateManager.CurrentReligionUID);
        Assert.Equal(DeityType.None, manager.ReligionStateManager.CurrentDeity);
        Assert.Null(manager.ReligionStateManager.CurrentReligionName);
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
        var manager = new GuiDialogManager(null!);
        manager.SelectBlessing("blessing456");
        Assert.Equal("blessing456", manager.SelectedBlessingId);
    }

    [Fact]
    public void TestClearSelection()
    {
        var manager = new GuiDialogManager(null!);
        manager.SelectBlessing("blessing456");
        manager.ClearSelection();
        Assert.Null(manager.SelectedBlessingId);
    }

    [Fact]
    public void TestHasReligion()
    {
        var manager = new GuiDialogManager(null!);
        Assert.False(manager.HasReligion());

        manager.Initialize("religion123", DeityType.Khoras, "God of Warriors");
        Assert.True(manager.HasReligion());

        manager.Reset();
        Assert.False(manager.HasReligion());
    }

    #region LoadBlessingStates Tests

    [Fact]
    public void LoadBlessingStates_WithEmptyLists_ClearsStates()
    {
        // Arrange
        var manager = new GuiDialogManager(null!);

        // Act

        // Assert
        Assert.Empty(manager.ReligionStateManager.PlayerBlessingStates);
        Assert.Empty(manager.ReligionStateManager.ReligionBlessingStates);
    }

    [Fact]
    public void LoadBlessingStates_WithPlayerBlessings_CreatesStates()
    {
        // Arrange
        var manager = new GuiDialogManager(null!);
        var playerBlessings = new List<Blessing>
        {
            TestFixtures.CreateTestBlessing("player1", "Player Blessing 1"),
            TestFixtures.CreateTestBlessing("player2", "Player Blessing 2")
        };

        // Act

        // Assert
        Assert.Equal(2, manager.ReligionStateManager.PlayerBlessingStates.Count);
        Assert.True(manager.ReligionStateManager.PlayerBlessingStates.ContainsKey("player1"));
        Assert.True(manager.ReligionStateManager.PlayerBlessingStates.ContainsKey("player2"));
        Assert.Empty(manager.ReligionStateManager.ReligionBlessingStates);
    }

    [Fact]
    public void LoadBlessingStates_WithReligionBlessings_CreatesStates()
    {
        // Arrange
        var manager = new GuiDialogManager(null!);
        var religionBlessings = new List<Blessing>
        {
            TestFixtures.CreateTestBlessing("religion1", "Religion Blessing 1"),
            TestFixtures.CreateTestBlessing("religion2", "Religion Blessing 2")
        };

        // Act

        // Assert
        Assert.Empty(manager.ReligionStateManager.PlayerBlessingStates);
        Assert.Equal(2, manager.ReligionStateManager.ReligionBlessingStates.Count);
        Assert.True(manager.ReligionStateManager.ReligionBlessingStates.ContainsKey("religion1"));
        Assert.True(manager.ReligionStateManager.ReligionBlessingStates.ContainsKey("religion2"));
    }

    [Fact]
    public void LoadBlessingStates_WithBothTypes_CreatesBothStates()
    {
        // Arrange
        var manager = new GuiDialogManager(null!);
        var playerBlessings = new List<Blessing>
        {
            TestFixtures.CreateTestBlessing("player1", "Player Blessing 1")
        };
        var religionBlessings = new List<Blessing>
        {
            TestFixtures.CreateTestBlessing("religion1", "Religion Blessing 1")
        };

        // Act

        // Assert
        Assert.Single(manager.ReligionStateManager.PlayerBlessingStates);
        Assert.Single(manager.ReligionStateManager.ReligionBlessingStates);
    }

    [Fact]
    public void LoadBlessingStates_CalledTwice_ReplacesExistingStates()
    {
        // Arrange
        var manager = new GuiDialogManager(null!);
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

        // Assert
        Assert.Equal(2, manager.ReligionStateManager.PlayerBlessingStates.Count);
        Assert.False(manager.ReligionStateManager.PlayerBlessingStates.ContainsKey("old1"));
        Assert.True(manager.ReligionStateManager.PlayerBlessingStates.ContainsKey("new1"));
        Assert.True(manager.ReligionStateManager.PlayerBlessingStates.ContainsKey("new2"));
    }

    #endregion

    #region GetBlessingState Tests

    [Fact]
    public void GetBlessingState_WithPlayerBlessing_ReturnsState()
    {
        // Arrange
        var manager = new GuiDialogManager(null!);
        var blessing = TestFixtures.CreateTestBlessing("player1", "Player Blessing");

        // Act
        var state = manager.ReligionStateManager.GetBlessingState("player1");

        // Assert
        Assert.NotNull(state);
        Assert.Equal("player1", state.Blessing.BlessingId);
    }

    [Fact]
    public void GetBlessingState_WithReligionBlessing_ReturnsState()
    {
        // Arrange
        var manager = new GuiDialogManager(null!);
        var blessing = TestFixtures.CreateTestBlessing("religion1", "Religion Blessing");

        // Act
        var state = manager.ReligionStateManager.GetBlessingState("religion1");

        // Assert
        Assert.NotNull(state);
        Assert.Equal("religion1", state.Blessing.BlessingId);
    }

    [Fact]
    public void GetBlessingState_WithNonExistentBlessing_ReturnsNull()
    {
        // Arrange
        var manager = new GuiDialogManager(null!);

        // Act
        var state = manager.ReligionStateManager.GetBlessingState("nonexistent");

        // Assert
        Assert.Null(state);
    }

    [Fact]
    public void GetBlessingState_PrefersPlayerBlessingOverReligion()
    {
        // Arrange
        var manager = new GuiDialogManager(null!);
        var playerBlessing = TestFixtures.CreateTestBlessing("shared-id", "Player Blessing");
        var religionBlessing = TestFixtures.CreateTestBlessing("shared-id", "Religion Blessing");

        // Act
        var state = manager.ReligionStateManager.GetBlessingState("shared-id");

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
        var manager = new GuiDialogManager(null!);

        // Act
        var state = manager.GetSelectedBlessingState();

        // Assert
        Assert.Null(state);
    }

    [Fact]
    public void GetSelectedBlessingState_WithSelection_ReturnsState()
    {
        // Arrange
        var manager = new GuiDialogManager(null!);
        var blessing = TestFixtures.CreateTestBlessing("blessing1", "Blessing 1");
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
        var manager = new GuiDialogManager(null!);
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
        var manager = new GuiDialogManager(null!);
        var blessing = TestFixtures.CreateTestBlessing("blessing1", "Blessing 1");

        // Act

        // Assert
        var state = manager.ReligionStateManager.GetBlessingState("blessing1");
        Assert.NotNull(state);
        Assert.True(state.IsUnlocked);
    }

    [Fact]
    public void SetBlessingUnlocked_UpdatesVisualState()
    {
        // Arrange
        var manager = new GuiDialogManager(null!);
        var blessing = TestFixtures.CreateTestBlessing("blessing1", "Blessing 1");

        // Act

        // Assert
        var state = manager.ReligionStateManager.GetBlessingState("blessing1");
        Assert.NotNull(state);
        Assert.Equal(BlessingNodeVisualState.Unlocked, state.VisualState);
    }

    [Fact]
    public void SetBlessingUnlocked_WithNonExistentBlessing_DoesNotThrow()
    {
        // Arrange
        var manager = new GuiDialogManager(null!);

        // Act & Assert - Should not throw
    }

    #endregion

    #region RefreshAllBlessingStates Tests

    [Fact]
    public void RefreshAllBlessingStates_UpdatesPlayerBlessingStates()
    {
        // Arrange
        var manager = new GuiDialogManager(null!);
        var blessing = TestFixtures.CreateTestBlessing("player1", "Player Blessing");
        blessing.Kind = BlessingKind.Player;
        blessing.RequiredFavorRank = 0; // No rank requirement
        manager.Initialize("religion1", DeityType.Khoras, "Test Religion", favorRank: 1);

        // Act

        // Assert
        var state = manager.ReligionStateManager.GetBlessingState("player1");
        Assert.NotNull(state);
        Assert.True(state.CanUnlock);
        Assert.Equal(BlessingNodeVisualState.Unlockable, state.VisualState);
    }

    [Fact]
    public void RefreshAllBlessingStates_UpdatesReligionBlessingStates()
    {
        // Arrange
        var manager = new GuiDialogManager(null!);
        var blessing = TestFixtures.CreateTestBlessing("religion1", "Religion Blessing");
        blessing.Kind = BlessingKind.Religion;
        blessing.RequiredPrestigeRank = 0; // No rank requirement
        manager.Initialize("religion1", DeityType.Khoras, "Test Religion", prestigeRank: 1);

        // Act

        // Assert
        var state = manager.ReligionStateManager.GetBlessingState("religion1");
        Assert.NotNull(state);
        Assert.True(state.CanUnlock);
        Assert.Equal(BlessingNodeVisualState.Unlockable, state.VisualState);
    }

    [Fact]
    public void RefreshAllBlessingStates_WithLockedBlessing_MarksAsNotUnlockable()
    {
        // Arrange
        var manager = new GuiDialogManager(null!);
        var blessing = TestFixtures.CreateTestBlessing("player1", "Player Blessing");
        blessing.Kind = BlessingKind.Player;
        blessing.RequiredFavorRank = 5; // High rank requirement
        manager.Initialize("religion1", DeityType.Khoras, "Test Religion", favorRank: 1);

        // Act

        // Assert
        var state = manager.ReligionStateManager.GetBlessingState("player1");
        Assert.NotNull(state);
        Assert.False(state.CanUnlock);
        Assert.Equal(BlessingNodeVisualState.Locked, state.VisualState);
    }

    #endregion

    #region GetPlayerFavorProgress Tests

    [Fact]
    public void GetPlayerFavorProgress_ReturnsCorrectData()
    {
        // Arrange
        var manager = new GuiDialogManager(null!);
        manager.Initialize("religion1", DeityType.Khoras, "Test Religion", favorRank: 2);
        manager.ReligionStateManager.TotalFavorEarned = 3500;

        // Act
        var progress = manager.ReligionStateManager.GetPlayerFavorProgress();

        // Assert
        Assert.Equal(3500, progress.CurrentFavor);
        Assert.Equal(5000, progress.RequiredFavor); // Rank 2 → 3 requires 5000
        Assert.Equal(2, progress.CurrentRank);
        Assert.Equal(3, progress.NextRank);
        Assert.False(progress.IsMaxRank);
    }

    [Fact]
    public void GetPlayerFavorProgress_AtMaxRank_ReturnsMaxRankTrue()
    {
        // Arrange
        var manager = new GuiDialogManager(null!);
        manager.Initialize("religion1", DeityType.Khoras, "Test Religion", favorRank: 4);
        manager.ReligionStateManager.TotalFavorEarned = 15000;

        // Act
        var progress = manager.ReligionStateManager.GetPlayerFavorProgress();

        // Assert
        Assert.True(progress.IsMaxRank);
        Assert.Equal(4, progress.CurrentRank);
    }

    #endregion

    #region GetReligionPrestigeProgress Tests

    [Fact]
    public void GetReligionPrestigeProgress_ReturnsCorrectData()
    {
        // Arrange
        var manager = new GuiDialogManager(null!);
        manager.Initialize("religion1", DeityType.Khoras, "Test Religion", prestigeRank: 1);
        manager.ReligionStateManager.CurrentPrestige = 1200;

        // Act
        var progress = manager.ReligionStateManager.GetReligionPrestigeProgress();

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
        var manager = new GuiDialogManager(null!);
        manager.Initialize("religion1", DeityType.Khoras, "Test Religion", prestigeRank: 4);
        manager.ReligionStateManager.CurrentPrestige = 15000;

        // Act
        var progress = manager.ReligionStateManager.GetReligionPrestigeProgress();

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
        var manager = new GuiDialogManager(null!);
        var blessing = TestFixtures.CreateTestBlessing("player1", "Player Blessing");
        blessing.Kind = BlessingKind.Player;
        blessing.RequiredFavorRank = 0;
        manager.Initialize("religion1", DeityType.Khoras, "Test Religion", favorRank: 1);

        // Act

        // Assert
        var state = manager.ReligionStateManager.GetBlessingState("player1");
        Assert.NotNull(state);
        Assert.False(state.CanUnlock); // Already unlocked
    }

    [Fact]
    public void CanUnlockBlessing_MissingPrerequisite_ReturnsFalse()
    {
        // Arrange
        var manager = new GuiDialogManager(null!);
        var prereq = TestFixtures.CreateTestBlessing("prereq1", "Prerequisite");
        var blessing = TestFixtures.CreateTestBlessing("player1", "Player Blessing");
        blessing.Kind = BlessingKind.Player;
        blessing.RequiredFavorRank = 0;
        blessing.PrerequisiteBlessings.Add("prereq1");

        manager.Initialize("religion1", DeityType.Khoras, "Test Religion", favorRank: 5);

        // Act

        // Assert
        var state = manager.ReligionStateManager.GetBlessingState("player1");
        Assert.NotNull(state);
        Assert.False(state.CanUnlock); // Prerequisite not unlocked
    }

    [Fact]
    public void CanUnlockBlessing_WithUnlockedPrerequisite_ReturnsTrue()
    {
        // Arrange
        var manager = new GuiDialogManager(null!);
        var prereq = TestFixtures.CreateTestBlessing("prereq1", "Prerequisite");
        var blessing = TestFixtures.CreateTestBlessing("player1", "Player Blessing");
        blessing.Kind = BlessingKind.Player;
        blessing.RequiredFavorRank = 0;
        blessing.PrerequisiteBlessings.Add("prereq1");

        manager.Initialize("religion1", DeityType.Khoras, "Test Religion", favorRank: 5);

        // Act

        // Assert
        var state = manager.ReligionStateManager.GetBlessingState("player1");
        Assert.NotNull(state);
        Assert.True(state.CanUnlock); // Prerequisite unlocked
    }

    [Fact]
    public void CanUnlockBlessing_PlayerBlessing_ChecksFavorRank()
    {
        // Arrange
        var manager = new GuiDialogManager(null!);
        var blessing = TestFixtures.CreateTestBlessing("player1", "Player Blessing");
        blessing.Kind = BlessingKind.Player;
        blessing.RequiredFavorRank = 3;
        manager.Initialize("religion1", DeityType.Khoras, "Test Religion", favorRank: 2);

        // Act

        // Assert
        var state = manager.ReligionStateManager.GetBlessingState("player1");
        Assert.NotNull(state);
        Assert.False(state.CanUnlock); // Favor rank too low (2 < 3)
    }

    [Fact]
    public void CanUnlockBlessing_ReligionBlessing_ChecksPrestigeRank()
    {
        // Arrange
        var manager = new GuiDialogManager(null!);
        var blessing = TestFixtures.CreateTestBlessing("religion1", "Religion Blessing");
        blessing.Kind = BlessingKind.Religion;
        blessing.RequiredPrestigeRank = 3;
        manager.Initialize("religion1", DeityType.Khoras, "Test Religion", prestigeRank: 2);

        // Act

        // Assert
        var state = manager.ReligionStateManager.GetBlessingState("religion1");
        Assert.NotNull(state);
        Assert.False(state.CanUnlock); // Prestige rank too low (2 < 3)
    }

    #endregion
}