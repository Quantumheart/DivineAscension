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
        drawList.AddRectFilled(bgMin, bgMax, bgColorU32, UiScale.Scaled(4f));

        // Fill (progress)
        if (percentage > 0)
        {
            var fillWidth = width * Math.Clamp(percentage, 0f, 1f);
            var fillMax = new Vector2(x + fillWidth, y + height);
            var fillColorU32 = ImGui.ColorConvertFloat4ToU32(fillColor);
            drawList.AddRectFilled(bgMin, fillMax, fillColorU32, UiScale.Scaled(4f));

            // Glow effect (if >80% progress)
            if (showGlow && percentage > 0.8f)
            {
                var glowAlpha = (float)(Math.Sin(ImGui.GetTime() * 3.0) * 0.3 + 0.7);
                var glowColor = new Vector4(fillColor.X, fillColor.Y, fillColor.Z, glowAlpha);
                var glowColorU32 = ImGui.ColorConvertFloat4ToU32(glowColor);
                drawList.AddRect(bgMin, fillMax, glowColorU32, UiScale.Scaled(4f), ImDrawFlags.None, UiScale.Scaled(2f));
            }
        }

        // Border
        var borderColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.Gold * 0.5f);
        drawList.AddRect(bgMin, bgMax, borderColor, UiScale.Scaled(4f), ImDrawFlags.None, UiScale.Scaled(1f));

        // Label text — drawn twice with clipping so each half reads against
        // whatever background sits behind it. Dark ink on the gold fill,
        // light cream on the dark-brown empty portion.
        // Skip when empty: ImGui.CalcTextSize pins the empty span to a null
        // pointer and UTF8Encoding.GetByteCount throws on it (e.g. the favor
        // bar passes "" at max rank).
        if (string.IsNullOrEmpty(labelText)) return;

        var textSize = ImGui.CalcTextSize(labelText);
        var textPos = new Vector2(
            x + (width - textSize.X) / 2,
            y + (height - textSize.Y) / 2
        );
        var fillEnd = x + width * Math.Clamp(percentage, 0f, 1f);
        var darkInk = ImGui.ColorConvertFloat4ToU32(ColorPalette.White);
        var lightInk = ImGui.ColorConvertFloat4ToU32(ColorPalette.LightText);
        var onFill = PickInk(fillColor, darkInk, lightInk);
        var onBg = PickInk(backgroundColor, darkInk, lightInk);

        drawList.PushClipRect(bgMin, new Vector2(fillEnd, bgMax.Y), true);
        drawList.AddText(textPos, onFill, labelText);
        drawList.PopClipRect();

        drawList.PushClipRect(new Vector2(fillEnd, bgMin.Y), bgMax, true);
        drawList.AddText(textPos, onBg, labelText);
        drawList.PopClipRect();
    }

    private static uint PickInk(Vector4 bg, uint darkInk, uint lightInk)
    {
        // Relative luminance (Rec. 709). Dark ink on light bg, cream on dark.
        var luminance = 0.2126f * bg.X + 0.7152f * bg.Y + 0.0722f * bg.Z;
        return luminance > 0.5f ? darkInk : lightInk;
    }
}