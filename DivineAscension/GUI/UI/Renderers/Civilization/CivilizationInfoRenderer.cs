using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using DivineAscension.Constants;
using DivineAscension.GUI.Events.Civilization;
using DivineAscension.GUI.Models.Civilization.Info;
using DivineAscension.GUI.UI.Components.Buttons;
using DivineAscension.GUI.UI.Components.Inputs;
using DivineAscension.GUI.UI.Components.Lists;
using DivineAscension.GUI.UI.Components.Overlays;
using DivineAscension.GUI.UI.Renderers.Civilization.Info;
using DivineAscension.GUI.UI.Renderers.Utilities;
using DivineAscension.GUI.UI.Utilities;
using DivineAscension.Network.Civilization;
using DivineAscension.Services;
using ImGuiNET;
using static DivineAscension.GUI.UI.Utilities.FontSizes;

namespace DivineAscension.GUI.UI.Renderers.Civilization;

/// <summary>
/// Pure renderer for the "This Realm" ledger chapter (#310). Sibling of
/// <see cref="Religion.ReligionInfoRenderer"/>. Title strip + prose intro +
/// dotted-leader stat block + founder-editable purpose prose + banner-orders
/// list + founder-only invite block + Leave / Disband footer.
/// </summary>
[ExcludeFromCodeCoverage]
internal static class CivilizationInfoRenderer
{
    private const float DividerHeight = 18f;
    private const float DividerYPadding = 6f;
    private const float SectionLabelHeight = 22f;
    private const float OrderRowHeight = 22f;
    private const float InviteInputHeight = 30f;
    private const float InviteButtonWidth = 130f;
    private const float InviteButtonHeight = 32f;
    private const float LetterRowHeight = 22f;
    private const float FooterTopPadding = 12f;
    private const float ScrollbarWidth = 16f;
    private const float DiamondHalfSize = 3.5f;
    private const float DiamondLeftPadding = 4f;
    private const float DiamondLabelGap = 10f;

    public static CivilizationInfoRendererResult Draw(
        CivilizationInfoViewModel vm,
        ImDrawListPtr drawList)
    {
        var events = new List<InfoEvent>();
        var x = vm.X;
        var y = vm.Y;
        var width = vm.Width;
        var height = vm.Height;

        if (vm.IsLoading)
        {
            DrawCenteredStateText(drawList,
                LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_INFO_LOADING),
                x, y, width, height);
            return new CivilizationInfoRendererResult(events, height);
        }

        if (!vm.HasCivilization)
        {
            DrawCenteredStateText(drawList,
                LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_INFO_NOT_IN_CIV),
                x, y, width, height);
            return new CivilizationInfoRendererResult(events, height);
        }

        var overlayOpen = vm.ShowDisbandConfirm || vm.IsKickConfirmOpen;

        var contentHeightEstimate = ComputeContentHeight(vm);
        var maxScroll = MathF.Max(0f, contentHeightEstimate - height);

        var mousePos = ImGui.GetMousePos();
        var isHover = mousePos.X >= x && mousePos.X <= x + width && mousePos.Y >= y && mousePos.Y <= y + height;
        var scrollY = vm.ScrollY;
        if (isHover)
        {
            var wheel = ImGui.GetIO().MouseWheel;
            if (wheel != 0)
            {
                var newScrollY = Math.Clamp(scrollY - wheel * 30f, 0f, maxScroll);
                if (Math.Abs(newScrollY - scrollY) > 0.001f)
                {
                    scrollY = newScrollY;
                    events.Add(new InfoEvent.ScrollChanged(newScrollY));
                }
            }
        }

        drawList.PushClipRect(new Vector2(x, y), new Vector2(x + width, y + height), true);

        var strip = ChapterStripRenderer.Draw(drawList, x, y, width, scrollY,
            LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_INFO_CHAPTER_TITLE),
            rightTitle: vm.CivName,
            showPencil: vm.IsFounder && !vm.IsEditingDescription);
        if (strip.PencilClicked)
            events.Add(new InfoEvent.EditIconClicked());
        var contentWidth = strip.ContentWidth;
        var currentY = strip.BodyY;

        currentY = CivilizationInfoHeaderRenderer.Draw(vm, drawList, x, currentY, contentWidth);

        currentY = DrawDivider(drawList, x, currentY, contentWidth);

        currentY = CivilizationInfoDescriptionRenderer.Draw(vm, drawList, x, currentY, contentWidth, events);

        currentY = DrawDivider(drawList, x, currentY, contentWidth);

        currentY = DrawBannerOrders(vm, drawList, x, currentY, contentWidth);

        if (vm.IsFounder)
        {
            currentY = DrawDivider(drawList, x, currentY, contentWidth);
            currentY = DrawInviteSection(vm, drawList, x, currentY, contentWidth, overlayOpen, events);
        }

        currentY += FooterTopPadding;
        currentY = CivilizationInfoActionsRenderer.Draw(vm, drawList, x, currentY, contentWidth, overlayOpen, events);

        drawList.PopClipRect();

        if (contentHeightEstimate > height)
            Scrollbar.Draw(drawList, x + width - ScrollbarWidth, y, ScrollbarWidth, height, scrollY, maxScroll);

        DrawOverlays(vm, events);

        return new CivilizationInfoRendererResult(events, height);
    }

    private static float DrawBannerOrders(
        CivilizationInfoViewModel vm,
        ImDrawListPtr drawList,
        float x, float y, float width)
    {
        var currentY = y;
        TextRenderer.DrawLabel(drawList,
            LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_INFO_BANNER_ORDERS_HEADING),
            x, currentY, SubsectionLabel, ColorPalette.Gold);
        currentY += SectionLabelHeight;

        if (vm.MemberCount == 0)
        {
            TextRenderer.DrawInfoText(drawList,
                LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_INFO_BANNER_ORDERS_EMPTY),
                x, currentY, width, Secondary, ColorPalette.Grey);
            currentY += OrderRowHeight;
            return currentY;
        }

        foreach (var member in vm.MemberReligions)
        {
            DrawOrderRow(drawList, member.ReligionName, x, currentY);
            currentY += OrderRowHeight;
        }

        return currentY + 4f;
    }

    private static void DrawOrderRow(ImDrawListPtr drawList, string name, float x, float y)
    {
        var centerY = y + OrderRowHeight / 2f;
        ChromeRenderer.DrawDiamond(drawList,
            x + DiamondLeftPadding + DiamondHalfSize, centerY,
            DiamondHalfSize,
            ColorPalette.Gold * 0.6f);

        var textX = x + DiamondLeftPadding + DiamondHalfSize * 2f + DiamondLabelGap;
        drawList.AddText(new Vector2(textX, centerY - Body * 0.5f),
            ImGui.ColorConvertFloat4ToU32(ColorPalette.White),
            name);
    }

    private static float DrawInviteSection(
        CivilizationInfoViewModel vm,
        ImDrawListPtr drawList,
        float x, float y, float width,
        bool overlayOpen,
        List<InfoEvent> events)
    {
        var currentY = y;

        TextRenderer.DrawLabel(drawList,
            LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_INFO_BID_LABEL),
            x, currentY, SubsectionLabel, ColorPalette.Grey);
        currentY += SectionLabelHeight;

        var inputWidth = width - InviteButtonWidth - 10f;
        if (inputWidth < 120f) inputWidth = MathF.Max(120f, width - 10f);

        var newInvite = TextInput.Draw(
            drawList,
            "##inviteReligion",
            vm.InviteReligionName,
            x,
            currentY,
            inputWidth,
            InviteInputHeight,
            LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_INFO_INVITE_PLACEHOLDER),
            64);
        if (newInvite != vm.InviteReligionName)
            events.Add(new InfoEvent.InviteReligionNameChanged(newInvite));

        var canInvite = !overlayOpen && vm.CanInvite && !vm.IsLoading;
        var inviteButtonX = x + inputWidth + 10f;
        if (ButtonRenderer.DrawButton(drawList,
                LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_INFO_INVITE_BUTTON),
                inviteButtonX, currentY, InviteButtonWidth, InviteButtonHeight,
                isPrimary: true, enabled: canInvite))
        {
            events.Add(new InfoEvent.InviteReligionClicked(vm.InviteReligionName));
        }

        currentY += InviteInputHeight + 10f;

        TextRenderer.DrawLabel(drawList,
            LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_INFO_LETTERS_HEADING),
            x, currentY, SubsectionLabel, ColorPalette.Grey);
        currentY += SectionLabelHeight;

        if (!vm.HasPendingInvites)
        {
            TextRenderer.DrawInfoText(drawList,
                LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_INFO_LETTERS_EMPTY),
                x, currentY, width, Secondary, ColorPalette.Grey);
            currentY += LetterRowHeight;
            return currentY;
        }

        var awaiting = LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_INFO_LETTERS_AWAITING);
        foreach (var invite in vm.PendingInvites)
        {
            DrawLetterRow(drawList, invite, awaiting, x, currentY, width);
            currentY += LetterRowHeight;
        }

        return currentY + 4f;
    }

    private static void DrawLetterRow(
        ImDrawListPtr drawList,
        CivilizationInfoResponsePacket.PendingInvite invite,
        string awaiting,
        float x, float y, float width)
    {
        var centerY = y + LetterRowHeight / 2f;
        ChromeRenderer.DrawDiamond(drawList,
            x + DiamondLeftPadding + DiamondHalfSize, centerY,
            DiamondHalfSize,
            ColorPalette.Gold * 0.4f);

        var leaderX = x + DiamondLeftPadding + DiamondHalfSize * 2f + DiamondLabelGap;
        var leaderWidth = MathF.Max(width - (leaderX - x) - 8f, 40f);
        var rowY = centerY - Body * 0.5f;
        ChromeRenderer.DrawLeader(drawList,
            invite.ReligionName,
            awaiting,
            leaderX, rowY, leaderWidth);
    }

    private static float DrawDivider(ImDrawListPtr drawList, float x, float y, float width)
    {
        var dividerY = y + DividerYPadding;
        ChromeRenderer.DrawDivider(drawList, x, dividerY, width);
        return y + DividerHeight;
    }

    private static float ComputeContentHeight(CivilizationInfoViewModel vm)
    {
        var h = 0f;
        h += PaneHeaderRenderer.TotalHeight;
        // Prose intro
        h += Body + LinePadding + 12f;
        // Stat block (3 rows)
        h += OrderRowHeight * 3 + 8f;

        h += DividerHeight;

        // Description
        if (vm.IsFounder && vm.IsEditingDescription)
            h += SectionLabelHeight + 80f + 6f + 26f + 8f;
        else
            h += SectionLabelHeight + Secondary + LinePadding + 8f;

        h += DividerHeight;

        // Banner orders
        h += SectionLabelHeight;
        h += vm.MemberCount == 0 ? OrderRowHeight : OrderRowHeight * vm.MemberCount + 4f;

        // Invite block (founder only)
        if (vm.IsFounder)
        {
            h += DividerHeight;
            h += SectionLabelHeight + InviteInputHeight + 10f;
            h += SectionLabelHeight;
            h += vm.HasPendingInvites
                ? LetterRowHeight * vm.PendingInvites.Count + 4f
                : LetterRowHeight;
        }

        // Footer
        h += FooterTopPadding + 34f + 6f;
        return h;
    }

    private static void DrawCenteredStateText(
        ImDrawListPtr drawList, string text, float x, float y, float width, float height)
    {
        var size = ImGui.CalcTextSize(text);
        var pos = new Vector2(x + (width - size.X) / 2f, y + (height - size.Y) / 2f);
        var color = ImGui.ColorConvertFloat4ToU32(ColorPalette.Grey);
        drawList.AddText(pos, color, text);
    }

    private static void DrawOverlays(CivilizationInfoViewModel vm, List<InfoEvent> events)
    {
        if (vm.ShowDisbandConfirm)
        {
            ConfirmOverlay.Draw(
                LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_INFO_DISBAND_CONFIRM_TITLE),
                LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_INFO_DISBAND_CONFIRM_MESSAGE),
                out var confirmed,
                out var canceled,
                LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_INFO_DISBAND_CONFIRM_BUTTON));

            if (confirmed) events.Add(new InfoEvent.DisbandConfirmed());
            if (canceled) events.Add(new InfoEvent.DisbandCancel());
        }

        if (vm.IsKickConfirmOpen)
        {
            var targetId = vm.KickConfirmReligionId!;
            var targetName = string.Empty;
            foreach (var m in vm.MemberReligions)
            {
                if (m.ReligionId == targetId)
                {
                    targetName = m.ReligionName;
                    break;
                }
            }
            if (string.IsNullOrEmpty(targetName))
                targetName = LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_INFO_UNKNOWN_RELIGION);

            ConfirmOverlay.Draw(
                LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_INFO_KICK_CONFIRM_TITLE),
                LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_INFO_KICK_CONFIRM_MESSAGE,
                    targetName),
                out var confirmed,
                out var canceled,
                LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_INFO_KICK_CONFIRM_BUTTON));

            if (confirmed) events.Add(new InfoEvent.KickConfirm(targetName));
            if (canceled) events.Add(new InfoEvent.KickCancel());
        }
    }
}
