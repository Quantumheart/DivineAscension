using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using DivineAscension.Constants;
using DivineAscension.GUI.UI.Layout;
using DivineAscension.GUI.UI.Renderers.Utilities;
using DivineAscension.GUI.UI.Utilities;
using DivineAscension.Services;
using ImGuiNET;

namespace DivineAscension.GUI.UI.Renderers.Sidebar;

/// <summary>
///     Paints the page-turn affordance at the bottom of the right-page content
///     rect: a back/forward button pair on the same row, then a centered
///     <c>─── page N of M ───</c> indicator below. Also defines invisible hit
///     regions along the left and right edges of the page (the "gutter") so
///     clicking the page margin flips. All routes emit the same
///     <see cref="SidebarEvent.ItemClicked" /> the sidebar uses, so navigation
///     stays single-pathed.
/// </summary>
[ExcludeFromCodeCoverage]
internal static class PageTurnFooterRenderer
{
    public const float FooterHeight = 56f;
    public const float GutterWidth = 14f;

    private const float ButtonWidth = 140f;
    private const float ButtonHeight = 26f;
    private const float ButtonChevronSize = 9f;
    private const float ButtonChevronPadding = 10f;
    private const float IndicatorBaseline = 38f;
    private const float IndicatorSidePadding = 12f;
    private const float IndicatorLineGap = 8f;

    /// <summary>
    ///     Draw the page-turn footer and gutter hit zones.
    /// </summary>
    /// <param name="footer">Strip allocated for buttons + indicator (bottom of the page).</param>
    /// <param name="pageRect">Full page content rect; gutters live along its left/right edges.</param>
    /// <param name="position">Current page position in the flattened enabled-page sequence.</param>
    public static IReadOnlyList<SidebarEvent> Draw(UiRect footer, UiRect pageRect,
        PageTurnNavigator.PagePosition position)
    {
        var events = new List<SidebarEvent>();
        if (footer.W <= 0f || footer.H <= 0f) return events;

        var drawList = ImGui.GetWindowDrawList();

        DrawButtonRow(footer, position, events);
        DrawPageIndicator(drawList, footer, position);
        DrawGutterHits(drawList, footer, pageRect, position, events);

        return events;
    }

    private static void DrawButtonRow(UiRect footer, PageTurnNavigator.PagePosition pos,
        List<SidebarEvent> events)
    {
        var label = LocalizationService.Instance.Get(LocalizationKeys.SIDEBAR_PAGE_TURN_PREVIOUS);
        var nextLabel = LocalizationService.Instance.Get(LocalizationKeys.SIDEBAR_PAGE_TURN_NEXT);
        var rowY = footer.Y + 4f;

        if (DrawTurnButton("##da-pageturn-prev", new Vector2(footer.X, rowY),
                label, pos.Previous.HasValue, ChromeRenderer.ChevronDirection.Left))
        {
            if (pos.Previous.HasValue) events.Add(new SidebarEvent.ItemClicked(pos.Previous.Value));
        }

        if (DrawTurnButton("##da-pageturn-next", new Vector2(footer.Right - ButtonWidth, rowY),
                nextLabel, pos.Next.HasValue, ChromeRenderer.ChevronDirection.Right))
        {
            if (pos.Next.HasValue) events.Add(new SidebarEvent.ItemClicked(pos.Next.Value));
        }
    }

    private static bool DrawTurnButton(string id, Vector2 origin, string label, bool enabled,
        ChromeRenderer.ChevronDirection chevronDirection)
    {
        ImGui.SetCursorScreenPos(origin);
        if (!enabled)
        {
            ImGui.PushStyleColor(ImGuiCol.Button, ColorPalette.WithAlpha(ColorPalette.DarkBrown, 0.35f));
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, ColorPalette.WithAlpha(ColorPalette.DarkBrown, 0.35f));
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, ColorPalette.WithAlpha(ColorPalette.DarkBrown, 0.35f));
            ImGui.PushStyleColor(ImGuiCol.Text, ColorPalette.DisabledGray);
        }
        else
        {
            ImGui.PushStyleColor(ImGuiCol.Button, ColorPalette.DarkBrown);
            ImGui.PushStyleColor(ImGuiCol.Text, ColorPalette.LightText);
        }

        // Empty-label Button owns hit + frame; the chevron + centred text are
        // painted on top so the triangle stays a primitive (no Geometric-
        // Shapes glyph dependency). Gate the click manually so disabled
        // boundary buttons stay non-interactive.
        var clicked = ImGui.Button(id, new Vector2(ButtonWidth, ButtonHeight)) && enabled;

        var drawList = ImGui.GetWindowDrawList();
        var fontSize = ImGui.GetFontSize();
        var textSize = ImGui.CalcTextSize(label);
        var textY = origin.Y + (ButtonHeight - fontSize) / 2f;
        var labelColor = enabled ? ColorPalette.LightText : ColorPalette.DisabledGray;
        var chevronColor = enabled ? ColorPalette.Gold : ColorPalette.Gold * 0.4f;

        // Layout: [chevron]  label  on the "leading" side. For Left chevron the
        // triangle sits on the left edge with text right of it; for Right the
        // triangle sits on the right edge with text left of it.
        var contentWidth = textSize.X + ButtonChevronSize + ButtonChevronPadding;
        var contentX = origin.X + (ButtonWidth - contentWidth) / 2f;
        float chevronCx;
        float textX;
        if (chevronDirection == ChromeRenderer.ChevronDirection.Left)
        {
            chevronCx = contentX + ButtonChevronSize / 2f;
            textX = chevronCx + ButtonChevronSize / 2f + ButtonChevronPadding;
        }
        else
        {
            textX = contentX;
            chevronCx = textX + textSize.X + ButtonChevronPadding + ButtonChevronSize / 2f;
        }
        var chevronCy = origin.Y + ButtonHeight / 2f;

        ChromeRenderer.DrawChevron(drawList, chevronCx, chevronCy, ButtonChevronSize,
            chevronDirection, chevronColor);
        drawList.AddText(new Vector2(textX, textY),
            ImGui.ColorConvertFloat4ToU32(labelColor), label);

        if (!enabled) ImGui.PopStyleColor(4);
        else ImGui.PopStyleColor(2);
        return clicked;
    }

    private static void DrawPageIndicator(ImDrawListPtr drawList, UiRect footer,
        PageTurnNavigator.PagePosition pos)
    {
        if (pos.Total <= 0) return;

        // -1 ⇒ active nav not in enabled list (defensive); skip the indicator
        // rather than render "page 0 of N".
        var displayIndex = pos.Index >= 0 ? pos.Index + 1 : 0;
        if (displayIndex <= 0) return;

        var text = LocalizationService.Instance.Get(
            LocalizationKeys.SIDEBAR_PAGE_TURN_INDICATOR, displayIndex, pos.Total);

        var textSize = ImGui.CalcTextSize(text);
        var y = footer.Y + IndicatorBaseline;
        var centerX = footer.X + footer.W / 2f;
        var textX = centerX - textSize.X / 2f;
        var textY = y - textSize.Y / 2f;

        var textColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.Gold * 0.85f);
        var lineColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.Gold * 0.55f);

        drawList.AddText(new Vector2(textX, textY), textColor, text);

        // Short flanking lines, mirroring the codex divider style but bracketing
        // the text instead of a diamond ornament.
        var leftEnd = textX - IndicatorLineGap;
        var rightStart = textX + textSize.X + IndicatorLineGap;
        var leftStart = footer.X + IndicatorSidePadding;
        var rightEnd = footer.Right - IndicatorSidePadding;

        if (leftEnd > leftStart)
        {
            drawList.AddLine(new Vector2(leftStart, y), new Vector2(leftEnd, y), lineColor, 1f);
        }
        if (rightStart < rightEnd)
        {
            drawList.AddLine(new Vector2(rightStart, y), new Vector2(rightEnd, y), lineColor, 1f);
        }
    }

    private static void DrawGutterHits(ImDrawListPtr drawList, UiRect footer, UiRect pageRect,
        PageTurnNavigator.PagePosition pos, List<SidebarEvent> events)
    {
        // Gutters span the page content above the footer so a margin click in
        // the body reads as "turn the page". Mouse interaction is manual
        // (HoveringRect + IsAnyItemHovered guard) so ImGui widgets inside the
        // content area keep priority — only true-margin clicks flip.
        var gutterTop = pageRect.Y;
        var gutterBottom = footer.Y;
        if (gutterBottom <= gutterTop) return;

        var leftRectMin = new Vector2(pageRect.X, gutterTop);
        var leftRectMax = new Vector2(pageRect.X + GutterWidth, gutterBottom);
        var rightRectMin = new Vector2(pageRect.Right - GutterWidth, gutterTop);
        var rightRectMax = new Vector2(pageRect.Right, gutterBottom);

        var leftHover = pos.Previous.HasValue
                        && ImGui.IsMouseHoveringRect(leftRectMin, leftRectMax)
                        && !ImGui.IsAnyItemHovered();
        var rightHover = pos.Next.HasValue
                         && ImGui.IsMouseHoveringRect(rightRectMin, rightRectMax)
                         && !ImGui.IsAnyItemHovered();

        if (leftHover)
        {
            var hoverColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.WithAlpha(ColorPalette.Gold, 0.08f));
            drawList.AddRectFilled(leftRectMin, leftRectMax, hoverColor);
            ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
            if (ImGui.IsMouseClicked(ImGuiMouseButton.Left))
            {
                events.Add(new SidebarEvent.ItemClicked(pos.Previous!.Value));
            }
        }

        if (rightHover)
        {
            var hoverColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.WithAlpha(ColorPalette.Gold, 0.08f));
            drawList.AddRectFilled(rightRectMin, rightRectMax, hoverColor);
            ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
            if (ImGui.IsMouseClicked(ImGuiMouseButton.Left))
            {
                events.Add(new SidebarEvent.ItemClicked(pos.Next!.Value));
            }
        }
    }
}
