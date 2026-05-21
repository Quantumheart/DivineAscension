using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using DivineAscension.Constants;
using DivineAscension.GUI.Events.Religion;
using DivineAscension.GUI.Models.Religion.Invites;
using DivineAscension.GUI.UI.Components.Buttons;
using DivineAscension.GUI.UI.Components.Lists;
using DivineAscension.GUI.UI.Renderers.Utilities;
using DivineAscension.GUI.UI.Utilities;
using DivineAscension.Models.Enum;
using DivineAscension.Services;
using ImGuiNET;
using static DivineAscension.GUI.UI.Utilities.FontSizes;

namespace DivineAscension.GUI.UI.Renderers.Religion;

/// <summary>
///     Ledger-chapter renderer for the Letters page (I.v). Replaces the old
///     filled-rect invite card list with envelope rows on the parchment:
///     chapter title strip, prose intro, multi-diamond section dividers
///     bracketing the letters, per-letter `── ✦ ──` separators, and a
///     closing line that's always present — even when the inbox is empty.
///
///     The renderer is pure: it consumes an immutable
///     <see cref="ReligionInvitesViewModel" /> and returns the user
///     interactions as <see cref="InvitesEvent" /> records.
/// </summary>
[ExcludeFromCodeCoverage]
internal static class ReligionInvitesRenderer
{
    private const float IntroBottomSpacing = 10f;
    private const float SectionDividerHeight = 18f;
    private const float SectionDividerYPadding = 6f;
    private const float LetterDividerHeight = 16f;
    private const float LetterTopSpacing = 8f;
    private const float LetterBottomSpacing = 6f;
    private const float SenderRowHeight = 22f;
    private const float QuoteRowHeight = 22f;
    private const float ButtonRowHeight = 32f;
    private const float ButtonRowTopSpacing = 6f;
    private const float LetterEntryHeight =
        LetterTopSpacing
        + SenderRowHeight
        + QuoteRowHeight
        + ButtonRowTopSpacing + ButtonRowHeight
        + LetterBottomSpacing;
    private const float EnvelopeSize = 16f;
    private const float DomainGlyphSize = 14f;
    private const float GlyphGap = 6f;
    private const float SenderToGlyphsGap = 8f;
    private const float LetterIndent = 28f;
    private const float ButtonWidth = 96f;
    private const float ButtonHeight = 28f;
    private const float ButtonGap = 10f;
    private const float ClosingLineHeight = 22f;
    private const float ClosingLineTopSpacing = 8f;
    private const float ScrollbarWidth = 16f;

    public static ReligionInvitesRenderResult Draw(
        ReligionInvitesViewModel viewModel,
        ImDrawListPtr drawList)
    {
        var events = new List<InvitesEvent>();
        var x = viewModel.X;
        var y = viewModel.Y;
        var width = viewModel.Width;
        var height = viewModel.Height;

        if (viewModel.IsLoading)
        {
            DrawCenteredStateText(drawList,
                LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_INVITES_LOADING),
                x, y, width, height, ColorPalette.Grey);
            return new ReligionInvitesRenderResult(events, height);
        }

        var contentHeight = ComputeContentHeight(viewModel, width);
        var maxScroll = MathF.Max(0f, contentHeight - height);

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
            LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_INVITES_CHAPTER_TITLE));
        var contentWidth = strip.ContentWidth;
        var currentY = strip.BodyY;

        // === INTRO ===
        var intro = LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_INVITES_INTRO);
        TextRenderer.DrawInfoText(drawList, intro, x, currentY, contentWidth, Body, ColorPalette.White);
        currentY += MathF.Max(TextRenderer.MeasureWrappedHeight(intro, contentWidth, Body), Body + 6f);
        currentY += IntroBottomSpacing;

        // === TOP SECTION DIVIDER ===
        currentY = DrawSectionDivider(drawList, x, currentY, contentWidth);

        // === LETTERS ===
        currentY = DrawLetters(viewModel, drawList, x, currentY, contentWidth, events);

        // === BOTTOM SECTION DIVIDER ===
        currentY = DrawSectionDivider(drawList, x, currentY, contentWidth);

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
            Scrollbar.Draw(drawList, x + width - ScrollbarWidth, y,
                ScrollbarWidth, height, scrollY, maxScroll);
        }

        return new ReligionInvitesRenderResult(events, height);
    }

    private static float DrawLetters(
        ReligionInvitesViewModel viewModel,
        ImDrawListPtr drawList,
        float x, float y, float width,
        List<InvitesEvent> events)
    {
        var currentY = y;
        for (var i = 0; i < viewModel.Invites.Count; i++)
        {
            if (i > 0)
                currentY = DrawLetterDivider(drawList, x, currentY, width);

            currentY = DrawLetter(viewModel.Invites[i], drawList, x, currentY, width,
                viewModel.IsLoading, events);
        }
        return currentY;
    }

    private static float DrawLetter(
        InviteData invite,
        ImDrawListPtr drawList,
        float x, float y, float width,
        bool isLoading,
        List<InvitesEvent> events)
    {
        var rowY = y + LetterTopSpacing;

        // Line 1: envelope + domain glyph + "From {ReligionName}"
        var glyphCy = rowY + SenderRowHeight / 2f;
        var envelopeCx = x + EnvelopeSize / 2f;
        ChromeRenderer.DrawEnvelope(drawList, envelopeCx, glyphCy, EnvelopeSize, ColorPalette.Gold);

        var domainGlyphX = x + EnvelopeSize + GlyphGap;
        if (invite.Domain != DeityDomain.None)
        {
            var glyphMin = new Vector2(domainGlyphX, glyphCy - DomainGlyphSize / 2f);
            var glyphMax = new Vector2(domainGlyphX + DomainGlyphSize, glyphCy + DomainGlyphSize / 2f);
            DomainGlyphRenderer.Draw(drawList, invite.Domain, glyphMin, glyphMax, ColorPalette.White);
        }

        var senderText = LocalizationService.Instance.Get(
            LocalizationKeys.UI_RELIGION_INVITES_LETTER_FROM, invite.ReligionName);
        var senderX = domainGlyphX
                      + (invite.Domain != DeityDomain.None ? DomainGlyphSize : 0f)
                      + SenderToGlyphsGap;
        drawList.AddText(ImGui.GetFont(), Body,
            new Vector2(senderX, rowY + 2f),
            ImGui.ColorConvertFloat4ToU32(ColorPalette.White),
            senderText);
        var nextY = rowY + SenderRowHeight;

        // Line 2: quote, indented under the sender.
        var quote = LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_INVITES_QUOTE_DEFAULT);
        drawList.AddText(ImGui.GetFont(), Secondary,
            new Vector2(x + LetterIndent, nextY + 2f),
            ImGui.ColorConvertFloat4ToU32(ColorPalette.Grey),
            quote);
        nextY += QuoteRowHeight;

        // Line 3: [ Accept ]  [ Refuse ]
        nextY += ButtonRowTopSpacing;
        var buttonsEnabled = !isLoading;
        var acceptX = x + LetterIndent;
        if (ButtonRenderer.DrawButton(drawList,
                LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_INVITES_ACCEPT),
                acceptX, nextY, ButtonWidth, ButtonHeight,
                isPrimary: true, enabled: buttonsEnabled))
        {
            events.Add(new InvitesEvent.AcceptInviteClicked(invite.InviteId));
        }

        var refuseX = acceptX + ButtonWidth + ButtonGap;
        if (ButtonRenderer.DrawButton(drawList,
                LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_INVITES_REFUSE),
                refuseX, nextY, ButtonWidth, ButtonHeight,
                isPrimary: false, enabled: buttonsEnabled))
        {
            events.Add(new InvitesEvent.DeclineInviteClicked(invite.InviteId));
        }
        nextY += ButtonRowHeight;

        return nextY + LetterBottomSpacing;
    }

    private static float DrawLetterDivider(ImDrawListPtr drawList, float x, float y, float width)
    {
        // Narrower per-letter divider — quarter of the page width, centred.
        // Lighter weight than the bracketing section dividers above and below.
        var dividerWidth = MathF.Min(width * 0.4f, 220f);
        var dividerX = x + (width - dividerWidth) / 2f;
        var dividerY = y + SectionDividerYPadding;
        ChromeRenderer.DrawDivider(drawList, dividerX, dividerY, dividerWidth,
            ColorPalette.Gold * 0.4f);
        return y + LetterDividerHeight;
    }

    private static float DrawSectionDivider(ImDrawListPtr drawList, float x, float y, float width)
    {
        var dividerY = y + SectionDividerYPadding;
        ChromeRenderer.DrawMultiDiamondDivider(drawList, x, dividerY, width, 3);
        return y + SectionDividerHeight;
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

    private static float ComputeContentHeight(ReligionInvitesViewModel viewModel, float width)
    {
        var contentWidth = width - ChapterStripRenderer.ScrollbarGutter;
        var introHeight = MathF.Max(
            TextRenderer.MeasureWrappedHeight(
                LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_INVITES_INTRO),
                contentWidth, Body),
            Body + 6f);

        var h = 0f;
        h += PaneHeaderRenderer.TotalHeight;
        h += introHeight + IntroBottomSpacing;
        h += SectionDividerHeight;
        h += viewModel.Invites.Count * LetterEntryHeight;
        if (viewModel.Invites.Count > 1)
            h += (viewModel.Invites.Count - 1) * LetterDividerHeight;
        h += SectionDividerHeight;
        h += ClosingLineTopSpacing + ClosingLineHeight;
        return h;
    }

    private static void DrawCenteredStateText(
        ImDrawListPtr drawList, string text,
        float x, float y, float width, float height, Vector4 color)
    {
        var size = ImGui.CalcTextSize(text);
        var pos = new Vector2(x + (width - size.X) / 2f, y + (height - size.Y) / 2f);
        drawList.AddText(ImGui.GetFont(), SubsectionLabel, pos,
            ImGui.ColorConvertFloat4ToU32(color), text);
    }
}
