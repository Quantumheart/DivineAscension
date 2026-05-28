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
        Assert.Equal(6, DomainHelper.DeityNames.Length);
        Assert.Contains("Craft", DomainHelper.DeityNames);
        Assert.Contains("Wild", DomainHelper.DeityNames);
        Assert.Contains("Conquest", DomainHelper.DeityNames);
        Assert.Contains("Harvest", DomainHelper.DeityNames);
        Assert.Contains("Stone", DomainHelper.DeityNames);
        Assert.Contains("Caravan", DomainHelper.DeityNames);
    }

    #endregion

    #region GetDeityColor (string) Tests

    [Theory]
    [InlineData("Craft", 0.698f, 0.416f, 0.165f)] // #B26A2A copper — Forge & Craft
    [InlineData("Wild", 0.361f, 0.431f, 0.165f)] // #5C6E2A olive — Hunt & Wild
    [InlineData("Conquest", 0.557f, 0.180f, 0.122f)] // #8E2E1F dried-blood red — Domination & Victory
    [InlineData("Harvest", 0.627f, 0.463f, 0.157f)] // #A07628 wheat ochre — Agriculture & Light
    [InlineData("Stone", 0.369f, 0.329f, 0.282f)] // #5E5448 warm slate — Earth & Stone
    [InlineData("Caravan", 0.761f, 0.541f, 0.118f)] // #C28A1E road ochre — Trade & Wayfaring
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
    public void GetDeityColor_String_UnknownDomain_ReturnsFadedInk()
    {
        // Act
        var color = DomainHelper.GetDeityColor("UnknownDomain");

        // Assert — #A89472 faded ink
        Assert.Equal(0.659f, color.X);
        Assert.Equal(0.580f, color.Y);
        Assert.Equal(0.447f, color.Z);
        Assert.Equal(1.0f, color.W);
    }

    #endregion

    #region GetDeityColor (DeityDomain) Tests

    [Theory]
    [InlineData(DeityDomain.Craft, 0.698f, 0.416f, 0.165f)]
    [InlineData(DeityDomain.Wild, 0.361f, 0.431f, 0.165f)]
    [InlineData(DeityDomain.Conquest, 0.557f, 0.180f, 0.122f)]
    [InlineData(DeityDomain.Harvest, 0.627f, 0.463f, 0.157f)]
    [InlineData(DeityDomain.Stone, 0.369f, 0.329f, 0.282f)]
    [InlineData(DeityDomain.Caravan, 0.761f, 0.541f, 0.118f)]
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
    public void GetDeityColor_Enum_NoneType_ReturnsFadedInk()
    {
        // Act
        var color = DomainHelper.GetDeityColor(DeityDomain.None);

        // Assert — #A89472 faded ink
        Assert.Equal(0.659f, color.X);
        Assert.Equal(0.580f, color.Y);
        Assert.Equal(0.447f, color.Z);
        Assert.Equal(1.0f, color.W);
    }

    #endregion

    #region GetDeityTitle (string) Tests

    [Theory]
    [InlineData("Craft", "of the Craft")]
    [InlineData("Wild", "of the Wild")]
    [InlineData("Conquest", "of Conquest")]
    [InlineData("Harvest", "of the Harvest")]
    [InlineData("Stone", "of the Stone")]
    [InlineData("Caravan", "of the Caravan")]
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
    [InlineData(DeityDomain.Conquest, "Domain of Conquest")]
    [InlineData(DeityDomain.Harvest, "Domain of the Harvest")]
    [InlineData(DeityDomain.Stone, "Domain of the Stone")]
    [InlineData(DeityDomain.Caravan, "Domain of the Caravan")]
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
    [InlineData("Conquest", DeityDomain.Conquest)]
    [InlineData("Harvest", DeityDomain.Harvest)]
    [InlineData("Stone", DeityDomain.Stone)]
    [InlineData("Caravan", DeityDomain.Caravan)]
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