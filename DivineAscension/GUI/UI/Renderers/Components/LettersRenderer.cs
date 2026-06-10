using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using DivineAscension.GUI.Events.Letters;
using DivineAscension.GUI.Models.Letters;
using DivineAscension.GUI.UI.Components.Buttons;
using DivineAscension.GUI.UI.Components.Lists;
using DivineAscension.GUI.UI.Renderers.Utilities;
using DivineAscension.GUI.UI.Utilities;
using ImGuiNET;
using static DivineAscension.GUI.UI.Utilities.FontSizes;

namespace DivineAscension.GUI.UI.Renderers.Components;

/// <summary>
///     Shared ledger-chapter renderer for "Letters" pages — religion invites
///     (I.v) and civilization invites (II.iii) both route through this so
///     the chapter strip, prose intro, section dividers, envelope row layout,
///     per-letter ornamental dividers, and the "No further letters lie
///     unopened." closing line stay pixel-identical between the two pages.
///     Owns chapter strip, scrolling, scrollbar, and clip rect; the only
///     things the caller picks are the chrome strings (title / intro /
///     closing / button labels), the per-letter glyph painter, and the
///     pre-formatted "From X" sender line.
/// </summary>
[ExcludeFromCodeCoverage]
internal static class LettersRenderer
{
    // Chapter-level layout.
    private static float DividerHeight => UiScale.Scaled(18f);
    private static float DividerYPadding => UiScale.Scaled(6f);
    private static float ScrollbarWidth => UiScale.Scaled(16f);
    private static float ClosingLineHeight => UiScale.Scaled(24f);
    private static float ClosingLineTopSpacing => UiScale.Scaled(6f);
    private static float IntroLineHeight => UiScale.Scaled(18f);
    private static float IntroBottomSpacing => UiScale.Scaled(10f);

    // Letter-row layout — see DrawLetter for the geometry.
    private static float EnvelopeSize => UiScale.Scaled(18f);
    private static float GlyphSize => UiScale.Scaled(16f);
    private static float GlyphGap => UiScale.Scaled(6f);
    private static float MarkColumnWidth => EnvelopeSize + GlyphGap + GlyphSize + UiScale.Scaled(8f);
    private static float HeaderLineHeight => UiScale.Scaled(22f);
    private static float QuoteLineHeight => UiScale.Scaled(20f);
    private static float ButtonHeight => UiScale.Scaled(26f);
    private static float ButtonWidth => UiScale.Scaled(88f);
    private static float ButtonGap => UiScale.Scaled(10f);
    private static float ButtonTopSpacing => UiScale.Scaled(6f);
    private static float ButtonBottomSpacing => UiScale.Scaled(8f);
    private static float RowLeftPadding => UiScale.Scaled(16f);

    private static float RowHeight =>
        HeaderLineHeight + QuoteLineHeight + ButtonTopSpacing + ButtonHeight + ButtonBottomSpacing;

    /// <summary>
    ///     Row height for read-only / informational letters (no Accept/Refuse).
    ///     Used by holiday-notice letters projected from the chronicle.
    /// </summary>
    private static float ReadOnlyRowHeight =>
        HeaderLineHeight + QuoteLineHeight + ButtonBottomSpacing;

    private static float SlimDividerHeight => UiScale.Scaled(18f);
    private static float SlimDividerYPadding => UiScale.Scaled(4f);

    public static LettersRenderResult Draw(
        ImDrawListPtr drawList,
        LettersViewModel vm)
    {
        var events = new List<LettersEvent>();
        var x = vm.X;
        var y = vm.Y;
        var width = vm.Width;
        var height = vm.Height;

        if (vm.IsLoading && !vm.HasLetters)
        {
            DrawCenteredStateText(drawList, vm.LoadingText, x, y, width, height, ColorPalette.Grey);
            return new LettersRenderResult(events, height);
        }

        var contentHeightEstimate = ComputeContentHeight(vm);
        var maxScroll = MathF.Max(0f, contentHeightEstimate - height);

        var mousePos = ImGui.GetMousePos();
        var isHover = mousePos.X >= x && mousePos.X <= x + width
                                      && mousePos.Y >= y && mousePos.Y <= y + height;
        var scrollY = vm.ScrollY;
        if (isHover && maxScroll > 0f)
        {
            var wheel = ImGui.GetIO().MouseWheel;
            if (wheel != 0)
            {
                var newScrollY = Math.Clamp(scrollY - wheel * UiScale.Scaled(30f), 0f, maxScroll);
                if (Math.Abs(newScrollY - scrollY) > 0.001f)
                {
                    scrollY = newScrollY;
                    events.Add(new LettersEvent.ScrollChanged(newScrollY));
                }
            }
        }

        drawList.PushClipRect(new Vector2(x, y), new Vector2(x + width, y + height), true);

        var strip = ChapterStripRenderer.Draw(drawList, x, y, width, scrollY, vm.Title);
        var contentWidth = strip.ContentWidth;
        var currentY = strip.BodyY;

        currentY = DrawIntro(drawList, vm.Intro, x, currentY, contentWidth);

        currentY = DrawDivider(drawList, x, currentY, contentWidth);
        if (vm.HasLetters)
        {
            currentY = DrawLetterList(drawList, vm, x, currentY, contentWidth, events);
            currentY = DrawDivider(drawList, x, currentY, contentWidth);
        }

        currentY += ClosingLineTopSpacing;
        DrawClosingLine(drawList, vm.ClosingLine, x, currentY, contentWidth);

        drawList.PopClipRect();

        if (maxScroll > 0f)
        {
            var newScrollY = Scrollbar.HandleDragging(scrollY, maxScroll,
                x + width - ScrollbarWidth, y, ScrollbarWidth, height);
            if (Math.Abs(newScrollY - scrollY) > 0.001f)
                events.Add(new LettersEvent.ScrollChanged(newScrollY));
            Scrollbar.Draw(drawList, x + width - ScrollbarWidth, y, ScrollbarWidth, height, scrollY, maxScroll);
        }

        return new LettersRenderResult(events, height);
    }

    private static float DrawIntro(ImDrawListPtr drawList, string intro, float x, float y, float width)
    {
        TextRenderer.DrawInfoText(drawList, intro, x, y, width, Body, ColorPalette.White);
        var introHeight = TextRenderer.MeasureWrappedHeight(intro, width, Body);
        return y + (introHeight > 0 ? introHeight : IntroLineHeight) + IntroBottomSpacing;
    }

    private static float DrawLetterList(
        ImDrawListPtr drawList,
        LettersViewModel vm,
        float x,
        float y,
        float width,
        List<LettersEvent> events)
    {
        var enabled = !vm.IsLoading;
        var currentY = y;

        for (var i = 0; i < vm.Letters.Count; i++)
        {
            var letter = vm.Letters[i];
            DrawLetter(drawList, letter,
                x + RowLeftPadding, currentY, width - RowLeftPadding,
                vm.AcceptLabel, vm.RefuseLabel,
                enabled, events);
            currentY += letter.ShowActions ? RowHeight : ReadOnlyRowHeight;

            if (i < vm.Letters.Count - 1)
                currentY = DrawSlimDivider(drawList, x, currentY, width);
        }

        return currentY;
    }

    private static void DrawLetter(
        ImDrawListPtr drawList,
        LetterEntry letter,
        float x, float y, float width,
        string acceptLabel, string refuseLabel,
        bool enabled,
        List<LettersEvent> events)
    {
        // Envelope + caller-painted glyph share a centred baseline at the
        // header line — caller picks the glyph (domain mark, banner, …) so
        // this renderer stays agnostic of the letter's source.
        var markCy = y + HeaderLineHeight / 2f;
        ChromeRenderer.DrawEnvelope(drawList,
            x + EnvelopeSize / 2f, markCy, EnvelopeSize, ColorPalette.White);

        var glyphX = x + EnvelopeSize + GlyphGap;
        var glyphMin = new Vector2(glyphX, markCy - GlyphSize / 2f);
        var glyphMax = new Vector2(glyphX + GlyphSize, markCy + GlyphSize / 2f);
        letter.GlyphPainter(drawList, glyphMin, glyphMax);

        var textX = x + MarkColumnWidth;
        drawList.AddText(ImGui.GetFont(), Body,
            new Vector2(textX, y + UiScale.Scaled(2f)),
            ImGui.ColorConvertFloat4ToU32(ColorPalette.White), letter.SenderText);

        var quoteY = y + HeaderLineHeight;
        drawList.AddText(ImGui.GetFont(), Body,
            new Vector2(textX, quoteY),
            ImGui.ColorConvertFloat4ToU32(ColorPalette.Grey), letter.QuoteLine);

        if (!letter.ShowActions) return;

        var buttonY = quoteY + QuoteLineHeight + ButtonTopSpacing;
        if (ButtonRenderer.DrawButton(drawList, acceptLabel,
                textX, buttonY, ButtonWidth, ButtonHeight, true, enabled))
            events.Add(new LettersEvent.AcceptClicked(letter.Id));

        if (ButtonRenderer.DrawButton(drawList, refuseLabel,
                textX + ButtonWidth + ButtonGap, buttonY, ButtonWidth, ButtonHeight, false, enabled))
            events.Add(new LettersEvent.RefuseClicked(letter.Id));
    }

    private static float DrawDivider(ImDrawListPtr drawList, float x, float y, float width)
    {
        var dividerY = y + DividerYPadding;
        ChromeRenderer.DrawDivider(drawList, x, dividerY, width);
        return y + DividerHeight;
    }

    private static float DrawSlimDivider(ImDrawListPtr drawList, float x, float y, float width)
    {
        // Tighter inset divider between adjacent letters so the section-level
        // divider above/below the list reads as the louder break.
        var inset = width * 0.20f;
        var dividerY = y + SlimDividerYPadding;
        ChromeRenderer.DrawDivider(drawList,
            x + inset, dividerY, width - inset * 2f,
            ColorPalette.Gold * 0.35f);
        return y + SlimDividerHeight;
    }

    private static void DrawClosingLine(ImDrawListPtr drawList, string text, float x, float y, float width)
    {
        var size = ImGui.CalcTextSize(text);
        var textX = x + (width - size.X) / 2f;
        drawList.AddText(ImGui.GetFont(), Body, new Vector2(textX, y),
            ImGui.ColorConvertFloat4ToU32(ColorPalette.Grey),
            text);
    }

    private static float ComputeContentHeight(LettersViewModel vm)
    {
        var h = 0f;
        h += PaneHeaderRenderer.TotalHeight;
        h += TextRenderer.MeasureWrappedHeight(vm.Intro, vm.Width - ChapterStripRenderer.ScrollbarGutter, Body);
        if (h == PaneHeaderRenderer.TotalHeight) h += IntroLineHeight;
        h += IntroBottomSpacing;
        h += DividerHeight;
        if (vm.HasLetters)
        {
            var rowsHeight = 0f;
            foreach (var letter in vm.Letters)
                rowsHeight += letter.ShowActions ? RowHeight : ReadOnlyRowHeight;
            h += rowsHeight + (vm.Letters.Count - 1) * SlimDividerHeight;
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
