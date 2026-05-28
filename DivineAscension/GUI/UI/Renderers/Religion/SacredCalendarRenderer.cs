using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using DivineAscension.Constants;
using DivineAscension.GUI.Events.Religion;
using DivineAscension.GUI.Models.Religion.SacredCalendar;
using DivineAscension.GUI.UI.Components.Buttons;
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

            // Founder gets a trash control to the right of custom feasts only.
            // Auto Founding/Patron are never removable.
            if (vm.IsFounder && (int)feast.Kind == (int)DivineAscension.Models.Enum.FeastKind.Custom)
            {
                var btnSize = 18f;
                var btnX = x + contentWidth - btnSize;
                if (ButtonRenderer.DrawButton(drawList, "×", btnX, currentY - 2f, btnSize, btnSize,
                        isPrimary: false, enabled: true))
                {
                    events.Add(new SacredCalendarEvent.RemoveRequested(feast.FeastId, feast.Name));
                }
            }

            currentY += RowHeight;
        }

        // Founder controls under the list: Add button / Add form / cap notice
        if (vm.IsFounder)
        {
            currentY += 6f;
            currentY = DrawFounderControls(vm, drawList, events, x, currentY, contentWidth);
        }

        currentY = DrawDivider(drawList, x, currentY, contentWidth);
        currentY += ClosingLineTopSpacing;
        DrawClosingLine(drawList, x, currentY, contentWidth);

        // Error banner + Remove confirm popup, drawn outside the clip so they
        // overlay the chapter cleanly.
        drawList.PopClipRect();

        if (maxScroll > 0f)
            Scrollbar.Draw(drawList, x + width - ScrollbarWidth, y, ScrollbarWidth, height, scrollY, maxScroll);

        if (vm.LastErrorMessage != null)
            DrawErrorBanner(vm, drawList, events, x, y, width);

        if (vm.RemoveConfirmFeastId.HasValue)
            DrawRemoveConfirm(vm, drawList, events, x, y, width, height);

        return new SacredCalendarRenderResult(events, height);
    }

    private static float DrawFounderControls(SacredCalendarViewModel vm, ImDrawListPtr drawList,
        List<SacredCalendarEvent> events, float x, float y, float width)
    {
        var loc = LocalizationService.Instance;
        var currentY = y;

        if (!vm.AddDialogOpen)
        {
            if (vm.AtCap)
            {
                TextRenderer.DrawInfoText(drawList, loc.Get(LocalizationKeys.UI_FEASTDAY_CAP),
                    x, currentY, width, Secondary, ColorPalette.Grey);
                return currentY + RowHeight;
            }
            if (vm.CustomCount >= vm.UnlockedSlots)
            {
                TextRenderer.DrawInfoText(drawList, loc.Get(LocalizationKeys.UI_FEASTDAY_LOCKED),
                    x, currentY, width, Secondary, ColorPalette.Grey);
                return currentY + RowHeight;
            }
            var addLabel = loc.Get(LocalizationKeys.UI_FEASTDAY_ADD);
            if (ButtonRenderer.DrawButton(drawList, addLabel, x, currentY, 160f, 22f,
                    isPrimary: true, enabled: true))
            {
                events.Add(new SacredCalendarEvent.AddDialogOpened());
            }
            return currentY + RowHeight + 4f;
        }

        // Inline editor: name field, month spinner, day spinner, Add/Cancel
        var fieldHeight = 22f;
        ImGui.SetCursorScreenPos(new Vector2(x, currentY));
        ImGui.PushItemWidth(width - 8f);
        var name = vm.AddName ?? string.Empty;
        if (ImGui.InputTextWithHint("##feastname", loc.Get(LocalizationKeys.UI_FEASTDAY_NAME_PLACEHOLDER),
                ref name, 32))
        {
            events.Add(new SacredCalendarEvent.AddNameChanged(name));
        }
        ImGui.PopItemWidth();
        currentY += fieldHeight + 4f;

        var monthMax = Math.Max(1, vm.MonthsPerYear);
        var dayMax = Math.Max(1, vm.DaysPerMonth);

        ImGui.SetCursorScreenPos(new Vector2(x, currentY));
        ImGui.PushItemWidth((width - 16f) / 2f);
        var month = Math.Clamp(vm.AddMonth, 1, monthMax);
        if (ImGui.SliderInt($"{loc.Get(LocalizationKeys.UI_FEASTDAY_MONTH)}##feastmonth",
                ref month, 1, monthMax))
        {
            events.Add(new SacredCalendarEvent.AddMonthChanged(month));
        }
        ImGui.SameLine(0f, 8f);
        var day = Math.Clamp(vm.AddDay, 1, dayMax);
        if (ImGui.SliderInt($"{loc.Get(LocalizationKeys.UI_FEASTDAY_DAY)}##feastday",
                ref day, 1, dayMax))
        {
            events.Add(new SacredCalendarEvent.AddDayChanged(day));
        }
        ImGui.PopItemWidth();
        currentY += fieldHeight + 4f;

        var canSubmit = !string.IsNullOrWhiteSpace(name);
        if (ButtonRenderer.DrawButton(drawList, loc.Get(LocalizationKeys.UI_FEASTDAY_SUBMIT),
                x, currentY, 80f, 22f, isPrimary: true, enabled: canSubmit))
        {
            events.Add(new SacredCalendarEvent.AddSubmitted(name.Trim(), month, day));
        }
        if (ButtonRenderer.DrawButton(drawList, loc.Get(LocalizationKeys.UI_FEASTDAY_CANCEL),
                x + 88f, currentY, 80f, 22f, isPrimary: false, enabled: true))
        {
            events.Add(new SacredCalendarEvent.AddDialogCancel());
        }
        return currentY + fieldHeight + 6f;
    }

    private static void DrawErrorBanner(SacredCalendarViewModel vm, ImDrawListPtr drawList,
        List<SacredCalendarEvent> events, float x, float y, float width)
    {
        var msg = vm.LastErrorMessage ?? string.Empty;
        DivineAscension.GUI.UI.Components.Banners.ErrorBannerRenderer.Draw(drawList,
            x, y, width, msg, out _, out var dismissClicked, showRetry: false);
        if (dismissClicked) events.Add(new SacredCalendarEvent.DismissError());
    }

    private static void DrawRemoveConfirm(SacredCalendarViewModel vm, ImDrawListPtr drawList,
        List<SacredCalendarEvent> events, float x, float y, float width, float height)
    {
        var loc = LocalizationService.Instance;
        var prompt = loc.Get(LocalizationKeys.UI_FEASTDAY_REMOVE_CONFIRM,
            vm.RemoveConfirmFeastName ?? string.Empty);

        var boxW = Math.Min(360f, width - 40f);
        var boxH = 110f;
        var boxX = x + (width - boxW) / 2f;
        var boxY = y + (height - boxH) / 2f;

        drawList.AddRectFilled(new Vector2(boxX, boxY), new Vector2(boxX + boxW, boxY + boxH),
            ImGui.ColorConvertFloat4ToU32(ColorPalette.TableBackground), 4f);
        drawList.AddRect(new Vector2(boxX, boxY), new Vector2(boxX + boxW, boxY + boxH),
            ImGui.ColorConvertFloat4ToU32(ColorPalette.Gold * 0.6f), 4f);

        TextRenderer.DrawInfoText(drawList, prompt,
            boxX + 12f, boxY + 12f, boxW - 24f, Body, ColorPalette.White);

        var btnY = boxY + boxH - 32f;
        if (ButtonRenderer.DrawButton(drawList, loc.Get(LocalizationKeys.UI_FEASTDAY_SUBMIT),
                boxX + boxW - 176f, btnY, 80f, 22f, isPrimary: true, enabled: true))
        {
            events.Add(new SacredCalendarEvent.RemoveConfirmed(vm.RemoveConfirmFeastId!.Value));
        }
        if (ButtonRenderer.DrawButton(drawList, loc.Get(LocalizationKeys.UI_FEASTDAY_CANCEL),
                boxX + boxW - 88f, btnY, 80f, 22f, isPrimary: false, enabled: true))
        {
            events.Add(new SacredCalendarEvent.RemoveCancel());
        }
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
