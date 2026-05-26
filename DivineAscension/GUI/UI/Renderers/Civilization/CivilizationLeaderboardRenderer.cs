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
using DivineAscension.Models.Enum;
using DivineAscension.Services;
using ImGuiNET;
using static DivineAscension.GUI.UI.Utilities.FontSizes;

namespace DivineAscension.GUI.UI.Renderers.Civilization;

/// <summary>
///     Pure renderer for the Standing of Realms leaderboard chapter (#497–#500).
///     Title strip + refresh glyph, prose intro, board selector, then every realm
///     as a ranked row: Roman rank, heraldic ethos glyph, name, tier label, a
///     proportional score bar, and the score. The viewer's own realm is pinned
///     (<c>▸ … ◂</c>) and highlighted in gold in its true ranked position, and a
///     closing summary line states where they stand among the rest.
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
    private const float SelectorRowHeight = 28f;
    private const float SelectorRowBottomSpacing = 6f;
    private const float SelectorChipGap = 18f;
    private const float SelectorChipPaddingX = 8f;
    private const float RowMarkerWidth = 16f; // ▸/◂ pin gutter on the viewer row
    private const float RowRankWidth = 46f;    // "VIII." rank column
    private const float RowGlyphSize = 16f;    // heraldic ethos mark
    private const float RowGlyphGap = 8f;
    private const float RowBarWidth = 72f;
    private const float RowBarHeight = 8f;
    private const float RowColumnGap = 12f;

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
        currentY = DrawBoardSelector(viewModel, drawList, x, currentY, contentWidth, events);
        currentY = DrawDivider(drawList, x, currentY, contentWidth);

        if (viewModel.Entries.Count == 0)
        {
            drawList.PopClipRect();
            DrawCenteredStateText(drawList,
                LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_LEADERBOARD_EMPTY),
                x, currentY, width, height - (currentY - y), ColorPalette.Grey);
            return new CivilizationLeaderboardRenderResult(events, height);
        }

        // Score bars are proportional to the board's top score. Entries arrive
        // sorted highest-first, but guard against an empty/zero top all the same.
        var maxScore = 0;
        foreach (var e in viewModel.Entries)
            if (e.Score > maxScore)
                maxScore = e.Score;

        foreach (var entry in viewModel.Entries)
        {
            var isViewer = viewModel.ViewerPosition > 0 && entry.Position == viewModel.ViewerPosition;
            DrawRow(drawList, entry, isViewer, maxScore, x, currentY, contentWidth);
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

    /// <summary>
    ///     One ranked row: pin gutter, Roman rank, ethos glyph, realm name on the
    ///     left; tier label, proportional score bar, and score on the right. The
    ///     viewer's own realm is bracketed (<c>▸ … ◂</c>), uppercased, and gold so
    ///     it stays findable in its true ranked position.
    /// </summary>
    private static void DrawRow(
        ImDrawListPtr drawList,
        Network.Civilization.LeaderboardResponsePacket.LeaderboardEntry entry,
        bool isViewer, int maxScore,
        float x, float y, float width)
    {
        var font = ImGui.GetFont();
        var rowMidY = y + RowHeight / 2f;
        var textY = rowMidY - Body / 2f;
        var nameColor = ImGui.ColorConvertFloat4ToU32(isViewer ? ColorPalette.Gold : ColorPalette.White);
        var glyphColor = isViewer ? ColorPalette.Gold : ColorPalette.Grey;

        // Pin gutter — bracket the viewer's realm so it reads as "you are here".
        // Drawn as chevrons rather than glyphs: ▸/◂ aren't in the ImGui font.
        if (isViewer)
        {
            const float pinSize = 9f;
            ChromeRenderer.DrawChevron(drawList, x + pinSize * 0.6f, rowMidY, pinSize,
                ChromeRenderer.ChevronDirection.Right, ColorPalette.Gold);
            ChromeRenderer.DrawChevron(drawList, x + width - pinSize * 0.6f, rowMidY, pinSize,
                ChromeRenderer.ChevronDirection.Left, ColorPalette.Gold);
        }

        var leftX = x + RowMarkerWidth;

        // Roman rank.
        drawList.AddText(font, Body, new Vector2(leftX, textY),
            ImGui.ColorConvertFloat4ToU32(ColorPalette.Grey), $"{ToRoman(entry.Position)}.");

        // Ethos glyph.
        var glyphX = leftX + RowRankWidth;
        EthosGlyphRenderer.Draw(drawList, (CivilizationEthos)entry.Ethos,
            new Vector2(glyphX, rowMidY - RowGlyphSize / 2f),
            new Vector2(glyphX + RowGlyphSize, rowMidY + RowGlyphSize / 2f),
            glyphColor);

        // Realm name (uppercased + prefixed for the viewer).
        var nameX = glyphX + RowGlyphSize + RowGlyphGap;
        var name = isViewer
            ? LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_LEADERBOARD_SELF_ROW, entry.Name)
            : entry.Name;
        drawList.AddText(font, Body, new Vector2(nameX, textY), nameColor, name);

        // Right block: score (right-aligned), bar, then tier label.
        var rightEdge = x + width - (isViewer ? RowMarkerWidth : 0f);
        var scoreText = entry.Score.ToString();
        var scoreSize = ImGui.CalcTextSize(scoreText);
        var scoreX = rightEdge - scoreSize.X;
        drawList.AddText(font, Body, new Vector2(scoreX, textY),
            ImGui.ColorConvertFloat4ToU32(ColorPalette.Gold), scoreText);

        var barRight = scoreX - RowColumnGap;
        var barLeft = barRight - RowBarWidth;
        DrawScoreBar(drawList, barLeft, rowMidY - RowBarHeight / 2f, RowBarWidth, RowBarHeight,
            maxScore <= 0 ? 0f : (float)entry.Score / maxScore, isViewer);

        // Tier label, right-aligned against the bar.
        var tierSize = ImGui.CalcTextSize(entry.TierLabel);
        drawList.AddText(font, Body, new Vector2(barLeft - RowColumnGap - tierSize.X, textY),
            ImGui.ColorConvertFloat4ToU32(ColorPalette.Grey), entry.TierLabel);
    }

    /// <summary>A proportional score bar: faint track with a filled gold portion.</summary>
    private static void DrawScoreBar(
        ImDrawListPtr drawList, float x, float y, float width, float height, float fraction, bool isViewer)
    {
        fraction = Math.Clamp(fraction, 0f, 1f);
        var trackCol = ImGui.ColorConvertFloat4ToU32(ColorPalette.BorderColor);
        var fillCol = ImGui.ColorConvertFloat4ToU32(isViewer ? ColorPalette.Gold : ColorPalette.Grey);

        drawList.AddRectFilled(new Vector2(x, y), new Vector2(x + width, y + height),
            ImGui.ColorConvertFloat4ToU32(ColorPalette.BorderColor * 0.4f), 2f);
        drawList.AddRect(new Vector2(x, y), new Vector2(x + width, y + height), trackCol, 2f);
        if (fraction > 0f)
            drawList.AddRectFilled(new Vector2(x, y), new Vector2(x + width * fraction, y + height), fillCol, 2f);
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

    /// <summary>
    ///     The board selector row: each board as a clickable label, the active
    ///     one bracketed in gold. Clicking an inactive board emits a
    ///     <see cref="LeaderboardEvent.BoardSelected" />.
    /// </summary>
    private static float DrawBoardSelector(
        CivilizationLeaderboardViewModel viewModel,
        ImDrawListPtr drawList,
        float x, float y, float width,
        List<LeaderboardEvent> events)
    {
        var mouse = ImGui.GetMousePos();
        var rowCenterY = y + SelectorRowHeight / 2f;
        var cursor = x;
        var font = ImGui.GetFont();

        foreach (var board in viewModel.Boards)
        {
            var isActive = board == viewModel.SelectedBoard;
            var label = BoardName(board);
            var labelSize = ImGui.CalcTextSize(label);

            var chipWidth = SelectorChipPaddingX + labelSize.X + SelectorChipPaddingX;
            if (cursor + chipWidth > x + width) break;

            var chipMinY = rowCenterY - labelSize.Y / 2f - 4f;
            var chipMaxY = rowCenterY + labelSize.Y / 2f + 4f;
            var chipMin = new Vector2(cursor, chipMinY);
            var chipMax = new Vector2(cursor + chipWidth, chipMaxY);

            var isHover = mouse.X >= chipMin.X && mouse.X <= chipMax.X &&
                          mouse.Y >= chipMin.Y && mouse.Y <= chipMax.Y;

            if (isActive)
                drawList.AddRect(chipMin, chipMax,
                    ImGui.ColorConvertFloat4ToU32(ColorPalette.Gold), 3f, ImDrawFlags.None, 1.5f);

            if (isHover && !isActive) ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);

            var textColor = isActive
                ? ColorPalette.Gold
                : (isHover ? ColorPalette.Gold : ColorPalette.Grey);
            drawList.AddText(font, Body,
                new Vector2(cursor + SelectorChipPaddingX, rowCenterY - labelSize.Y / 2f),
                ImGui.ColorConvertFloat4ToU32(textColor), label);

            if (isHover && !isActive && ImGui.IsMouseClicked(ImGuiMouseButton.Left))
                events.Add(new LeaderboardEvent.BoardSelected(board));

            cursor += chipWidth + SelectorChipGap;
        }

        return y + SelectorRowHeight + SelectorRowBottomSpacing;
    }

    private static string BoardName(LeaderboardMetric metric)
    {
        var key = metric switch
        {
            LeaderboardMetric.Standing => LocalizationKeys.UI_CIVILIZATION_LEADERBOARD_BOARD_STANDING,
            LeaderboardMetric.Conquest => LocalizationKeys.UI_CIVILIZATION_LEADERBOARD_BOARD_CONQUEST,
            LeaderboardMetric.Endurance => LocalizationKeys.UI_CIVILIZATION_LEADERBOARD_BOARD_ENDURANCE,
            LeaderboardMetric.Deeds => LocalizationKeys.UI_CIVILIZATION_LEADERBOARD_BOARD_DEEDS,
            _ => LocalizationKeys.UI_CIVILIZATION_LEADERBOARD_BOARD_STANDING
        };
        return LocalizationService.Instance.Get(key);
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
        h += SelectorRowHeight + SelectorRowBottomSpacing; // board selector
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
