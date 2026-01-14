using System.Diagnostics.CodeAnalysis;
using DivineAscension.Systems;

namespace DivineAscension.Tests.Systems;

[ExcludeFromCodeCoverage]
public class RankRequirementsTests
{
    [Theory]
    [InlineData(0, 500)] // Initiate → Devoted
    [InlineData(1, 2000)] // Devoted → Zealot
    [InlineData(2, 5000)] // Zealot → Champion
    [InlineData(3, 10000)] // Champion → Exalted
    [InlineData(4, 0)] // Max rank
    public void GetRequiredFavorForNextRank_ReturnsCorrectValue(int currentRank, int expectedFavor)
    {
        // Act
        var required = RankRequirements.GetRequiredFavorForNextRank(currentRank);

        // Assert
        Assert.Equal(expectedFavor, required);
    }

    [Theory]
    [InlineData(0, 2500)] // Fledgling → Established (5x scaling)
    [InlineData(1, 10000)] // Established → Renowned (5x scaling)
    [InlineData(2, 25000)] // Renowned → Legendary (5x scaling)
    [InlineData(3, 50000)] // Legendary → Mythic (5x scaling)
    [InlineData(4, 0)] // Max rank
    public void GetRequiredPrestigeForNextRank_ReturnsCorrectValue(int currentRank, int expectedPrestige)
    {
        // Act
        var required = RankRequirements.GetRequiredPrestigeForNextRank(currentRank);

        // Assert
        Assert.Equal(expectedPrestige, required);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(5)]
    [InlineData(10)]
    public void GetRequiredFavorForNextRank_WithInvalidRank_ReturnsZero(int invalidRank)
    {
        // Act
        var required = RankRequirements.GetRequiredFavorForNextRank(invalidRank);

        // Assert
        Assert.Equal(0, required);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(5)]
    [InlineData(10)]
    public void GetRequiredPrestigeForNextRank_WithInvalidRank_ReturnsZero(int invalidRank)
    {
        // Act
        var required = RankRequirements.GetRequiredPrestigeForNextRank(invalidRank);

        // Assert
        Assert.Equal(0, required);
    }

    [Theory]
    [InlineData(0, "Initiate")]
    [InlineData(1, "Disciple")]
    [InlineData(2, "Zealot")]
    [InlineData(3, "Champion")]
    [InlineData(4, "Avatar")]
    public void GetFavorRankName_ReturnsCorrectName(int rank, string expectedName)
    {
        // Act
        var name = RankRequirements.GetFavorRankName(rank);

        // Assert
        Assert.Equal(expectedName, name);
    }

    [Theory]
    [InlineData(0, "Fledgling")]
    [InlineData(1, "Established")]
    [InlineData(2, "Renowned")]
    [InlineData(3, "Legendary")]
    [InlineData(4, "Mythic")]
    public void GetPrestigeRankName_ReturnsCorrectName(int rank, string expectedName)
    {
        // Act
        var name = RankRequirements.GetPrestigeRankName(rank);

        // Assert
        Assert.Equal(expectedName, name);
    }
}