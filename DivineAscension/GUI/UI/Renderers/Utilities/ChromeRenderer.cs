using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using DivineAscension.GUI.UI.Utilities;
using ImGuiNET;

namespace DivineAscension.GUI.UI.Renderers.Utilities;

/// <summary>
///     Shared codex-chrome helpers for ornamental dividers and dotted-leader
///     stat lines. Pure drawList primitives — no state, no events.
/// </summary>
[ExcludeFromCodeCoverage]
internal static class ChromeRenderer
{
    private const string DividerOrnament = "✦"; // ✦ BLACK FOUR POINTED STAR
    private const string LeaderDot = "·";

    /// <summary>
    ///     Paint a slim horizontal divider with a single centered ornament:
    ///     <c>─── ✦ ───</c>. Lines flank the glyph; the glyph is drawn on the
    ///     same baseline as the lines so they read as one unit.
    /// </summary>
    public static void DrawDivider(ImDrawListPtr drawList, float x, float y, float width,
        Vector4? colorOverride = null)
    {
        if (width <= 0f) return;

        var color = colorOverride ?? ColorPalette.Gold * 0.55f;
        var colorU32 = ImGui.ColorConvertFloat4ToU32(color);

        var ornamentSize = ImGui.CalcTextSize(DividerOrnament);
        // Vertical anchor: centre the ornament glyph on the line; the line
        // sits at the glyph's vertical midpoint.
        var lineY = y + ornamentSize.Y / 2f;

        const float sideGap = 8f;
        var halfOrnamentWidth = ornamentSize.X / 2f;
        var centerX = x + width / 2f;
        var leftLineEnd = centerX - halfOrnamentWidth - sideGap;
        var rightLineStart = centerX + halfOrnamentWidth + sideGap;

        if (leftLineEnd > x)
        {
            drawList.AddLine(new Vector2(x, lineY), new Vector2(leftLineEnd, lineY), colorU32, 1f);
        }
        if (rightLineStart < x + width)
        {
            drawList.AddLine(new Vector2(rightLineStart, lineY),
                new Vector2(x + width, lineY), colorU32, 1f);
        }

        var ornamentX = centerX - halfOrnamentWidth;
        drawList.AddText(new Vector2(ornamentX, y), colorU32, DividerOrnament);
    }

    /// <summary>
    ///     Paint a leader row: <c>Label · · · · · · Value</c> spanning
    ///     <paramref name="width" />, with the dot run sized to exactly fill
    ///     the gap between the label end and the right-aligned value.
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

        // Use spaced dots ("·") for a leader rather than a solid run; computed
        // count uses 2x the dot width so the dots breathe.
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
