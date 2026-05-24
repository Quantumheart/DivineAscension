using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using DivineAscension.Constants;
using DivineAscension.GUI.Events.Religion;
using DivineAscension.GUI.Models.Religion.Browse;
using DivineAscension.GUI.UI.Components.Buttons;
using DivineAscension.GUI.UI.Components.Inputs;
using DivineAscension.GUI.UI.Components.Lists;
using DivineAscension.GUI.UI.Renderers.Components;
using DivineAscension.GUI.UI.Renderers.Utilities;
using DivineAscension.GUI.UI.Utilities;
using DivineAscension.Network;
using DivineAscension.Services;
using ImGuiNET;
using static DivineAscension.GUI.UI.Utilities.FontSizes;

namespace DivineAscension.GUI.UI.Renderers.Religion;

/// <summary>
///     Pure renderer for the "Of Other Orders" ledger chapter (#314).
///     Serif chapter title and prose intro at the top, inline link-style
///     deity filter sub-index with a right-aligned refresh glyph, ornamental
///     divider, then ledger rows (✦ Name · · · Domain · N souls ▸) and a
///     centered counter line. Tabular column header is gone; row click
///     opens detail via the existing Selected event.
/// </summary>
[ExcludeFromCodeCoverage]
internal static class ReligionBrowseRenderer
{
    private const float ProseLineHeight = 18f;
    private const float ProseBottomSpacing = 12f;
    private const float SearchRowHeight = 26f;
    private const float SearchRowBottomSpacing = 6f;
    private const float FilterRowHeight = 26f;
    private const float FilterRowBottomSpacing = 8f;
    private const float FilterChipGap = 10f;
    private const float FilterChipPaddingX = 8f;
    private const float ActiveMarkerSize = 4f;
    private const float ActiveMarkerGap = 6f;
    private const float RefreshGlyphSize = 24f;
    private const float DividerHeight = 18f;
    private const float DividerYPadding = 6f;
    private const float RowHeight = 28f;
    private const float RowSpacing = 4f;
    private const float RowOrnamentSize = 5f;
    private const float RowOrnamentGap = 10f;
    private const float ChevronSize = 10f;
    private const float ChevronGap = 8f;
    private const float CounterTopSpacing = 8f;
    private const float CounterHeight = 22f;
    private const float ScrollbarWidth = 16f;

    public static ReligionBrowseRenderResult Draw(
        ReligionBrowseViewModel viewModel,
        ImDrawListPtr drawList)
    {
        var events = new List<BrowseEvent>();
        var x = viewModel.X;
        var y = viewModel.Y;
        var width = viewModel.Width;
        var height = viewModel.Height;

        if (viewModel.IsLoading && viewModel.Religions.Count == 0)
        {
            DrawCenteredStateText(drawList,
                LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_BROWSE_LOADING),
                x, y, width, height);
            return new ReligionBrowseRenderResult(events, null, height);
        }

        // === SCROLL CONTAINER ===
        var contentHeightEstimate = ComputeContentHeight(viewModel);
        var maxScroll = MathF.Max(0f, contentHeightEstimate - height);

        var mousePos = ImGui.GetMousePos();
        var isHover = mousePos.X >= x && mousePos.X <= x + width &&
                      mousePos.Y >= y && mousePos.Y <= y + height;
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
                    events.Add(new BrowseEvent.ScrollChanged(newScrollY));
                }
            }
        }

        drawList.PushClipRect(new Vector2(x, y), new Vector2(x + width, y + height), true);

        // === TITLE STRIP (shared chapter chrome) ===
        var strip = ChapterStripRenderer.Draw(drawList, x, y, width, scrollY,
            LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_BROWSE_CHAPTER_TITLE));
        var contentWidth = strip.ContentWidth;
        var currentY = strip.BodyY;

        // === PROSE INTRO ===
        currentY = DrawProseIntro(viewModel, drawList, x, currentY, contentWidth);

        // === TYPEAHEAD SEARCH ===
        currentY = DrawSearchRow(viewModel, drawList, x, currentY, contentWidth, events);

        // === FILTER SUB-INDEX + REFRESH ===
        currentY = DrawFilterRow(viewModel, drawList, x, currentY, contentWidth, events);

        // === ORNAMENTAL DIVIDER ===
        currentY = DrawDivider(drawList, x, currentY, contentWidth);

        // === LEDGER LIST ===
        ReligionListResponsePacket.ReligionInfo? hovered = null;
        currentY = DrawList(viewModel, drawList, x, currentY, contentWidth, events, ref hovered);

        // === COUNTER LINE ===
        if (viewModel.Religions.Count > 0)
        {
            currentY += CounterTopSpacing;
            DrawCounterLine(viewModel.Religions.Count, x, currentY, contentWidth, drawList);
        }

        drawList.PopClipRect();

        if (contentHeightEstimate > height)
            Scrollbar.Draw(drawList, x + width - ScrollbarWidth, y, ScrollbarWidth, height, scrollY, maxScroll);

        return new ReligionBrowseRenderResult(events, hovered, height);
    }

    private static float DrawProseIntro(
        ReligionBrowseViewModel vm,
        ImDrawListPtr drawList,
        float x, float y, float width)
    {
        var count = vm.Religions.Count;
        string prose;
        if (count <= 0)
        {
            prose = LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_BROWSE_INTRO_MANY, 0);
        }
        else if (count == 1)
        {
            prose = LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_BROWSE_INTRO_ONE);
        }
        else
        {
            prose = LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_BROWSE_INTRO_MANY, count);
        }

        TextRenderer.DrawInfoText(drawList, prose, x, y, width, Body, ColorPalette.White);
        var lines = TextRenderer.MeasureWrappedHeight(prose, width, Body);
        return y + (lines > 0 ? lines : ProseLineHeight) + ProseBottomSpacing;
    }

    private static float DrawSearchRow(
        ReligionBrowseViewModel vm,
        ImDrawListPtr drawList,
        float x, float y, float width,
        List<BrowseEvent> events)
    {
        var placeholder = LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_BROWSE_SEARCH_PLACEHOLDER);
        var updated = TextInput.Draw(drawList, "##religionBrowseSearch", vm.SearchText ?? string.Empty,
            x, y, width, SearchRowHeight, placeholder, maxLength: 64);
        if (updated != (vm.SearchText ?? string.Empty))
            events.Add(new BrowseEvent.SearchTextChanged(updated));
        return y + SearchRowHeight + SearchRowBottomSpacing;
    }

    private static float DrawFilterRow(
        ReligionBrowseViewModel vm,
        ImDrawListPtr drawList,
        float x, float y, float width,
        List<BrowseEvent> events)
    {
        var label = LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_BROWSE_FILTER_BY_DEITY);
        var labelSize = ImGui.CalcTextSize(label);
        var labelColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.Grey);
        var rowCenterY = y + FilterRowHeight / 2f;
        drawList.AddText(new Vector2(x, rowCenterY - labelSize.Y / 2f), labelColor, label);

        var cursor = x + labelSize.X + 16f;

        // Refresh glyph anchored at right edge of contentWidth (scrollbar gutter
        // already reserved by the caller).
        var refreshX = x + width - RefreshGlyphSize;
        var refreshY = y + (FilterRowHeight - RefreshGlyphSize) / 2f;
        var chipLimitX = refreshX - FilterChipGap;

        var current = string.IsNullOrEmpty(vm.CurrentDomainFilter) ? "All" : vm.CurrentDomainFilter;
        var mouse = ImGui.GetMousePos();

        for (var i = 0; i < vm.DomainFilters.Length; i++)
        {
            var filter = vm.DomainFilters[i];
            var isActive = filter == current;
            var chipLabel = LocalizeFilter(filter);
            var chipSize = ImGui.CalcTextSize(chipLabel);

            var markerWidth = isActive ? ActiveMarkerSize * 2f + ActiveMarkerGap : 0f;
            var chipWidth = FilterChipPaddingX + markerWidth + chipSize.X + FilterChipPaddingX;
            if (cursor + chipWidth > chipLimitX) break;

            var chipMinY = y + (FilterRowHeight - chipSize.Y) / 2f - 2f;
            var chipMaxY = chipMinY + chipSize.Y + 4f;
            var chipMin = new Vector2(cursor, chipMinY);
            var chipMax = new Vector2(cursor + chipWidth, chipMaxY);

            var isHover = mouse.X >= chipMin.X && mouse.X <= chipMax.X &&
                          mouse.Y >= chipMin.Y && mouse.Y <= chipMax.Y;

            if (isActive)
            {
                var underlineColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.Gold * 0.55f);
                drawList.AddLine(
                    new Vector2(chipMin.X + 2f, chipMaxY + 1f),
                    new Vector2(chipMax.X - 2f, chipMaxY + 1f),
                    underlineColor, 1f);
            }

            if (isHover)
                ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);

            var textX = cursor + FilterChipPaddingX;
            if (isActive)
            {
                var markerCx = textX + ActiveMarkerSize;
                var markerCy = (chipMin.Y + chipMax.Y) / 2f;
                ChromeRenderer.DrawDiamond(drawList, markerCx, markerCy, ActiveMarkerSize, ColorPalette.Gold);
                textX += ActiveMarkerSize * 2f + ActiveMarkerGap;
            }

            var textColor = isActive
                ? ColorPalette.White
                : (isHover ? ColorPalette.Gold : ColorPalette.Grey);
            drawList.AddText(ImGui.GetFont(), Body,
                new Vector2(textX, (chipMin.Y + chipMax.Y) / 2f - chipSize.Y / 2f),
                ImGui.ColorConvertFloat4ToU32(textColor), chipLabel);

            if (isHover && !isActive && ImGui.IsMouseClicked(ImGuiMouseButton.Left))
            {
                events.Add(new BrowseEvent.DeityFilterChanged(filter));
            }

            cursor += chipWidth + FilterChipGap;
        }

        // Refresh glyph — small dark button, circular-arrow primitive.
        DrawRefreshGlyph(drawList, refreshX, refreshY, RefreshGlyphSize, vm.IsLoading, events);

        return y + FilterRowHeight + FilterRowBottomSpacing;
    }

    private static string LocalizeFilter(string filter)
    {
        return filter == "All"
            ? LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_BROWSE_ALL)
            : filter;
    }

    private static void DrawRefreshGlyph(
        ImDrawListPtr drawList,
        float x, float y, float size, bool isLoading,
        List<BrowseEvent> events)
    {
        // Empty-label ButtonRenderer.DrawButton for hit + frame (proven
        // hit-test path used across panes); paint the ↻ primitive on top
        // since bundled font lacks Dingbats coverage. Disabled state →
        // enabled=false handles the dim/grey style automatically.
        var clicked = ButtonRenderer.DrawButton(drawList, string.Empty,
            x, y, size, size, isPrimary: false, enabled: !isLoading);
        DrawRefreshPrimitive(drawList, x, y, size);
        if (clicked) events.Add(new BrowseEvent.RefreshClicked());
    }

    private static void DrawRefreshPrimitive(ImDrawListPtr drawList, float x, float y, float size)
    {
        var cx = x + size / 2f;
        var cy = y + size / 2f;
        var r = size * 0.30f;
        var ink = ImGui.ColorConvertFloat4ToU32(ColorPalette.LightText);
        const int segments = 16;
        var arcStart = -MathF.PI / 2f;
        var arcSweep = MathF.PI * 1.5f;
        Vector2 prev = default;
        for (var i = 0; i <= segments; i++)
        {
            var t = i / (float)segments;
            var a = arcStart + arcSweep * t;
            var p = new Vector2(cx + MathF.Cos(a) * r, cy + MathF.Sin(a) * r);
            if (i > 0) drawList.AddLine(prev, p, ink, 1.5f);
            prev = p;
        }
        var endA = arcStart + arcSweep;
        var endP = new Vector2(cx + MathF.Cos(endA) * r, cy + MathF.Sin(endA) * r);
        var tipA = new Vector2(endP.X - r * 0.55f, endP.Y - r * 0.20f);
        var tipB = new Vector2(endP.X - r * 0.55f, endP.Y + r * 0.55f);
        drawList.AddTriangleFilled(endP, tipA, tipB, ink);
    }

    private static float DrawDivider(ImDrawListPtr drawList, float x, float y, float width)
    {
        var dividerY = y + DividerYPadding;
        ChromeRenderer.DrawDivider(drawList, x, dividerY, width);
        return y + DividerHeight;
    }

    private static float DrawList(
        ReligionBrowseViewModel vm,
        ImDrawListPtr drawList,
        float x, float y, float width,
        List<BrowseEvent> events,
        ref ReligionListResponsePacket.ReligionInfo? hovered)
    {
        if (vm.Religions.Count == 0)
        {
            var searchActive = !string.IsNullOrWhiteSpace(vm.SearchText);
            var emptyKey = searchActive
                ? LocalizationKeys.UI_RELIGION_BROWSE_EMPTY_FILTERED
                : LocalizationKeys.UI_RELIGION_BROWSE_EMPTY_CHAPTER;
            var emptyText = LocalizationService.Instance.Get(emptyKey);
            TextRenderer.DrawInfoText(drawList, emptyText, x, y, width, Body, ColorPalette.Grey);
            var measured = TextRenderer.MeasureWrappedHeight(emptyText, width, Body);
            return y + (measured > 0 ? measured : ProseLineHeight) + 8f;
        }

        var rowY = y;
        var mouse = ImGui.GetMousePos();

        for (var i = 0; i < vm.Religions.Count; i++)
        {
            var religion = vm.Religions[i];
            var rowMin = new Vector2(x, rowY);
            var rowMax = new Vector2(x + width, rowY + RowHeight);
            var isHover = mouse.X >= rowMin.X && mouse.X <= rowMax.X &&
                          mouse.Y >= rowMin.Y && mouse.Y <= rowMax.Y;

            if (isHover)
            {
                hovered = religion;
                ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
                var hoverBg = ImGui.ColorConvertFloat4ToU32(ColorPalette.LightBrown * 0.25f);
                drawList.AddRectFilled(rowMin, rowMax, hoverBg, 2f);
            }

            DrawRow(drawList, religion, x, rowY, width);

            if (isHover && ImGui.IsMouseClicked(ImGuiMouseButton.Left))
            {
                events.Add(new BrowseEvent.Selected(religion.ReligionUID, vm.ScrollY));
            }

            rowY += RowHeight + RowSpacing;
        }

        return rowY;
    }

    private static void DrawRow(
        ImDrawListPtr drawList,
        ReligionListResponsePacket.ReligionInfo religion,
        float x, float y, float width)
    {
        var centerY = y + RowHeight / 2f;
        var bodyAscent = ImGui.CalcTextSize("M").Y;
        var textY = centerY - bodyAscent / 2f;

        // Diamond ornament at row head.
        var ornCx = x + RowOrnamentGap + RowOrnamentSize;
        ChromeRenderer.DrawDiamond(drawList, ornCx, centerY, RowOrnamentSize, ColorPalette.Gold);

        // Order name (rubric Gold for order title).
        var nameX = ornCx + RowOrnamentSize + RowOrnamentGap;
        var nameColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.Gold);
        drawList.AddText(ImGui.GetFont(), Body, new Vector2(nameX, textY), nameColor, religion.ReligionName);
        var nameSize = ImGui.CalcTextSize(religion.ReligionName);

        // Right edge: chevron, then "N souls", then "·", then domain — built right-to-left.
        var chevronCx = x + width - RowOrnamentGap - ChevronSize / 2f;
        ChromeRenderer.DrawChevron(drawList, chevronCx, centerY, ChevronSize,
            ChromeRenderer.ChevronDirection.Right, ColorPalette.Gold * 0.8f);

        var soulsKey = religion.MemberCount == 1
            ? LocalizationKeys.UI_RELIGION_BROWSE_SOULS_ONE
            : LocalizationKeys.UI_RELIGION_BROWSE_SOULS_MANY;
        var soulsText = religion.MemberCount == 1
            ? LocalizationService.Instance.Get(soulsKey)
            : LocalizationService.Instance.Get(soulsKey, religion.MemberCount);

        var soulsSize = ImGui.CalcTextSize(soulsText);
        var soulsX = chevronCx - ChevronSize / 2f - ChevronGap - soulsSize.X;
        drawList.AddText(ImGui.GetFont(), Body, new Vector2(soulsX, textY),
            ImGui.ColorConvertFloat4ToU32(ColorPalette.White), soulsText);

        const string sep = " · ";
        var sepSize = ImGui.CalcTextSize(sep);
        var sepX = soulsX - sepSize.X;
        drawList.AddText(ImGui.GetFont(), Body, new Vector2(sepX, textY),
            ImGui.ColorConvertFloat4ToU32(ColorPalette.Gold * 0.55f), sep);

        var domainText = religion.Domain;
        var domainSize = ImGui.CalcTextSize(domainText);
        var domainX = sepX - domainSize.X;
        drawList.AddText(ImGui.GetFont(), Body, new Vector2(domainX, textY),
            ImGui.ColorConvertFloat4ToU32(ColorPalette.White), domainText);

        // Dotted leaders between name end and domain start.
        const float padding = 6f;
        var leaderStart = nameX + nameSize.X + padding;
        var leaderEnd = domainX - padding;
        DrawLeaderDots(drawList, leaderStart, leaderEnd, textY);
    }

    private static void DrawLeaderDots(ImDrawListPtr drawList, float startX, float endX, float y)
    {
        const string dot = "·";
        var dotWidth = ImGui.CalcTextSize(dot).X;
        if (dotWidth <= 0f) return;
        var step = dotWidth * 2f;
        var gap = endX - startX;
        if (gap <= 0f) return;
        var count = (int)(gap / step);
        if (count <= 0) return;
        var totalWidth = count * step - (step - dotWidth);
        var dotsX = startX + (gap - totalWidth) / 2f;
        var color = ImGui.ColorConvertFloat4ToU32(ColorPalette.Gold * 0.45f);
        for (var i = 0; i < count; i++)
            drawList.AddText(new Vector2(dotsX + i * step, y), color, dot);
    }

    private static void DrawCounterLine(
        int count, float x, float y, float width, ImDrawListPtr drawList)
    {
        var key = count == 1
            ? LocalizationKeys.UI_RELIGION_BROWSE_COUNTER_ONE
            : LocalizationKeys.UI_RELIGION_BROWSE_COUNTER_MANY;
        var text = LocalizationService.Instance.Get(key, count, count);

        var textSize = ImGui.CalcTextSize(text);
        var centerX = x + width / 2f;
        var textX = centerX - textSize.X / 2f;
        var color = ImGui.ColorConvertFloat4ToU32(ColorPalette.Grey);

        var lineColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.Gold * 0.45f);
        var lineY = y + textSize.Y / 2f;
        const float sideGap = 12f;
        if (textX - sideGap > x)
            drawList.AddLine(new Vector2(x + sideGap, lineY),
                new Vector2(textX - sideGap, lineY), lineColor, 1f);
        if (textX + textSize.X + sideGap < x + width)
            drawList.AddLine(new Vector2(textX + textSize.X + sideGap, lineY),
                new Vector2(x + width - sideGap, lineY), lineColor, 1f);

        drawList.AddText(new Vector2(textX, y), color, text);
    }

    private static float ComputeContentHeight(ReligionBrowseViewModel vm)
    {
        var h = ChapterStripRenderer.TopPadding;
        h += PaneHeaderRenderer.TotalHeight;
        h += ProseLineHeight * 2f + ProseBottomSpacing;
        h += SearchRowHeight + SearchRowBottomSpacing;
        h += FilterRowHeight + FilterRowBottomSpacing;
        h += DividerHeight;
        if (vm.Religions.Count == 0)
            h += ProseLineHeight + 8f;
        else
            h += vm.Religions.Count * (RowHeight + RowSpacing);
        h += CounterTopSpacing + CounterHeight;
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
}
