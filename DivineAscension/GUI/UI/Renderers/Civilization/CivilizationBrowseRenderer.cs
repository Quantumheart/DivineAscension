using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using DivineAscension.Constants;
using DivineAscension.GUI.Events.Civilization;
using DivineAscension.GUI.Models.Civilization.Browse;
using DivineAscension.GUI.UI.Components.Lists;
using DivineAscension.GUI.UI.Renderers.Utilities;
using DivineAscension.GUI.UI.Utilities;
using DivineAscension.Services;
using ImGuiNET;
using static DivineAscension.GUI.UI.Utilities.FontSizes;

namespace DivineAscension.GUI.UI.Renderers.Civilization;

/// <summary>
///     Pure renderer for the "Of Other Realms" ledger chapter (#323).
///     Serif title strip, auto-generated prose intro, right-aligned refresh
///     glyph, ornamental divider, then two-line ledger rows (✦ Name · · ·
///     N Orders ▸ on line 1, indented quoted description on line 2) and a
///     centered counter. The deity filter is intentionally absent in v1.
/// </summary>
[ExcludeFromCodeCoverage]
internal static class CivilizationBrowseRenderer
{
    private const float ProseLineHeight = 18f;
    private const float ProseBottomSpacing = 12f;
    private const float RefreshRowHeight = 26f;
    private const float RefreshRowBottomSpacing = 8f;
    private const float RefreshGlyphSize = 24f;
    private const float DividerHeight = 18f;
    private const float DividerYPadding = 6f;
    private const float RowLine1Height = 26f;
    private const float RowLine2Spacing = 2f;
    private const float RowGap = 10f;
    private const float DescriptionIndent = 24f;
    private const float RowOrnamentSize = 5f;
    private const float RowOrnamentGap = 10f;
    private const float ChevronSize = 10f;
    private const float ChevronGap = 8f;
    private const float CounterTopSpacing = 8f;
    private const float CounterHeight = 22f;
    private const float ScrollbarWidth = 16f;

    public static CivilizationBrowseRenderResult Draw(
        CivilizationBrowseViewModel viewModel,
        ImDrawListPtr drawList)
    {
        var events = new List<BrowseEvent>();
        var x = viewModel.X;
        var y = viewModel.Y;
        var width = viewModel.Width;
        var height = viewModel.Height;

        if (viewModel.IsLoading && viewModel.Civilizations.Count == 0)
        {
            DrawCenteredStateText(drawList,
                LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_BROWSE_LOADING),
                x, y, width, height);
            return new CivilizationBrowseRenderResult(events, height);
        }

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

        var strip = ChapterStripRenderer.Draw(drawList, x, y, width, scrollY,
            LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_BROWSE_CHAPTER_TITLE));
        var contentWidth = strip.ContentWidth;
        var currentY = strip.BodyY;

        currentY = DrawProseIntro(viewModel, drawList, x, currentY, contentWidth);
        currentY = DrawRefreshRow(viewModel, drawList, x, currentY, contentWidth, events);
        currentY = DrawDivider(drawList, x, currentY, contentWidth);
        currentY = DrawList(viewModel, drawList, x, currentY, contentWidth, events);

        if (viewModel.Civilizations.Count > 0)
        {
            currentY += CounterTopSpacing;
            DrawCounterLine(viewModel.Civilizations.Count, x, currentY, contentWidth, drawList);
        }

        drawList.PopClipRect();

        if (contentHeightEstimate > height)
            Scrollbar.Draw(drawList, x + width - ScrollbarWidth, y, ScrollbarWidth, height, scrollY, maxScroll);

        return new CivilizationBrowseRenderResult(events, height);
    }

    private static float DrawProseIntro(
        CivilizationBrowseViewModel vm,
        ImDrawListPtr drawList,
        float x, float y, float width)
    {
        var count = vm.Civilizations.Count;
        string prose;
        if (count <= 0)
            prose = LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_BROWSE_INTRO_MANY, 0);
        else if (count == 1)
            prose = LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_BROWSE_INTRO_ONE);
        else
            prose = LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_BROWSE_INTRO_MANY, count);

        TextRenderer.DrawInfoText(drawList, prose, x, y, width, Body, ColorPalette.White);
        var lines = TextRenderer.MeasureWrappedHeight(prose, width, Body);
        return y + (lines > 0 ? lines : ProseLineHeight) + ProseBottomSpacing;
    }

    private static float DrawRefreshRow(
        CivilizationBrowseViewModel vm,
        ImDrawListPtr drawList,
        float x, float y, float width,
        List<BrowseEvent> events)
    {
        var refreshX = x + width - RefreshGlyphSize;
        var refreshY = y + (RefreshRowHeight - RefreshGlyphSize) / 2f;
        DrawRefreshGlyph(drawList, refreshX, refreshY, RefreshGlyphSize, vm.IsLoading, events);
        return y + RefreshRowHeight + RefreshRowBottomSpacing;
    }

    private static void DrawRefreshGlyph(
        ImDrawListPtr drawList,
        float x, float y, float size, bool isLoading,
        List<BrowseEvent> events)
    {
        var mouse = ImGui.GetMousePos();
        var min = new Vector2(x, y);
        var max = new Vector2(x + size, y + size);
        var hover = mouse.X >= min.X && mouse.X <= max.X &&
                    mouse.Y >= min.Y && mouse.Y <= max.Y;

        Vector4 bg;
        if (isLoading) bg = ColorPalette.DarkBrown * 0.5f;
        else if (hover) bg = ColorPalette.DarkBrown * 1.2f;
        else bg = ColorPalette.DarkBrown * 0.8f;

        drawList.AddRectFilled(min, max, ImGui.ColorConvertFloat4ToU32(bg), 4f);
        drawList.AddRect(min, max,
            ImGui.ColorConvertFloat4ToU32(isLoading ? ColorPalette.BorderColor : ColorPalette.Gold * 0.7f),
            4f, ImDrawFlags.None, 1.5f);

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

        if (hover && !isLoading)
        {
            ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
            if (ImGui.IsMouseClicked(ImGuiMouseButton.Left))
                events.Add(new BrowseEvent.RefreshClicked());
        }
    }

    private static float DrawDivider(ImDrawListPtr drawList, float x, float y, float width)
    {
        var dividerY = y + DividerYPadding;
        ChromeRenderer.DrawDivider(drawList, x, dividerY, width);
        return y + DividerHeight;
    }

    private static float DrawList(
        CivilizationBrowseViewModel vm,
        ImDrawListPtr drawList,
        float x, float y, float width,
        List<BrowseEvent> events)
    {
        if (vm.Civilizations.Count == 0)
        {
            var emptyText = LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_BROWSE_EMPTY_CHAPTER);
            TextRenderer.DrawInfoText(drawList, emptyText, x, y, width, Body, ColorPalette.Grey);
            var measured = TextRenderer.MeasureWrappedHeight(emptyText, width, Body);
            return y + (measured > 0 ? measured : ProseLineHeight) + 8f;
        }

        var rowY = y;
        var mouse = ImGui.GetMousePos();

        for (var i = 0; i < vm.Civilizations.Count; i++)
        {
            var civ = vm.Civilizations[i];
            var hasDescription = !string.IsNullOrWhiteSpace(civ.Description);
            var descTextX = x + DescriptionIndent;
            var descTextWidth = width - DescriptionIndent;
            var descHeight = hasDescription
                ? MathF.Max(TextRenderer.MeasureWrappedHeight($"\"{civ.Description}\"", descTextWidth, Secondary),
                    ProseLineHeight)
                : 0f;
            var rowHeight = RowLine1Height + (hasDescription ? RowLine2Spacing + descHeight : 0f);

            var rowMin = new Vector2(x, rowY);
            var rowMax = new Vector2(x + width, rowY + rowHeight);
            var isHover = mouse.X >= rowMin.X && mouse.X <= rowMax.X &&
                          mouse.Y >= rowMin.Y && mouse.Y <= rowMax.Y;

            if (isHover)
            {
                ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
                var hoverBg = ImGui.ColorConvertFloat4ToU32(ColorPalette.LightBrown * 0.25f);
                drawList.AddRectFilled(rowMin, rowMax, hoverBg, 2f);
            }

            DrawRowLine1(drawList, civ, x, rowY, width);

            if (hasDescription)
            {
                var descY = rowY + RowLine1Height + RowLine2Spacing;
                TextRenderer.DrawInfoText(drawList, $"\"{civ.Description}\"",
                    descTextX, descY, descTextWidth, Secondary, ColorPalette.Grey);
            }

            if (isHover && ImGui.IsMouseClicked(ImGuiMouseButton.Left))
                events.Add(new BrowseEvent.Selected(civ.CivId, vm.ScrollY));

            rowY += rowHeight + RowGap;
        }

        return rowY;
    }

    private static void DrawRowLine1(
        ImDrawListPtr drawList,
        Network.Civilization.CivilizationListResponsePacket.CivilizationInfo civ,
        float x, float y, float width)
    {
        var centerY = y + RowLine1Height / 2f;
        var bodyAscent = ImGui.CalcTextSize("M").Y;
        var textY = centerY - bodyAscent / 2f;

        var ornCx = x + RowOrnamentGap + RowOrnamentSize;
        ChromeRenderer.DrawDiamond(drawList, ornCx, centerY, RowOrnamentSize, ColorPalette.Gold);

        var nameX = ornCx + RowOrnamentSize + RowOrnamentGap;
        var nameColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.Gold);
        drawList.AddText(ImGui.GetFont(), Body, new Vector2(nameX, textY), nameColor, civ.Name);
        var nameSize = ImGui.CalcTextSize(civ.Name);

        var chevronCx = x + width - RowOrnamentGap - ChevronSize / 2f;
        ChromeRenderer.DrawChevron(drawList, chevronCx, centerY, ChevronSize,
            ChromeRenderer.ChevronDirection.Right, ColorPalette.Gold * 0.8f);

        var orderCount = civ.MemberReligionNames?.Count ?? 0;
        var ordersKey = orderCount == 1
            ? LocalizationKeys.UI_CIVILIZATION_BROWSE_ORDERS_ONE
            : LocalizationKeys.UI_CIVILIZATION_BROWSE_ORDERS_MANY;
        var ordersText = orderCount == 1
            ? LocalizationService.Instance.Get(ordersKey)
            : LocalizationService.Instance.Get(ordersKey, orderCount);

        var ordersSize = ImGui.CalcTextSize(ordersText);
        var ordersX = chevronCx - ChevronSize / 2f - ChevronGap - ordersSize.X;
        drawList.AddText(ImGui.GetFont(), Body, new Vector2(ordersX, textY),
            ImGui.ColorConvertFloat4ToU32(ColorPalette.White), ordersText);

        const float padding = 6f;
        var leaderStart = nameX + nameSize.X + padding;
        var leaderEnd = ordersX - padding;
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
            ? LocalizationKeys.UI_CIVILIZATION_BROWSE_COUNTER_ONE
            : LocalizationKeys.UI_CIVILIZATION_BROWSE_COUNTER_MANY;
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

    private static float ComputeContentHeight(CivilizationBrowseViewModel vm)
    {
        var h = ChapterStripRenderer.TopPadding;
        h += PaneHeaderRenderer.TotalHeight;
        h += ProseLineHeight * 2f + ProseBottomSpacing;
        h += RefreshRowHeight + RefreshRowBottomSpacing;
        h += DividerHeight;
        if (vm.Civilizations.Count == 0)
        {
            h += ProseLineHeight + 8f;
        }
        else
        {
            var contentWidth = vm.Width - ChapterStripRenderer.ScrollbarGutter;
            var descWidth = contentWidth - DescriptionIndent;
            foreach (var civ in vm.Civilizations)
            {
                h += RowLine1Height;
                if (!string.IsNullOrWhiteSpace(civ.Description))
                {
                    var dh = MathF.Max(
                        TextRenderer.MeasureWrappedHeight($"\"{civ.Description}\"", descWidth, Secondary),
                        ProseLineHeight);
                    h += RowLine2Spacing + dh;
                }
                h += RowGap;
            }
        }
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
