using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using DivineAscension.Constants;
using DivineAscension.GUI.Events.Religion;
using DivineAscension.GUI.Models.Religion.Info;
using DivineAscension.GUI.UI.Components.Lists;
using DivineAscension.GUI.UI.Components.Overlays;
using DivineAscension.GUI.UI.Renderers.Components;
using DivineAscension.GUI.UI.Renderers.Religion.Info;
using DivineAscension.GUI.UI.Renderers.Utilities;
using DivineAscension.GUI.UI.Utilities;
using DivineAscension.Network;
using DivineAscension.Services;
using ImGuiNET;
using static DivineAscension.GUI.UI.Utilities.FontSizes;

namespace DivineAscension.GUI.UI.Renderers.Religion;

/// <summary>
/// Pure renderer for the "This Order" ledger chapter (#309). Order title and
/// stat block at the top, prose Of the Order's Purpose section in the middle,
/// founder-only Stricken from the Ledger section below (collapsed when
/// empty), and Leave / Disband footer actions. Roster and Invite live on the
/// sibling II.ii chapter.
/// </summary>
[ExcludeFromCodeCoverage]
internal static class ReligionInfoRenderer
{
    private const float DividerHeight = 18f;
    private const float DividerYPadding = 6f;
    private const float SectionLabelHeight = 22f;
    private const float FooterTopPadding = 12f;
    private const float ScrollbarWidth = 16f;
    private const float BanListHeight = 120f;

    public static ReligionInfoRenderResult Draw(
        ReligionInfoViewModel viewModel,
        ImDrawListPtr drawList)
    {
        var events = new List<InfoEvent>();
        var x = viewModel.X;
        var y = viewModel.Y;
        var width = viewModel.Width;
        var height = viewModel.Height;

        if (viewModel.IsLoading)
        {
            DrawCenteredStateText(drawList,
                LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_INFO_LOADING),
                x, y, width, height);
            return new ReligionInfoRenderResult(events, height);
        }

        if (!viewModel.HasReligion)
        {
            DrawCenteredStateText(drawList,
                LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_INFO_NO_RELIGION),
                x, y, width, height);
            return new ReligionInfoRenderResult(events, height);
        }

        var contentHeightEstimate = ComputeContentHeight(viewModel);
        var maxScroll = MathF.Max(0f, contentHeightEstimate - height);

        var mousePos = ImGui.GetMousePos();
        var isHover = mousePos.X >= x && mousePos.X <= x + width && mousePos.Y >= y && mousePos.Y <= y + height;
        var scrollY = viewModel.ScrollY;
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

        // === SHARED CHAPTER STRIP ===
        var deityDomain = DomainHelper.ParseDeityType(viewModel.Deity);
        var strip = ChapterStripRenderer.Draw(drawList, x, y, width, scrollY,
            LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_TAB_INFO),
            rightTitle: viewModel.ReligionName,
            rightGlyph: deityDomain,
            showPencil: viewModel.IsFounder && !viewModel.IsEditingDeityName);
        if (strip.PencilClicked)
            events.Add(new InfoEvent.EditDeityNameOpen());
        var contentWidth = strip.ContentWidth;
        var currentY = strip.BodyY;

        // === PROSE INTRO + STAT BLOCK ===
        currentY = ReligionInfoHeaderRenderer.Draw(viewModel, drawList, x, currentY, contentWidth, events);

        currentY = DrawDivider(drawList, x, currentY, contentWidth);

        // === OF THE ORDER'S PURPOSE ===
        currentY = ReligionInfoDescriptionRenderer.Draw(viewModel, drawList, x, currentY, contentWidth, events);

        // === STRICKEN FROM THE LEDGER (founder-only) ===
        if (viewModel.IsFounder)
        {
            currentY = DrawDivider(drawList, x, currentY, contentWidth);
            currentY = DrawStrickenSection(viewModel, drawList, x, currentY, contentWidth, events);
        }

        // === FOOTER ACTIONS ===
        currentY += FooterTopPadding;
        currentY = ReligionInfoActionsRenderer.Draw(viewModel, drawList, x, currentY, contentWidth, events);

        drawList.PopClipRect();

        if (contentHeightEstimate > height)
            Scrollbar.Draw(drawList, x + width - ScrollbarWidth, y, ScrollbarWidth, height, scrollY, maxScroll);

        if (viewModel.ShowDisbandConfirm)
            DrawDisbandConfirmation(events);

        if (viewModel.BanConfirmPlayerUID != null)
            DrawBanConfirmation(viewModel.BanConfirmPlayerName ?? viewModel.BanConfirmPlayerUID,
                viewModel.BanConfirmPlayerUID, events);

        return new ReligionInfoRenderResult(events, height);
    }

    private static float DrawDivider(ImDrawListPtr drawList, float x, float y, float width)
    {
        var dividerY = y + DividerYPadding;
        ChromeRenderer.DrawDivider(drawList, x, dividerY, width);
        return y + DividerHeight;
    }

    private static float DrawStrickenSection(
        ReligionInfoViewModel viewModel,
        ImDrawListPtr drawList,
        float x, float y, float width,
        List<InfoEvent> events)
    {
        var currentY = y;
        TextRenderer.DrawLabel(drawList,
            LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_INFO_STRICKEN_HEADING),
            x, currentY, SubsectionLabel, ColorPalette.Gold);
        currentY += SectionLabelHeight;

        if (!viewModel.HasBannedPlayers)
        {
            // Collapsed-when-empty: single italic-feeling line, no panel reserved.
            TextRenderer.DrawInfoText(drawList,
                LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_INFO_STRICKEN_EMPTY),
                x, currentY, width, Secondary, ColorPalette.Grey);
            currentY += 22f;
            return currentY;
        }

        var banListScrollY = DrawBanList(drawList, viewModel, x, currentY, width, BanListHeight, events);
        if (Math.Abs(banListScrollY - viewModel.BanListScrollY) > 0.001f)
            events.Add(new InfoEvent.BanListScrollChanged(banListScrollY));

        currentY += BanListHeight + 8f;
        return currentY;
    }

    private static float DrawBanList(
        ImDrawListPtr drawList,
        ReligionInfoViewModel viewModel,
        float x, float y, float width, float height,
        List<InfoEvent> events)
    {
        return BanListRenderer.Draw(
            drawList,
            null!, // API not needed for pure rendering
            x, y, width, height,
            new List<PlayerReligionInfoResponsePacket.BanInfo>(viewModel.BannedPlayers),
            viewModel.BanListScrollY,
            playerUid => { events.Add(new InfoEvent.UnbanClicked(playerUid)); });
    }

    private static float ComputeContentHeight(ReligionInfoViewModel viewModel)
    {
        var h = 0f;
        // Pane header (icon row + divider below)
        h += PaneHeaderRenderer.TotalHeight;
        // Prose intro (~2 lines)
        h += 36f;
        // Stat block: deity / founder / members / prestige
        h += 22f * 4 + 8f;
        if (viewModel.IsEditingDeityName) h += 80f;

        // Divider
        h += DividerHeight;

        // Description block
        if (viewModel.IsFounder && viewModel.IsEditingDescription)
            h += 22f + 80f + 6f + 26f + 8f;
        else
            h += 22f + 40f;

        if (viewModel.IsFounder)
        {
            h += DividerHeight;
            h += 22f; // stricken heading
            h += viewModel.HasBannedPlayers ? BanListHeight + 8f : 22f;
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

    private static void DrawDisbandConfirmation(List<InfoEvent> events)
    {
        ConfirmOverlay.Draw(
            LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_INFO_DISBAND_TITLE),
            LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_INFO_DISBAND_MESSAGE),
            out var confirmed, out var cancelled,
            LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_INFO_DISBAND_CONFIRM));

        if (confirmed) events.Add(new InfoEvent.DisbandConfirm());
        if (cancelled) events.Add(new InfoEvent.DisbandCancel());
    }

    private static void DrawBanConfirmation(string playerName, string playerUid, List<InfoEvent> events)
    {
        ConfirmOverlay.Draw(
            LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_INFO_BAN_TITLE),
            LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_INFO_BAN_MESSAGE, playerName),
            out var confirmed, out var cancelled,
            LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_INFO_BAN_CONFIRM));

        if (confirmed) events.Add(new InfoEvent.BanConfirm(playerUid));
        if (cancelled) events.Add(new InfoEvent.BanCancel());
    }
}
