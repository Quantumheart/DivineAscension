using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using DivineAscension.Constants;
using DivineAscension.GUI.Events.Religion;
using DivineAscension.GUI.Models.Religion.Invites;
using DivineAscension.GUI.UI.Components.Lists;
using DivineAscension.GUI.UI.Renderers.Religion.Letters;
using DivineAscension.GUI.UI.Renderers.Utilities;
using DivineAscension.GUI.UI.Utilities;
using DivineAscension.Services;
using ImGuiNET;
using static DivineAscension.GUI.UI.Utilities.FontSizes;

namespace DivineAscension.GUI.UI.Renderers.Religion;

/// <summary>
/// Ledger-chapter renderer for the Letters (I.v). Chapter strip, prose intro,
/// section dividers above and below the letter list, each letter painted as
/// an envelope + domain glyph row (sender, default quote, Accept / Refuse),
/// and a closing "No further letters lie unopened." line that is shown even
/// when there are zero letters.
/// </summary>
[ExcludeFromCodeCoverage]
internal static class ReligionInvitesRenderer
{
    private const float DividerHeight = 18f;
    private const float DividerYPadding = 6f;
    private const float ScrollbarWidth = 16f;
    private const float ClosingLineHeight = 24f;
    private const float ClosingLineTopSpacing = 6f;

    public static ReligionInvitesRenderResult Draw(
        ReligionInvitesViewModel viewModel,
        ImDrawListPtr drawList)
    {
        var events = new List<InvitesEvent>();
        var x = viewModel.X;
        var y = viewModel.Y;
        var width = viewModel.Width;
        var height = viewModel.Height;

        if (viewModel.IsLoading && !viewModel.HasInvites)
        {
            DrawCenteredStateText(drawList,
                LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_INVITES_LOADING),
                x, y, width, height, ColorPalette.Grey);
            return new ReligionInvitesRenderResult(events, height);
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
                    events.Add(new InvitesEvent.ScrollChanged(newScrollY));
                }
            }
        }

        drawList.PushClipRect(new Vector2(x, y), new Vector2(x + width, y + height), true);

        // === CHAPTER STRIP ===
        var strip = ChapterStripRenderer.Draw(drawList, x, y, width, scrollY,
            LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_TAB_INVITES));
        var contentWidth = strip.ContentWidth;
        var currentY = strip.BodyY;

        // === INTRO ===
        currentY = ReligionLettersHeaderRenderer.Draw(drawList, x, currentY, contentWidth);

        if (viewModel.HasInvites)
        {
            currentY = DrawDivider(drawList, x, currentY, contentWidth);
            currentY = ReligionLettersListRenderer.Draw(drawList, viewModel, x, currentY, contentWidth, events);
            currentY = DrawDivider(drawList, x, currentY, contentWidth);
        }
        else
        {
            currentY = DrawDivider(drawList, x, currentY, contentWidth);
        }

        // === CLOSING LINE ===
        currentY += ClosingLineTopSpacing;
        DrawClosingLine(drawList, x, currentY, contentWidth);

        drawList.PopClipRect();

        if (maxScroll > 0f)
        {
            var newScrollY = Scrollbar.HandleDragging(scrollY, maxScroll,
                x + width - ScrollbarWidth, y, ScrollbarWidth, height);
            if (Math.Abs(newScrollY - scrollY) > 0.001f)
                events.Add(new InvitesEvent.ScrollChanged(newScrollY));
            Scrollbar.Draw(drawList, x + width - ScrollbarWidth, y, ScrollbarWidth, height, scrollY, maxScroll);
        }

        return new ReligionInvitesRenderResult(events, height);
    }

    private static float DrawDivider(ImDrawListPtr drawList, float x, float y, float width)
    {
        var dividerY = y + DividerYPadding;
        ChromeRenderer.DrawDivider(drawList, x, dividerY, width);
        return y + DividerHeight;
    }

    private static void DrawClosingLine(ImDrawListPtr drawList, float x, float y, float width)
    {
        var text = LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_INVITES_FOOTER_CLOSING);
        var size = ImGui.CalcTextSize(text);
        var textX = x + (width - size.X) / 2f;
        drawList.AddText(ImGui.GetFont(), Body, new Vector2(textX, y),
            ImGui.ColorConvertFloat4ToU32(ColorPalette.Grey),
            text);
    }

    private static float ComputeContentHeight(ReligionInvitesViewModel viewModel)
    {
        var h = 0f;
        h += PaneHeaderRenderer.TotalHeight;
        h += ReligionLettersHeaderRenderer.IntroLineHeight + ReligionLettersHeaderRenderer.IntroBottomSpacing;
        h += DividerHeight;
        if (viewModel.HasInvites)
        {
            h += ReligionLettersListRenderer.MeasureHeight(viewModel);
            h += DividerHeight;
        }
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
