using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using DivineAscension.Constants;
using DivineAscension.GUI.Events.Religion;
using DivineAscension.GUI.Models.Religion.SacredCalendar;
using DivineAscension.GUI.UI.Components.Buttons;
using DivineAscension.GUI.UI.Components.Inputs;
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

        const float removeBtnSize = 18f;
        const float removeBtnGap = 8f;
        foreach (var feast in vm.Feasts)
        {
            var isRemovable = vm.IsFounder &&
                              (int)feast.Kind == (int)DivineAscension.Models.Enum.FeastKind.Custom;
            // Reserve room for the trash button so the right-aligned countdown
            // doesn't slide under it.
            var leaderWidth = isRemovable ? contentWidth - removeBtnSize - removeBtnGap : contentWidth;

            var date = FormatDate(feast.Month, feast.Day);
            var countdown = FormatCountdown(feast.DaysUntil);
            var rowLabel = $"{feast.Name}  ·  {date}";
            ChromeRenderer.DrawLeader(drawList, rowLabel, countdown, x, currentY, leaderWidth);

            if (isRemovable)
            {
                var btnX = x + contentWidth - removeBtnSize;
                if (ButtonRenderer.DrawButton(drawList, "x", btnX, currentY - 2f,
                        removeBtnSize, removeBtnSize, isPrimary: false, enabled: true))
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

        if (vm.AddDialogOpen)
            DrawAddDialog(vm, drawList, events);

        return new SacredCalendarRenderResult(events, height);
    }

    /// <summary>
    ///     Inline founder controls: the call-to-action button (or the cap /
    ///     prestige-locked notice). The actual Add editor is a centered modal
    ///     drawn later via <see cref="DrawAddDialog"/>, matching the
    ///     <c>ReligionRosterRenderer</c> invite-dialog convention.
    /// </summary>
    private static float DrawFounderControls(SacredCalendarViewModel vm, ImDrawListPtr drawList,
        List<SacredCalendarEvent> events, float x, float y, float width)
    {
        var loc = LocalizationService.Instance;

        if (vm.AtCap)
        {
            TextRenderer.DrawInfoText(drawList, loc.Get(LocalizationKeys.UI_FEASTDAY_CAP),
                x, y, width, Secondary, ColorPalette.Grey);
            return y + RowHeight;
        }
        if (vm.CustomCount >= vm.UnlockedSlots)
        {
            TextRenderer.DrawInfoText(drawList, loc.Get(LocalizationKeys.UI_FEASTDAY_LOCKED),
                x, y, width, Secondary, ColorPalette.Grey);
            return y + RowHeight;
        }

        var addLabel = loc.Get(LocalizationKeys.UI_FEASTDAY_ADD);
        if (ButtonRenderer.DrawButton(drawList, addLabel, x, y, 160f, 22f,
                isPrimary: true, enabled: true))
        {
            events.Add(new SacredCalendarEvent.AddDialogOpened());
        }
        return y + RowHeight + 4f;
    }

    /// <summary>
    ///     Centered parchment modal for adding a custom feast (#422). Mirrors
    ///     <c>ReligionRosterRenderer.DrawInviteDialog</c>: dim backdrop,
    ///     ChromeRenderer-styled box, gold-rubric title, divider, body fields,
    ///     right-aligned Cancel + primary Add buttons. Escape cancels.
    /// </summary>
    private static void DrawAddDialog(SacredCalendarViewModel vm, ImDrawListPtr drawList,
        List<SacredCalendarEvent> events)
    {
        ModalInputGuard.MarkOpen();
        var loc = LocalizationService.Instance;

        var winPos = ImGui.GetWindowPos();
        var winSize = ImGui.GetWindowSize();

        drawList.AddRectFilled(winPos, new Vector2(winPos.X + winSize.X, winPos.Y + winSize.Y),
            ImGui.ColorConvertFloat4ToU32(ColorPalette.BlackOverlay));

        const float dialogWidth = 460f;
        const float dialogHeight = 260f;
        var dlgX = winPos.X + (winSize.X - dialogWidth) / 2f;
        var dlgY = winPos.Y + (winSize.Y - dialogHeight) / 2f;

        drawList.AddRectFilled(new Vector2(dlgX, dlgY), new Vector2(dlgX + dialogWidth, dlgY + dialogHeight),
            ImGui.ColorConvertFloat4ToU32(ColorPalette.Background), 6f);
        drawList.AddRect(new Vector2(dlgX, dlgY), new Vector2(dlgX + dialogWidth, dlgY + dialogHeight),
            ImGui.ColorConvertFloat4ToU32(ColorPalette.BorderColor), 6f, ImDrawFlags.None, 1.5f);

        const float padding = 18f;
        var bodyWidth = dialogWidth - padding * 2f;
        var curX = dlgX + padding;
        var curY = dlgY + padding;

        TextRenderer.DrawLabel(drawList,
            loc.Get(LocalizationKeys.UI_FEASTDAY_ADD_TITLE),
            curX, curY, PageTitle, ColorPalette.Gold);
        curY += PageTitle + 6f;
        ChromeRenderer.DrawDivider(drawList, curX, curY, bodyWidth);
        curY += 16f;

        // Name field
        TextRenderer.DrawInfoText(drawList,
            loc.Get(LocalizationKeys.UI_FEASTDAY_NAME_PLACEHOLDER),
            curX, curY, bodyWidth, Body, ColorPalette.White);
        curY += 22f;
        var newName = TextInput.Draw(drawList, "##feastname", vm.AddName ?? string.Empty,
            curX, curY, bodyWidth, 32f,
            loc.Get(LocalizationKeys.UI_FEASTDAY_NAME_PLACEHOLDER));
        if (newName != (vm.AddName ?? string.Empty))
            events.Add(new SacredCalendarEvent.AddNameChanged(newName));
        curY += 40f;

        // Month + Day steppers, side by side
        var monthMax = Math.Max(1, vm.MonthsPerYear);
        var dayMax = Math.Max(1, vm.DaysPerMonth);
        var month = Math.Clamp(vm.AddMonth, 1, monthMax);
        var day = Math.Clamp(vm.AddDay, 1, dayMax);

        var halfWidth = (bodyWidth - 16f) / 2f;
        var monthLabel = loc.Get(LocalizationKeys.UI_FEASTDAY_MONTH);
        var dayLabel = loc.Get(LocalizationKeys.UI_FEASTDAY_DAY);
        var monthDisplay = month is >= 1 and <= 12
            ? loc.Get(LocalizationKeys.CalendarMonth(month))
            : month.ToString();
        var nextMonth = DrawStepper(drawList, events, curX, curY, halfWidth,
            monthLabel, month, monthDisplay, 1, monthMax);
        if (nextMonth != month) events.Add(new SacredCalendarEvent.AddMonthChanged(nextMonth));

        var nextDay = DrawStepper(drawList, events, curX + halfWidth + 16f, curY, halfWidth,
            dayLabel, day, day.ToString(), 1, dayMax);
        if (nextDay != day) events.Add(new SacredCalendarEvent.AddDayChanged(nextDay));

        // Footer: Cancel + Add, right-aligned, same metrics as InviteDialog.
        const float btnWidth = 120f;
        const float btnHeight = 32f;
        const float btnGap = 10f;
        var btnY = dlgY + dialogHeight - padding - btnHeight;
        var addX = dlgX + dialogWidth - padding - btnWidth;
        var cancelX = addX - btnWidth - btnGap;

        if (ButtonRenderer.DrawButton(drawList,
                loc.Get(LocalizationKeys.UI_FEASTDAY_CANCEL),
                cancelX, btnY, btnWidth, btnHeight))
            events.Add(new SacredCalendarEvent.AddDialogCancel());

        var canAdd = !string.IsNullOrWhiteSpace(vm.AddName);
        if (ButtonRenderer.DrawButton(drawList,
                loc.Get(LocalizationKeys.UI_FEASTDAY_ADD_ACTION),
                addX, btnY, btnWidth, btnHeight, isPrimary: true, enabled: canAdd)
            && canAdd)
            events.Add(new SacredCalendarEvent.AddSubmitted((vm.AddName ?? string.Empty).Trim(), month, day));

        if (ImGui.IsKeyPressed(ImGuiKey.Escape))
            events.Add(new SacredCalendarEvent.AddDialogCancel());
    }

    /// <summary>
    ///     Stepper widget: <c>label  [−] value [+]</c> inside a parchment-toned
    ///     row. Returns the desired value after this frame's click; caller
    ///     diffs against the prior value to decide whether to emit an event.
    /// </summary>
    private static int DrawStepper(ImDrawListPtr drawList, List<SacredCalendarEvent> events,
        float x, float y, float width, string label, int value, string displayText, int min, int max)
    {
        const float rowHeight = 30f;
        const float btnW = 28f;
        const float labelW = 60f;

        TextRenderer.DrawLabel(drawList, label, x, y + 6f, Body, ColorPalette.Grey);

        var fieldX = x + labelW;
        var fieldW = width - labelW - btnW * 2f - 8f;
        drawList.AddRectFilled(new Vector2(fieldX, y),
            new Vector2(fieldX + fieldW, y + rowHeight),
            ImGui.ColorConvertFloat4ToU32(ColorPalette.DarkBrown * 0.7f), 4f);
        drawList.AddRect(new Vector2(fieldX, y),
            new Vector2(fieldX + fieldW, y + rowHeight),
            ImGui.ColorConvertFloat4ToU32(ColorPalette.BorderColor), 4f, ImDrawFlags.None, 1f);
        var textSize = ImGui.CalcTextSize(displayText);
        drawList.AddText(
            new Vector2(fieldX + (fieldW - textSize.X) / 2f, y + (rowHeight - textSize.Y) / 2f),
            ImGui.ColorConvertFloat4ToU32(ColorPalette.LightText), displayText);

        var next = value;
        var decX = fieldX + fieldW + 4f;
        var incX = decX + btnW + 4f;
        // ASCII hyphen — the bundled font lacks U+2212 (minus sign) and U+2013
        // (en-dash), which both rendered as a "?" tofu glyph. Plain '-' is in
        // every fallback so it draws correctly.
        if (ButtonRenderer.DrawButton(drawList, "-",
                decX, y, btnW, rowHeight, isPrimary: false, enabled: value > min))
        {
            next = Math.Max(min, value - 1);
        }
        if (ButtonRenderer.DrawButton(drawList, "+",
                incX, y, btnW, rowHeight, isPrimary: false, enabled: value < max))
        {
            next = Math.Min(max, value + 1);
        }
        _ = events;
        return next;
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
        ModalInputGuard.MarkOpen();
        var loc = LocalizationService.Instance;

        var winPos = ImGui.GetWindowPos();
        var winSize = ImGui.GetWindowSize();
        drawList.AddRectFilled(winPos, new Vector2(winPos.X + winSize.X, winPos.Y + winSize.Y),
            ImGui.ColorConvertFloat4ToU32(ColorPalette.BlackOverlay));

        const float dialogWidth = 420f;
        const float dialogHeight = 170f;
        var dlgX = winPos.X + (winSize.X - dialogWidth) / 2f;
        var dlgY = winPos.Y + (winSize.Y - dialogHeight) / 2f;

        drawList.AddRectFilled(new Vector2(dlgX, dlgY), new Vector2(dlgX + dialogWidth, dlgY + dialogHeight),
            ImGui.ColorConvertFloat4ToU32(ColorPalette.Background), 6f);
        drawList.AddRect(new Vector2(dlgX, dlgY), new Vector2(dlgX + dialogWidth, dlgY + dialogHeight),
            ImGui.ColorConvertFloat4ToU32(ColorPalette.BorderColor), 6f, ImDrawFlags.None, 1.5f);

        const float padding = 18f;
        var bodyWidth = dialogWidth - padding * 2f;
        var curX = dlgX + padding;
        var curY = dlgY + padding;

        TextRenderer.DrawLabel(drawList,
            loc.Get(LocalizationKeys.UI_FEASTDAY_REMOVE_CONFIRM, vm.RemoveConfirmFeastName ?? string.Empty),
            curX, curY, PageTitle, ColorPalette.Gold);
        curY += PageTitle + 6f;
        ChromeRenderer.DrawDivider(drawList, curX, curY, bodyWidth);

        const float btnWidth = 120f;
        const float btnHeight = 32f;
        const float btnGap = 10f;
        var btnY = dlgY + dialogHeight - padding - btnHeight;
        var removeX = dlgX + dialogWidth - padding - btnWidth;
        var cancelX = removeX - btnWidth - btnGap;

        if (ButtonRenderer.DrawButton(drawList,
                loc.Get(LocalizationKeys.UI_FEASTDAY_CANCEL),
                cancelX, btnY, btnWidth, btnHeight))
            events.Add(new SacredCalendarEvent.RemoveCancel());

        if (ButtonRenderer.DrawButton(drawList,
                loc.Get(LocalizationKeys.UI_FEASTDAY_REMOVE_ACTION),
                removeX, btnY, btnWidth, btnHeight, isPrimary: true, enabled: true))
            events.Add(new SacredCalendarEvent.RemoveConfirmed(vm.RemoveConfirmFeastId!.Value));

        if (ImGui.IsKeyPressed(ImGuiKey.Escape))
            events.Add(new SacredCalendarEvent.RemoveCancel());

        _ = x; _ = y; _ = width; _ = height;
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
