using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using DivineAscension.Constants;
using DivineAscension.GUI.Events.Religion;
using DivineAscension.GUI.Models.Religion.Roster;
using DivineAscension.GUI.UI.Components.Buttons;
using DivineAscension.GUI.UI.Components.Inputs;
using DivineAscension.GUI.UI.Components.Lists;
using DivineAscension.GUI.UI.Components.Overlays;
using DivineAscension.GUI.UI.Renderers.Religion.Roster;
using DivineAscension.GUI.UI.Renderers.Utilities;
using DivineAscension.GUI.UI.Utilities;
using DivineAscension.Services;
using ImGuiNET;
using static DivineAscension.GUI.UI.Utilities.FontSizes;

namespace DivineAscension.GUI.UI.Renderers.Religion;

/// <summary>
/// Pure renderer for the Roster chapter (II.ii — issue #313). Sibling page to
/// "This Order" (II.i). Header with order name + prose intro and a founder-only
/// "+" to inscribe a new soul, dotted-leader member rows with inline
/// founder-only kick/strike actions, and the founder-only Stricken from the
/// Ledger section. Inviting happens in a modal Inscription of Souls dialog.
/// </summary>
[ExcludeFromCodeCoverage]
internal static class ReligionRosterRenderer
{
    private static float TopPadding => UiScale.Scaled(8f);
    private static float DividerHeight => UiScale.Scaled(18f);
    private static float DividerYPadding => UiScale.Scaled(6f);
    private static float ScrollbarWidth => UiScale.Scaled(16f);
    private static float SectionLabelHeight => UiScale.Scaled(22f);

    public static ReligionRosterRenderResult Draw(ReligionRosterViewModel vm, ImDrawListPtr drawList)
    {
        var events = new List<RosterEvent>();
        var x = vm.X;
        var y = vm.Y;
        var width = vm.Width;
        var height = vm.Height;

        if (vm.IsLoading)
        {
            DrawCentered(drawList,
                LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_INFO_LOADING),
                x, y, width, height);
            return new ReligionRosterRenderResult(events, height);
        }

        if (!vm.HasReligion)
        {
            DrawCentered(drawList,
                LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_INFO_NO_RELIGION),
                x, y, width, height);
            return new ReligionRosterRenderResult(events, height);
        }

        var contentHeight = ComputeContentHeight(vm);
        var maxScroll = MathF.Max(0f, contentHeight - height);

        var scrollY = vm.ScrollY;
        var mousePos = ImGui.GetMousePos();
        var isHover = mousePos.X >= x && mousePos.X <= x + width && mousePos.Y >= y && mousePos.Y <= y + height;
        if (isHover)
        {
            var wheel = ImGui.GetIO().MouseWheel;
            if (wheel != 0)
            {
                var newScrollY = Math.Clamp(scrollY - wheel * 30f, 0f, maxScroll);
                if (Math.Abs(newScrollY - scrollY) > 0.001f)
                {
                    scrollY = newScrollY;
                    events.Add(new RosterEvent.ScrollChanged(newScrollY));
                }
            }
        }

        drawList.PushClipRect(new Vector2(x, y), new Vector2(x + width, y + height), true);
        var contentWidth = contentHeight > height ? width - ScrollbarWidth : width;
        var currentY = y + TopPadding - scrollY;

        // === HEADER (title + prose intro) ===
        currentY = ReligionRosterHeaderRenderer.Draw(vm, drawList, x, currentY, contentWidth);

        currentY = DrawDivider(drawList, x, currentY, contentWidth);

        // === ROWS + COUNTER ===
        currentY = ReligionRosterRowsRenderer.Draw(vm, drawList, x, currentY, contentWidth, events);

        // === STRICKEN FROM THE LEDGER (founder only) ===
        if (vm.IsFounder)
        {
            currentY = DrawDivider(drawList, x, currentY, contentWidth);
            currentY = DrawStrickenSection(vm, drawList, x, currentY, contentWidth, events);
        }

        drawList.PopClipRect();

        if (contentHeight > height)
            Scrollbar.Draw(drawList, x + width - ScrollbarWidth, y, ScrollbarWidth, height, scrollY, maxScroll);

        // Founder-only "+" to inscribe a new soul — fixed at the chapter
        // header's right edge so it stays reachable regardless of scroll. The
        // field + submit live in a modal dialog (Inscription of Souls).
        if (vm.IsFounder)
            DrawAddButton(drawList, x, y, contentWidth, events);

        if (vm.ShowInviteDialog)
            DrawInviteDialog(vm, drawList, events);

        if (vm.StrikeConfirmPlayerUID != null)
            DrawStrikeConfirm(vm.StrikeConfirmPlayerName ?? vm.StrikeConfirmPlayerUID,
                vm.StrikeConfirmPlayerUID, events);

        return new ReligionRosterRenderResult(events, height);
    }

    private static float AddButtonSize => UiScale.Scaled(26f);

    private static void DrawAddButton(
        ImDrawListPtr drawList, float x, float y, float contentWidth, List<RosterEvent> events)
    {
        var bx = x + contentWidth - AddButtonSize;
        var by = y + TopPadding + UiScale.Scaled(2f);
        if (ButtonRenderer.DrawButton(drawList, string.Empty, bx, by, AddButtonSize, AddButtonSize,
                isPrimary: false, enabled: true))
        {
            events.Add(new RosterEvent.InviteDialogOpened());
        }

        ChromeRenderer.DrawPlus(drawList,
            bx + AddButtonSize / 2f, by + AddButtonSize / 2f,
            AddButtonSize - UiScale.Scaled(12f), ColorPalette.LightText);
    }

    private static void DrawInviteDialog(
        ReligionRosterViewModel vm, ImDrawListPtr drawList, List<RosterEvent> events)
    {
        // Block click-through to the roster behind the dim backdrop this frame.
        ModalInputGuard.MarkOpen();
        var loc = LocalizationService.Instance;

        var winPos = ImGui.GetWindowPos();
        var winSize = ImGui.GetWindowSize();

        // Warm-dark page dim, per palette §4.
        drawList.AddRectFilled(winPos, new Vector2(winPos.X + winSize.X, winPos.Y + winSize.Y),
            ImGui.ColorConvertFloat4ToU32(ColorPalette.BlackOverlay));

        var dialogWidth = UiScale.Scaled(460f);
        var dialogHeight = UiScale.Scaled(220f);
        var dlgX = winPos.X + (winSize.X - dialogWidth) / 2f;
        var dlgY = winPos.Y + (winSize.Y - dialogHeight) / 2f;

        // Parchment mini-page with a faded-ink border, matching the role-edit dialog.
        drawList.AddRectFilled(new Vector2(dlgX, dlgY), new Vector2(dlgX + dialogWidth, dlgY + dialogHeight),
            ImGui.ColorConvertFloat4ToU32(ColorPalette.Background), UiScale.Scaled(6f));
        drawList.AddRect(new Vector2(dlgX, dlgY), new Vector2(dlgX + dialogWidth, dlgY + dialogHeight),
            ImGui.ColorConvertFloat4ToU32(ColorPalette.BorderColor), UiScale.Scaled(6f), ImDrawFlags.None, UiScale.Scaled(1.5f));

        var padding = UiScale.Scaled(18f);
        var bodyWidth = dialogWidth - padding * 2f;
        var curX = dlgX + padding;
        var curY = dlgY + padding;

        // Title — gold rubric, ornamental divider below.
        TextRenderer.DrawLabel(drawList,
            loc.Get(LocalizationKeys.UI_RELIGION_ROSTER_INVITE_TITLE),
            curX, curY, PageTitle, ColorPalette.Gold);
        curY += PageTitle + UiScale.Scaled(6f);
        ChromeRenderer.DrawDivider(drawList, curX, curY, bodyWidth);
        curY += UiScale.Scaled(16f);

        // Prompt + name field.
        TextRenderer.DrawInfoText(drawList,
            loc.Get(LocalizationKeys.UI_RELIGION_ROSTER_INVITE_LABEL),
            curX, curY, bodyWidth, Body, ColorPalette.White);
        curY += UiScale.Scaled(24f);

        var newName = TextInput.Draw(drawList, "##rosterInvite", vm.InvitePlayerName,
            curX, curY, bodyWidth, UiScale.Scaled(32f),
            loc.Get(LocalizationKeys.UI_RELIGION_ROSTER_INVITE_PLACEHOLDER));
        if (newName != vm.InvitePlayerName)
            events.Add(new RosterEvent.InviteNameChanged(newName));

        // Footer: Cancel + Invite (right-aligned).
        var btnWidth = UiScale.Scaled(120f);
        var btnHeight = UiScale.Scaled(32f);
        var btnGap = UiScale.Scaled(10f);
        var btnY = dlgY + dialogHeight - padding - btnHeight;
        var inviteX = dlgX + dialogWidth - padding - btnWidth;
        var cancelX = inviteX - btnWidth - btnGap;

        if (ButtonRenderer.DrawButton(drawList,
                loc.Get(LocalizationKeys.UI_COMMON_CANCEL),
                cancelX, btnY, btnWidth, btnHeight))
            events.Add(new RosterEvent.InviteDialogCancel());

        var canInvite = !string.IsNullOrWhiteSpace(vm.InvitePlayerName);
        if (ButtonRenderer.DrawButton(drawList,
                loc.Get(LocalizationKeys.UI_RELIGION_ROSTER_INVITE_BUTTON),
                inviteX, btnY, btnWidth, btnHeight, isPrimary: true, enabled: canInvite)
            && canInvite)
            events.Add(new RosterEvent.InviteClicked(vm.InvitePlayerName.Trim()));

        if (ImGui.IsKeyPressed(ImGuiKey.Escape))
            events.Add(new RosterEvent.InviteDialogCancel());
    }

    private static float DrawDivider(ImDrawListPtr drawList, float x, float y, float width)
    {
        var dividerY = y + DividerYPadding;
        ChromeRenderer.DrawDivider(drawList, x, dividerY, width);
        return y + DividerHeight;
    }

    private static float DrawStrickenSection(
        ReligionRosterViewModel vm,
        ImDrawListPtr drawList,
        float x, float y, float width,
        List<RosterEvent> events)
    {
        var currentY = y;
        TextRenderer.DrawLabel(drawList,
            LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_INFO_STRICKEN_HEADING),
            x, currentY, SubsectionLabel, ColorPalette.Gold);
        currentY += SectionLabelHeight;

        return ReligionRosterStrickenRenderer.Draw(vm, drawList, x, currentY, width, events);
    }

    private static float ComputeContentHeight(ReligionRosterViewModel vm)
    {
        var h = TopPadding;
        h += PaneHeaderRenderer.TotalHeight;
        h += UiScale.Scaled(22f); // intro line
        h += DividerHeight;
        if (vm.HasMembers)
        {
            h += vm.MemberCount * ReligionRosterRowsRenderer.RowHeight;
            if (vm.IsFounder
                && vm.ExpandedMemberUID != null)
                h += ReligionRosterRowsRenderer.ActionStripHeight;
            h += ReligionRosterRowsRenderer.CounterHeight;
        }
        else
        {
            h += ReligionRosterRowsRenderer.RowHeight;
        }
        if (vm.IsFounder)
        {
            // Stricken from the Ledger section
            h += DividerHeight;
            h += SectionLabelHeight;
            if (vm.HasBannedPlayers)
            {
                h += vm.BannedPlayers.Count * ReligionRosterStrickenRenderer.RowHeight;
                if (vm.ExpandedBanUID != null)
                    h += ReligionRosterStrickenRenderer.ActionStripHeight;
            }
            else
            {
                h += ReligionRosterStrickenRenderer.RowHeight;
            }
        }
        return h;
    }

    private static void DrawCentered(ImDrawListPtr drawList, string text, float x, float y, float width, float height)
    {
        var size = ImGui.CalcTextSize(text);
        var pos = new Vector2(x + (width - size.X) / 2f, y + (height - size.Y) / 2f);
        var color = ImGui.ColorConvertFloat4ToU32(ColorPalette.Grey);
        drawList.AddText(pos, color, text);
    }

    private static void DrawStrikeConfirm(string playerName, string playerUid, List<RosterEvent> events)
    {
        ConfirmOverlay.Draw(
            LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_ROSTER_STRIKE_TITLE),
            LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_ROSTER_STRIKE_MESSAGE, playerName),
            out var confirmed, out var canceled,
            LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_ROSTER_STRIKE_CONFIRM));

        if (confirmed) events.Add(new RosterEvent.StrikeConfirm(playerUid));
        if (canceled) events.Add(new RosterEvent.StrikeCancel());
    }
}
