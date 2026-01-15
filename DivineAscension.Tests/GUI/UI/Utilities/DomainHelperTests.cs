using System.Diagnostics.CodeAnalysis;
using DivineAscension.GUI.UI.Utilities;
using DivineAscension.Models.Enum;

namespace DivineAscension.Tests.GUI.UI.Utilities;

/// <summary>
///     Unit tests for DeityHelper
/// </summary>
[ExcludeFromCodeCoverage]
public class DomainHelperTests
{
    #region DeityNames Tests

    [Fact]
    public void DeityNames_ContainsAllDomains()
    {
        // Assert - DeityNames is now an alias for DomainNames
        Assert.Equal(5, DomainHelper.DeityNames.Length);
        Assert.Contains("Craft", DomainHelper.DeityNames);
        Assert.Contains("Wild", DomainHelper.DeityNames);
        Assert.Contains("War", DomainHelper.DeityNames);
        Assert.Contains("Harvest", DomainHelper.DeityNames);
        Assert.Contains("Stone", DomainHelper.DeityNames);
    }

    #endregion

    #region GetDeityColor (string) Tests

    [Theory]
    [InlineData("Craft", 0.8f, 0.2f, 0.2f)] // Red - Forge & Craft
    [InlineData("Wild", 0.4f, 0.8f, 0.3f)] // Green - Hunt & Wild
    [InlineData("War", 0.6f, 0.1f, 0.3f)] // Crimson - Blood & Battle
    [InlineData("Harvest", 0.9f, 0.9f, 0.6f)] // Light yellow - Agriculture & Light
    [InlineData("Stone", 0.5f, 0.4f, 0.2f)] // Brown - Earth & Stone
    public void GetDeityColor_String_ReturnsCorrectColor(string domain, float r, float g, float b)
    {
        // Act
        var color = DomainHelper.GetDeityColor(domain);

        // Assert
        Assert.Equal(r, color.X);
        Assert.Equal(g, color.Y);
        Assert.Equal(b, color.Z);
        Assert.Equal(1.0f, color.W); // Alpha always 1.0
    }

    [Fact]
    public void GetDeityColor_String_UnknownDomain_ReturnsGrey()
    {
        // Act
        var color = DomainHelper.GetDeityColor("UnknownDomain");

        // Assert
        Assert.Equal(0.5f, color.X);
        Assert.Equal(0.5f, color.Y);
        Assert.Equal(0.5f, color.Z);
        Assert.Equal(1.0f, color.W);
    }

    #endregion

    #region GetDeityColor (DeityDomain) Tests

    [Theory]
    [InlineData(DeityDomain.Craft, 0.8f, 0.2f, 0.2f)]
    [InlineData(DeityDomain.Wild, 0.4f, 0.8f, 0.3f)]
    [InlineData(DeityDomain.War, 0.6f, 0.1f, 0.3f)]
    [InlineData(DeityDomain.Harvest, 0.9f, 0.9f, 0.6f)]
    [InlineData(DeityDomain.Stone, 0.5f, 0.4f, 0.2f)]
    public void GetDeityColor_Enum_ReturnsCorrectColor(DeityDomain deity, float r, float g, float b)
    {
        // Act
        var color = DomainHelper.GetDeityColor(deity);

        // Assert
        Assert.Equal(r, color.X);
        Assert.Equal(g, color.Y);
        Assert.Equal(b, color.Z);
        Assert.Equal(1.0f, color.W);
    }

    [Fact]
    public void GetDeityColor_Enum_NoneType_ReturnsGrey()
    {
        // Act
        var color = DomainHelper.GetDeityColor(DeityDomain.None);

        // Assert
        Assert.Equal(0.5f, color.X);
        Assert.Equal(0.5f, color.Y);
        Assert.Equal(0.5f, color.Z);
        Assert.Equal(1.0f, color.W);
    }

    #endregion

    #region GetDeityTitle (string) Tests

    [Theory]
    [InlineData("Craft", "of the Craft")]
    [InlineData("Wild", "of the Wild")]
    [InlineData("War", "of War")]
    [InlineData("Harvest", "of the Harvest")]
    [InlineData("Stone", "of the Stone")]
    public void GetDeityTitle_String_ReturnsCorrectTitle(string domain, string expectedTitle)
    {
        // Act
        var title = DomainHelper.GetDeityTitle(domain);

        // Assert
        Assert.Equal(expectedTitle, title);
    }

    [Fact]
    public void GetDeityTitle_String_UnknownDomain_ReturnsUnknown()
    {
        // Act
        var title = DomainHelper.GetDeityTitle("UnknownDomain");

        // Assert
        Assert.Equal("Unknown Domain", title);
    }

    #endregion

    #region GetDeityTitle (DeityDomain) Tests

    [Theory]
    [InlineData(DeityDomain.Craft, "Domain of the Craft")]
    [InlineData(DeityDomain.Wild, "Domain of the Wild")]
    [InlineData(DeityDomain.War, "Domain of War")]
    [InlineData(DeityDomain.Harvest, "Domain of the Harvest")]
    [InlineData(DeityDomain.Stone, "Domain of the Stone")]
    public void GetDeityTitle_Enum_ReturnsCorrectTitle(DeityDomain domain, string expectedTitle)
    {
        // Act
        var title = DomainHelper.GetDeityTitle(domain);

        // Assert
        Assert.Equal(expectedTitle, title);
    }

    [Fact]
    public void GetDeityTitle_Enum_NoneType_ReturnsUnknown()
    {
        // Act
        var title = DomainHelper.GetDeityTitle(DeityDomain.None);

        // Assert
        Assert.Equal("Unknown Domain", title);
    }

    #endregion

    #region ParseDeityDomain Tests

    [Theory]
    [InlineData("Craft", DeityDomain.Craft)]
    [InlineData("Wild", DeityDomain.Wild)]
    [InlineData("War", DeityDomain.War)]
    [InlineData("Harvest", DeityDomain.Harvest)]
    [InlineData("Stone", DeityDomain.Stone)]
    public void ParseDeityType_ValidName_ReturnsCorrectEnum(string domainName, DeityDomain expected)
    {
        // Act
        var result = DomainHelper.ParseDeityType(domainName);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void ParseDeityType_UnknownName_ReturnsNone()
    {
        // Act
        var result = DomainHelper.ParseDeityType("UnknownDomain");

        // Assert
        Assert.Equal(DeityDomain.None, result);
    }

    [Fact]
    public void ParseDeityType_EmptyString_ReturnsNone()
    {
        // Act
        var result = DomainHelper.ParseDeityType("");

        // Assert
        Assert.Equal(DeityDomain.None, result);
    }

    #endregion
}