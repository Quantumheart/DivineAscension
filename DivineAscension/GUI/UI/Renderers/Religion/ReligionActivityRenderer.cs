using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using DivineAscension.Constants;
using DivineAscension.GUI.Events.Religion;
using DivineAscension.GUI.Models.Religion.Activity;
using DivineAscension.GUI.UI.Components.Buttons;
using DivineAscension.GUI.UI.Components.Lists;
using DivineAscension.GUI.UI.Renderers.Religion.Activity;
using DivineAscension.GUI.UI.Renderers.Utilities;
using DivineAscension.GUI.UI.Utilities;
using DivineAscension.Services;
using ImGuiNET;
using static DivineAscension.GUI.UI.Utilities.FontSizes;

namespace DivineAscension.GUI.UI.Renderers.Religion;

/// <summary>
/// Ledger-chapter renderer for the Annals (I.iii). Chapter strip with refresh
/// glyph, prose intro, deity filter sub-index, ornamental dividers above and
/// below the day-grouped feed, and a closing "No further deeds are recorded."
/// line. Entries are grouped by real-world calendar day — TimestampTicks is
/// UTC wall-clock time and stays the source of truth (issue #316 spec note).
/// </summary>
[ExcludeFromCodeCoverage]
internal static class ReligionActivityRenderer
{
    private const float DividerHeight = 18f;
    private const float DividerYPadding = 6f;
    private const float ScrollbarWidth = 16f;
    private const float ClosingLineHeight = 24f;
    private const float ClosingLineTopSpacing = 6f;
    private const float RefreshGlyphSize = 22f;
    private const float RefreshGlyphGap = 6f;

    public static ReligionActivityRenderResult Draw(ReligionActivityViewModel viewModel)
    {
        var events = new List<ActivityEvent>();
        var drawList = ImGui.GetWindowDrawList();
        var x = viewModel.X;
        var y = viewModel.Y;
        var width = viewModel.Width;
        var height = viewModel.Height;

        if (viewModel.IsLoading)
        {
            DrawCenteredStateText(drawList,
                LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_ACTIVITY_LOADING),
                x, y, width, height, ColorPalette.Grey);
            return new ReligionActivityRenderResult(events, height);
        }

        if (!string.IsNullOrEmpty(viewModel.ErrorMessage))
        {
            DrawCenteredStateText(drawList,
                LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_ACTIVITY_ERROR),
                x, y, width, height, ColorPalette.Vermilion);
            return new ReligionActivityRenderResult(events, height);
        }

        var contentHeightEstimate = ComputeContentHeight(viewModel);
        var maxScroll = MathF.Max(0f, contentHeightEstimate - height);

        var mousePos = ImGui.GetMousePos();
        var isHover = mousePos.X >= x && mousePos.X <= x + width && mousePos.Y >= y && mousePos.Y <= y + height;
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
                    events.Add(new ActivityEvent.ScrollChanged(newScrollY));
                }
            }
        }

        drawList.PushClipRect(new Vector2(x, y), new Vector2(x + width, y + height), true);

        // === CHAPTER STRIP + REFRESH GLYPH ===
        var strip = ChapterStripRenderer.Draw(drawList, x, y, width, scrollY,
            LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_TAB_ACTIVITY));
        var contentWidth = strip.ContentWidth;

        if (DrawRefreshGlyph(drawList, x, y, contentWidth, scrollY))
            events.Add(new ActivityEvent.RefreshRequested());

        var currentY = strip.BodyY;

        // === INTRO ===
        currentY = ReligionActivityHeaderRenderer.Draw(viewModel, drawList, x, currentY, contentWidth);

        currentY = DrawDivider(drawList, x, currentY, contentWidth);

        // === DAY-GROUPED FEED ===
        currentY = ReligionActivityFeedRenderer.Draw(viewModel, drawList, x, currentY, contentWidth);

        currentY = DrawDivider(drawList, x, currentY, contentWidth);

        // === CLOSING LINE ===
        currentY += ClosingLineTopSpacing;
        DrawClosingLine(drawList, x, currentY, contentWidth);

        drawList.PopClipRect();

        if (maxScroll > 0f)
        {
            var newScrollY = Scrollbar.HandleDragging(scrollY, maxScroll,
                x + width - ScrollbarWidth, y, ScrollbarWidth, height);
            if (Math.Abs(newScrollY - scrollY) > 0.001f)
                events.Add(new ActivityEvent.ScrollChanged(newScrollY));
            Scrollbar.Draw(drawList, x + width - ScrollbarWidth, y, ScrollbarWidth, height, scrollY, maxScroll);
        }

        return new ReligionActivityRenderResult(events, height);
    }

    private static bool DrawRefreshGlyph(ImDrawListPtr drawList,
        float x, float paneY, float contentWidth, float scrollY)
    {
        var stripY = paneY + ChapterStripRenderer.TopPadding - scrollY;
        var glyphX = x + contentWidth - RefreshGlyphSize - RefreshGlyphGap;
        var glyphY = stripY + 6f;

        var clicked = ButtonRenderer.DrawButton(drawList, string.Empty,
            glyphX, glyphY, RefreshGlyphSize, RefreshGlyphSize,
            isPrimary: false, enabled: true);

        ChromeRenderer.DrawRefreshArrow(drawList,
            glyphX + RefreshGlyphSize / 2f,
            glyphY + RefreshGlyphSize / 2f,
            RefreshGlyphSize - 6f,
            ColorPalette.LightText);

        return clicked;
    }

    private static float DrawDivider(ImDrawListPtr drawList, float x, float y, float width)
    {
        var dividerY = y + DividerYPadding;
        ChromeRenderer.DrawDivider(drawList, x, dividerY, width);
        return y + DividerHeight;
    }

    private static void DrawClosingLine(ImDrawListPtr drawList, float x, float y, float width)
    {
        var text = LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_ACTIVITY_FOOTER_CLOSING);
        var size = ImGui.CalcTextSize(text);
        var textX = x + (width - size.X) / 2f;
        drawList.AddText(ImGui.GetFont(), Body, new Vector2(textX, y),
            ImGui.ColorConvertFloat4ToU32(ColorPalette.Grey),
            text);
    }

    private static float ComputeContentHeight(ReligionActivityViewModel viewModel)
    {
        var h = 0f;
        // Chapter strip (title + divider below).
        h += PaneHeaderRenderer.TotalHeight;
        // Prose intro.
        h += ReligionActivityHeaderRenderer.IntroLineHeight + ReligionActivityHeaderRenderer.IntroBottomSpacing;
        // Top divider.
        h += DividerHeight;
        // Feed.
        h += ReligionActivityFeedRenderer.MeasureHeight(viewModel);
        // Bottom divider.
        h += DividerHeight;
        // Closing line.
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
