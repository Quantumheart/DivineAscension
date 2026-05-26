using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using DivineAscension.Constants;
using DivineAscension.GUI.Events.Civilization;
using DivineAscension.GUI.Models.Civilization.Leaderboard;
using DivineAscension.GUI.UI.Components.Buttons;
using DivineAscension.GUI.UI.Components.Lists;
using DivineAscension.GUI.UI.Renderers.Components;
using DivineAscension.GUI.UI.Renderers.Utilities;
using DivineAscension.GUI.UI.Utilities;
using DivineAscension.Services;
using ImGuiNET;
using static DivineAscension.GUI.UI.Utilities.FontSizes;

namespace DivineAscension.GUI.UI.Renderers.Civilization;

/// <summary>
///     Pure renderer for the Standing of Realms leaderboard chapter (#497,
///     #498). Title strip + refresh glyph, prose intro, then every realm
///     ranked by Standing as a plain row: position, name, tier label, score.
///     The viewer's own realm is pinned/highlighted, and a closing summary
///     line states where they stand among the rest.
/// </summary>
[ExcludeFromCodeCoverage]
internal static class CivilizationLeaderboardRenderer
{
    private const float DividerHeight = 18f;
    private const float DividerYPadding = 6f;
    private const float ProseBottomSpacing = 12f;
    private const float RowHeight = 24f;
    private const float RefreshGlyphSize = 22f;
    private const float ScrollbarWidth = 16f;
    private const float SummaryTopSpacing = 6f;

    public static CivilizationLeaderboardRenderResult Draw(
        CivilizationLeaderboardViewModel viewModel,
        ImDrawListPtr drawList)
    {
        var events = new List<LeaderboardEvent>();
        var x = viewModel.X;
        var y = viewModel.Y;
        var width = viewModel.Width;
        var height = viewModel.Height;

        if (viewModel.IsLoading)
        {
            DrawCenteredStateText(drawList,
                LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_LEADERBOARD_LOADING),
                x, y, width, height, ColorPalette.Grey);
            return new CivilizationLeaderboardRenderResult(events, height);
        }

        if (!string.IsNullOrEmpty(viewModel.ErrorMsg))
        {
            var errText = LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_LEADERBOARD_ERROR)
                .Replace("{0}", viewModel.ErrorMsg!);
            DrawCenteredStateText(drawList, errText, x, y, width, height, ColorPalette.ErrorRed);
            return new CivilizationLeaderboardRenderResult(events, height);
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
                    events.Add(new LeaderboardEvent.ScrollChanged(newScrollY));
                }
            }
        }

        drawList.PushClipRect(new Vector2(x, y), new Vector2(x + width, y + height), true);

        var strip = ChapterStripRenderer.Draw(drawList, x, y, width, scrollY,
            LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_LEADERBOARD_TITLE));
        var contentWidth = strip.ContentWidth;
        var currentY = strip.BodyY;

        DrawRefreshButton(viewModel, drawList, x, y - scrollY, contentWidth, events);

        currentY = DrawProseIntro(drawList, x, currentY, contentWidth);
        currentY = DrawDivider(drawList, x, currentY, contentWidth);

        if (viewModel.Entries.Count == 0)
        {
            drawList.PopClipRect();
            DrawCenteredStateText(drawList,
                LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_LEADERBOARD_EMPTY),
                x, currentY, width, height - (currentY - y), ColorPalette.Grey);
            return new CivilizationLeaderboardRenderResult(events, height);
        }

        foreach (var entry in viewModel.Entries)
        {
            var isViewer = viewModel.ViewerPosition > 0 && entry.Position == viewModel.ViewerPosition;
            var value = LocalizationService.Instance.Get(
                LocalizationKeys.UI_CIVILIZATION_LEADERBOARD_ROW_VALUE, entry.TierLabel, entry.Score);

            if (isViewer)
            {
                // Pin and highlight the viewer's own realm so it's always findable.
                var name = LocalizationService.Instance.Get(
                    LocalizationKeys.UI_CIVILIZATION_LEADERBOARD_SELF_ROW, entry.Name);
                var label = $"▸ {entry.Position}.  {name}";
                ChromeRenderer.DrawLeader(drawList, label, $"{value}  ◂", x, currentY, contentWidth,
                    labelColor: ColorPalette.Gold, valueColor: ColorPalette.Gold);
            }
            else
            {
                var label = $"{entry.Position}.  {entry.Name}";
                ChromeRenderer.DrawLeader(drawList, label, value, x, currentY, contentWidth,
                    valueColor: ColorPalette.Gold);
            }

            currentY += RowHeight;
        }

        currentY = DrawDivider(drawList, x, currentY, contentWidth);
        DrawStandingSummary(viewModel, drawList, x, currentY, contentWidth);

        drawList.PopClipRect();

        if (contentHeightEstimate > height)
            Scrollbar.Draw(drawList, x + width - ScrollbarWidth, y, ScrollbarWidth, height, scrollY, maxScroll);

        return new CivilizationLeaderboardRenderResult(events, height);
    }

    private static void DrawRefreshButton(
        CivilizationLeaderboardViewModel viewModel,
        ImDrawListPtr drawList,
        float paneX,
        float stripTopY,
        float contentWidth,
        List<LeaderboardEvent> events)
    {
        var bx = paneX + contentWidth - RefreshGlyphSize;
        var by = stripTopY + ChapterStripRenderer.TopPadding + 6f;
        if (ButtonRenderer.DrawButton(drawList, string.Empty,
                bx, by, RefreshGlyphSize, RefreshGlyphSize,
                isPrimary: false, enabled: !viewModel.IsLoading))
        {
            events.Add(new LeaderboardEvent.RefreshClicked());
        }
        ChromeRenderer.DrawRefreshArrow(drawList,
            bx + RefreshGlyphSize / 2f,
            by + RefreshGlyphSize / 2f,
            RefreshGlyphSize - 6f,
            ColorPalette.LightText);
    }

    private static float DrawProseIntro(
        ImDrawListPtr drawList,
        float x, float y, float width)
    {
        var prose = LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_LEADERBOARD_INTRO);
        TextRenderer.DrawInfoText(drawList, prose, x, y, width, Body, ColorPalette.White);
        var lines = TextRenderer.MeasureWrappedHeight(prose, width, Body);
        return y + (lines > 0 ? lines : Body + 6f) + ProseBottomSpacing;
    }

    private static float DrawDivider(ImDrawListPtr drawList, float x, float y, float width)
    {
        var dividerY = y + DividerYPadding;
        ChromeRenderer.DrawDividerOrnate(drawList, x, dividerY, width);
        return y + DividerHeight;
    }

    /// <summary>
    ///     The closing hook line: where the viewer's own realm stands among the
    ///     rest, or a graceful no-realm variant for players who belong to none.
    /// </summary>
    private static void DrawStandingSummary(
        CivilizationLeaderboardViewModel viewModel,
        ImDrawListPtr drawList, float x, float y, float width)
    {
        var text = viewModel.ViewerPosition > 0
            ? LocalizationService.Instance.Get(
                LocalizationKeys.UI_CIVILIZATION_LEADERBOARD_SUMMARY,
                ToRoman(viewModel.ViewerPosition), ToRoman(viewModel.TotalRealms))
            : LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_LEADERBOARD_SUMMARY_NONE);

        TextRenderer.DrawInfoText(drawList, text, x, y + SummaryTopSpacing, width, Body, ColorPalette.Gold);
    }

    private static float ComputeContentHeight(CivilizationLeaderboardViewModel viewModel)
    {
        var h = PaneHeaderRenderer.TotalHeight;
        h += 36f + ProseBottomSpacing; // prose intro (~2 lines)
        h += DividerHeight;
        h += viewModel.Entries.Count * RowHeight;
        h += DividerHeight; // closing divider
        h += SummaryTopSpacing + RowHeight; // standing summary line
        return h;
    }

    /// <summary>Roman numerals for the standing line (e.g. "IV among XII").</summary>
    private static string ToRoman(int n)
    {
        if (n <= 0) return n.ToString();

        var pairs = new (int Value, string Symbol)[]
        {
            (1000, "M"), (900, "CM"), (500, "D"), (400, "CD"),
            (100, "C"), (90, "XC"), (50, "L"), (40, "XL"),
            (10, "X"), (9, "IX"), (5, "V"), (4, "IV"), (1, "I")
        };

        var sb = new System.Text.StringBuilder();
        foreach (var (value, symbol) in pairs)
            while (n >= value)
            {
                sb.Append(symbol);
                n -= value;
            }

        return sb.ToString();
    }

    private static void DrawCenteredStateText(
        ImDrawListPtr drawList, string text,
        float x, float y, float width, float height, Vector4 color)
    {
        var size = ImGui.CalcTextSize(text);
        var pos = new Vector2(x + (width - size.X) / 2f, y + (height - size.Y) / 2f);
        drawList.AddText(pos, ImGui.ColorConvertFloat4ToU32(color), text);
    }
}
