using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using DivineAscension.GUI.UI.Utilities;
using DivineAscension.Network;
using ImGuiNET;
using static DivineAscension.GUI.UI.Utilities.FontSizes;

namespace DivineAscension.GUI.UI.Renderers.Civilization;

/// <summary>
///     Renders tooltips for milestone items in the Milestones sub-tab.
///     Displays description, trigger requirement, and rewards on hover.
/// </summary>
[ExcludeFromCodeCoverage]
internal static class MilestoneTooltipRenderer
{
    private const float TOOLTIP_MAX_WIDTH = 320f;
    private const float TOOLTIP_PADDING = 12f;
    private const float LINE_SPACING = 4f;
    private const float SECTION_SPACING = 8f;

    /// <summary>
    ///     Draw a tooltip for a milestone when hovering over a milestone item
    /// </summary>
    public static void Draw(
        MilestoneProgressDto milestone,
        float mouseX,
        float mouseY,
        float windowWidth,
        float windowHeight)
    {
        var drawList = ImGui.GetForegroundDrawList();

        // Build tooltip content
        var lines = BuildTooltipLines(milestone);
        var contentHeight = CalculateHeight(lines);
        var tooltipHeight = contentHeight + TOOLTIP_PADDING * 2;

        // Position tooltip (with edge detection)
        var (tooltipX, tooltipY) = CalculatePosition(
            mouseX, mouseY, windowWidth, windowHeight,
            TOOLTIP_MAX_WIDTH, tooltipHeight);

        // Draw background and border
        DrawBackground(drawList, tooltipX, tooltipY, TOOLTIP_MAX_WIDTH, tooltipHeight);

        // Draw text content
        DrawContent(drawList, lines, tooltipX, tooltipY);
    }

    private static List<TooltipLine> BuildTooltipLines(MilestoneProgressDto milestone)
    {
        var lines = new List<TooltipLine>();
        var goldColor = ColorPalette.Gold;
        var greenColor = new Vector4(0.3f, 0.9f, 0.3f, 1f);
        var contentWidth = TOOLTIP_MAX_WIDTH - TOOLTIP_PADDING * 2;

        // Title in gold
        lines.Add(new TooltipLine(milestone.MilestoneName ?? "Unknown Milestone", goldColor, SectionHeader, SECTION_SPACING));

        // Type badge
        if (!string.IsNullOrEmpty(milestone.MilestoneType))
        {
            var typeText = milestone.MilestoneType == "Major" ? "Major Milestone" : "Minor Milestone";
            lines.Add(new TooltipLine(typeText, ColorPalette.Grey, Secondary, SECTION_SPACING));
        }

        // Description (word-wrapped)
        if (!string.IsNullOrEmpty(milestone.Description))
        {
            var wrappedDesc = WrapText(milestone.Description, contentWidth, Body);
            foreach (var line in wrappedDesc)
                lines.Add(new TooltipLine(line, ColorPalette.White, Body, LINE_SPACING));

            // Add extra spacing after description
            if (wrappedDesc.Count > 0)
                lines[^1] = lines[^1] with { SpacingAfter = SECTION_SPACING };
        }

        // Requirement
        if (!string.IsNullOrEmpty(milestone.TriggerType))
        {
            lines.Add(new TooltipLine("Requirement:", ColorPalette.Grey, SubsectionLabel, LINE_SPACING));
            var triggerDesc = FormatTriggerDescription(milestone.TriggerType, milestone.TriggerThreshold);
            var wrappedTrigger = WrapText(triggerDesc, contentWidth, Body);
            foreach (var line in wrappedTrigger)
                lines.Add(new TooltipLine(line, ColorPalette.White, Body, LINE_SPACING));

            if (wrappedTrigger.Count > 0)
                lines[^1] = lines[^1] with { SpacingAfter = SECTION_SPACING };
        }

        // Rewards section
        var hasRewards = milestone.PrestigePayout > 0 || milestone.RankReward > 0 ||
                         !string.IsNullOrEmpty(milestone.PermanentBenefitDescription) ||
                         !string.IsNullOrEmpty(milestone.TemporaryBenefitDescription);

        if (hasRewards)
        {
            lines.Add(new TooltipLine("Rewards:", ColorPalette.Grey, SubsectionLabel, LINE_SPACING));

            if (milestone.PrestigePayout > 0)
                AddWrappedLines(lines, $"  +{milestone.PrestigePayout} Prestige", greenColor, contentWidth);

            if (milestone.RankReward > 0)
                AddWrappedLines(lines, $"  +{milestone.RankReward} Civilization Rank", greenColor, contentWidth);

            if (!string.IsNullOrEmpty(milestone.PermanentBenefitDescription))
                AddWrappedLines(lines, $"  {milestone.PermanentBenefitDescription} (permanent)", greenColor,
                    contentWidth);

            if (!string.IsNullOrEmpty(milestone.TemporaryBenefitDescription))
                AddWrappedLines(lines, $"  {milestone.TemporaryBenefitDescription}", greenColor, contentWidth);
        }

        return lines;
    }

    private static void AddWrappedLines(List<TooltipLine> lines, string text, Vector4 color, float maxWidth)
    {
        var wrapped = WrapText(text, maxWidth, Body);
        foreach (var line in wrapped)
            lines.Add(new TooltipLine(line, color, Body, LINE_SPACING));
    }

    private static string FormatTriggerDescription(string triggerType, int threshold)
    {
        return triggerType switch
        {
            "ReligionCount" => $"Have {threshold} religion{(threshold != 1 ? "s" : "")} in the civilization",
            "DomainCount" => $"Have {threshold} unique deity domain{(threshold != 1 ? "s" : "")} represented",
            "HolySiteCount" => $"Establish {threshold} holy site{(threshold != 1 ? "s" : "")} across all religions",
            "RitualCount" => $"Complete {threshold} ritual{(threshold != 1 ? "s" : "")} across all religions",
            "MemberCount" => $"Have {threshold} total member{(threshold != 1 ? "s" : "")} across all religions",
            "WarKillCount" => $"Achieve {threshold} PvP kill{(threshold != 1 ? "s" : "")} during active wars",
            "HolySiteTier" => $"Upgrade a holy site to tier {threshold}",
            "DiplomaticRelationship" =>
                $"Form {threshold} diplomatic relationship{(threshold != 1 ? "s" : "")} with other civilizations",
            "AllMajorMilestones" => "Complete all other major milestones",
            _ => $"Reach {threshold}"
        };
    }

    private static float CalculateHeight(List<TooltipLine> lines)
    {
        var totalHeight = 0f;
        foreach (var line in lines)
            totalHeight += line.Height + line.SpacingAfter;

        return totalHeight;
    }

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

        // Check right edge
        if (tooltipX - windowPos.X + tooltipWidth > windowWidth)
            tooltipX = mouseX - tooltipWidth - offsetX;

        // Check bottom edge
        if (tooltipY - windowPos.Y + tooltipHeight > windowHeight)
            tooltipY = mouseY - tooltipHeight - offsetY;

        // Ensure doesn't go off left edge
        if (tooltipX < windowPos.X)
            tooltipX = windowPos.X + 4f;

        // Ensure doesn't go off top edge
        if (tooltipY < windowPos.Y)
            tooltipY = windowPos.Y + 4f;

        return (tooltipX, tooltipY);
    }

    private static void DrawBackground(ImDrawListPtr drawList, float x, float y, float width, float height)
    {
        var bgStart = new Vector2(x, y);
        var bgEnd = new Vector2(x + width, y + height);

        var bgColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.DarkBrown);
        drawList.AddRectFilled(bgStart, bgEnd, bgColor, 4f);

        var borderColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.Gold * 0.6f);
        drawList.AddRect(bgStart, bgEnd, borderColor, 4f, ImDrawFlags.None, 2f);
    }

    private static void DrawContent(ImDrawListPtr drawList, List<TooltipLine> lines, float tooltipX, float tooltipY)
    {
        var currentY = tooltipY + TOOLTIP_PADDING;
        foreach (var line in lines)
        {
            if (string.IsNullOrEmpty(line.Text))
            {
                currentY += line.Height + line.SpacingAfter;
                continue;
            }

            var textPos = new Vector2(tooltipX + TOOLTIP_PADDING, currentY);
            var textColor = ImGui.ColorConvertFloat4ToU32(line.Color);

            drawList.AddText(ImGui.GetFont(), line.FontSize, textPos, textColor, line.Text);

            currentY += line.Height + line.SpacingAfter;
        }
    }

    private static List<string> WrapText(string? text, float maxWidth, float fontSize)
    {
        var result = new List<string>();
        if (string.IsNullOrEmpty(text)) return result;

        var words = text.Split(' ');
        var currentLine = "";

        foreach (var word in words)
        {
            if (string.IsNullOrEmpty(word)) continue;

            var testLine = string.IsNullOrEmpty(currentLine) ? word : currentLine + " " + word;
            var testSize = ImGui.CalcTextSize(testLine);
            var scaledWidth = testSize.X * (fontSize / ImGui.GetFontSize());

            if (scaledWidth > maxWidth && !string.IsNullOrEmpty(currentLine))
            {
                result.Add(currentLine);
                currentLine = word;
            }
            else
            {
                currentLine = testLine;
            }
        }

        if (!string.IsNullOrEmpty(currentLine))
            result.Add(currentLine);

        return result;
    }

    private record TooltipLine(string Text, Vector4 Color, float FontSize, float SpacingAfter)
    {
        public float Height => FontSize + 4f;
    }
}
