using System;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using DivineAscension.GUI.UI.Components.Buttons;
using DivineAscension.GUI.UI.Utilities;
using DivineAscension.Models.Enum;
using ImGuiNET;

namespace DivineAscension.GUI.UI.Renderers.Utilities;

/// <summary>
///     Stateless chapter-chrome widget. Every ledger-chapter page in the
///     dialog goes through this so the scrollbar gutter, top padding, title
///     strip, divider, right-anchored entity name, domain glyph, and founder
///     pencil all sit at the same pixel positions across panes.
///
///     Callers own scroll detection and clip rects. This widget computes
///     <see cref="Result.ContentWidth" /> (= paneWidth − scrollbar gutter)
///     and returns the Y after the strip's divider so the body content
///     starts in a predictable place.
/// </summary>
[ExcludeFromCodeCoverage]
internal static class ChapterStripRenderer
{
    /// <summary>
    ///     Reserved horizontal space for the scrollbar gutter (scrollbar +
    ///     visual breathing room). Every chapter subtracts this from its
    ///     pane width so chrome lines up regardless of whether the page is
    ///     actually scrolling right now.
    /// </summary>
    public static float ScrollbarGutter => UiScale.Scaled(20f);

    /// <summary>Top padding above the title strip — first line of vellum.</summary>
    public static float TopPadding => UiScale.Scaled(8f);

    private static float GlyphGap => UiScale.Scaled(8f);
    private static float PencilGap => UiScale.Scaled(8f);
    private static float PencilWidth => UiScale.Scaled(22f);
    private static float PencilHeight => UiScale.Scaled(22f);

    public readonly record struct Result(float ContentWidth, float BodyY, bool PencilClicked);

    /// <summary>
    ///     Draw the chapter title strip. <paramref name="paneY" /> is the
    ///     unscrolled top of the pane; <paramref name="scrollY" /> is the
    ///     current vertical scroll offset (subtracted internally). Returns
    ///     the layout context the body content should use.
    /// </summary>
    public static Result Draw(
        ImDrawListPtr drawList,
        float x,
        float paneY,
        float paneWidth,
        float scrollY,
        string title,
        string? rightTitle = null,
        DeityDomain? rightGlyph = null,
        bool showPencil = false,
        IntPtr iconTextureId = default,
        string? rankTag = null,
        Vector4? rankColor = null,
        Vector4? dropCapColor = null)
    {
        var stripY = paneY + TopPadding - scrollY;
        var contentWidth = paneWidth - ScrollbarGutter;

        var pencilReservation = showPencil ? PencilWidth + PencilGap : 0f;
        var glyphReservation = rightGlyph.HasValue
            ? PaneHeaderRenderer.IconSize + GlyphGap
            : 0f;

        // Title + divider — divider spans full contentWidth so it lines up
        // with every section divider drawn at the same width below. Every
        // chapter pane opens with an illuminated drop cap. Colour priority:
        //   1. explicit dropCapColor from the caller,
        //   2. the right-side domain glyph (page is about this domain),
        //   3. the player's patron domain (player's own ink across their book),
        //   4. chrome gold (player has no patron yet).
        Vector4 effectiveDropCap;
        if (dropCapColor.HasValue)
            effectiveDropCap = dropCapColor.Value;
        else if (rightGlyph.HasValue)
            effectiveDropCap = DomainHelper.GetDeityColor(rightGlyph.Value);
        else if (ChromeContext.PlayerPatronDomain.HasValue)
            effectiveDropCap = DomainHelper.GetDeityColor(ChromeContext.PlayerPatronDomain.Value);
        else
            effectiveDropCap = ColorPalette.Gold;

        var bodyY = PaneHeaderRenderer.Draw(drawList, title, x, stripY, contentWidth,
            iconTextureId: iconTextureId, rankTag: rankTag, rankColor: rankColor,
            dropCapColor: effectiveDropCap);

        if (!string.IsNullOrEmpty(rightTitle))
        {
            var rightTextWidth = TextRenderer.MeasureSerifLabel(rightTitle, FontSizes.PageTitle);
            var anchorX = x + contentWidth - pencilReservation - glyphReservation;
            if (glyphReservation > 0f) anchorX -= GlyphGap;
            var rightX = anchorX - rightTextWidth;
            TextRenderer.DrawSerifLabel(drawList, rightTitle, rightX, stripY + UiScale.Scaled(4f),
                FontSizes.PageTitle, ColorPalette.Gold);
        }

        if (rightGlyph.HasValue)
        {
            var glyphMinX = x + contentWidth - pencilReservation - PaneHeaderRenderer.IconSize;
            var glyphMin = new Vector2(glyphMinX, stripY);
            var glyphMax = new Vector2(glyphMinX + PaneHeaderRenderer.IconSize,
                stripY + PaneHeaderRenderer.IconSize);
            DomainGlyphRenderer.Draw(drawList, rightGlyph.Value, glyphMin, glyphMax);
            drawList.AddRect(glyphMin, glyphMax,
                ImGui.ColorConvertFloat4ToU32(ColorPalette.Gold * 0.6f),
                UiScale.Scaled(4f), ImDrawFlags.None, UiScale.Scaled(1f));
        }

        var pencilClicked = false;
        if (showPencil)
        {
            var px = x + contentWidth - PencilWidth;
            var py = stripY + UiScale.Scaled(6f);
            if (ButtonRenderer.DrawButton(drawList, string.Empty,
                    px, py, PencilWidth, PencilHeight,
                    isPrimary: false, enabled: true))
            {
                pencilClicked = true;
            }
            ChromeRenderer.DrawPencil(drawList,
                px + PencilWidth / 2f,
                py + PencilHeight / 2f,
                PencilHeight - UiScale.Scaled(8f),
                ColorPalette.LightText);
        }

        return new Result(contentWidth, bodyY, pencilClicked);
    }
}
