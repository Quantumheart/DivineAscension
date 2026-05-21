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
    public const float ScrollbarGutter = 20f;

    /// <summary>Top padding above the title strip — first line of vellum.</summary>
    public const float TopPadding = 8f;

    private const float GlyphGap = 8f;
    private const float PencilGap = 8f;
    private const float PencilWidth = 22f;
    private const float PencilHeight = 22f;

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
        bool showPencil = false)
    {
        var stripY = paneY + TopPadding - scrollY;
        var contentWidth = paneWidth - ScrollbarGutter;

        var pencilReservation = showPencil ? PencilWidth + PencilGap : 0f;
        var glyphReservation = rightGlyph.HasValue
            ? PaneHeaderRenderer.IconSize + GlyphGap
            : 0f;

        // Title + divider — divider spans full contentWidth so it lines up
        // with every section divider drawn at the same width below.
        var bodyY = PaneHeaderRenderer.Draw(drawList, title, x, stripY, contentWidth);

        if (!string.IsNullOrEmpty(rightTitle))
        {
            var rightScale = FontSizes.PageTitle / FontSizes.SubsectionLabel;
            var rightTextWidth = ImGui.CalcTextSize(rightTitle).X * rightScale;
            var anchorX = x + contentWidth - pencilReservation - glyphReservation;
            if (glyphReservation > 0f) anchorX -= GlyphGap;
            var rightX = anchorX - rightTextWidth;
            drawList.AddText(ImGui.GetFont(), FontSizes.PageTitle,
                new Vector2(rightX, stripY + 4f),
                ImGui.ColorConvertFloat4ToU32(ColorPalette.Gold), rightTitle);
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
                4f, ImDrawFlags.None, 1f);
        }

        var pencilClicked = false;
        if (showPencil)
        {
            var px = x + contentWidth - PencilWidth;
            var py = stripY + 6f;
            if (ButtonRenderer.DrawButton(drawList, string.Empty,
                    px, py, PencilWidth, PencilHeight,
                    isPrimary: false, enabled: true))
            {
                pencilClicked = true;
            }
            ChromeRenderer.DrawPencil(drawList,
                px + PencilWidth / 2f,
                py + PencilHeight / 2f,
                PencilHeight - 8f,
                ColorPalette.LightText);
        }

        return new Result(contentWidth, bodyY, pencilClicked);
    }
}
