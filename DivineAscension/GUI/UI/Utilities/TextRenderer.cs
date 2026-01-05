using System.Diagnostics.CodeAnalysis;
using System.Numerics;
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
    ///     Draw a label (white text, 14pt)
    /// </summary>
    /// <param name="drawList">ImGui draw list</param>
    /// <param name="text">Text to display</param>
    /// <param name="x">X position</param>
    /// <param name="y">Y position</param>
    /// <param name="fontSize">Font size (default 14f)</param>
    /// <param name="color">Text color (default white)</param>
    public static void DrawLabel(
        ImDrawListPtr drawList,
        string text,
        float x,
        float y,
        float fontSize = 14f,
        Vector4? color = null)
    {
        var textColor = ImGui.ColorConvertFloat4ToU32(color ?? ColorPalette.White);
        drawList.AddText(ImGui.GetFont(), fontSize, new Vector2(x, y), textColor, text);
    }

    /// <summary>
    ///     Draw info text (light grey text, 12pt, word-wrapped)
    /// </summary>
    /// <param name="drawList">ImGui draw list</param>
    /// <param name="text">Text to display</param>
    /// <param name="x">X position</param>
    /// <param name="y">Y position</param>
    /// <param name="width">Maximum width for word wrapping</param>
    /// <param name="fontSize">Font size (default 12f)</param>
    /// <param name="color">Optional color override (default lighter grey for better contrast)</param>
    public static void DrawInfoText(
        ImDrawListPtr drawList,
        string text,
        float x,
        float y,
        float width,
        float fontSize = 12f,
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
    /// <param name="fontSize">Font size used when rendering (default 12f)</param>
    /// <returns>Total height in pixels required to render the wrapped text</returns>
    public static float MeasureWrappedHeight(string text, float width, float fontSize = 12f)
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
    ///     Draw error text (red text, 13pt)
    /// </summary>
    /// <param name="drawList">ImGui draw list</param>
    /// <param name="text">Error message to display</param>
    /// <param name="x">X position</param>
    /// <param name="y">Y position</param>
    /// <param name="fontSize">Font size (default 13f)</param>
    public static void DrawErrorText(
        ImDrawListPtr drawList,
        string text,
        float x,
        float y,
        float fontSize = 13f)
    {
        var textColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.Red);
        drawList.AddText(ImGui.GetFont(), fontSize, new Vector2(x, y), textColor, text);
    }

    /// <summary>
    ///     Draw success text (green text, 13pt)
    /// </summary>
    /// <param name="drawList">ImGui draw list</param>
    /// <param name="text">Success message to display</param>
    /// <param name="x">X position</param>
    /// <param name="y">Y position</param>
    /// <param name="fontSize">Font size (default 13f)</param>
    public static void DrawSuccessText(
        ImDrawListPtr drawList,
        string text,
        float x,
        float y,
        float fontSize = 13f)
    {
        var textColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.Green);
        drawList.AddText(ImGui.GetFont(), fontSize, new Vector2(x, y), textColor, text);
    }

    /// <summary>
    ///     Draw warning text (yellow text, 13pt)
    /// </summary>
    /// <param name="drawList">ImGui draw list</param>
    /// <param name="text">Warning message to display</param>
    /// <param name="x">X position</param>
    /// <param name="y">Y position</param>
    /// <param name="fontSize">Font size (default 13f)</param>
    public static void DrawWarningText(
        ImDrawListPtr drawList,
        string text,
        float x,
        float y,
        float fontSize = 13f)
    {
        var textColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.Yellow);
        drawList.AddText(ImGui.GetFont(), fontSize, new Vector2(x, y), textColor, text);
    }
}