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
    public void DeityNames_ContainsAllDeities()
    {
        // Assert
        Assert.Equal(8, DeityHelper.DeityNames.Length);
        Assert.Contains("Khoras", DeityHelper.DeityNames);
        Assert.Contains("Lysa", DeityHelper.DeityNames);
        Assert.Contains("Morthen", DeityHelper.DeityNames);
        Assert.Contains("Aethra", DeityHelper.DeityNames);
        Assert.Contains("Umbros", DeityHelper.DeityNames);
        Assert.Contains("Tharos", DeityHelper.DeityNames);
        Assert.Contains("Gaia", DeityHelper.DeityNames);
        Assert.Contains("Vex", DeityHelper.DeityNames);
    }

    #endregion

    #region GetDeityColor (string) Tests

    [Theory]
    [InlineData("Khoras", 0.8f, 0.2f, 0.2f)]  // Red - War
    [InlineData("Lysa", 0.4f, 0.8f, 0.3f)]    // Green - Hunt
    [InlineData("Morthen", 0.3f, 0.1f, 0.4f)] // Purple - Death
    [InlineData("Aethra", 0.9f, 0.9f, 0.6f)]  // Light yellow - Light
    [InlineData("Umbros", 0.2f, 0.2f, 0.3f)]  // Dark grey - Shadows
    [InlineData("Tharos", 0.3f, 0.6f, 0.9f)]  // Blue - Storms
    [InlineData("Gaia", 0.5f, 0.4f, 0.2f)]    // Brown - Earth
    [InlineData("Vex", 0.7f, 0.2f, 0.7f)]     // Magenta - Madness
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
    [InlineData(DeityType.Aethra, 0.8f, 0.2f, 0.2f)]
    [InlineData(DeityType.Gaia, 0.4f, 0.8f, 0.3f)]
    [InlineData(DeityType.Morthen, 0.3f, 0.1f, 0.4f)]
    [InlineData(DeityType.Aethra, 0.9f, 0.9f, 0.6f)]
    [InlineData(DeityType.Morthen, 0.2f, 0.2f, 0.3f)]
    [InlineData(DeityType.Aethra, 0.3f, 0.6f, 0.9f)]
    [InlineData(DeityType.Gaia, 0.5f, 0.4f, 0.2f)]
    [InlineData(DeityType.Morthen, 0.7f, 0.2f, 0.7f)]
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
    [InlineData("Khoras", "God of War")]
    [InlineData("Lysa", "Goddess of the Hunt")]
    [InlineData("Morthen", "God of Death")]
    [InlineData("Aethra", "Goddess of Light")]
    [InlineData("Umbros", "God of Shadows")]
    [InlineData("Tharos", "God of Storms")]
    [InlineData("Gaia", "Goddess of Earth")]
    [InlineData("Vex", "God of Madness")]
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
    [InlineData(DeityType.Aethra, "God of War")]
    [InlineData(DeityType.Gaia, "Goddess of the Hunt")]
    [InlineData(DeityType.Morthen, "God of Death")]
    [InlineData(DeityType.Aethra, "Goddess of Light")]
    [InlineData(DeityType.Morthen, "God of Shadows")]
    [InlineData(DeityType.Aethra, "God of Storms")]
    [InlineData(DeityType.Gaia, "Goddess of Earth")]
    [InlineData(DeityType.Morthen, "God of Madness")]
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
    [InlineData("Khoras", DeityType.Aethra)]
    [InlineData("Lysa", DeityType.Gaia)]
    [InlineData("Morthen", DeityType.Morthen)]
    [InlineData("Aethra", DeityType.Aethra)]
    [InlineData("Umbros", DeityType.Morthen)]
    [InlineData("Tharos", DeityType.Aethra)]
    [InlineData("Gaia", DeityType.Gaia)]
    [InlineData("Vex", DeityType.Morthen)]
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
    [InlineData("Khoras", "Khoras - God of War")]
    [InlineData("Lysa", "Lysa - Goddess of the Hunt")]
    [InlineData("Morthen", "Morthen - God of Death")]
    [InlineData("Aethra", "Aethra - Goddess of Light")]
    [InlineData("Umbros", "Umbros - God of Shadows")]
    [InlineData("Tharos", "Tharos - God of Storms")]
    [InlineData("Gaia", "Gaia - Goddess of Earth")]
    [InlineData("Vex", "Vex - God of Madness")]
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
    [InlineData(DeityType.Aethra, "Khoras - God of War")]
    [InlineData(DeityType.Gaia, "Lysa - Goddess of the Hunt")]
    [InlineData(DeityType.Morthen, "Morthen - God of Death")]
    [InlineData(DeityType.Aethra, "Aethra - Goddess of Light")]
    [InlineData(DeityType.Morthen, "Umbros - God of Shadows")]
    [InlineData(DeityType.Aethra, "Tharos - God of Storms")]
    [InlineData(DeityType.Gaia, "Gaia - Goddess of Earth")]
    [InlineData(DeityType.Morthen, "Vex - God of Madness")]
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
