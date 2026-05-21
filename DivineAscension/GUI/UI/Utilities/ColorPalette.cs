using System;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;

namespace DivineAscension.GUI.UI.Utilities;

/// <summary>
///     Centralized color palette for all UI components. Modelled on a
///     real illuminated manuscript: aged-vellum page, iron-gall ink for
///     primary text, sepia for secondary, gold leaf for accents, with
///     lapis blue, vermilion red, and verdigris green as the historical
///     accent inks.
///
///     Naming notes:
///     - <see cref="White" /> is the primary text colour and now reads
///       as iron-gall ink (dark) — most renderers draw text on the
///       parchment page, so "primary text" = dark on cream.
///     - Use <see cref="LightText" /> when explicitly painting text onto
///       a dark surface (buttons, tooltips, banners, the title strip).
/// </summary>
[ExcludeFromCodeCoverage]
internal static class ColorPalette
{
    // Primary Colors
    public static readonly Vector4 Gold = new(0.722f, 0.525f, 0.180f, 1.0f); // #B8862E gold leaf
    public static readonly Vector4 White = new(0.176f, 0.141f, 0.094f, 1.0f); // #2D2418 iron-gall ink (primary text)
    public static readonly Vector4 Grey = new(0.420f, 0.337f, 0.220f, 1.0f); // #6B5638 sepia ink (secondary text)

    // Background Colors
    public static readonly Vector4 DarkBrown = new(0.361f, 0.271f, 0.157f, 1.0f); // #5C4528 deep sepia panel (tooltips, title strip, buttons)
    public static readonly Vector4 LightBrown = new(0.478f, 0.361f, 0.220f, 1.0f); // #7A5C38 mid sepia (button hover, card body)
    public static readonly Vector4 Background = new(0.937f, 0.894f, 0.800f, 1.0f); // #EFE4CC parchment page
    public static readonly Vector4 BorderColor = new(0.659f, 0.580f, 0.447f, 1.0f); // #A89472 faded ink edge

    public static readonly Vector4
        TableBackground = new(0.851f, 0.769f, 0.612f, 1.0f); // #D9C49C folded vellum edge (sidebar / rail / table rows)

    // Manuscript accent inks
    public static readonly Vector4 Lapis = new(0.180f, 0.290f, 0.431f, 1.0f); // #2E4A6E lapis blue (active state, secondary identity)
    public static readonly Vector4 Vermilion = new(0.612f, 0.165f, 0.122f, 1.0f); // #9C2A1F rubric red (founder / error)
    public static readonly Vector4 Verdigris = new(0.310f, 0.431f, 0.231f, 1.0f); // #4F6E3B aged-copper green (success / unlocked)

    // State Colors — semantic aliases onto the manuscript inks above.
    public static readonly Vector4 Red = Vermilion; // Error / Danger
    public static readonly Vector4 Green = Verdigris; // Success
    public static readonly Vector4 Yellow = new(0.710f, 0.522f, 0.169f, 1.0f); // #B5852B ochre warning

    // Brighter status variants used for badges, active indicators, completed states
    public static readonly Vector4 SuccessGreen = new(0.416f, 0.561f, 0.310f, 1.0f); // #6A8F4F brighter verdigris
    public static readonly Vector4 ErrorRed = new(0.722f, 0.227f, 0.173f, 1.0f); // #B83A2C brighter vermilion

    // Neutral text variants
    public static readonly Vector4 LightText = new(0.898f, 0.859f, 0.761f, 1.0f); // #E5DBC2 warm cream (text on dark surfaces)
    public static readonly Vector4 DisabledGray = new(0.659f, 0.596f, 0.486f, 1.0f); // #A8987C faded ink (disabled labels)
    public static readonly Vector4 MutedText = new(0.557f, 0.478f, 0.361f, 1.0f); // #8E7A5C faded sepia (hints, secondary captions)

    // Opacity Variants — warmed so modal dims still feel like a dimmed page rather than a cold black wash.
    public static readonly Vector4 BlackOverlay = new(0.18f, 0.13f, 0.08f, 0.8f); // Warm dark modal overlay
    public static readonly Vector4 BlackOverlayLight = new(0.18f, 0.13f, 0.08f, 0.7f); // Lighter warm overlay

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
