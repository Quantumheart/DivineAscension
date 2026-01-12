using System.Diagnostics.CodeAnalysis;
using DivineAscension.GUI.UI.Utilities;
using DivineAscension.Models.Enum;

namespace DivineAscension.Tests.GUI.UI.Utilities;

/// <summary>
///     Unit tests for DeityHelper
/// </summary>
[ExcludeFromCodeCoverage]
public class DeityHelperTests
{
    #region DeityNames Tests

    [Fact]
    public void DeityNames_ContainsAllDomains()
    {
        // Assert - DeityNames is now an alias for DomainNames
        Assert.Equal(4, DeityHelper.DeityNames.Length);
        Assert.Contains("Craft", DeityHelper.DeityNames);
        Assert.Contains("Wild", DeityHelper.DeityNames);
        Assert.Contains("Harvest", DeityHelper.DeityNames);
        Assert.Contains("Stone", DeityHelper.DeityNames);
    }

    #endregion

    #region GetDeityColor (string) Tests

    [Theory]
    [InlineData("Craft", 0.8f, 0.2f, 0.2f)] // Red - Forge & Craft
    [InlineData("Wild", 0.4f, 0.8f, 0.3f)] // Green - Hunt & Wild
    [InlineData("Harvest", 0.9f, 0.9f, 0.6f)] // Light yellow - Agriculture & Light
    [InlineData("Stone", 0.5f, 0.4f, 0.2f)] // Brown - Earth & Stone
    public void GetDeityColor_String_ReturnsCorrectColor(string domain, float r, float g, float b)
    {
        // Act
        var color = DeityHelper.GetDeityColor(domain);

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
        var color = DeityHelper.GetDeityColor("UnknownDomain");

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
    [InlineData(DeityDomain.Harvest, 0.9f, 0.9f, 0.6f)]
    [InlineData(DeityDomain.Stone, 0.5f, 0.4f, 0.2f)]
    public void GetDeityColor_Enum_ReturnsCorrectColor(DeityDomain deity, float r, float g, float b)
    {
        // Act
        var color = DeityHelper.GetDeityColor(deity);

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
        var color = DeityHelper.GetDeityColor(DeityDomain.None);

        // Assert
        Assert.Equal(0.5f, color.X);
        Assert.Equal(0.5f, color.Y);
        Assert.Equal(0.5f, color.Z);
        Assert.Equal(1.0f, color.W);
    }

    #endregion

    #region GetDeityTitle (string) Tests

    [Theory]
    [InlineData("Craft", "Domain of the Forge & Craft")]
    [InlineData("Wild", "Domain of the Hunt & Wild")]
    [InlineData("Harvest", "Domain of Agriculture & Light")]
    [InlineData("Stone", "Domain of Earth & Stone")]
    public void GetDeityTitle_String_ReturnsCorrectTitle(string domain, string expectedTitle)
    {
        // Act
        var title = DeityHelper.GetDeityTitle(domain);

        // Assert
        Assert.Equal(expectedTitle, title);
    }

    [Fact]
    public void GetDeityTitle_String_UnknownDomain_ReturnsUnknown()
    {
        // Act
        var title = DeityHelper.GetDeityTitle("UnknownDomain");

        // Assert
        Assert.Equal("Unknown Domain", title);
    }

    #endregion

    #region GetDeityTitle (DeityDomain) Tests

    [Theory]
    [InlineData(DeityDomain.Craft, "Domain of the Forge & Craft")]
    [InlineData(DeityDomain.Wild, "Domain of the Hunt & Wild")]
    [InlineData(DeityDomain.Harvest, "Domain of Agriculture & Light")]
    [InlineData(DeityDomain.Stone, "Domain of Earth & Stone")]
    public void GetDeityTitle_Enum_ReturnsCorrectTitle(DeityDomain domain, string expectedTitle)
    {
        // Act
        var title = DeityHelper.GetDeityTitle(domain);

        // Assert
        Assert.Equal(expectedTitle, title);
    }

    [Fact]
    public void GetDeityTitle_Enum_NoneType_ReturnsUnknown()
    {
        // Act
        var title = DeityHelper.GetDeityTitle(DeityDomain.None);

        // Assert
        Assert.Equal("Unknown Domain", title);
    }

    #endregion

    #region ParseDeityDomain Tests

    [Theory]
    [InlineData("Craft", DeityDomain.Craft)]
    [InlineData("Wild", DeityDomain.Wild)]
    [InlineData("Harvest", DeityDomain.Harvest)]
    [InlineData("Stone", DeityDomain.Stone)]
    public void ParseDeityType_ValidName_ReturnsCorrectEnum(string domainName, DeityDomain expected)
    {
        // Act
        var result = DeityHelper.ParseDeityType(domainName);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void ParseDeityType_UnknownName_ReturnsNone()
    {
        // Act
        var result = DeityHelper.ParseDeityType("UnknownDomain");

        // Assert
        Assert.Equal(DeityDomain.None, result);
    }

    [Fact]
    public void ParseDeityType_EmptyString_ReturnsNone()
    {
        // Act
        var result = DeityHelper.ParseDeityType("");

        // Assert
        Assert.Equal(DeityDomain.None, result);
    }

    #endregion

    #region GetDeityDisplayText (string) Tests

    [Theory]
    [InlineData("Craft", "Craft", "Craft - Domain of the Forge & Craft")]
    [InlineData("Wild", "Wild", "Wild - Domain of the Hunt & Wild")]
    [InlineData("Harvest", "Harvest", "Harvest - Domain of Agriculture & Light")]
    [InlineData("Stone", "Stone", "Stone - Domain of Earth & Stone")]
    public void GetDeityDisplayText_String_ReturnsFormattedText(string deityName, string domain, string expected)
    {
        // Act
        var displayText = DeityHelper.GetDeityDisplayText(deityName, domain);

        // Assert
        Assert.Equal(expected, displayText);
    }

    [Fact]
    public void GetDeityDisplayText_String_EmptyDeityName_ReturnsDomainTitleOnly()
    {
        // Act
        var displayText = DeityHelper.GetDeityDisplayText("", "Craft");

        // Assert
        Assert.Equal("Domain of the Forge & Craft", displayText);
    }

    #endregion

    #region GetDeityDisplayText (DeityDomain) Tests

    [Theory]
    [InlineData(DeityDomain.Craft, "Craft - Domain of the Forge & Craft")]
    [InlineData(DeityDomain.Wild, "Wild - Domain of the Hunt & Wild")]
    [InlineData(DeityDomain.Harvest, "Harvest - Domain of Agriculture & Light")]
    [InlineData(DeityDomain.Stone, "Stone - Domain of Earth & Stone")]
    public void GetDeityDisplayText_Enum_ReturnsFormattedText(DeityDomain domain, string expected)
    {
        // Act
        var displayText = DeityHelper.GetDeityDisplayText(domain);

        // Assert
        Assert.Equal(expected, displayText);
    }

    [Fact]
    public void GetDeityDisplayText_Enum_NoneType_ReturnsFormattedUnknown()
    {
        // Act
        var displayText = DeityHelper.GetDeityDisplayText(DeityDomain.None);

        // Assert
        Assert.Equal("None - Unknown Domain", displayText);
    }

    #endregion
}