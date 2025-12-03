using System.Diagnostics.CodeAnalysis;
using PantheonWars.GUI.UI.Utilities;
using PantheonWars.Models.Enum;

namespace PantheonWars.Tests.GUI.UI.Utilities;

/// <summary>
///     Unit tests for DeityHelper
/// </summary>
[ExcludeFromCodeCoverage]
public class DeityHelperTests
{
    #region DeityNames Tests

    [Fact]
    public void DeityNames_ContainsAllDeities()
    {
        // Assert
        Assert.Equal(4, DeityHelper.DeityNames.Length);
        Assert.Contains("Khoras", DeityHelper.DeityNames);
        Assert.Contains("Lysa", DeityHelper.DeityNames);
        Assert.Contains("Aethra", DeityHelper.DeityNames);
        Assert.Contains("Gaia", DeityHelper.DeityNames);
    }

    #endregion

    #region GetDeityColor (string) Tests

    [Theory]
    [InlineData("Khoras", 0.8f, 0.2f, 0.2f)]  // Red - War
    [InlineData("Lysa", 0.4f, 0.8f, 0.3f)]    // Green - Hunt
    [InlineData("Aethra", 0.9f, 0.9f, 0.6f)]  // Light yellow - Light
    [InlineData("Gaia", 0.5f, 0.4f, 0.2f)]    // Brown - Earth
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
    [InlineData(DeityType.Khoras, 0.8f, 0.2f, 0.2f)]
    [InlineData(DeityType.Lysa, 0.4f, 0.8f, 0.3f)]
    [InlineData(DeityType.Aethra, 0.9f, 0.9f, 0.6f)]
    [InlineData(DeityType.Gaia, 0.5f, 0.4f, 0.2f)]
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
    [InlineData("Khoras", "God of the Forge & Craft")]
    [InlineData("Lysa", "Goddess of the Hunt & Wild")]
    [InlineData("Aethra", "Goddess of Agriculture & Light")]
    [InlineData("Gaia", "Goddess of Earth & Stone")]
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
    [InlineData(DeityType.Khoras, "God of the Forge & Craft")]
    [InlineData(DeityType.Lysa, "Goddess of the Hunt & Wild")]
    [InlineData(DeityType.Aethra, "Goddess of Agriculture & Light")]
    [InlineData(DeityType.Gaia, "Goddess of Earth & Stone")]
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
    [InlineData("Khoras", DeityType.Khoras)]
    [InlineData("Lysa", DeityType.Lysa)]
    [InlineData("Aethra", DeityType.Aethra)]
    [InlineData("Gaia", DeityType.Gaia)]
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
    [InlineData("Khoras", "Khoras - God of the Forge & Craft")]
    [InlineData("Lysa", "Lysa - Goddess of the Hunt & Wild")]
    [InlineData("Aethra", "Aethra - Goddess of Agriculture & Light")]
    [InlineData("Gaia", "Gaia - Goddess of Earth & Stone")]
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
    [InlineData(DeityType.Khoras, "Khoras - God of the Forge & Craft")]
    [InlineData(DeityType.Lysa, "Lysa - Goddess of the Hunt & Wild")]
    [InlineData(DeityType.Aethra, "Aethra - Goddess of Agriculture & Light")]
    [InlineData(DeityType.Gaia, "Gaia - Goddess of Earth & Stone")]
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
