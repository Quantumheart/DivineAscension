using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using DivineAscension.GUI.UI.Utilities;

namespace DivineAscension.Tests.GUI.UI.Utilities;

/// <summary>
///     Unit tests for ColorPalette utility class
/// </summary>
[ExcludeFromCodeCoverage]
public class ColorPaletteTests
{
    #region Color Constants Tests

    [Fact]
    public void PrimaryColors_HaveCorrectValues()
    {
        // Assert
        Assert.Equal(new Vector4(0.996f, 0.682f, 0.204f, 1.0f), ColorPalette.Gold);
        Assert.Equal(new Vector4(0.9f, 0.9f, 0.9f, 1.0f), ColorPalette.White);
        Assert.Equal(new Vector4(0.573f, 0.502f, 0.416f, 1.0f), ColorPalette.Grey);
    }

    [Fact]
    public void BackgroundColors_HaveCorrectValues()
    {
        // Assert
        Assert.Equal(new Vector4(0.24f, 0.18f, 0.13f, 1.0f), ColorPalette.DarkBrown);
        Assert.Equal(new Vector4(0.35f, 0.26f, 0.19f, 1.0f), ColorPalette.LightBrown);
        Assert.Equal(new Vector4(0.161f, 0.118f, 0.086f, 1f), ColorPalette.Background);
    }

    [Fact]
    public void StateColors_HaveCorrectValues()
    {
        // Assert
        Assert.Equal(new Vector4(0.8f, 0.2f, 0.2f, 1.0f), ColorPalette.Red);
        Assert.Equal(new Vector4(0.2f, 0.8f, 0.2f, 1.0f), ColorPalette.Green);
        Assert.Equal(new Vector4(0.8f, 0.8f, 0.2f, 1.0f), ColorPalette.Yellow);
    }

    [Fact]
    public void OverlayColors_HaveCorrectValues()
    {
        // Assert
        Assert.Equal(new Vector4(0f, 0f, 0f, 0.8f), ColorPalette.BlackOverlay);
        Assert.Equal(new Vector4(0f, 0f, 0f, 0.7f), ColorPalette.BlackOverlayLight);
    }

    #endregion

    #region Darken Tests

    [Fact]
    public void Darken_WithDefaultFactor_Darkens70Percent()
    {
        // Arrange
        var color = new Vector4(1.0f, 0.8f, 0.6f, 0.9f);

        // Act
        var darkened = ColorPalette.Darken(color);

        // Assert
        Assert.Equal(0.7f, darkened.X);
        Assert.Equal(0.56f, darkened.Y, precision: 5);
        Assert.Equal(0.42f, darkened.Z, precision: 5);
        Assert.Equal(0.9f, darkened.W); // Alpha unchanged
    }

    [Fact]
    public void Darken_WithCustomFactor_DarkensCorrectly()
    {
        // Arrange
        var color = new Vector4(1.0f, 0.8f, 0.6f, 1.0f);

        // Act
        var darkened = ColorPalette.Darken(color, 0.5f);

        // Assert
        Assert.Equal(0.5f, darkened.X);
        Assert.Equal(0.4f, darkened.Y);
        Assert.Equal(0.3f, darkened.Z);
        Assert.Equal(1.0f, darkened.W);
    }

    [Fact]
    public void Darken_PreservesAlpha()
    {
        // Arrange
        var color = new Vector4(0.5f, 0.5f, 0.5f, 0.3f);

        // Act
        var darkened = ColorPalette.Darken(color, 0.8f);

        // Assert
        Assert.Equal(0.3f, darkened.W);
    }

    #endregion

    #region Lighten Tests

    [Fact]
    public void Lighten_WithDefaultFactor_Lightens30Percent()
    {
        // Arrange
        var color = new Vector4(0.5f, 0.4f, 0.3f, 0.9f);

        // Act
        var lightened = ColorPalette.Lighten(color);

        // Assert
        Assert.Equal(0.65f, lightened.X);
        Assert.Equal(0.52f, lightened.Y, precision: 5);
        Assert.Equal(0.39f, lightened.Z, precision: 5);
        Assert.Equal(0.9f, lightened.W); // Alpha unchanged
    }

    [Fact]
    public void Lighten_WithCustomFactor_LightensCorrectly()
    {
        // Arrange
        var color = new Vector4(0.5f, 0.4f, 0.3f, 1.0f);

        // Act
        var lightened = ColorPalette.Lighten(color, 2.0f);

        // Assert
        Assert.Equal(1.0f, lightened.X); // Capped at 1.0
        Assert.Equal(0.8f, lightened.Y);
        Assert.Equal(0.6f, lightened.Z);
        Assert.Equal(1.0f, lightened.W);
    }

    [Fact]
    public void Lighten_ClampValuesAtOne()
    {
        // Arrange
        var color = new Vector4(0.9f, 0.8f, 0.7f, 1.0f);

        // Act
        var lightened = ColorPalette.Lighten(color, 2.0f);

        // Assert
        Assert.Equal(1.0f, lightened.X); // Clamped
        Assert.Equal(1.0f, lightened.Y); // Clamped
        Assert.Equal(1.0f, lightened.Z); // Clamped
    }

    [Fact]
    public void Lighten_PreservesAlpha()
    {
        // Arrange
        var color = new Vector4(0.5f, 0.5f, 0.5f, 0.3f);

        // Act
        var lightened = ColorPalette.Lighten(color, 1.5f);

        // Assert
        Assert.Equal(0.3f, lightened.W);
    }

    #endregion

    #region WithAlpha Tests

    [Fact]
    public void WithAlpha_ChangesAlphaOnly()
    {
        // Arrange
        var color = new Vector4(0.8f, 0.6f, 0.4f, 1.0f);

        // Act
        var withAlpha = ColorPalette.WithAlpha(color, 0.5f);

        // Assert
        Assert.Equal(0.8f, withAlpha.X);
        Assert.Equal(0.6f, withAlpha.Y);
        Assert.Equal(0.4f, withAlpha.Z);
        Assert.Equal(0.5f, withAlpha.W);
    }

    [Fact]
    public void WithAlpha_CanSetToZero()
    {
        // Arrange
        var color = ColorPalette.Gold;

        // Act
        var transparent = ColorPalette.WithAlpha(color, 0f);

        // Assert
        Assert.Equal(0f, transparent.W);
    }

    [Fact]
    public void WithAlpha_CanSetToOne()
    {
        // Arrange
        var color = new Vector4(0.5f, 0.5f, 0.5f, 0.5f);

        // Act
        var opaque = ColorPalette.WithAlpha(color, 1f);

        // Assert
        Assert.Equal(1f, opaque.W);
    }

    #endregion

    #region Integration Tests - Method Chaining

    [Fact]
    public void ColorModifications_CanBeChained()
    {
        // Arrange
        var baseColor = ColorPalette.Gold;

        // Act
        var modified = ColorPalette.Darken(baseColor, 0.8f);
        modified = ColorPalette.WithAlpha(modified, 0.5f);

        // Assert
        Assert.Equal(0.8f * 0.996f, modified.X, precision: 5);
        Assert.Equal(0.8f * 0.682f, modified.Y, precision: 5);
        Assert.Equal(0.8f * 0.204f, modified.Z, precision: 5);
        Assert.Equal(0.5f, modified.W);
    }

    [Fact]
    public void ColorModifications_LightenThenDarken_IsReversible()
    {
        // Arrange
        var original = new Vector4(0.5f, 0.5f, 0.5f, 1.0f);

        // Act
        var lightened = ColorPalette.Lighten(original, 1.3f);
        var backToDark = ColorPalette.Darken(lightened, 1.0f / 1.3f);

        // Assert
        Assert.Equal(original.X, backToDark.X, precision: 5);
        Assert.Equal(original.Y, backToDark.Y, precision: 5);
        Assert.Equal(original.Z, backToDark.Z, precision: 5);
        Assert.Equal(original.W, backToDark.W);
    }

    #endregion
}