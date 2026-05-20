using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using DivineAscension.GUI.Events.EdgeBookmarks;
using DivineAscension.GUI.UI.Layout;
using DivineAscension.GUI.UI.Renderers.Utilities;
using DivineAscension.GUI.UI.Utilities;
using ImGuiNET;

namespace DivineAscension.GUI.UI.Renderers.EdgeBookmarks;

/// <summary>
///     Paints colored ribbons protruding from the right page edge of the
///     codex. Each ribbon is a fast-jump shortcut to one main section.
///     Pure renderer — accepts the view model and a <see cref="UiRect"/>,
///     returns click events for the layout coordinator.
/// </summary>
[ExcludeFromCodeCoverage]
internal static class EdgeBookmarkRenderer
{
    private const float RibbonHeight = 44f;
    private const float RibbonSpacing = 10f;
    private const float TopOffset = 12f;
    private const float NotchDepth = 8f;
    private const float ActiveExtrude = 4f;

    public static IReadOnlyList<EdgeBookmarkEvent> Draw(UiRect rect, EdgeBookmarkRibbonStack stack)
    {
        var events = new List<EdgeBookmarkEvent>();
        if (rect.W <= 0f || rect.H <= 0f || stack.Bookmarks.Count == 0) return events;

        var drawList = ImGui.GetWindowDrawList();
        var y = rect.Y + TopOffset;

        for (var i = 0; i < stack.Bookmarks.Count; i++)
        {
            var bm = stack.Bookmarks[i];

            // Active bookmark extrudes slightly further out so it reads as
            // "the page currently flipped to". Disabled bookmarks render at
            // a muted alpha and skip click handling.
            var extrude = bm.IsActive ? ActiveExtrude : 0f;
            var x0 = rect.X - extrude;
            var x1 = rect.Right;
            var y0 = y;
            var y1 = y + RibbonHeight;

            // Click target spans the ribbon body.
            ImGui.SetCursorScreenPos(new Vector2(x0, y0));
            var clicked = ImGui.InvisibleButton(
                $"##da-bookmark-{i}",
                new Vector2(x1 - x0, RibbonHeight));
            var hovered = ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled);

            if (clicked && !bm.IsDisabled)
            {
                events.Add(new EdgeBookmarkEvent.Jump(bm.Target));
            }
            if (hovered && !bm.IsDisabled)
            {
                ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
            }

            DrawRibbon(drawList, x0, y0, x1, y1, bm, hovered);

            y += RibbonHeight + RibbonSpacing;
            if (y > rect.Bottom) break;

            if (hovered)
            {
                using var _ = ChromeRenderer.BeginStyledTooltip();
                ImGui.TextUnformatted(bm.Tooltip);
            }
        }

        return events;
    }

    /// <summary>
    ///     Cloth-ribbon shape: rectangle body, V-notch cut into the right edge
    ///     so it reads as fabric hanging from the page. Color from the deity
    ///     domain palette; muted alpha when disabled or non-active; gold trim
    ///     for the active ribbon.
    /// </summary>
    private static void DrawRibbon(ImDrawListPtr drawList, float x0, float y0, float x1, float y1,
        EdgeBookmarkViewModel bm, bool hovered)
    {
        var baseColor = bm.IsDisabled
            ? ColorPalette.WithAlpha(bm.RibbonColor, 0.25f)
            : (hovered ? ColorPalette.Lighten(bm.RibbonColor, 1.15f) : bm.RibbonColor);
        var shadowColor = ColorPalette.Darken(baseColor, 0.55f);
        var fill = ImGui.ColorConvertFloat4ToU32(baseColor);
        var shadow = ImGui.ColorConvertFloat4ToU32(shadowColor);

        var midY = (y0 + y1) * 0.5f;
        var notchX = x1 - NotchDepth;

        // Main ribbon body — pentagon with notched right edge.
        var body = new[]
        {
            new Vector2(x0, y0),
            new Vector2(notchX, y0),
            new Vector2(x1, midY),
            new Vector2(notchX, y1),
            new Vector2(x0, y1)
        };
        drawList.AddConvexPolyFilled(ref body[0], 5, fill);

        // Shadow stripe along the bottom edge to suggest cloth thickness.
        var shadowPoly = new[]
        {
            new Vector2(x0, y1 - 3f),
            new Vector2(notchX, y1 - 3f),
            new Vector2(x1, midY + 1.5f),
            new Vector2(notchX, y1),
            new Vector2(x0, y1)
        };
        drawList.AddConvexPolyFilled(ref shadowPoly[0], 5, shadow);

        // Active ribbon picks up a gold outline.
        if (bm.IsActive)
        {
            var trim = ImGui.ColorConvertFloat4ToU32(ColorPalette.Gold);
            var outline = new[]
            {
                new Vector2(x0, y0),
                new Vector2(notchX, y0),
                new Vector2(x1, midY),
                new Vector2(notchX, y1),
                new Vector2(x0, y1),
                new Vector2(x0, y0)
            };
            drawList.AddPolyline(ref outline[0], outline.Length, trim, ImDrawFlags.None, 1.5f);
        }

        // Stamp letter centered on the ribbon body. Cream-white on colored
        // ribbons, grey on the disabled '?' ribbon.
        var textColor = ImGui.ColorConvertFloat4ToU32(bm.IsDisabled
            ? ColorPalette.Grey
            : ColorPalette.White);
        var stampSize = ImGui.CalcTextSize(bm.Stamp);
        var bodyCenter = (x0 + notchX) * 0.5f;
        drawList.AddText(
            new Vector2(bodyCenter - stampSize.X * 0.5f, midY - stampSize.Y * 0.5f),
            textColor,
            bm.Stamp);
    }
}
