using DivineAscension.Extensions;
using DivineAscension.Models.Enum;
using DivineAscension.Tests.Helpers;

namespace DivineAscension.Tests.Extensions;

public class EnumLocalizationExtensionsTests
{
    public EnumLocalizationExtensionsTests()
    {
        TestFixtures.InitializeLocalizationForTests();
    }

    #region FavorRank Tests

    [Theory]
    [InlineData(FavorRank.Initiate, "Initiate")]
    [InlineData(FavorRank.Disciple, "Disciple")]
    [InlineData(FavorRank.Zealot, "Zealot")]
    [InlineData(FavorRank.Champion, "Champion")]
    [InlineData(FavorRank.Avatar, "Avatar")]
    public void FavorRank_ToLocalizedString_ReturnsExpectedValue(FavorRank rank, string expected)
    {
        // Act
        var result = rank.ToLocalizedString();

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void FavorRank_ToLocalizedString_AllValuesReturnNonEmpty()
    {
        // Arrange
        var allRanks = Enum.GetValues<FavorRank>();

        // Act & Assert
        foreach (var rank in allRanks)
        {
            var result = rank.ToLocalizedString();
            Assert.False(string.IsNullOrEmpty(result), $"FavorRank.{rank} returned empty string");
        }
    }

    #endregion

    #region PrestigeRank Tests

    [Theory]
    [InlineData(PrestigeRank.Fledgling, "Fledgling")]
    [InlineData(PrestigeRank.Established, "Established")]
    [InlineData(PrestigeRank.Renowned, "Renowned")]
    [InlineData(PrestigeRank.Legendary, "Legendary")]
    [InlineData(PrestigeRank.Mythic, "Mythic")]
    public void PrestigeRank_ToLocalizedString_ReturnsExpectedValue(PrestigeRank rank, string expected)
    {
        // Act
        var result = rank.ToLocalizedString();

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void PrestigeRank_ToLocalizedString_AllValuesReturnNonEmpty()
    {
        // Arrange
        var allRanks = Enum.GetValues<PrestigeRank>();

        // Act & Assert
        foreach (var rank in allRanks)
        {
            var result = rank.ToLocalizedString();
            Assert.False(string.IsNullOrEmpty(result), $"PrestigeRank.{rank} returned empty string");
        }
    }

    #endregion

    #region DeityType Tests

    [Theory]
    [InlineData(DeityDomain.Craft, "Khoras")]
    [InlineData(DeityDomain.Wild, "Lysa")]
    [InlineData(DeityDomain.Harvest, "Aethra")]
    [InlineData(DeityDomain.Stone, "Gaia")]
    [InlineData(DeityDomain.None, "Unknown Deity")]
    public void DeityType_ToLocalizedString_ReturnsExpectedValue(DeityDomain deity, string expected)
    {
        // Act
        var result = deity.ToLocalizedString();

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void DeityType_ToLocalizedString_AllValuesReturnNonEmpty()
    {
        // Arrange
        var allDeities = Enum.GetValues<DeityDomain>();

        // Act & Assert
        foreach (var deity in allDeities)
        {
            var result = deity.ToLocalizedString();
            Assert.False(string.IsNullOrEmpty(result), $"DeityType.{deity} returned empty string");
        }
    }

    [Theory]
    [InlineData(DeityDomain.Craft)]
    [InlineData(DeityDomain.Wild)]
    [InlineData(DeityDomain.Harvest)]
    [InlineData(DeityDomain.Stone)]
    public void DeityType_ToLocalizedStringWithTitle_ContainsNameAndTitle(DeityDomain deity)
    {
        // Act
        var result = deity.ToLocalizedStringWithTitle();

        // Assert
        Assert.Contains(deity.ToLocalizedString(), result);
        Assert.Contains(" - ", result); // Should have separator
    }

    [Fact]
    public void DeityType_None_ToLocalizedStringWithTitle_ReturnsJustName()
    {
        // Act
        var result = DeityDomain.None.ToLocalizedStringWithTitle();

        // Assert
        Assert.Equal("Unknown Deity", result);
        Assert.DoesNotContain(" - ", result); // No separator for None
    }

    [Theory]
    [InlineData(DeityDomain.Craft)]
    [InlineData(DeityDomain.Wild)]
    [InlineData(DeityDomain.Harvest)]
    [InlineData(DeityDomain.Stone)]
    public void DeityType_ToLocalizedDescription_ReturnsNonEmpty(DeityDomain deity)
    {
        // Act
        var result = deity.ToLocalizedDescription();

        // Assert
        Assert.False(string.IsNullOrEmpty(result), $"DeityType.{deity} description is empty");
        Assert.NotEqual(deity.ToString(), result); // Should not just return enum name
    }

    #endregion
}