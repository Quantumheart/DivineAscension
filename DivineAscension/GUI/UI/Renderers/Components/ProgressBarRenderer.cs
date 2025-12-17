using System;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using DivineAscension.GUI.UI.Utilities;
using ImGuiNET;

namespace DivineAscension.GUI.UI.Renderers.Components;

/// <summary>
///     Renders progress bars for favor and prestige tracking
/// </summary>
[ExcludeFromCodeCoverage]
internal static class ProgressBarRenderer
{
    /// <summary>
    ///     Draw a progress bar
    /// </summary>
    /// <param name="drawList">ImGui draw list</param>
    /// <param name="x">X position</param>
    /// <param name="y">Y position</param>
    /// <param name="width">Bar width</param>
    /// <param name="height">Bar height</param>
    /// <param name="percentage">Progress percentage (0.0 to 1.0)</param>
    /// <param name="fillColor">Fill color</param>
    /// <param name="backgroundColor">Background color</param>
    /// <param name="labelText">Label text to display</param>
    /// <param name="showGlow">Whether to show glow effect</param>
    public static void DrawProgressBar(
        ImDrawListPtr drawList,
        float x, float y, float width, float height,
        float percentage,
        Vector4 fillColor,
        Vector4 backgroundColor,
        string labelText,
        bool showGlow = false)
    {
        // Background
        var bgMin = new Vector2(x, y);
        var bgMax = new Vector2(x + width, y + height);
        var bgColorU32 = ImGui.ColorConvertFloat4ToU32(backgroundColor);
        drawList.AddRectFilled(bgMin, bgMax, bgColorU32, 4f);

        // Fill (progress)
        if (percentage > 0)
        {
            var fillWidth = width * Math.Clamp(percentage, 0f, 1f);
            var fillMax = new Vector2(x + fillWidth, y + height);
            var fillColorU32 = ImGui.ColorConvertFloat4ToU32(fillColor);
            drawList.AddRectFilled(bgMin, fillMax, fillColorU32, 4f);

            // Glow effect (if >80% progress)
            if (showGlow && percentage > 0.8f)
            {
                var glowAlpha = (float)(Math.Sin(ImGui.GetTime() * 3.0) * 0.3 + 0.7);
                var glowColor = new Vector4(fillColor.X, fillColor.Y, fillColor.Z, glowAlpha);
                var glowColorU32 = ImGui.ColorConvertFloat4ToU32(glowColor);
                drawList.AddRect(bgMin, fillMax, glowColorU32, 4f, ImDrawFlags.None, 2f);
            }
        }

        // Border
        var borderColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.Gold * 0.5f);
        drawList.AddRect(bgMin, bgMax, borderColor, 4f, ImDrawFlags.None, 1f);

        // Label text (centered)
        var textSize = ImGui.CalcTextSize(labelText);
        var textPos = new Vector2(
            x + (width - textSize.X) / 2,
            y + (height - textSize.Y) / 2
        );
        var textColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.White);
        drawList.AddText(textPos, textColor, labelText);
    }
}