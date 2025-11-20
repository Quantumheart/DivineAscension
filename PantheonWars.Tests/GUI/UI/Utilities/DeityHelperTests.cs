using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using PantheonWars.GUI.UI.Utilities;
using PantheonWars.Models.Enum;
using Xunit;

namespace PantheonWars.Tests.GUI.UI.Utilities;

/// <summary>
///     Unit tests for DeityHelper
/// </summary>
[ExcludeFromCodeCoverage]
public class DeityHelperTests
{
    #region DeityNames Tests

    [Fact]
    public void DeityNames_ContainsAllThreeDeities()
    {
        // Assert
        Assert.Equal(3, DeityHelper.DeityNames.Length);
        Assert.Contains("Aethra", DeityHelper.DeityNames);
        Assert.Contains("Gaia", DeityHelper.DeityNames);
        Assert.Contains("Morthen", DeityHelper.DeityNames);
    }

    #endregion

    #region GetDeityColor (string) Tests

    [Theory]
    [InlineData("Aethra", 0.9f, 0.9f, 0.6f)]  // Light yellow - Light (Good)
    [InlineData("Gaia", 0.5f, 0.4f, 0.2f)]    // Brown - Nature (Neutral)
    [InlineData("Morthen", 0.3f, 0.1f, 0.4f)] // Purple - Shadow & Death (Evil)
    public void GetDeityColor_String_ReturnsCorrectColor(string deity, float r, float g, float b)
    {
        // Act
        var color = DeityHelper.GetDeityColor(deity);

        // Assert
        Assert.Equal(r, color.X);
        Assert.Equal(g, color.Y);
        Assert.Equal(b, color.Z);
        Assert.Equal(1.0f, color.W); // Alpha always 1.0
    }

    [Fact]
    public void GetDeityColor_String_UnknownDeity_ReturnsGrey()
    {
        // Act
        var color = DeityHelper.GetDeityColor("UnknownDeity");

        // Assert
        Assert.Equal(0.5f, color.X);
        Assert.Equal(0.5f, color.Y);
        Assert.Equal(0.5f, color.Z);
        Assert.Equal(1.0f, color.W);
    }

    #endregion

    #region GetDeityColor (DeityType) Tests

    [Theory]
    [InlineData(DeityType.Aethra, 0.9f, 0.9f, 0.6f)]  // Light yellow - Light (Good)
    [InlineData(DeityType.Gaia, 0.5f, 0.4f, 0.2f)]    // Brown - Nature (Neutral)
    [InlineData(DeityType.Morthen, 0.3f, 0.1f, 0.4f)] // Purple - Shadow & Death (Evil)
    public void GetDeityColor_Enum_ReturnsCorrectColor(DeityType deity, float r, float g, float b)
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
        var color = DeityHelper.GetDeityColor(DeityType.None);

        // Assert
        Assert.Equal(0.5f, color.X);
        Assert.Equal(0.5f, color.Y);
        Assert.Equal(0.5f, color.Z);
        Assert.Equal(1.0f, color.W);
    }

    #endregion

    #region GetDeityTitle (string) Tests

    [Theory]
    [InlineData("Aethra", "Goddess of Light")]
    [InlineData("Gaia", "Goddess of Nature")]
    [InlineData("Morthen", "God of Shadow & Death")]
    public void GetDeityTitle_String_ReturnsCorrectTitle(string deity, string expectedTitle)
    {
        // Act
        var title = DeityHelper.GetDeityTitle(deity);

        // Assert
        Assert.Equal(expectedTitle, title);
    }

    [Fact]
    public void GetDeityTitle_String_UnknownDeity_ReturnsUnknown()
    {
        // Act
        var title = DeityHelper.GetDeityTitle("UnknownDeity");

        // Assert
        Assert.Equal("Unknown Deity", title);
    }

    #endregion

    #region GetDeityTitle (DeityType) Tests

    [Theory]
    [InlineData(DeityType.Aethra, "Goddess of Light")]
    [InlineData(DeityType.Gaia, "Goddess of Nature")]
    [InlineData(DeityType.Morthen, "God of Shadow & Death")]
    public void GetDeityTitle_Enum_ReturnsCorrectTitle(DeityType deity, string expectedTitle)
    {
        // Act
        var title = DeityHelper.GetDeityTitle(deity);

        // Assert
        Assert.Equal(expectedTitle, title);
    }

    [Fact]
    public void GetDeityTitle_Enum_NoneType_ReturnsUnknown()
    {
        // Act
        var title = DeityHelper.GetDeityTitle(DeityType.None);

        // Assert
        Assert.Equal("Unknown Deity", title);
    }

    #endregion

    #region ParseDeityType Tests

    [Theory]
    [InlineData("Aethra", DeityType.Aethra)]
    [InlineData("Gaia", DeityType.Gaia)]
    [InlineData("Morthen", DeityType.Morthen)]
    public void ParseDeityType_ValidName_ReturnsCorrectEnum(string deityName, DeityType expected)
    {
        // Act
        var result = DeityHelper.ParseDeityType(deityName);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void ParseDeityType_UnknownName_ReturnsNone()
    {
        // Act
        var result = DeityHelper.ParseDeityType("UnknownDeity");

        // Assert
        Assert.Equal(DeityType.None, result);
    }

    [Fact]
    public void ParseDeityType_EmptyString_ReturnsNone()
    {
        // Act
        var result = DeityHelper.ParseDeityType("");

        // Assert
        Assert.Equal(DeityType.None, result);
    }

    #endregion

    #region GetDeityDisplayText (string) Tests

    [Theory]
    [InlineData("Aethra", "Aethra - Goddess of Light")]
    [InlineData("Gaia", "Gaia - Goddess of Nature")]
    [InlineData("Morthen", "Morthen - God of Shadow & Death")]
    public void GetDeityDisplayText_String_ReturnsFormattedText(string deity, string expected)
    {
        // Act
        var displayText = DeityHelper.GetDeityDisplayText(deity);

        // Assert
        Assert.Equal(expected, displayText);
    }

    [Fact]
    public void GetDeityDisplayText_String_UnknownDeity_ReturnsFormattedUnknown()
    {
        // Act
        var displayText = DeityHelper.GetDeityDisplayText("UnknownDeity");

        // Assert
        Assert.Equal("UnknownDeity - Unknown Deity", displayText);
    }

    #endregion

    #region GetDeityDisplayText (DeityType) Tests

    [Theory]
    [InlineData(DeityType.Aethra, "Aethra - Goddess of Light")]
    [InlineData(DeityType.Gaia, "Gaia - Goddess of Nature")]
    [InlineData(DeityType.Morthen, "Morthen - God of Shadow & Death")]
    public void GetDeityDisplayText_Enum_ReturnsFormattedText(DeityType deity, string expected)
    {
        // Act
        var displayText = DeityHelper.GetDeityDisplayText(deity);

        // Assert
        Assert.Equal(expected, displayText);
    }

    [Fact]
    public void GetDeityDisplayText_Enum_NoneType_ReturnsFormattedUnknown()
    {
        // Act
        var displayText = DeityHelper.GetDeityDisplayText(DeityType.None);

        // Assert
        Assert.Equal("None - Unknown Deity", displayText);
    }

    #endregion
}
