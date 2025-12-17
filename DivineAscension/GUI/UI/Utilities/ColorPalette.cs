using System;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;

namespace PantheonWars.GUI.UI.Utilities;

/// <summary>
///     Centralized color palette for all UI components
///     Ensures consistent theming across all overlays and dialogs
/// </summary>
[ExcludeFromCodeCoverage]
internal static class ColorPalette
{
    // Primary Colors
    public static readonly Vector4 Gold = new(0.996f, 0.682f, 0.204f, 1.0f); // #feae34
    public static readonly Vector4 White = new(0.9f, 0.9f, 0.9f, 1.0f); // Text
    public static readonly Vector4 Grey = new(0.573f, 0.502f, 0.416f, 1.0f); // #92806a

    // Background Colors
    public static readonly Vector4 DarkBrown = new(0.24f, 0.18f, 0.13f, 1.0f); // #3d2e20
    public static readonly Vector4 LightBrown = new(0.35f, 0.26f, 0.19f, 1.0f); // Hover
    public static readonly Vector4 Background = new(0.16f, 0.12f, 0.09f, 0.95f); // Panel background

    // State Colors
    public static readonly Vector4 Red = new(0.8f, 0.2f, 0.2f, 1.0f); // Error/Danger
    public static readonly Vector4 Green = new(0.2f, 0.8f, 0.2f, 1.0f); // Success
    public static readonly Vector4 Yellow = new(0.8f, 0.8f, 0.2f, 1.0f); // Warning

    // Opacity Variants
    public static readonly Vector4 BlackOverlay = new(0f, 0f, 0f, 0.8f); // Modal background
    public static readonly Vector4 BlackOverlayLight = new(0f, 0f, 0f, 0.7f); // Lighter overlay

    // Common Color Modifications
    public static Vector4 Darken(Vector4 color, float factor = 0.7f)
    {
        return new Vector4(color.X * factor, color.Y * factor, color.Z * factor, color.W);
    }

    public static Vector4 Lighten(Vector4 color, float factor = 1.3f)
    {
        return new Vector4(
            Math.Min(1.0f, color.X * factor),
            Math.Min(1.0f, color.Y * factor),
            Math.Min(1.0f, color.Z * factor),
            color.W
        );
    }

    public static Vector4 WithAlpha(Vector4 color, float alpha)
    {
        return new Vector4(color.X, color.Y, color.Z, alpha);
    }
}