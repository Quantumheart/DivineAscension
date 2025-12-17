using System.Diagnostics.CodeAnalysis;
using DivineAscension.Models;
using PantheonWars.Tests.Helpers;

namespace PantheonWars.Tests.Models;

/// <summary>
///     Unit tests for BlessingNodeState model
/// </summary>
[ExcludeFromCodeCoverage]
public class BlessingNodeStateTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_InitializesWithDefaultValues()
    {
        // Arrange
        var blessing = TestFixtures.CreateTestBlessing("test-blessing", "Test Blessing");

        // Act
        var state = new BlessingNodeState(blessing);

        // Assert
        Assert.Same(blessing, state.Blessing);
        Assert.Equal(BlessingNodeVisualState.Locked, state.VisualState);
        Assert.False(state.IsUnlocked);
        Assert.False(state.CanUnlock);
        Assert.Equal(0f, state.PositionX);
        Assert.Equal(0f, state.PositionY);
        Assert.Equal(64f, state.Width);
        Assert.Equal(64f, state.Height);
        Assert.Equal(0f, state.GlowAlpha);
        Assert.False(state.IsHovered);
        Assert.False(state.IsSelected);
        Assert.Equal(0, state.Tier);
    }

    #endregion

    #region Property Tests

    [Fact]
    public void Properties_CanBeSetAndRetrieved()
    {
        // Arrange
        var blessing = TestFixtures.CreateTestBlessing("test-blessing", "Test Blessing");
        var state = new BlessingNodeState(blessing);

        // Act
        state.VisualState = BlessingNodeVisualState.Unlockable;
        state.IsUnlocked = true;
        state.CanUnlock = true;
        state.PositionX = 100f;
        state.PositionY = 200f;
        state.Width = 80f;
        state.Height = 90f;
        state.GlowAlpha = 0.5f;
        state.IsHovered = true;
        state.IsSelected = true;
        state.Tier = 2;

        // Assert
        Assert.Equal(BlessingNodeVisualState.Unlockable, state.VisualState);
        Assert.True(state.IsUnlocked);
        Assert.True(state.CanUnlock);
        Assert.Equal(100f, state.PositionX);
        Assert.Equal(200f, state.PositionY);
        Assert.Equal(80f, state.Width);
        Assert.Equal(90f, state.Height);
        Assert.Equal(0.5f, state.GlowAlpha);
        Assert.True(state.IsHovered);
        Assert.True(state.IsSelected);
        Assert.Equal(2, state.Tier);
    }

    #endregion

    #region BlessingNodeVisualState Enum Tests

    [Fact]
    public void BlessingNodeVisualState_HasExpectedValues()
    {
        // Arrange & Act & Assert
        Assert.Equal(0, (int)BlessingNodeVisualState.Locked);
        Assert.Equal(1, (int)BlessingNodeVisualState.Unlockable);
        Assert.Equal(2, (int)BlessingNodeVisualState.Unlocked);
    }

    #endregion

    #region UpdateVisualState Tests

    [Fact]
    public void UpdateVisualState_WhenUnlocked_SetsUnlockedState()
    {
        // Arrange
        var blessing = TestFixtures.CreateTestBlessing("test-blessing", "Test Blessing");
        var state = new BlessingNodeState(blessing)
        {
            IsUnlocked = true,
            CanUnlock = false
        };

        // Act
        state.UpdateVisualState();

        // Assert
        Assert.Equal(BlessingNodeVisualState.Unlocked, state.VisualState);
    }

    [Fact]
    public void UpdateVisualState_WhenCanUnlock_SetsUnlockableState()
    {
        // Arrange
        var blessing = TestFixtures.CreateTestBlessing("test-blessing", "Test Blessing");
        var state = new BlessingNodeState(blessing)
        {
            IsUnlocked = false,
            CanUnlock = true
        };

        // Act
        state.UpdateVisualState();

        // Assert
        Assert.Equal(BlessingNodeVisualState.Unlockable, state.VisualState);
    }

    [Fact]
    public void UpdateVisualState_WhenLocked_SetsLockedState()
    {
        // Arrange
        var blessing = TestFixtures.CreateTestBlessing("test-blessing", "Test Blessing");
        var state = new BlessingNodeState(blessing)
        {
            IsUnlocked = false,
            CanUnlock = false
        };

        // Act
        state.UpdateVisualState();

        // Assert
        Assert.Equal(BlessingNodeVisualState.Locked, state.VisualState);
    }

    [Fact]
    public void UpdateVisualState_UnlockedTakesPrecedence_OverCanUnlock()
    {
        // Arrange
        var blessing = TestFixtures.CreateTestBlessing("test-blessing", "Test Blessing");
        var state = new BlessingNodeState(blessing)
        {
            IsUnlocked = true,
            CanUnlock = true // Both true, but IsUnlocked should take precedence
        };

        // Act
        state.UpdateVisualState();

        // Assert
        Assert.Equal(BlessingNodeVisualState.Unlocked, state.VisualState);
    }

    #endregion
}