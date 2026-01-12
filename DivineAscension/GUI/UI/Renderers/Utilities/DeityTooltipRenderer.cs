using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using DivineAscension.Constants;
using DivineAscension.GUI.UI.Utilities;
using DivineAscension.Services;
using ImGuiNET;

namespace DivineAscension.GUI.UI.Renderers.Utilities;

/// <summary>
/// Renders tooltips for deity tabs in the religion create UI
/// Displays basic deity information on hover
/// </summary>
[ExcludeFromCodeCoverage]
internal static class DeityTooltipRenderer
{
    private const float TOOLTIP_MAX_WIDTH = 280f;
    private const float TOOLTIP_PADDING = 12f;
    private const float LINE_SPACING = 4f;
    private const float SECTION_SPACING = 8f;

    /// <summary>
    /// Draw a tooltip for a deity when hovering over deity tabs
    /// </summary>
    /// <param name="deityName">Name of the deity</param>
    /// <param name="mouseX">Mouse X position (screen space)</param>
    /// <param name="mouseY">Mouse Y position (screen space)</param>
    /// <param name="windowWidth">Window width for edge detection</param>
    /// <param name="windowHeight">Window height for edge detection</param>
    public static void Draw(
        string deityName,
        float mouseX,
        float mouseY,
        float windowWidth,
        float windowHeight)
    {
        var deityInfo = DeityInfoHelper.GetDeityInfo(deityName);
        if (deityInfo == null) return;

        var deityType = DeityHelper.ParseDeityType(deityName);
        var deityColor = DeityHelper.GetDeityColor(deityType);
        var iconTextureId = DeityIconLoader.GetDeityTextureId(deityType);

        var drawList = ImGui.GetForegroundDrawList();

        // Calculate dimensions
        var lines = BuildTooltipLines(deityInfo, deityColor);
        var contentHeight = CalculateHeight(lines);
        var tooltipHeight = contentHeight + TOOLTIP_PADDING * 2;

        // Position tooltip (with edge detection)
        var (tooltipX, tooltipY) = CalculatePosition(
            mouseX, mouseY, windowWidth, windowHeight,
            TOOLTIP_MAX_WIDTH, tooltipHeight);

        // Draw background and border
        DrawBackground(drawList, tooltipX, tooltipY, TOOLTIP_MAX_WIDTH, tooltipHeight);

        // Draw icon in top-right corner (32x32)
        if (iconTextureId != IntPtr.Zero)
        {
            DrawIcon(drawList, iconTextureId,
                tooltipX + TOOLTIP_MAX_WIDTH - 32f - TOOLTIP_PADDING,
                tooltipY + TOOLTIP_PADDING);
        }

        // Draw text content
        DrawContent(drawList, lines, tooltipX, tooltipY);
    }

    /// <summary>
    /// Build formatted lines for tooltip content
    /// </summary>
    private static List<TooltipLine> BuildTooltipLines(DeityInfo info, Vector4 deityColor)
    {
        var lines = new List<TooltipLine>();

        // Deity name in deity color, bold, 18px
        lines.Add(new TooltipLine(info.Name, deityColor, 18f, true, SECTION_SPACING));

        // Title in gold, 14px
        lines.Add(new TooltipLine(info.Title, ColorPalette.Gold, 14f, false, SECTION_SPACING));

        // Domain in grey, 13px
        lines.Add(new TooltipLine(
            $"{LocalizationService.Instance.Get(LocalizationKeys.DOMAIN_LABEL)} {info.Domain}", ColorPalette.Grey,
            13f, false, SECTION_SPACING));

        // Description wrapped, white, 13px (reserve space for icon on right)
        var wrappedDesc = WrapText(info.Description, TOOLTIP_MAX_WIDTH - TOOLTIP_PADDING * 2 - 40f, 13f);
        foreach (var line in wrappedDesc)
        {
            lines.Add(new TooltipLine(line, ColorPalette.White, 13f, false, LINE_SPACING));
        }

        return lines;
    }

    /// <summary>
    /// Calculate total height needed for tooltip content
    /// </summary>
    private static float CalculateHeight(List<TooltipLine> lines)
    {
        var totalHeight = 0f;
        foreach (var line in lines)
        {
            totalHeight += line.Height + line.SpacingAfter;
        }

        return totalHeight;
    }

    /// <summary>
    /// Calculate tooltip position with edge detection
    /// </summary>
    private static (float x, float y) CalculatePosition(
        float mouseX, float mouseY,
        float windowWidth, float windowHeight,
        float tooltipWidth, float tooltipHeight)
    {
        var windowPos = ImGui.GetWindowPos();
        var offsetX = 16f;
        var offsetY = 16f;

        var tooltipX = mouseX + offsetX;
        var tooltipY = mouseY + offsetY;

        // Check right edge (relative to window)
        if (tooltipX - windowPos.X + tooltipWidth > windowWidth)
            tooltipX = mouseX - tooltipWidth - offsetX; // Show on left side

        // Check bottom edge (relative to window)
        if (tooltipY - windowPos.Y + tooltipHeight > windowHeight)
            tooltipY = mouseY - tooltipHeight - offsetY; // Show above cursor

        // Ensure doesn't go off left edge
        if (tooltipX < windowPos.X)
            tooltipX = windowPos.X + 4f;

        // Ensure doesn't go off top edge
        if (tooltipY < windowPos.Y)
            tooltipY = windowPos.Y + 4f;

        return (tooltipX, tooltipY);
    }

    /// <summary>
    /// Draw tooltip background and border
    /// </summary>
    private static void DrawBackground(ImDrawListPtr drawList, float x, float y, float width, float height)
    {
        var bgStart = new Vector2(x, y);
        var bgEnd = new Vector2(x + width, y + height);

        // Dark brown background
        var bgColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.DarkBrown);
        drawList.AddRectFilled(bgStart, bgEnd, bgColor, 4f);

        // Gold border
        var borderColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.Gold * 0.6f);
        drawList.AddRect(bgStart, bgEnd, borderColor, 4f, ImDrawFlags.None, 2f);
    }

    /// <summary>
    /// Draw deity icon
    /// </summary>
    private static void DrawIcon(ImDrawListPtr drawList, IntPtr textureId, float x, float y)
    {
        const float iconSize = 32f;
        var iconMin = new Vector2(x, y);
        var iconMax = new Vector2(x + iconSize, y + iconSize);

        var tintColor = ImGui.ColorConvertFloat4ToU32(new Vector4(1f, 1f, 1f, 1f));
        drawList.AddImage(textureId, iconMin, iconMax, Vector2.Zero, Vector2.One, tintColor);
    }

    /// <summary>
    /// Draw tooltip text content
    /// </summary>
    private static void DrawContent(ImDrawListPtr drawList, List<TooltipLine> lines, float tooltipX, float tooltipY)
    {
        var currentY = tooltipY + TOOLTIP_PADDING;
        foreach (var line in lines)
        {
            var textPos = new Vector2(tooltipX + TOOLTIP_PADDING, currentY);
            var textColor = ImGui.ColorConvertFloat4ToU32(line.Color);

            // Use same font for both bold and regular (ImGui doesn't have built-in bold support)
            drawList.AddText(ImGui.GetFont(), line.FontSize, textPos, textColor, line.Text);

            currentY += line.Height + line.SpacingAfter;
        }
    }

    /// <summary>
    /// Wrap text to fit within max width
    /// </summary>
    private static List<string> WrapText(string text, float maxWidth, float fontSize)
    {
        var result = new List<string>();
        if (string.IsNullOrEmpty(text)) return result;

        // Split by words
        var words = text.Split(' ');
        var currentLine = "";

        foreach (var word in words)
        {
            var testLine = string.IsNullOrEmpty(currentLine) ? word : currentLine + " " + word;
            var testSize = ImGui.CalcTextSize(testLine);

            // Scale based on font size (CalcTextSize uses default font size)
            var scaledWidth = testSize.X * (fontSize / ImGui.GetFontSize());

            if (scaledWidth > maxWidth && !string.IsNullOrEmpty(currentLine))
            {
                // Line too long, save current line and start new one
                result.Add(currentLine);
                currentLine = word;
            }
            else
            {
                currentLine = testLine;
            }
        }

        // Add last line
        if (!string.IsNullOrEmpty(currentLine))
            result.Add(currentLine);

        return result;
    }

    /// <summary>
    /// Represents a single line in a tooltip
    /// </summary>
    private record TooltipLine(string Text, Vector4 Color, float FontSize, bool IsBold, float SpacingAfter)
    {
        public float Height => FontSize + 4f; // Font size + small buffer
    }
}