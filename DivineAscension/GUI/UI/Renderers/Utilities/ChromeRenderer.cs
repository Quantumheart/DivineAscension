using System;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using DivineAscension.GUI.UI.Utilities;
using ImGuiNET;

namespace DivineAscension.GUI.UI.Renderers.Utilities;

/// <summary>
///     Shared codex-chrome helpers for ornamental dividers, drawn diamonds,
///     dotted-leader stat lines, and styled tooltip popups. Pure drawList /
///     ImGui primitives — no state, no events. Diamonds are painted as quads
///     rather than text glyphs because ImGui's default font ranges exclude
///     the Dingbats codepoints we'd otherwise reach for (`✦` etc. would
///     render as `?`).
/// </summary>
[ExcludeFromCodeCoverage]
internal static class ChromeRenderer
{
    private const string LeaderDot = "·"; // Middle dot — U+00B7, inside Latin-1.

    /// <summary>
    ///     Open a tooltip styled to match the rest of the dialog chrome:
    ///     dark-brown popup background, gold border, white text. Caller is
    ///     responsible for writing tooltip content and disposing the returned
    ///     scope (a <c>using</c> block matches the End/Pop pairing).
    /// </summary>
    public static StyledTooltipScope BeginStyledTooltip()
    {
        ImGui.PushStyleColor(ImGuiCol.PopupBg, ColorPalette.DarkBrown);
        ImGui.PushStyleColor(ImGuiCol.Border, ColorPalette.Gold * 0.6f);
        ImGui.PushStyleColor(ImGuiCol.Text, ColorPalette.LightText);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 2f);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 4f);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(10f, 6f));
        ImGui.BeginTooltip();
        return new StyledTooltipScope();
    }

    /// <summary>
    ///     Disposable scope returned by <see cref="BeginStyledTooltip" />. The
    ///     <c>Dispose</c> call closes the tooltip and unwinds the style stack
    ///     in the matching order.
    /// </summary>
    public readonly struct StyledTooltipScope : IDisposable
    {
        public void Dispose()
        {
            ImGui.EndTooltip();
            ImGui.PopStyleVar(3);
            ImGui.PopStyleColor(3);
        }
    }

    /// <summary>
    ///     Paint a small filled rhombus centered at (<paramref name="cx" />,
    ///     <paramref name="cy" />). <paramref name="halfSize" /> is the
    ///     half-extent along each axis (so the diamond spans
    ///     <c>2 * halfSize</c> in width and height).
    /// </summary>
    public static void DrawDiamond(ImDrawListPtr drawList, float cx, float cy, float halfSize,
        Vector4? colorOverride = null)
    {
        if (halfSize <= 0f) return;
        var color = ImGui.ColorConvertFloat4ToU32(colorOverride ?? ColorPalette.Gold);
        var top = new Vector2(cx, cy - halfSize);
        var right = new Vector2(cx + halfSize, cy);
        var bottom = new Vector2(cx, cy + halfSize);
        var left = new Vector2(cx - halfSize, cy);
        drawList.AddQuadFilled(top, right, bottom, left, color);
    }

    /// <summary>Direction a <see cref="DrawChevron" /> triangle points.</summary>
    public enum ChevronDirection { Right, Down, Left, Up }

    /// <summary>
    ///     Paint a small filled triangle as a disclosure chevron. Painted as
    ///     primitives so it renders without Geometric-Shapes glyph coverage in
    ///     the loaded font.
    /// </summary>
    public static void DrawChevron(ImDrawListPtr drawList, float cx, float cy, float size,
        ChevronDirection direction, Vector4? colorOverride = null)
    {
        if (size <= 0f) return;
        var color = ImGui.ColorConvertFloat4ToU32(colorOverride ?? ColorPalette.Gold);
        var half = size / 2f;
        Vector2 a, b, c;
        switch (direction)
        {
            case ChevronDirection.Down:
                a = new Vector2(cx - half, cy - half / 2f);
                b = new Vector2(cx + half, cy - half / 2f);
                c = new Vector2(cx, cy + half / 2f);
                break;
            case ChevronDirection.Up:
                a = new Vector2(cx - half, cy + half / 2f);
                b = new Vector2(cx + half, cy + half / 2f);
                c = new Vector2(cx, cy - half / 2f);
                break;
            case ChevronDirection.Left:
                a = new Vector2(cx + half / 2f, cy - half);
                b = new Vector2(cx - half / 2f, cy);
                c = new Vector2(cx + half / 2f, cy + half);
                break;
            case ChevronDirection.Right:
            default:
                a = new Vector2(cx - half / 2f, cy - half);
                b = new Vector2(cx + half / 2f, cy);
                c = new Vector2(cx - half / 2f, cy + half);
                break;
        }
        drawList.AddTriangleFilled(a, b, c, color);
    }

    /// <summary>
    ///     Paint a slim horizontal divider with a single centered diamond
    ///     ornament: <c>──── ◆ ────</c>. Lines flank the diamond on a shared
    ///     vertical baseline.
    /// </summary>
    public static void DrawDivider(ImDrawListPtr drawList, float x, float y, float width,
        Vector4? colorOverride = null)
    {
        if (width <= 0f) return;

        var color = colorOverride ?? ColorPalette.Gold * 0.55f;
        var colorU32 = ImGui.ColorConvertFloat4ToU32(color);

        const float diamondHalfSize = 4f;
        const float sideGap = 8f;
        var centerX = x + width / 2f;
        var lineY = y + diamondHalfSize; // baseline through the diamond's centre

        var leftLineEnd = centerX - diamondHalfSize - sideGap;
        var rightLineStart = centerX + diamondHalfSize + sideGap;

        if (leftLineEnd > x)
        {
            drawList.AddLine(new Vector2(x, lineY), new Vector2(leftLineEnd, lineY), colorU32, 1f);
        }
        if (rightLineStart < x + width)
        {
            drawList.AddLine(new Vector2(rightLineStart, lineY),
                new Vector2(x + width, lineY), colorU32, 1f);
        }

        DrawDiamond(drawList, centerX, lineY, diamondHalfSize, color);
    }

    /// <summary>
    ///     Paint a triple-diamond divider (<c>──── ✦ ──── ✦ ──── ✦ ────</c>) as
    ///     the louder section break between groups of dividers drawn by
    ///     <see cref="DrawDivider" />. Used as the top/bottom rule around the
    ///     Hallows Order list, separating it from the chapter intro and the
    ///     closing footer.
    /// </summary>
    public static void DrawDividerOrnate(ImDrawListPtr drawList, float x, float y, float width,
        Vector4? colorOverride = null)
    {
        if (width <= 0f) return;

        var color = colorOverride ?? ColorPalette.Gold * 0.55f;
        var colorU32 = ImGui.ColorConvertFloat4ToU32(color);

        const float diamondHalfSize = 4f;
        const float sideGap = 8f;
        var lineY = y + diamondHalfSize;

        // Three diamonds equally spaced across the strip.
        var d0X = x + width * 0.25f;
        var d1X = x + width * 0.50f;
        var d2X = x + width * 0.75f;

        void Segment(float startX, float endX)
        {
            if (endX > startX)
                drawList.AddLine(new Vector2(startX, lineY), new Vector2(endX, lineY), colorU32, 1f);
        }

        Segment(x, d0X - diamondHalfSize - sideGap);
        Segment(d0X + diamondHalfSize + sideGap, d1X - diamondHalfSize - sideGap);
        Segment(d1X + diamondHalfSize + sideGap, d2X - diamondHalfSize - sideGap);
        Segment(d2X + diamondHalfSize + sideGap, x + width);

        DrawDiamond(drawList, d0X, lineY, diamondHalfSize, color);
        DrawDiamond(drawList, d1X, lineY, diamondHalfSize, color);
        DrawDiamond(drawList, d2X, lineY, diamondHalfSize, color);
    }

    /// <summary>
    ///     Paint a small pencil glyph (✎ U+270E) as primitives so it renders
    ///     in fonts without Dingbats coverage. Diagonal filled shaft from
    ///     upper-right to lower-left, triangular graphite tip continuing past
    ///     the shoulder, ferrule band near the eraser end, eraser cap at the
    ///     tail. Drawn at 45° around (cx, cy).
    /// </summary>
    public static void DrawPencil(ImDrawListPtr drawList, float cx, float cy, float size,
        Vector4? colorOverride = null)
    {
        if (size <= 0f) return;
        var bodyVec = colorOverride ?? ColorPalette.Gold;
        var body = ImGui.ColorConvertFloat4ToU32(bodyVec);
        var trim = ImGui.ColorConvertFloat4ToU32(bodyVec * 0.55f);
        var lead = ImGui.ColorConvertFloat4ToU32(new Vector4(0.05f, 0.05f, 0.05f, bodyVec.W));

        // Shaft axis: upper-right → lower-left at 45°.
        const float inv = 0.70710677f; // 1/sqrt(2)
        var d = new Vector2(-inv, inv);  // toward tip
        var n = new Vector2(inv, inv);   // perpendicular (right-of-axis)

        var w = size * 0.18f;             // half-width of shaft
        var tail = new Vector2(cx, cy) - d * (size * 0.45f);
        var shoulder = new Vector2(cx, cy) + d * (size * 0.20f);
        var tipPoint = new Vector2(cx, cy) + d * (size * 0.50f);
        var ferruleEdge = tail + d * (size * 0.22f);
        var eraserEnd = tail - d * (size * 0.04f);

        // Eraser cap (lighter trim block) at the tail end.
        drawList.AddQuadFilled(
            eraserEnd + n * w,
            eraserEnd - n * w,
            ferruleEdge - n * w,
            ferruleEdge + n * w,
            trim);

        // Wood shaft — main filled quad from ferrule edge to the shoulder.
        drawList.AddQuadFilled(
            ferruleEdge + n * w,
            ferruleEdge - n * w,
            shoulder - n * w,
            shoulder + n * w,
            body);

        // Ferrule band — short darker stripe straddling the shaft / eraser join.
        var bandHalf = size * 0.05f;
        var bandA = ferruleEdge - d * bandHalf;
        var bandB = ferruleEdge + d * bandHalf;
        drawList.AddQuadFilled(
            bandA + n * w,
            bandA - n * w,
            bandB - n * w,
            bandB + n * w,
            trim);

        // Wooden tip taper — triangle from shoulder to the lead point.
        drawList.AddTriangleFilled(
            shoulder + n * w,
            shoulder - n * w,
            tipPoint,
            body);

        // Graphite point — small dark triangle at the very tip.
        var leadBase = new Vector2(cx, cy) + d * (size * 0.40f);
        drawList.AddTriangleFilled(
            leadBase + n * (w * 0.45f),
            leadBase - n * (w * 0.45f),
            tipPoint,
            lead);

        // Outline along the shaft so it reads at small sizes.
        drawList.AddLine(ferruleEdge + n * w, shoulder + n * w, trim, 1f);
        drawList.AddLine(ferruleEdge - n * w, shoulder - n * w, trim, 1f);
    }

    /// <summary>
    ///     Paint a dagger glyph († U+2020) as primitives so it renders in
    ///     fonts without that codepoint. Vertical blade, short cross-guard
    ///     near the top, and a small filled pommel at the top end.
    /// </summary>
    public static void DrawDagger(ImDrawListPtr drawList, float cx, float cy, float size,
        Vector4? colorOverride = null)
    {
        if (size <= 0f) return;
        var color = ImGui.ColorConvertFloat4ToU32(colorOverride ?? ColorPalette.Gold);
        var half = size / 2f;

        // Blade — full vertical line.
        drawList.AddLine(new Vector2(cx, cy - half), new Vector2(cx, cy + half), color, 2f);

        // Cross-guard a third of the way down.
        var guardY = cy - half * 0.35f;
        drawList.AddLine(new Vector2(cx - half * 0.45f, guardY),
            new Vector2(cx + half * 0.45f, guardY), color, 2f);

        // Pommel cap.
        drawList.AddCircleFilled(new Vector2(cx, cy - half), MathF.Max(1.5f, half * 0.18f), color);
    }

    /// <summary>
    ///     Paint a sealed-envelope glyph (✉ U+2709) as primitives so it
    ///     renders without Dingbats coverage in the loaded font. A wide
    ///     horizontal rectangle with the front flap drawn as two diagonals
    ///     meeting at the top center.
    /// </summary>
    public static void DrawEnvelope(ImDrawListPtr drawList, float cx, float cy, float size,
        Vector4? colorOverride = null)
    {
        if (size <= 0f) return;
        var color = ImGui.ColorConvertFloat4ToU32(colorOverride ?? ColorPalette.Gold);
        var halfW = size * 0.55f;
        var halfH = size * 0.38f;

        var topLeft = new Vector2(cx - halfW, cy - halfH);
        var topRight = new Vector2(cx + halfW, cy - halfH);
        var bottomLeft = new Vector2(cx - halfW, cy + halfH);
        var bottomRight = new Vector2(cx + halfW, cy + halfH);

        // Envelope body — outlined rectangle.
        drawList.AddRect(topLeft, bottomRight, color, 1f, ImDrawFlags.None, 1.5f);

        // Flap — two diagonals from the top corners to the top centre.
        var apex = new Vector2(cx, cy);
        drawList.AddLine(topLeft, apex, color, 1.5f);
        drawList.AddLine(topRight, apex, color, 1.5f);
    }

    /// <summary>
    ///     Paint a heraldic banner / standard glyph (⚐ U+2690) as primitives
    ///     so it renders without Miscellaneous-Symbols coverage in the loaded
    ///     font. Vertical staff with a triangular pennant flag attached to
    ///     the upper half — used by the civilization Letters chapter where
    ///     realms aren't deity-aligned and the deity-domain glyph wouldn't
    ///     apply.
    /// </summary>
    public static void DrawBanner(ImDrawListPtr drawList, float cx, float cy, float size,
        Vector4? colorOverride = null)
    {
        if (size <= 0f) return;
        var color = ImGui.ColorConvertFloat4ToU32(colorOverride ?? ColorPalette.Gold);
        var half = size / 2f;

        // Staff — full vertical line slightly off-centre so the flag has
        // room to swing out to the right of it.
        var staffX = cx - half * 0.35f;
        drawList.AddLine(new Vector2(staffX, cy - half),
            new Vector2(staffX, cy + half), color, 2f);

        // Pennant — triangle hanging from the staff's upper third, pointing
        // out to the right.
        var hoistTop = new Vector2(staffX, cy - half * 0.85f);
        var hoistBottom = new Vector2(staffX, cy - half * 0.10f);
        var fly = new Vector2(cx + half * 0.7f, cy - half * 0.48f);
        drawList.AddTriangleFilled(hoistTop, hoistBottom, fly, color);
    }

    /// <summary>
    ///     Paint a refresh arrow glyph (↻ U+21BB) as primitives so it renders
    ///     without Arrows-block coverage in the loaded font. Three-quarter
    ///     circular arc opening toward the upper-right with a small triangular
    ///     arrowhead at the end of the arc.
    /// </summary>
    public static void DrawRefreshArrow(ImDrawListPtr drawList, float cx, float cy, float size,
        Vector4? colorOverride = null)
    {
        if (size <= 0f) return;
        var color = ImGui.ColorConvertFloat4ToU32(colorOverride ?? ColorPalette.Gold);
        var radius = size * 0.4f;

        // Arc from ~30° (upper-right) sweeping counter-clockwise ~270° to the
        // right side, leaving a gap in the upper-right for the arrowhead.
        const int segments = 24;
        const float arcStart = -MathF.PI * 0.10f;       // just above 3-o'clock
        const float arcEnd = MathF.PI * 1.55f;          // ~280° sweep
        Vector2 prev = default;
        for (var i = 0; i <= segments; i++)
        {
            var t = i / (float)segments;
            var theta = arcStart + (arcEnd - arcStart) * t;
            var p = new Vector2(cx + radius * MathF.Cos(theta), cy + radius * MathF.Sin(theta));
            if (i > 0) drawList.AddLine(prev, p, color, 2f);
            prev = p;
        }

        // Arrowhead at arc start — small triangle pointing tangent to the circle.
        var tipAngle = arcStart;
        var tipBase = new Vector2(cx + radius * MathF.Cos(tipAngle), cy + radius * MathF.Sin(tipAngle));
        var headLen = size * 0.18f;
        var ax = tipBase.X + headLen * MathF.Cos(tipAngle + MathF.PI / 2f);
        var ay = tipBase.Y + headLen * MathF.Sin(tipAngle + MathF.PI / 2f);
        var bx = tipBase.X + headLen * MathF.Cos(tipAngle - MathF.PI / 2f);
        var by = tipBase.Y + headLen * MathF.Sin(tipAngle - MathF.PI / 2f);
        var tip = new Vector2(
            tipBase.X + headLen * 1.2f * MathF.Cos(tipAngle),
            tipBase.Y + headLen * 1.2f * MathF.Sin(tipAngle));
        drawList.AddTriangleFilled(new Vector2(ax, ay), new Vector2(bx, by), tip, color);
    }

    /// <summary>
    ///     Paint a leader row: <c>Label · · · · · · Value</c> spanning
    ///     <paramref name="width" />, with the dot run sized to fill the gap
    ///     between the label end and the right-aligned value.
    /// </summary>
    public static void DrawLeader(ImDrawListPtr drawList, string label, string value,
        float x, float y, float width,
        Vector4? labelColor = null, Vector4? valueColor = null, Vector4? dotColor = null)
    {
        var labelCol = ImGui.ColorConvertFloat4ToU32(labelColor ?? ColorPalette.Grey);
        var valueCol = ImGui.ColorConvertFloat4ToU32(valueColor ?? ColorPalette.White);
        var dotCol = ImGui.ColorConvertFloat4ToU32(dotColor ?? (ColorPalette.Gold * 0.45f));

        var labelSize = ImGui.CalcTextSize(label);
        var valueSize = ImGui.CalcTextSize(value);

        drawList.AddText(new Vector2(x, y), labelCol, label);
        drawList.AddText(new Vector2(x + width - valueSize.X, y), valueCol, value);

        const float padding = 6f;
        var leadersStart = x + labelSize.X + padding;
        var leadersEnd = x + width - valueSize.X - padding;
        var leadersWidth = leadersEnd - leadersStart;
        if (leadersWidth <= 0f) return;

        var dotWidth = ImGui.CalcTextSize(LeaderDot).X;
        if (dotWidth <= 0f) return;

        var step = dotWidth * 2f;
        var dotCount = (int)(leadersWidth / step);
        if (dotCount <= 0) return;

        var dotsTextWidth = dotCount * step - (step - dotWidth);
        var dotsX = leadersStart + (leadersWidth - dotsTextWidth) / 2f;
        for (var i = 0; i < dotCount; i++)
        {
            drawList.AddText(new Vector2(dotsX + i * step, y), dotCol, LeaderDot);
        }
    }
}
