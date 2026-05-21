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
    ///     Paint a small pencil glyph (✎ U+270E) as primitives so it renders
    ///     in fonts without Dingbats coverage. Diagonal body from upper-right
    ///     to lower-left, triangular graphite tip at the lower-left end, and
    ///     a square ferrule at the upper-right end.
    /// </summary>
    public static void DrawPencil(ImDrawListPtr drawList, float cx, float cy, float size,
        Vector4? colorOverride = null)
    {
        if (size <= 0f) return;
        var color = ImGui.ColorConvertFloat4ToU32(colorOverride ?? ColorPalette.Gold);
        var half = size / 2f;

        // Body line: upper-right → lower-left diagonal.
        var bodyStart = new Vector2(cx + half * 0.7f, cy - half * 0.7f);
        var bodyEnd = new Vector2(cx - half * 0.3f, cy + half * 0.3f);
        drawList.AddLine(bodyStart, bodyEnd, color, 2f);

        // Graphite tip — small triangle pointing further down-left.
        var tipA = new Vector2(cx - half * 0.3f, cy + half * 0.3f);
        var tipB = new Vector2(cx - half * 0.1f, cy + half * 0.55f);
        var tipC = new Vector2(cx - half * 0.55f, cy + half * 0.1f);
        drawList.AddTriangleFilled(tipA, tipB, tipC, color);

        // Ferrule — short cross-line at the eraser end.
        var ferruleA = new Vector2(cx + half * 0.4f, cy - half * 0.85f);
        var ferruleB = new Vector2(cx + half * 0.85f, cy - half * 0.4f);
        drawList.AddLine(ferruleA, ferruleB, color, 2f);
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
