using System.Diagnostics.CodeAnalysis;
using PantheonWars.Models;

namespace PantheonWars.Tests.Models;

[ExcludeFromCodeCoverage]
public class PlayerFavorProgressTests
{
    [Fact]
    public void ProgressPercentage_WithCurrentAndRequired_CalculatesCorrectly()
    {
        // Arrange
        var progress = new PlayerFavorProgress
        {
            CurrentFavor = 250,
            RequiredFavor = 500,
            CurrentRank = 2,
            NextRank = 3,
            IsMaxRank = false
        };

        // Act
        var percentage = progress.ProgressPercentage;

        // Assert
        Assert.Equal(0.5f, percentage);
    }

    [Fact]
    public void ProgressPercentage_AtZeroProgress_ReturnsZero()
    {
        // Arrange
        var progress = new PlayerFavorProgress
        {
            CurrentFavor = 0,
            RequiredFavor = 500,
            CurrentRank = 0,
            NextRank = 1,
            IsMaxRank = false
        };

        // Act
        var percentage = progress.ProgressPercentage;

        // Assert
        Assert.Equal(0f, percentage);
    }

    [Fact]
    public void ProgressPercentage_AtMaxRank_ReturnsOne()
    {
        // Arrange
        var progress = new PlayerFavorProgress
        {
            CurrentFavor = 1500,
            RequiredFavor = 0, // Max rank has no requirement
            CurrentRank = 4,
            NextRank = 4,
            IsMaxRank = true
        };

        // Act
        var percentage = progress.ProgressPercentage;

        // Assert
        Assert.Equal(1f, percentage);
    }

    [Fact]
    public void ProgressPercentage_NearCompletion_CalculatesAccurately()
    {
        // Arrange
        var progress = new PlayerFavorProgress
        {
            CurrentFavor = 487,
            RequiredFavor = 500,
            CurrentRank = 2,
            NextRank = 3,
            IsMaxRank = false
        };

        // Act
        var percentage = progress.ProgressPercentage;

        // Assert
        Assert.Equal(0.974f, percentage, precision: 3);
    }

    [Fact]
    public void IsNearRankUp_WhenAbove80Percent_ReturnsTrue()
    {
        // Arrange
        var progress = new PlayerFavorProgress
        {
            CurrentFavor = 450,
            RequiredFavor = 500,
            CurrentRank = 2,
            NextRank = 3,
            IsMaxRank = false
        };

        // Act
        var isNear = progress.ProgressPercentage > 0.8f;

        // Assert
        Assert.True(isNear);
    }
}