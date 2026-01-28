using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using DivineAscension.Constants;
using DivineAscension.GUI.UI.Utilities;
using DivineAscension.Models;
using DivineAscension.Models.Enum;
using DivineAscension.Services;
using ImGuiNET;
using static DivineAscension.GUI.UI.Utilities.FontSizes;

namespace DivineAscension.GUI.UI.Renderers.Blessing;

/// <summary>
///     Renders rich tooltips when hovering over blessing nodes
///     Displays blessing details, requirements, stats, and unlock status
/// </summary>
[ExcludeFromCodeCoverage]
internal static class TooltipRenderer
{
    private const float TOOLTIP_MAX_WIDTH = 450f;
    private const float TOOLTIP_PADDING = 16f;
    private const float LINE_SPACING = 6f;
    private const float SECTION_SPACING = 20f;
    private const float TITLE_TO_SUBTEXT = 10f;

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

        // Draw status badge in top-right corner
        if (!tooltipData.IsUnlocked && !tooltipData.CanUnlock)
        {
            // LOCKED badge (red)
            var lockText = LocalizationService.Instance.Get(LocalizationKeys.UI_BLESSING_LOCKED);
            var lockTextSize = ImGui.CalcTextSize(lockText);
            var lockPos = new Vector2(
                tooltipX + tooltipWidth - TOOLTIP_PADDING - lockTextSize.X,
                tooltipY + TOOLTIP_PADDING
            );
            var lockColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.Red);
            drawList.AddText(ImGui.GetFont(), SubsectionLabel, lockPos, lockColor, lockText);
        }
        else if (tooltipData.IsUnlocked)
        {
            // UNLOCKED badge (green)
            var unlockText = LocalizationService.Instance.Get(LocalizationKeys.UI_BLESSING_UNLOCKED);
            var unlockTextSize = ImGui.CalcTextSize(unlockText);
            var unlockPos = new Vector2(
                tooltipX + tooltipWidth - TOOLTIP_PADDING - unlockTextSize.X,
                tooltipY + TOOLTIP_PADDING
            );
            var unlockColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.Green);
            drawList.AddText(ImGui.GetFont(), SubsectionLabel, unlockPos, unlockColor, unlockText);
        }

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
            FontSize = SectionHeader,
            IsBold = true,
            SpacingAfter = TITLE_TO_SUBTEXT
        });

        // Category and Tier
        var categoryText =
            LocalizationService.Instance.Get(LocalizationKeys.UI_BLESSING_CATEGORY_TIER, data.Category, data.Tier);
        if (data.Kind == BlessingKind.Religion)
            categoryText += " " + LocalizationService.Instance.Get(LocalizationKeys.UI_BLESSING_RELIGION_LABEL);

        lines.Add(new TooltipLine
        {
            Text = categoryText,
            Color = ColorPalette.Grey,
            FontSize = Secondary,
            SpacingAfter = SECTION_SPACING
        });

        // Requirements section
        var hasRequirements = false;

        // Rank requirement
        if (!string.IsNullOrEmpty(data.RequiredFavorRank))
        {
            // Green if requirements met (unlocked or can unlock), red if not met
            var rankColor = (data.IsUnlocked || data.CanUnlock) ? ColorPalette.Green : ColorPalette.Red;
            lines.Add(new TooltipLine
            {
                Text = LocalizationService.Instance.Get(LocalizationKeys.UI_BLESSING_REQUIRES_FAVOR_RANK,
                    data.RequiredFavorRank),
                Color = rankColor,
                FontSize = Body,
                SpacingAfter = LINE_SPACING
            });
            hasRequirements = true;
        }
        else if (!string.IsNullOrEmpty(data.RequiredPrestigeRank))
        {
            // Green if requirements met (unlocked or can unlock), red if not met
            var rankColor = (data.IsUnlocked || data.CanUnlock) ? ColorPalette.Green : ColorPalette.Red;
            lines.Add(new TooltipLine
            {
                Text = LocalizationService.Instance.Get(LocalizationKeys.UI_BLESSING_REQUIRES_PRESTIGE_RANK,
                    data.RequiredPrestigeRank),
                Color = rankColor,
                FontSize = Body,
                SpacingAfter = LINE_SPACING
            });
            hasRequirements = true;
        }

        // Prerequisites
        if (data.PrerequisiteNames.Count > 0)
        {
            foreach (var prereq in data.PrerequisiteNames)
            {
                // Green if requirements met (unlocked or can unlock), red if not met
                var prereqColor = (data.IsUnlocked || data.CanUnlock) ? ColorPalette.Green : ColorPalette.Red;
                lines.Add(new TooltipLine
                {
                    Text = LocalizationService.Instance.Get(LocalizationKeys.UI_BLESSING_REQUIRES_BLESSING, prereq),
                    Color = prereqColor,
                    FontSize = Body,
                    SpacingAfter = LINE_SPACING
                });
            }

            hasRequirements = true;
        }

        // Add spacing after requirements
        if (hasRequirements && lines.Count > 0)
            lines[lines.Count - 1].SpacingAfter = SECTION_SPACING;

        // Description (wrap if too long)
        if (!string.IsNullOrEmpty(data.Description))
        {
            var wrappedLines = WrapText(data.Description, TOOLTIP_MAX_WIDTH - TOOLTIP_PADDING * 2, SubsectionLabel);
            foreach (var wrappedLine in wrappedLines)
                lines.Add(new TooltipLine
                {
                    Text = wrappedLine,
                    Color = ColorPalette.White,
                    FontSize = SubsectionLabel,
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
                var wrappedEffects = WrapText("- " + effect, TOOLTIP_MAX_WIDTH - TOOLTIP_PADDING * 2, Body);
                foreach (var wrappedLine in wrappedEffects)
                    lines.Add(new TooltipLine
                    {
                        Text = wrappedLine,
                        Color = ColorPalette.White,
                        FontSize = Body,
                        SpacingAfter = LINE_SPACING
                    });
            }

            // Add spacing after effects section
            if (lines.Count > 0)
                lines[^1].SpacingAfter = SECTION_SPACING;
        }

        // "Click to unlock" instruction (only shown when can unlock but not yet unlocked)
        if (data.CanUnlock && !data.IsUnlocked)
        {
            lines.Add(new TooltipLine
            {
                Text = LocalizationService.Instance.Get(LocalizationKeys.UI_BLESSING_CLICK_TO_UNLOCK),
                Color = ColorPalette.Green,
                FontSize = Body,
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
        public float FontSize { get; set; } = SubsectionLabel;
        public bool IsBold { get; set; }
        public float SpacingAfter { get; set; }

        public float Height => FontSize + 4f; // Font size + small buffer
    }
}