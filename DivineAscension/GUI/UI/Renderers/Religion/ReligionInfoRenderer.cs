using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using DivineAscension.Constants;
using DivineAscension.GUI.Events.Religion;
using DivineAscension.GUI.Models.Religion.Info;
using DivineAscension.GUI.UI.Components.Lists;
using DivineAscension.GUI.UI.Components.Overlays;
using DivineAscension.GUI.UI.Renderers.Religion.Info;
using DivineAscension.GUI.UI.Renderers.Utilities;
using DivineAscension.GUI.UI.Utilities;
using DivineAscension.Services;
using ImGuiNET;

namespace DivineAscension.GUI.UI.Renderers.Religion;

/// <summary>
/// Pure renderer for the "This Order" ledger chapter (#309). Order title and
/// stat block at the top, prose Of the Order's Purpose and Of the Order's
/// Founding sections in the middle, and Leave / Disband footer actions. Roster,
/// Invite, and the founder-only Stricken from the Ledger list live on the
/// sibling II.ii Roster chapter.
/// </summary>
[ExcludeFromCodeCoverage]
internal static class ReligionInfoRenderer
{
    private const float DividerHeight = 18f;
    private const float DividerYPadding = 6f;
    private const float FooterTopPadding = 12f;
    private const float ScrollbarWidth = 16f;

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

        // === CREED ===
        currentY = ReligionInfoMottoRenderer.Draw(viewModel, drawList, x, currentY, contentWidth, events);

        currentY = DrawDivider(drawList, x, currentY, contentWidth);

        // === DYNAMIC PROSE FRAMES ===
        // Purpose and Myth reserve a frame sized for content but clamp to
        // the available pane height so a shrunk window doesn't push the
        // prose past the footer. Myth absorbs the squeeze first since it
        // owns the bulk of the vertical budget.
        var paneBottomY = y + height;
        var actionsFooterReserve = FooterTopPadding + 34f + 6f;
        var dividerBetween = DividerHeight;
        const float headingAndSpacing = 22f + 8f;
        const float purposeMax = 80f;
        const float mythMax = 540f;
        // Remaining height for both prose bodies after fixed reservations.
        var remainingForProse = paneBottomY - currentY
            - headingAndSpacing       // purpose heading + spacing
            - dividerBetween          // divider between purpose and myth
            - headingAndSpacing       // myth heading + spacing
            - actionsFooterReserve;
        // Purpose keeps its small max unless the pane is so small it forces
        // a share; floor at a readable 40px.
        var purposeBody = MathF.Max(40f, MathF.Min(purposeMax, remainingForProse * 0.15f));
        if (remainingForProse < purposeMax + 80f) purposeBody = MathF.Max(40f, remainingForProse * 0.25f);
        var mythBody = MathF.Max(60f, MathF.Min(mythMax, remainingForProse - purposeBody));

        // === OF THE ORDER'S PURPOSE ===
        currentY = ReligionInfoDescriptionRenderer.Draw(viewModel, drawList, x, currentY, contentWidth, events, purposeBody);

        currentY = DrawDivider(drawList, x, currentY, contentWidth);

        // === OF THE ORDER'S FOUNDING ===
        currentY = ReligionInfoFoundingMythRenderer.Draw(viewModel, drawList, x, currentY, contentWidth, events, mythBody);

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

        // Motto block
        if (viewModel.IsFounder && viewModel.IsEditingMotto)
            h += 22f + 28f + 6f + 26f + 8f;
        else
            h += 22f + 28f;

        // Divider
        h += DividerHeight;

        // Description block — prose path reserves a fixed 80f frame
        // (matches ReligionInfoDescriptionRenderer.ProseBodyHeight).
        if (viewModel.IsFounder && viewModel.IsEditingDescription)
            h += 22f + 80f + 6f + 26f + 8f;
        else
            h += 22f + 80f + 8f;

        // Divider
        h += DividerHeight;

        // Founding myth block — prose path reserves a fixed 540f frame
        // (matches ReligionInfoFoundingMythRenderer.ProseBodyHeight).
        if (viewModel.IsFounder && viewModel.IsEditingFoundingMyth)
            h += 22f + 200f + 6f + 26f + 8f;
        else
            h += 22f + 540f + 8f;

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
