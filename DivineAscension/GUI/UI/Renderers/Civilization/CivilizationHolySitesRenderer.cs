using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Numerics;
using DivineAscension.Constants;
using DivineAscension.GUI.Events.Civilization;
using DivineAscension.GUI.Models.Civilization.HolySites;
using DivineAscension.GUI.UI.Components.Buttons;
using DivineAscension.GUI.UI.Components.Lists;
using DivineAscension.GUI.UI.Renderers.Utilities;
using DivineAscension.GUI.UI.Utilities;
using DivineAscension.Models.Enum;
using DivineAscension.Network.HolySite;
using DivineAscension.Services;
using ImGuiNET;
using static DivineAscension.GUI.UI.Utilities.FontSizes;

namespace DivineAscension.GUI.UI.Renderers.Civilization;

/// <summary>
/// Ledger-chapter renderer for the Hallows (II.vi). Chapter strip with a
/// refresh-glyph button, prose intro keyed to the civilization name,
/// triple-diamond top/bottom rules around a list of collapsible Order
/// sub-headers, and a closing "No further hallows have been claimed." line
/// that is always present even when the list is empty.
/// </summary>
[ExcludeFromCodeCoverage]
internal static class CivilizationHolySitesRenderer
{
    private const float DividerHeight = 18f;
    private const float DividerYPadding = 6f;
    private const float ScrollbarWidth = 16f;

    private const float IntroLineHeight = 18f;
    private const float IntroBottomSpacing = 10f;

    private const float OrderHeaderHeight = 26f;
    private const float OrderHeaderTopSpacing = 4f;
    private const float OrderHeaderBottomSpacing = 6f;
    private const float CaretSize = 12f;
    private const float CaretToTextGap = 6f;
    private const float OrderLeftPadding = 8f;

    private const float SiteRowLeftPadding = 32f;
    private const float SiteNameLineHeight = 20f;
    private const float SiteClaimLineHeight = 18f;
    private const float SiteRowBottomSpacing = 6f;
    private const float SiteGlyphSize = 16f;
    private const float SiteGlyphToTextGap = 8f;
    private const float SiteClaimIndent = SiteGlyphSize + SiteGlyphToTextGap;

    private const float PerOrderDividerHeight = 16f;
    private const float ClosingLineHeight = 24f;
    private const float ClosingLineTopSpacing = 6f;

    private const float RefreshButtonSize = 22f;

    public static CivilizationHolySitesRenderResult Draw(
        CivilizationHolySitesViewModel viewModel,
        ImDrawListPtr drawList)
    {
        var events = new List<HolySitesEvent>();
        var x = viewModel.X;
        var y = viewModel.Y;
        var width = viewModel.Width;
        var height = viewModel.Height;

        if (viewModel.IsLoading && viewModel.SitesByReligion.Count == 0)
        {
            DrawCenteredStateText(drawList,
                LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_HOLYSITES_LOADING),
                x, y, width, height, ColorPalette.Grey);
            return new CivilizationHolySitesRenderResult(events, height);
        }

        if (!string.IsNullOrEmpty(viewModel.ErrorMsg))
        {
            var errorText = LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_HOLYSITES_ERROR)
                .Replace("{0}", viewModel.ErrorMsg ?? string.Empty);
            DrawCenteredStateText(drawList, errorText, x, y, width, height, ColorPalette.Vermilion);
            return new CivilizationHolySitesRenderResult(events, height);
        }

        var contentHeightEstimate = ComputeContentHeight(viewModel);
        var maxScroll = MathF.Max(0f, contentHeightEstimate - height);

        var mousePos = ImGui.GetMousePos();
        var isHover = mousePos.X >= x && mousePos.X <= x + width
                                      && mousePos.Y >= y && mousePos.Y <= y + height;
        var scrollY = viewModel.ScrollY;
        if (isHover && maxScroll > 0f)
        {
            var wheel = ImGui.GetIO().MouseWheel;
            if (wheel != 0)
            {
                var newScrollY = Math.Clamp(scrollY - wheel * 30f, 0f, maxScroll);
                if (Math.Abs(newScrollY - scrollY) > 0.001f)
                {
                    scrollY = newScrollY;
                    events.Add(new HolySitesEvent.ScrollChanged(newScrollY));
                }
            }
        }

        drawList.PushClipRect(new Vector2(x, y), new Vector2(x + width, y + height), true);

        DrawRefreshGlyph(drawList, x, y, width, scrollY, viewModel.IsLoading, events);

        // === CHAPTER STRIP ===
        var strip = ChapterStripRenderer.Draw(drawList, x, y, width, scrollY,
            LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_TAB_HOLYSITES));
        var contentWidth = strip.ContentWidth;
        var currentY = strip.BodyY;

        // === INTRO ===
        currentY = DrawIntro(drawList, viewModel.CivilizationName, x, currentY, contentWidth);

        // === TOP ORNATE RULE ===
        currentY = DrawOrnateDivider(drawList, x, currentY, contentWidth);

        // === ORDERS ===
        currentY = DrawOrders(viewModel, drawList, x, currentY, contentWidth, events);

        // === BOTTOM ORNATE RULE ===
        currentY = DrawOrnateDivider(drawList, x, currentY, contentWidth);

        // === CLOSING LINE ===
        currentY += ClosingLineTopSpacing;
        DrawClosingLine(drawList, x, currentY, contentWidth);

        drawList.PopClipRect();

        if (maxScroll > 0f)
        {
            var newScrollY = Scrollbar.HandleDragging(scrollY, maxScroll,
                x + width - ScrollbarWidth, y, ScrollbarWidth, height);
            if (Math.Abs(newScrollY - scrollY) > 0.001f)
                events.Add(new HolySitesEvent.ScrollChanged(newScrollY));
            Scrollbar.Draw(drawList, x + width - ScrollbarWidth, y, ScrollbarWidth, height, scrollY, maxScroll);
        }

        return new CivilizationHolySitesRenderResult(events, height);
    }

    private static void DrawRefreshGlyph(
        ImDrawListPtr drawList,
        float x, float paneY, float width, float scrollY,
        bool isLoading,
        List<HolySitesEvent> events)
    {
        var stripY = paneY + ChapterStripRenderer.TopPadding - scrollY;
        var px = x + width - ChapterStripRenderer.ScrollbarGutter - RefreshButtonSize;
        var py = stripY + 6f;

        if (ButtonRenderer.DrawButton(drawList, string.Empty,
                px, py, RefreshButtonSize, RefreshButtonSize,
                isPrimary: false, enabled: !isLoading))
        {
            events.Add(new HolySitesEvent.RefreshClicked());
        }

        ChromeRenderer.DrawRefreshArrow(drawList,
            px + RefreshButtonSize / 2f,
            py + RefreshButtonSize / 2f,
            RefreshButtonSize - 6f,
            ColorPalette.LightText);
    }

    private static float DrawIntro(ImDrawListPtr drawList, string civilizationName,
        float x, float y, float width)
    {
        var text = LocalizationService.Instance.Get(
            LocalizationKeys.UI_CIVILIZATION_HOLYSITES_INTRO, civilizationName);
        TextRenderer.DrawInfoText(drawList, text, x, y, width, Body, ColorPalette.White);
        var introHeight = TextRenderer.MeasureWrappedHeight(text, width, Body);
        return y + (introHeight > 0 ? introHeight : IntroLineHeight) + IntroBottomSpacing;
    }

    private static float DrawOrders(
        CivilizationHolySitesViewModel viewModel,
        ImDrawListPtr drawList,
        float x, float y, float width,
        List<HolySitesEvent> events)
    {
        if (viewModel.SitesByReligion.Count == 0) return y;

        var sortedReligions = viewModel.SitesByReligion
            .OrderBy(kvp => viewModel.ReligionNames.GetValueOrDefault(kvp.Key, "Unknown"),
                StringComparer.CurrentCultureIgnoreCase)
            .ToList();

        var currentY = y;
        for (var i = 0; i < sortedReligions.Count; i++)
        {
            var (religionUID, sites) = sortedReligions[i];
            var religionName = viewModel.ReligionNames.GetValueOrDefault(religionUID, "Unknown Order");
            var isExpanded = viewModel.ExpandedReligions.Contains(religionUID);

            currentY = DrawOrderHeader(drawList, religionUID, religionName, sites.Count,
                isExpanded, x, currentY, width, events);

            if (isExpanded)
            {
                var domain = DomainHelper.ParseDeityType(
                    viewModel.ReligionDomains.GetValueOrDefault(religionUID, string.Empty));
                var sortedSites = sites
                    .OrderBy(s => s.CreationDate)
                    .ToList();

                foreach (var site in sortedSites)
                {
                    currentY = DrawSiteRow(drawList, site, domain, x, currentY, width, events);
                }
            }

            if (i < sortedReligions.Count - 1)
                currentY = DrawPerOrderDivider(drawList, x, currentY, width);
        }

        return currentY;
    }

    private static float DrawOrderHeader(
        ImDrawListPtr drawList,
        string religionUID,
        string religionName,
        int siteCount,
        bool isExpanded,
        float x, float y, float width,
        List<HolySitesEvent> events)
    {
        var rowY = y + OrderHeaderTopSpacing;
        var rowMin = new Vector2(x, rowY);
        var rowMax = new Vector2(x + width, rowY + OrderHeaderHeight);

        var mousePos = ImGui.GetMousePos();
        var isHovered = mousePos.X >= rowMin.X && mousePos.X <= rowMax.X
                                              && mousePos.Y >= rowMin.Y && mousePos.Y <= rowMax.Y;
        if (isHovered)
        {
            var hoverColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.Gold * 0.10f);
            drawList.AddRectFilled(rowMin, rowMax, hoverColor, 2f);
            ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
            if (ImGui.IsMouseClicked(ImGuiMouseButton.Left))
                events.Add(new HolySitesEvent.ReligionToggled(religionUID));
        }

        var caretCx = x + OrderLeftPadding + CaretSize / 2f;
        var caretCy = rowY + OrderHeaderHeight / 2f;
        ChromeRenderer.DrawChevron(drawList, caretCx, caretCy, CaretSize,
            isExpanded ? ChromeRenderer.ChevronDirection.Down : ChromeRenderer.ChevronDirection.Right,
            ColorPalette.Gold);

        var textX = x + OrderLeftPadding + CaretSize + CaretToTextGap;
        var textY = rowY + (OrderHeaderHeight - SubsectionLabel) / 2f;

        var countText = LocalizationService.Instance.Get(
            siteCount == 1
                ? LocalizationKeys.UI_CIVILIZATION_HOLYSITES_COUNT_ONE
                : LocalizationKeys.UI_CIVILIZATION_HOLYSITES_COUNT_MANY,
            siteCount);

        var leaderWidth = (x + width) - textX;
        ChromeRenderer.DrawLeader(drawList, religionName, countText,
            textX, textY, leaderWidth,
            labelColor: ColorPalette.White,
            valueColor: ColorPalette.Grey);

        return rowY + OrderHeaderHeight + OrderHeaderBottomSpacing;
    }

    private static float DrawSiteRow(
        ImDrawListPtr drawList,
        HolySiteResponsePacket.HolySiteInfo site,
        DeityDomain domain,
        float x, float y, float width,
        List<HolySitesEvent> events)
    {
        var rowMin = new Vector2(x + SiteRowLeftPadding, y);
        var rowMax = new Vector2(x + width,
            y + SiteNameLineHeight + SiteClaimLineHeight + SiteRowBottomSpacing);

        var mousePos = ImGui.GetMousePos();
        var isHovered = mousePos.X >= rowMin.X && mousePos.X <= rowMax.X
                                              && mousePos.Y >= rowMin.Y && mousePos.Y <= rowMax.Y;
        if (isHovered)
        {
            var hoverColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.Gold * 0.08f);
            drawList.AddRectFilled(rowMin, rowMax, hoverColor, 2f);
            ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
            if (ImGui.IsMouseClicked(ImGuiMouseButton.Left))
                events.Add(new HolySitesEvent.SiteSelected(site.SiteUID));
        }

        var glyphCy = y + SiteNameLineHeight / 2f;
        var glyphMin = new Vector2(x + SiteRowLeftPadding, glyphCy - SiteGlyphSize / 2f);
        var glyphMax = new Vector2(x + SiteRowLeftPadding + SiteGlyphSize, glyphCy + SiteGlyphSize / 2f);
        DomainGlyphRenderer.Draw(drawList, domain, glyphMin, glyphMax);

        var nameX = x + SiteRowLeftPadding + SiteGlyphSize + SiteGlyphToTextGap;
        var nameY = y + (SiteNameLineHeight - Body) / 2f;
        var coordsText = $"({site.CenterX}, {site.CenterY}, {site.CenterZ})";
        var leaderWidth = (x + width) - nameX;
        ChromeRenderer.DrawLeader(drawList, site.SiteName, coordsText,
            nameX, nameY, leaderWidth,
            labelColor: ColorPalette.White,
            valueColor: ColorPalette.Grey);

        var claimDate = site.CreationDate == default
            ? string.Empty
            : site.CreationDate.ToLocalTime().ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        if (!string.IsNullOrEmpty(claimDate))
        {
            var claimText = LocalizationService.Instance.Get(
                LocalizationKeys.UI_CIVILIZATION_HOLYSITES_CLAIMED_ON, claimDate);
            drawList.AddText(ImGui.GetFont(), Secondary,
                new Vector2(nameX + SiteClaimIndent, y + SiteNameLineHeight),
                ImGui.ColorConvertFloat4ToU32(ColorPalette.Grey),
                claimText);
        }

        return rowMax.Y;
    }

    private static float DrawPerOrderDivider(ImDrawListPtr drawList, float x, float y, float width)
    {
        var inset = width * 0.30f;
        var dividerY = y + 4f;
        ChromeRenderer.DrawDivider(drawList,
            x + inset, dividerY, width - inset * 2f,
            ColorPalette.Gold * 0.40f);
        return y + PerOrderDividerHeight;
    }

    private static float DrawOrnateDivider(ImDrawListPtr drawList, float x, float y, float width)
    {
        var dividerY = y + DividerYPadding;
        ChromeRenderer.DrawDividerOrnate(drawList, x, dividerY, width);
        return y + DividerHeight;
    }

    private static void DrawClosingLine(ImDrawListPtr drawList, float x, float y, float width)
    {
        var text = LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_HOLYSITES_FOOTER_CLOSING);
        var size = ImGui.CalcTextSize(text);
        var textX = x + (width - size.X) / 2f;
        drawList.AddText(ImGui.GetFont(), Body, new Vector2(textX, y),
            ImGui.ColorConvertFloat4ToU32(ColorPalette.Grey),
            text);
    }

    private static float ComputeContentHeight(CivilizationHolySitesViewModel viewModel)
    {
        var h = 0f;
        h += PaneHeaderRenderer.TotalHeight;
        h += IntroLineHeight + IntroBottomSpacing;
        h += DividerHeight;
        if (viewModel.SitesByReligion.Count > 0)
        {
            var orderCount = viewModel.SitesByReligion.Count;
            h += orderCount * (OrderHeaderTopSpacing + OrderHeaderHeight + OrderHeaderBottomSpacing);
            h += (orderCount - 1) * PerOrderDividerHeight;
            foreach (var (uid, sites) in viewModel.SitesByReligion)
            {
                if (viewModel.ExpandedReligions.Contains(uid))
                {
                    h += sites.Count *
                         (SiteNameLineHeight + SiteClaimLineHeight + SiteRowBottomSpacing);
                }
            }
        }
        h += DividerHeight;
        h += ClosingLineTopSpacing + ClosingLineHeight;
        return h;
    }

    private static void DrawCenteredStateText(
        ImDrawListPtr drawList, string text, float x, float y, float width, float height, Vector4 color)
    {
        var size = ImGui.CalcTextSize(text);
        var pos = new Vector2(x + (width - size.X) / 2f, y + (height - size.Y) / 2f);
        drawList.AddText(ImGui.GetFont(), SubsectionLabel, pos,
            ImGui.ColorConvertFloat4ToU32(color), text);
    }
}
