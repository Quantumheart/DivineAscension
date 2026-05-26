using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using DivineAscension.Constants;
using DivineAscension.GUI.Events.Religion;
using DivineAscension.GUI.Models.Religion.Roster;
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
/// "This Order" (II.i). Header with order name + prose intro, dotted-leader
/// member rows with inline founder-only kick/strike actions, founder-only
/// invite block at the foot of the page.
/// </summary>
[ExcludeFromCodeCoverage]
internal static class ReligionRosterRenderer
{
    private const float TopPadding = 8f;
    private const float DividerHeight = 18f;
    private const float DividerYPadding = 6f;
    private const float ScrollbarWidth = 16f;
    private const float FooterTopPadding = 12f;
    private const float SectionLabelHeight = 22f;

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

        // === STRICKEN FROM THE LEDGER + INVITE BLOCK (founder only) ===
        if (vm.IsFounder)
        {
            currentY = DrawDivider(drawList, x, currentY, contentWidth);
            currentY = DrawStrickenSection(vm, drawList, x, currentY, contentWidth, events);

            currentY = DrawDivider(drawList, x, currentY, contentWidth);
            currentY += FooterTopPadding;
            currentY = ReligionRosterInviteRenderer.Draw(vm, drawList, x, currentY, contentWidth, events);
        }

        drawList.PopClipRect();

        if (contentHeight > height)
            Scrollbar.Draw(drawList, x + width - ScrollbarWidth, y, ScrollbarWidth, height, scrollY, maxScroll);

        if (vm.StrikeConfirmPlayerUID != null)
            DrawStrikeConfirm(vm.StrikeConfirmPlayerName ?? vm.StrikeConfirmPlayerUID,
                vm.StrikeConfirmPlayerUID, events);

        return new ReligionRosterRenderResult(events, height);
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
        h += 22f; // intro line
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
            // Invite block
            h += DividerHeight + FooterTopPadding + ReligionRosterInviteRenderer.TotalHeight;
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
