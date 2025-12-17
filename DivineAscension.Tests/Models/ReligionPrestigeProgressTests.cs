using System.Diagnostics.CodeAnalysis;
using DivineAscension.Models;

namespace PantheonWars.Tests.Models;

[ExcludeFromCodeCoverage]
public class ReligionPrestigeProgressTests
{
    [Fact]
    public void ProgressPercentage_WithCurrentAndRequired_CalculatesCorrectly()
    {
        // Arrange
        var progress = new ReligionPrestigeProgress
        {
            CurrentPrestige = 1000,
            RequiredPrestige = 2000,
            CurrentRank = 1,
            NextRank = 2,
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
        var progress = new ReligionPrestigeProgress
        {
            CurrentPrestige = 0,
            RequiredPrestige = 500,
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
        var progress = new ReligionPrestigeProgress
        {
            CurrentPrestige = 10000,
            RequiredPrestige = 0,
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
    public void ProgressPercentage_LargeNumbers_CalculatesAccurately()
    {
        // Arrange
        var progress = new ReligionPrestigeProgress
        {
            CurrentPrestige = 6500,
            RequiredPrestige = 7500,
            CurrentRank = 3,
            NextRank = 4,
            IsMaxRank = false
        };

        // Act
        var percentage = progress.ProgressPercentage;

        // Assert
        Assert.Equal(0.8667f, percentage, precision: 3);
    }
}