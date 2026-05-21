using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Text.RegularExpressions;
using DivineAscension.Constants;
using DivineAscension.GUI.Models.Religion.Activity;
using DivineAscension.GUI.UI.Renderers.Utilities;
using DivineAscension.GUI.UI.Utilities;
using DivineAscension.Network;
using DivineAscension.Services;
using ImGuiNET;
using static DivineAscension.GUI.UI.Utilities.FontSizes;

namespace DivineAscension.GUI.UI.Renderers.Religion.Activity;

/// <summary>
/// Paints the day-grouped feed: a sub-heading per real-world calendar day
/// ("Today" / "Yesterday" / formatted date) followed by one line per entry —
/// domain glyph, "PlayerName actionVerb.", right-aligned "+N" favor.
/// </summary>
internal static class ReligionActivityFeedRenderer
{
    public const float DayHeadingHeight = 24f;
    public const float DayHeadingTopSpacing = 4f;
    public const float EntryRowHeight = 22f;
    public const float EntryLeftPadding = 16f;
    public const float GlyphSize = 14f;
    public const float GlyphToTextGap = 8f;
    public const float DayBottomSpacing = 8f;

    public static float Draw(
        ReligionActivityViewModel viewModel,
        ImDrawListPtr drawList,
        float x,
        float y,
        float width)
    {
        if (viewModel.Entries.Count == 0) return y;

        var groups = viewModel.Entries
            .GroupBy(e => new DateTime(e.TimestampTicks, DateTimeKind.Utc).ToLocalTime().Date)
            .OrderByDescending(g => g.Key);

        var today = DateTime.Now.Date;
        var yesterday = today.AddDays(-1);
        var currentY = y;

        foreach (var group in groups)
        {
            currentY += DayHeadingTopSpacing;
            var heading = FormatDayHeading(group.Key, today, yesterday);
            drawList.AddText(ImGui.GetFont(), SubsectionLabel,
                new Vector2(x, currentY),
                ImGui.ColorConvertFloat4ToU32(ColorPalette.Gold),
                heading);
            currentY += DayHeadingHeight;

            foreach (var entry in group)
            {
                DrawEntry(entry, drawList, x + EntryLeftPadding, currentY, width - EntryLeftPadding);
                currentY += EntryRowHeight;
            }

            currentY += DayBottomSpacing;
        }

        return currentY;
    }

    public static float MeasureHeight(ReligionActivityViewModel viewModel)
    {
        if (viewModel.Entries.Count == 0) return 0f;

        var dayCount = viewModel.Entries
            .Select(e => new DateTime(e.TimestampTicks, DateTimeKind.Utc).ToLocalTime().Date)
            .Distinct()
            .Count();

        return dayCount * (DayHeadingTopSpacing + DayHeadingHeight + DayBottomSpacing)
               + viewModel.Entries.Count * EntryRowHeight;
    }

    private static string FormatDayHeading(DateTime day, DateTime today, DateTime yesterday)
    {
        if (day == today)
            return LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_ACTIVITY_DAY_TODAY);
        if (day == yesterday)
            return LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_ACTIVITY_DAY_YESTERDAY);
        return day.ToString("dddd, d MMMM yyyy", CultureInfo.CurrentCulture);
    }

    private static void DrawEntry(
        ActivityLogResponsePacket.ActivityEntry entry,
        ImDrawListPtr drawList,
        float x, float y, float width)
    {
        var domain = DomainHelper.ParseDeityType(entry.DeityDomain);

        // Domain glyph — small ink mark to the left of the row.
        var glyphCy = y + EntryRowHeight / 2f;
        var glyphMin = new Vector2(x, glyphCy - GlyphSize / 2f);
        var glyphMax = new Vector2(x + GlyphSize, glyphCy + GlyphSize / 2f);
        DomainGlyphRenderer.Draw(drawList, domain, glyphMin, glyphMax, ColorPalette.White);

        var textX = x + GlyphSize + GlyphToTextGap;
        var textY = y + 2f;

        var actionPhrase = FormatActionPhrase(entry.ActionType);
        var line = LocalizationService.Instance.Get(
            LocalizationKeys.UI_RELIGION_ACTIVITY_ENTRY_LINE,
            entry.PlayerName, actionPhrase);

        // Right-aligned favor amount.
        var favorText = $"+{entry.FavorAmount}";
        var favorSize = ImGui.CalcTextSize(favorText);
        var favorX = x + width - favorSize.X;
        drawList.AddText(ImGui.GetFont(), Body, new Vector2(favorX, textY),
            ImGui.ColorConvertFloat4ToU32(ColorPalette.Vermilion), favorText);

        // Body line — clip to the gap before the favor column.
        drawList.PushClipRect(new Vector2(textX, textY - 2f),
            new Vector2(favorX - 8f, textY + EntryRowHeight), true);
        drawList.AddText(ImGui.GetFont(), Body, new Vector2(textX, textY),
            ImGui.ColorConvertFloat4ToU32(ColorPalette.White), line);
        drawList.PopClipRect();
    }

    private static string FormatActionPhrase(string actionType)
    {
        var cleaned = Regex.Replace(actionType, @"[-_]", " ");
        cleaned = Regex.Replace(cleaned, @"\s+(adult|male|female|baby|young)(?=\s|$)", "",
            RegexOptions.IgnoreCase);
        cleaned = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(cleaned.ToLower());

        var words = cleaned.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length > 6)
            cleaned = string.Join(" ", words.Take(6)) + "...";
        return cleaned;
    }
}
