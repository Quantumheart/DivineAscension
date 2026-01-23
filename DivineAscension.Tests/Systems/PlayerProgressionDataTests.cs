using System.Diagnostics.CodeAnalysis;
using DivineAscension.Models.Enum;
using DivineAscension.Systems;

namespace DivineAscension.Tests.Systems;

/// <summary>
///     Unit tests for PlayerProgressionData
///     Tests HashSet blessing storage and computed FavorRank
/// </summary>
[ExcludeFromCodeCoverage]
public class PlayerProgressionDataTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_WithId_SetsId()
    {
        // Act
        var data = new PlayerProgressionData("player-123");

        // Assert
        Assert.Equal("player-123", data.Id);
    }

    [Fact]
    public void Constructor_Parameterless_InitializesDefaults()
    {
        // Act
        var data = new PlayerProgressionData();

        // Assert
        Assert.Equal(string.Empty, data.Id);
        Assert.Equal(0, data.Favor);
        Assert.Equal(0, data.TotalFavorEarned);
        Assert.Equal(0f, data.AccumulatedFractionalFavor);
        Assert.Equal(3, data.DataVersion);
        Assert.Empty(data.UnlockedBlessings);
    }

    #endregion

    #region HashSet Blessing Storage Tests

    [Fact]
    public void UnlockBlessing_AddsToHashSet()
    {
        // Arrange
        var data = new PlayerProgressionData("player-123");

        // Act
        data.UnlockBlessing("blessing1");

        // Assert
        Assert.Contains("blessing1", data.UnlockedBlessings);
        Assert.Single(data.UnlockedBlessings);
    }

    [Fact]
    public void UnlockBlessing_MultipleBlessings_AddsAll()
    {
        // Arrange
        var data = new PlayerProgressionData("player-123");

        // Act
        data.UnlockBlessing("blessing1");
        data.UnlockBlessing("blessing2");
        data.UnlockBlessing("blessing3");

        // Assert
        Assert.Equal(3, data.UnlockedBlessings.Count);
        Assert.Contains("blessing1", data.UnlockedBlessings);
        Assert.Contains("blessing2", data.UnlockedBlessings);
        Assert.Contains("blessing3", data.UnlockedBlessings);
    }

    [Fact]
    public void UnlockBlessing_Duplicate_DoesNotAddTwice()
    {
        // Arrange
        var data = new PlayerProgressionData("player-123");

        // Act
        data.UnlockBlessing("blessing1");
        data.UnlockBlessing("blessing1"); // Duplicate

        // Assert - HashSet should prevent duplicates
        Assert.Single(data.UnlockedBlessings);
        Assert.Contains("blessing1", data.UnlockedBlessings);
    }

    [Fact]
    public void IsBlessingUnlocked_WithUnlockedBlessing_ReturnsTrue()
    {
        // Arrange
        var data = new PlayerProgressionData("player-123");
        data.UnlockBlessing("blessing1");

        // Act
        var isUnlocked = data.IsBlessingUnlocked("blessing1");

        // Assert
        Assert.True(isUnlocked);
    }

    [Fact]
    public void IsBlessingUnlocked_WithLockedBlessing_ReturnsFalse()
    {
        // Arrange
        var data = new PlayerProgressionData("player-123");

        // Act
        var isUnlocked = data.IsBlessingUnlocked("blessing1");

        // Assert
        Assert.False(isUnlocked);
    }

    [Fact]
    public void IsBlessingUnlocked_CheckMultipleBlessings_ReturnsCorrectStatus()
    {
        // Arrange
        var data = new PlayerProgressionData("player-123");
        data.UnlockBlessing("blessing1");
        data.UnlockBlessing("blessing3");

        // Act & Assert
        Assert.True(data.IsBlessingUnlocked("blessing1"));
        Assert.False(data.IsBlessingUnlocked("blessing2"));
        Assert.True(data.IsBlessingUnlocked("blessing3"));
    }

    [Fact]
    public void ClearUnlockedBlessings_RemovesAllBlessings()
    {
        // Arrange
        var data = new PlayerProgressionData("player-123");
        data.UnlockBlessing("blessing1");
        data.UnlockBlessing("blessing2");
        data.UnlockBlessing("blessing3");

        // Act
        data.ClearUnlockedBlessings();

        // Assert
        Assert.Empty(data.UnlockedBlessings);
        Assert.False(data.IsBlessingUnlocked("blessing1"));
        Assert.False(data.IsBlessingUnlocked("blessing2"));
        Assert.False(data.IsBlessingUnlocked("blessing3"));
    }

    [Fact]
    public void ClearUnlockedBlessings_WithEmptySet_DoesNotThrow()
    {
        // Arrange
        var data = new PlayerProgressionData("player-123");

        // Act & Assert - Should not throw
        var exception = Record.Exception(() => data.ClearUnlockedBlessings());
        Assert.Null(exception);
        Assert.Empty(data.UnlockedBlessings);
    }

    #endregion

    #region Favor Management Tests

    [Fact]
    public void AddFavor_IncreasesCurrentAndTotal()
    {
        // Arrange
        var data = new PlayerProgressionData("player-123");

        // Act
        data.AddFavor(100);

        // Assert
        Assert.Equal(100, data.Favor);
        Assert.Equal(100, data.TotalFavorEarned);
    }

    [Fact]
    public void AddFavor_WithNegativeAmount_DoesNothing()
    {
        // Arrange
        var data = new PlayerProgressionData("player-123")
        {
            Favor = 50,
            TotalFavorEarned = 50
        };

        // Act
        data.AddFavor(-10);

        // Assert
        Assert.Equal(50, data.Favor);
        Assert.Equal(50, data.TotalFavorEarned);
    }

    [Fact]
    public void AddFavor_WithZero_DoesNothing()
    {
        // Arrange
        var data = new PlayerProgressionData("player-123")
        {
            Favor = 50,
            TotalFavorEarned = 50
        };

        // Act
        data.AddFavor(0);

        // Assert
        Assert.Equal(50, data.Favor);
        Assert.Equal(50, data.TotalFavorEarned);
    }

    [Fact]
    public void RemoveFavor_WithSufficientFavor_RemovesAndReturnsTrue()
    {
        // Arrange
        var data = new PlayerProgressionData("player-123")
        {
            Favor = 100
        };

        // Act
        var result = data.RemoveFavor(50);

        // Assert
        Assert.True(result);
        Assert.Equal(50, data.Favor);
        Assert.Equal(0, data.TotalFavorEarned); // TotalFavorEarned should not decrease
    }

    [Fact]
    public void RemoveFavor_WithInsufficientFavor_ReturnsFalse()
    {
        // Arrange
        var data = new PlayerProgressionData("player-123")
        {
            Favor = 30
        };

        // Act
        var result = data.RemoveFavor(50);

        // Assert
        Assert.False(result);
        Assert.Equal(30, data.Favor); // Favor should not change
    }

    [Fact]
    public void AddFractionalFavor_AccumulatesUntilWhole()
    {
        // Arrange
        var data = new PlayerProgressionData("player-123");

        // Act
        data.AddFractionalFavor(0.3f);
        Assert.Equal(0, data.Favor);
        Assert.Equal(0.3f, data.AccumulatedFractionalFavor, precision: 5);

        data.AddFractionalFavor(0.4f);
        Assert.Equal(0, data.Favor);
        Assert.Equal(0.7f, data.AccumulatedFractionalFavor, precision: 5);

        data.AddFractionalFavor(0.5f); // Total: 1.2
        Assert.Equal(1, data.Favor);
        Assert.Equal(0.2f, data.AccumulatedFractionalFavor, precision: 5);
        Assert.Equal(1, data.TotalFavorEarned);
    }

    [Fact]
    public void ApplySwitchPenalty_ResetsFavorAndBlessings()
    {
        // Arrange
        var data = new PlayerProgressionData("player-123")
        {
            Favor = 100,
            TotalFavorEarned = 2500 // Champion rank
        };
        data.UnlockBlessing("blessing1");
        data.UnlockBlessing("blessing2");

        // Act
        data.ApplySwitchPenalty();

        // Assert
        Assert.Equal(0, data.Favor);
        Assert.Equal(2500, data.TotalFavorEarned); // Should not change
        // Note: FavorRank is now calculated by the manager, not the data model
        Assert.Empty(data.UnlockedBlessings);
    }

    #endregion
}