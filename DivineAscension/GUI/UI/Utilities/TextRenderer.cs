using System;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using DivineAscension.Services.UI;
using ImGuiNET;

namespace DivineAscension.GUI.UI.Utilities;

/// <summary>
///     Utility class for rendering various text styles
///     Provides consistent text rendering for labels, info text, errors, etc.
/// </summary>
[ExcludeFromCodeCoverage]
public static class TextRenderer
{
    /// <summary>
    ///     Draw a label (white text, SubsectionLabel size)
    /// </summary>
    /// <param name="drawList">ImGui draw list</param>
    /// <param name="text">Text to display</param>
    /// <param name="x">X position</param>
    /// <param name="y">Y position</param>
    /// <param name="fontSize">Font size (default FontSizes.SubsectionLabel)</param>
    /// <param name="color">Text color (default white)</param>
    public static void DrawLabel(
        ImDrawListPtr drawList,
        string text,
        float x,
        float y,
        float fontSize = FontSizes.SubsectionLabel,
        Vector4? color = null)
    {
        var textColor = ImGui.ColorConvertFloat4ToU32(color ?? ColorPalette.White);
        drawList.AddText(ImGui.GetFont(), fontSize, new Vector2(x, y), textColor, text);
    }

    /// <summary>
    ///     Draw a chapter-style title in Cinzel Regular at the closest baked
    ///     size (falls back to the default font when Cinzel isn't loaded yet).
    ///     Returns the rendered text width so callers can chain rank tags or
    ///     right-anchored siblings on the same baseline without Montserrat-vs-
    ///     Cinzel width drift.
    /// </summary>
    public static float DrawSerifLabel(
        ImDrawListPtr drawList,
        string text,
        float x,
        float y,
        float fontSize,
        Vector4? color = null)
    {
        var textColor = ImGui.ColorConvertFloat4ToU32(color ?? ColorPalette.White);
        var serif = CinzelFontSystem.GetRegular(NearestBakedSize((int)fontSize));
        if (serif.HasValue)
        {
            var font = serif.Value;
            ImGui.PushFont(font);
            var width = ImGui.CalcTextSize(text).X;
            ImGui.PopFont();
            drawList.AddText(font, font.FontSize, new Vector2(x, y), textColor, text);
            return width;
        }

        drawList.AddText(ImGui.GetFont(), fontSize, new Vector2(x, y), textColor, text);
        return ImGui.CalcTextSize(text).X * (fontSize / ImGui.GetFontSize());
    }

    /// <summary>
    ///     Measure a string in the same face <see cref="DrawSerifLabel" /> would
    ///     render it in. Used by header renderers that need to anchor sibling
    ///     elements against the right edge of the title before drawing it.
    /// </summary>
    public static float MeasureSerifLabel(string text, float fontSize)
    {
        var serif = CinzelFontSystem.GetRegular(NearestBakedSize((int)fontSize));
        if (serif.HasValue)
        {
            ImGui.PushFont(serif.Value);
            var width = ImGui.CalcTextSize(text).X;
            ImGui.PopFont();
            return width;
        }
        return ImGui.CalcTextSize(text).X * (fontSize / ImGui.GetFontSize());
    }

    private static int NearestBakedSize(int requested)
    {
        // VSImGui's default font sizes (FontManager.Sizes).
        ReadOnlySpan<int> baked = stackalloc int[] { 6, 8, 10, 14, 18, 24, 30, 36, 48, 60 };
        var best = baked[0];
        var bestDelta = int.MaxValue;
        foreach (var s in baked)
        {
            var delta = System.Math.Abs(s - requested);
            if (delta < bestDelta)
            {
                best = s;
                bestDelta = delta;
            }
        }
        return best;
    }

    /// <summary>
    ///     Draw info text (light grey text, Secondary size, word-wrapped)
    /// </summary>
    /// <param name="drawList">ImGui draw list</param>
    /// <param name="text">Text to display</param>
    /// <param name="x">X position</param>
    /// <param name="y">Y position</param>
    /// <param name="width">Maximum width for word wrapping</param>
    /// <param name="fontSize">Font size (default FontSizes.Secondary)</param>
    /// <param name="color">Optional color override (default lighter grey for better contrast)</param>
    public static void DrawInfoText(
        ImDrawListPtr drawList,
        string text,
        float x,
        float y,
        float width,
        float fontSize = FontSizes.Secondary,
        Vector4? color = null)
    {
        // Use a lighter grey (0.8, 0.8, 0.8) for better contrast on dark backgrounds
        var defaultColor = new Vector4(0.8f, 0.8f, 0.8f, 1.0f);
        var textColor = ImGui.ColorConvertFloat4ToU32(color ?? defaultColor);

        // Simple word wrap
        var words = text.Split(' ');
        var currentLine = "";
        var lineY = y;
        var lineHeight = fontSize + 6f; // Spacing between lines

        foreach (var word in words)
        {
            var testLine = string.IsNullOrEmpty(currentLine) ? word : $"{currentLine} {word}";
            var testSize = ImGui.CalcTextSize(testLine);

            if (testSize.X > width && !string.IsNullOrEmpty(currentLine))
            {
                drawList.AddText(ImGui.GetFont(), fontSize, new Vector2(x, lineY), textColor, currentLine);
                lineY += lineHeight;
                currentLine = word;
            }
            else
            {
                currentLine = testLine;
            }
        }

        if (!string.IsNullOrEmpty(currentLine))
            drawList.AddText(ImGui.GetFont(), fontSize, new Vector2(x, lineY), textColor, currentLine);
    }

    /// <summary>
    ///     Measure the pixel height of a block of word-wrapped info text for a given width.
    ///     Mirrors the simple wrapping algorithm in <see cref="DrawInfoText" /> so callers can layout correctly.
    /// </summary>
    /// <param name="text">Text to measure</param>
    /// <param name="width">Maximum width for wrapping</param>
    /// <param name="fontSize">Font size used when rendering (default FontSizes.Secondary)</param>
    /// <returns>Total height in pixels required to render the wrapped text</returns>
    public static float MeasureWrappedHeight(string text, float width, float fontSize = FontSizes.Secondary)
    {
        // Mirror the wrapping logic to keep measurements consistent
        var words = text.Split(' ');
        var currentLine = "";
        var lines = 0;
        var lineHeight = fontSize + 6f; // keep spacing identical to DrawInfoText

        foreach (var word in words)
        {
            var testLine = string.IsNullOrEmpty(currentLine) ? word : $"{currentLine} {word}";
            var testSize = ImGui.CalcTextSize(testLine);

            if (testSize.X > width && !string.IsNullOrEmpty(currentLine))
            {
                lines++;
                currentLine = word;
            }
            else
            {
                currentLine = testLine;
            }
        }

        if (!string.IsNullOrEmpty(currentLine)) lines++;

        return lines <= 0 ? 0f : lines * lineHeight;
    }

    /// <summary>
    ///     Draw error text (red text, Body size)
    /// </summary>
    /// <param name="drawList">ImGui draw list</param>
    /// <param name="text">Error message to display</param>
    /// <param name="x">X position</param>
    /// <param name="y">Y position</param>
    /// <param name="fontSize">Font size (default FontSizes.Body)</param>
    public static void DrawErrorText(
        ImDrawListPtr drawList,
        string text,
        float x,
        float y,
        float fontSize = FontSizes.Body)
    {
        var textColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.Red);
        drawList.AddText(ImGui.GetFont(), fontSize, new Vector2(x, y), textColor, text);
    }

    /// <summary>
    ///     Draw success text (green text, Body size)
    /// </summary>
    /// <param name="drawList">ImGui draw list</param>
    /// <param name="text">Success message to display</param>
    /// <param name="x">X position</param>
    /// <param name="y">Y position</param>
    /// <param name="fontSize">Font size (default FontSizes.Body)</param>
    public static void DrawSuccessText(
        ImDrawListPtr drawList,
        string text,
        float x,
        float y,
        float fontSize = FontSizes.Body)
    {
        var textColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.Green);
        drawList.AddText(ImGui.GetFont(), fontSize, new Vector2(x, y), textColor, text);
    }

    /// <summary>
    ///     Draw warning text (yellow text, Body size)
    /// </summary>
    /// <param name="drawList">ImGui draw list</param>
    /// <param name="text">Warning message to display</param>
    /// <param name="x">X position</param>
    /// <param name="y">Y position</param>
    /// <param name="fontSize">Font size (default FontSizes.Body)</param>
    public static void DrawWarningText(
        ImDrawListPtr drawList,
        string text,
        float x,
        float y,
        float fontSize = FontSizes.Body)
    {
        var textColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.Yellow);
        drawList.AddText(ImGui.GetFont(), fontSize, new Vector2(x, y), textColor, text);
    }
}