using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using ImGuiNET;
using PantheonWars.GUI.UI.Utilities;
using PantheonWars.Models;
using PantheonWars.Models.Enum;

namespace PantheonWars.GUI.UI.Renderers.Blessing;

/// <summary>
///     Renders rich tooltips when hovering over blessing nodes
///     Displays blessing details, requirements, stats, and unlock status
/// </summary>
[ExcludeFromCodeCoverage]
internal static class TooltipRenderer
{
    private const float TOOLTIP_MAX_WIDTH = 320f;
    private const float TOOLTIP_PADDING = 12f;
    private const float LINE_SPACING = 4f;
    private const float SECTION_SPACING = 8f;

    /// <summary>
    ///     Draw a tooltip for a blessing node when hovering
    /// </summary>
    /// <param name="tooltipData">Tooltip data to display</param>
    /// <param name="mouseX">Mouse X position (screen space)</param>
    /// <param name="mouseY">Mouse Y position (screen space)</param>
    /// <param name="windowWidth">Window width for edge detection</param>
    /// <param name="windowHeight">Window height for edge detection</param>
    public static void Draw(
        BlessingTooltipData? tooltipData,
        float mouseX,
        float mouseY,
        float windowWidth,
        float windowHeight)
    {
        if (tooltipData == null) return;

        // Use foreground draw list to ensure tooltip renders on top of everything
        var drawList = ImGui.GetForegroundDrawList();

        // Calculate tooltip content
        var lines = BuildTooltipLines(tooltipData);

        // Calculate tooltip dimensions
        var contentHeight = CalculateTooltipHeight(lines);
        var tooltipWidth = TOOLTIP_MAX_WIDTH;
        var tooltipHeight = contentHeight + TOOLTIP_PADDING * 2;

        // Get window position to work in screen space
        var windowPos = ImGui.GetWindowPos();

        // Position tooltip (offset from mouse, check screen edges)
        var offsetX = 16f; // Offset from cursor
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
            tooltipX = windowPos.X + 4f; // Small padding from left edge

        // Ensure doesn't go off top edge
        if (tooltipY < windowPos.Y)
            tooltipY = windowPos.Y + 4f; // Small padding from top edge

        // Draw tooltip background
        var bgStart = new Vector2(tooltipX, tooltipY);
        var bgEnd = new Vector2(tooltipX + tooltipWidth, tooltipY + tooltipHeight);
        var bgColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.DarkBrown);
        drawList.AddRectFilled(bgStart, bgEnd, bgColor, 4f);

        // Draw border
        var borderColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.Gold * 0.6f);
        drawList.AddRect(bgStart, bgEnd, borderColor, 4f, ImDrawFlags.None, 2f);

        // Render content
        var currentY = tooltipY + TOOLTIP_PADDING;
        foreach (var line in lines)
        {
            var textPos = new Vector2(tooltipX + TOOLTIP_PADDING, currentY);
            var textColor = ImGui.ColorConvertFloat4ToU32(line.Color);

            if (line.IsBold)
                drawList.AddText(ImGui.GetFont(), line.FontSize, textPos, textColor, line.Text);
            else
                drawList.AddText(ImGui.GetFont(), line.FontSize, textPos, textColor, line.Text);

            currentY += line.Height + line.SpacingAfter;
        }
    }

    /// <summary>
    ///     Build formatted lines for tooltip content
    /// </summary>
    private static List<TooltipLine> BuildTooltipLines(BlessingTooltipData data)
    {
        var lines = new List<TooltipLine>();

        // Title (blessing name) - Gold, larger font
        lines.Add(new TooltipLine
        {
            Text = data.Name,
            Color = ColorPalette.Gold,
            FontSize = 18f,
            IsBold = true,
            SpacingAfter = SECTION_SPACING
        });

        // Category and Tier
        var categoryText = $"{data.Category} | Tier {data.Tier}";
        if (data.Kind == BlessingKind.Religion)
            categoryText += " (Religion)";

        lines.Add(new TooltipLine
        {
            Text = categoryText,
            Color = ColorPalette.Grey,
            FontSize = 12f,
            SpacingAfter = SECTION_SPACING
        });

        // Description (wrap if too long)
        if (!string.IsNullOrEmpty(data.Description))
        {
            var wrappedLines = WrapText(data.Description, TOOLTIP_MAX_WIDTH - TOOLTIP_PADDING * 2, 14f);
            foreach (var wrappedLine in wrappedLines)
                lines.Add(new TooltipLine
                {
                    Text = wrappedLine,
                    Color = ColorPalette.White,
                    FontSize = 14f,
                    SpacingAfter = LINE_SPACING
                });

            // Add section spacing after last description line
            if (lines.Count > 0)
                lines[^1].SpacingAfter = SECTION_SPACING;
        }


        // Special effects (wrap if too long)
        if (data.SpecialEffectDescriptions.Count > 0)
        {
            foreach (var effect in data.SpecialEffectDescriptions)
            {
                var wrappedEffects = WrapText("- " + effect, TOOLTIP_MAX_WIDTH - TOOLTIP_PADDING * 2, 13f);
                foreach (var wrappedLine in wrappedEffects)
                    lines.Add(new TooltipLine
                    {
                        Text = wrappedLine,
                        Color = ColorPalette.White,
                        FontSize = 13f,
                        SpacingAfter = LINE_SPACING
                    });
            }

            // Add spacing after effects section
            if (lines.Count > 0)
                lines[lines.Count - 1].SpacingAfter = SECTION_SPACING;
        }

        // Requirements section
        var hasRequirements = false;

        // Rank requirement
        if (!string.IsNullOrEmpty(data.RequiredFavorRank))
        {
            // Green if unlocked, white if can unlock, red if locked
            var rankColor = data.IsUnlocked ? ColorPalette.Green :
                data.CanUnlock ? ColorPalette.White : ColorPalette.Red;
            lines.Add(new TooltipLine
            {
                Text = $"Requires: {data.RequiredFavorRank} (Favor)",
                Color = rankColor,
                FontSize = 13f,
                SpacingAfter = LINE_SPACING
            });
            hasRequirements = true;
        }
        else if (!string.IsNullOrEmpty(data.RequiredPrestigeRank))
        {
            // Green if unlocked, white if can unlock, red if locked
            var rankColor = data.IsUnlocked ? ColorPalette.Green :
                data.CanUnlock ? ColorPalette.White : ColorPalette.Red;
            lines.Add(new TooltipLine
            {
                Text = $"Requires: {data.RequiredPrestigeRank} (Prestige)",
                Color = rankColor,
                FontSize = 13f,
                SpacingAfter = LINE_SPACING
            });
            hasRequirements = true;
        }

        // Prerequisites
        if (data.PrerequisiteNames.Count > 0)
        {
            foreach (var prereq in data.PrerequisiteNames)
            {
                // Green if unlocked, white if can unlock, red if locked
                var prereqColor = data.IsUnlocked ? ColorPalette.Green :
                    data.CanUnlock ? ColorPalette.White : ColorPalette.Red;
                lines.Add(new TooltipLine
                {
                    Text = $"Requires: {prereq}",
                    Color = prereqColor,
                    FontSize = 13f,
                    SpacingAfter = LINE_SPACING
                });
            }

            hasRequirements = true;
        }

        // Add spacing after requirements
        if (hasRequirements && lines.Count > 0)
            lines[lines.Count - 1].SpacingAfter = SECTION_SPACING;

        // Unlock status
        if (data.IsUnlocked)
        {
            lines.Add(new TooltipLine
            {
                Text = "[UNLOCKED]",
                Color = ColorPalette.Green,
                FontSize = 14f,
                IsBold = true,
                SpacingAfter = 0
            });
        }
        else if (data.CanUnlock)
        {
            lines.Add(new TooltipLine
            {
                Text = "Click to unlock",
                Color = ColorPalette.Green,
                FontSize = 13f,
                SpacingAfter = 0
            });
        }
        else
        {
            // Show lock reason if available, otherwise show generic locked message
            var lockMessage = !string.IsNullOrEmpty(data.UnlockBlockReason)
                ? data.UnlockBlockReason
                : "LOCKED";

            lines.Add(new TooltipLine
            {
                Text = lockMessage,
                Color = ColorPalette.Red,
                FontSize = 13f,
                SpacingAfter = 0
            });
        }

        return lines;
    }

    /// <summary>
    ///     Calculate total height needed for tooltip content
    /// </summary>
    private static float CalculateTooltipHeight(List<TooltipLine> lines)
    {
        var totalHeight = 0f;
        foreach (var line in lines) totalHeight += line.Height + line.SpacingAfter;

        return totalHeight;
    }

    /// <summary>
    ///     Wrap text to fit within max width
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
    ///     Represents a single line in a tooltip
    /// </summary>
    private class TooltipLine
    {
        public string Text { get; set; } = string.Empty;
        public Vector4 Color { get; set; } = new(1, 1, 1, 1);
        public float FontSize { get; set; } = 14f;
        public bool IsBold { get; set; }
        public float SpacingAfter { get; set; }

        public float Height => FontSize + 4f; // Font size + small buffer
    }
}