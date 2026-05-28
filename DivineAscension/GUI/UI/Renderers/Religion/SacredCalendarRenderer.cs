using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using DivineAscension.Constants;
using DivineAscension.GUI.Events.Religion;
using DivineAscension.GUI.Models.Religion.SacredCalendar;
using DivineAscension.GUI.UI.Components.Lists;
using DivineAscension.GUI.UI.Renderers.Utilities;
using DivineAscension.GUI.UI.Utilities;
using DivineAscension.Services;
using ImGuiNET;
using static DivineAscension.GUI.UI.Utilities.FontSizes;

namespace DivineAscension.GUI.UI.Renderers.Religion;

/// <summary>
///     Ledger-chapter renderer for the Sacred Calendar (#375). Sibling page to
///     the Chronicle (II.iii) and "This Order" (II.i). Chapter strip with the
///     order name + domain glyph, a prose intro, an ornamental divider,
///     dotted-leader feast rows (name · "12th of January" · "today"/"in N days"),
///     a closing divider, and a centered closing line. Read-only.
/// </summary>
[ExcludeFromCodeCoverage]
internal static class SacredCalendarRenderer
{
    private const float DividerHeight = 18f;
    private const float DividerYPadding = 6f;
    private const float IntroBottomSpacing = 10f;
    private const float IntroLineHeight = 18f;
    private const float RowHeight = 22f;
    private const float ScrollbarWidth = 16f;
    private const float ClosingLineHeight = 24f;
    private const float ClosingLineTopSpacing = 6f;

    public static SacredCalendarRenderResult Draw(SacredCalendarViewModel vm, ImDrawListPtr drawList)
    {
        var events = new List<SacredCalendarEvent>();
        var x = vm.X;
        var y = vm.Y;
        var width = vm.Width;
        var height = vm.Height;

        if (vm.IsLoading)
        {
            DrawCentered(drawList,
                LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_INFO_LOADING),
                x, y, width, height);
            return new SacredCalendarRenderResult(events, height);
        }

        if (!vm.HasReligion)
        {
            DrawCentered(drawList,
                LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_INFO_NO_RELIGION),
                x, y, width, height);
            return new SacredCalendarRenderResult(events, height);
        }

        var contentHeight = ComputeContentHeight(vm);
        var maxScroll = MathF.Max(0f, contentHeight - height);

        var scrollY = vm.ScrollY;
        var mousePos = ImGui.GetMousePos();
        var isHover = mousePos.X >= x && mousePos.X <= x + width && mousePos.Y >= y && mousePos.Y <= y + height;
        if (isHover && maxScroll > 0f)
        {
            var wheel = ImGui.GetIO().MouseWheel;
            if (wheel != 0)
            {
                var newScrollY = Math.Clamp(scrollY - wheel * 30f, 0f, maxScroll);
                if (Math.Abs(newScrollY - scrollY) > 0.001f)
                {
                    scrollY = newScrollY;
                    events.Add(new SacredCalendarEvent.ScrollChanged(newScrollY));
                }
            }
        }

        drawList.PushClipRect(new Vector2(x, y), new Vector2(x + width, y + height), true);

        var deityDomain = DomainHelper.ParseDeityType(vm.Deity);
        var strip = ChapterStripRenderer.Draw(drawList, x, y, width, scrollY,
            LocalizationService.Instance.Get(LocalizationKeys.UI_SACREDCALENDAR_TITLE),
            rightTitle: vm.ReligionName,
            rightGlyph: deityDomain);
        var contentWidth = strip.ContentWidth;
        var currentY = strip.BodyY;

        if (!vm.HasFeasts)
        {
            drawList.PopClipRect();
            DrawCentered(drawList,
                LocalizationService.Instance.Get(LocalizationKeys.UI_SACREDCALENDAR_EMPTY),
                x, currentY, width, height - (currentY - y));
            return new SacredCalendarRenderResult(events, height);
        }

        currentY = DrawIntro(drawList, x, currentY, contentWidth);
        currentY = DrawDivider(drawList, x, currentY, contentWidth);

        foreach (var feast in vm.Feasts)
        {
            var date = FormatDate(feast.Month, feast.Day);
            var countdown = FormatCountdown(feast.DaysUntil);
            var rowLabel = $"{feast.Name}  ·  {date}";
            ChromeRenderer.DrawLeader(drawList, rowLabel, countdown, x, currentY, contentWidth);
            currentY += RowHeight;
        }

        currentY = DrawDivider(drawList, x, currentY, contentWidth);
        currentY += ClosingLineTopSpacing;
        DrawClosingLine(drawList, x, currentY, contentWidth);

        drawList.PopClipRect();

        if (maxScroll > 0f)
            Scrollbar.Draw(drawList, x + width - ScrollbarWidth, y, ScrollbarWidth, height, scrollY, maxScroll);

        return new SacredCalendarRenderResult(events, height);
    }

    private static float DrawIntro(ImDrawListPtr drawList, float x, float y, float width)
    {
        var intro = LocalizationService.Instance.Get(LocalizationKeys.UI_SACREDCALENDAR_INTRO);
        TextRenderer.DrawInfoText(drawList, intro, x, y, width, Body, ColorPalette.White);
        var introHeight = TextRenderer.MeasureWrappedHeight(intro, width, Body);
        return y + (introHeight > 0 ? introHeight : IntroLineHeight) + IntroBottomSpacing;
    }

    private static float DrawDivider(ImDrawListPtr drawList, float x, float y, float width)
    {
        var dividerY = y + DividerYPadding;
        ChromeRenderer.DrawDivider(drawList, x, dividerY, width);
        return y + DividerHeight;
    }

    private static void DrawClosingLine(ImDrawListPtr drawList, float x, float y, float width)
    {
        var text = LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_CHRONICLE_FOOTER_CLOSING);
        var size = ImGui.CalcTextSize(text);
        var textX = x + (width - size.X) / 2f;
        drawList.AddText(ImGui.GetFont(), Body, new Vector2(textX, y),
            ImGui.ColorConvertFloat4ToU32(ColorPalette.Grey), text);
    }

    private static float ComputeContentHeight(SacredCalendarViewModel vm)
    {
        var h = PaneHeaderRenderer.TotalHeight;
        if (!vm.HasFeasts) return h;
        h += IntroLineHeight + IntroBottomSpacing;
        h += DividerHeight;
        h += vm.Feasts.Count * RowHeight;
        h += DividerHeight + ClosingLineTopSpacing + ClosingLineHeight;
        return h;
    }

    private static string FormatDate(int month, int day)
    {
        if (month is < 1 or > 12 || day < 1) return string.Empty;
        var monthName = LocalizationService.Instance.Get(LocalizationKeys.CalendarMonth(month));
        return $"{Ordinal(day)} of {monthName}";
    }

    private static string FormatCountdown(int daysUntil)
    {
        if (daysUntil == 0)
            return LocalizationService.Instance.Get(LocalizationKeys.UI_SACREDCALENDAR_TODAY);
        if (daysUntil <= 0 || daysUntil == int.MaxValue) return string.Empty;
        return LocalizationService.Instance.Get(LocalizationKeys.UI_SACREDCALENDAR_INDAYS, daysUntil);
    }

    private static string Ordinal(int n)
    {
        var suffix = (n % 100) is >= 11 and <= 13
            ? "th"
            : (n % 10) switch
            {
                1 => "st",
                2 => "nd",
                3 => "rd",
                _ => "th"
            };
        return $"{n}{suffix}";
    }

    private static void DrawCentered(ImDrawListPtr drawList, string text, float x, float y, float width, float height)
    {
        var size = ImGui.CalcTextSize(text);
        var pos = new Vector2(x + (width - size.X) / 2f, y + (height - size.Y) / 2f);
        var color = ImGui.ColorConvertFloat4ToU32(ColorPalette.Grey);
        drawList.AddText(pos, color, text);
    }
}
