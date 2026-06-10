using System;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using DivineAscension.GUI.UI.Utilities;
using DivineAscension.Models.Enum;
using ImGuiNET;

namespace DivineAscension.GUI.UI.Renderers.Utilities;

/// <summary>
///     Paint a realm's heraldic mark inside the rect <c>(min, max)</c> using
///     drawList primitives only — crown (Sovereign), scales (Mercantile),
///     crossed swords (Martial), sparkle (Mystic), sun (Ascetic). Reuses the
///     ink-on-vellum vocabulary of <see cref="DomainGlyphRenderer" /> so the
///     Standing of Realms rows read as a manuscript ledger (#500).
/// </summary>
[ExcludeFromCodeCoverage]
internal static class EthosGlyphRenderer
{
    public static void Draw(ImDrawListPtr drawList, CivilizationEthos ethos,
        Vector2 min, Vector2 max, Vector4? colorOverride = null)
    {
        var color = ImGui.ColorConvertFloat4ToU32(colorOverride ?? ColorPalette.Grey);
        var w = max.X - min.X;
        var h = max.Y - min.Y;
        if (w <= 2f || h <= 2f) return;

        var cx = (min.X + max.X) * 0.5f;
        var cy = (min.Y + max.Y) * 0.5f;
        var s = MathF.Min(w, h);

        switch (ethos)
        {
            case CivilizationEthos.Sovereign:
                DrawCrown(drawList, cx, cy, s, color);
                return;
            case CivilizationEthos.Mercantile:
                DrawScales(drawList, cx, cy, s, color);
                return;
            case CivilizationEthos.Martial:
                DrawCrossedSwords(drawList, cx, cy, s, color);
                return;
            case CivilizationEthos.Mystic:
                DrawSparkle(drawList, cx, cy, s, color);
                return;
            case CivilizationEthos.Ascetic:
                DrawSun(drawList, cx, cy, s, color);
                return;
            default:
                drawList.AddCircle(new Vector2(cx, cy), s * 0.3f, color, 16, UiScale.Scaled(1.5f));
                return;
        }
    }

    private static void DrawCrown(ImDrawListPtr drawList, float cx, float cy, float s, uint color)
    {
        var halfW = s * 0.38f;
        var baseY = cy + s * 0.22f;
        var topY = cy - s * 0.26f;
        var bandH = MathF.Max(2f, s * 0.10f);

        // Three points rising to a band.
        for (var i = -1; i <= 1; i++)
        {
            var px = cx + i * halfW;
            drawList.AddTriangleFilled(
                new Vector2(px - halfW * 0.5f, baseY),
                new Vector2(px + halfW * 0.5f, baseY),
                new Vector2(px, topY),
                color);
        }

        drawList.AddRectFilled(
            new Vector2(cx - halfW, baseY - bandH * 0.5f),
            new Vector2(cx + halfW, baseY + bandH * 0.5f),
            color, UiScale.Scaled(1f));
    }

    private static void DrawScales(ImDrawListPtr drawList, float cx, float cy, float s, uint color)
    {
        var beam = MathF.Max(1.5f, s * 0.05f);
        var armX = s * 0.34f;
        var beamY = cy - s * 0.22f;

        // Central post + crossbeam.
        drawList.AddLine(new Vector2(cx, beamY - s * 0.12f), new Vector2(cx, cy + s * 0.28f), color, beam);
        drawList.AddLine(new Vector2(cx - armX, beamY), new Vector2(cx + armX, beamY), color, beam);

        // Two pans hanging from the beam ends.
        var panY = beamY + s * 0.22f;
        var panR = s * 0.13f;
        for (var side = -1; side <= 1; side += 2)
        {
            var ex = cx + side * armX;
            drawList.AddLine(new Vector2(ex, beamY), new Vector2(ex, panY), color, MathF.Max(1f, s * 0.03f));
            drawList.AddCircle(new Vector2(ex, panY + panR * 0.4f), panR, color, 16, MathF.Max(1.2f, s * 0.035f));
        }
    }

    private static void DrawCrossedSwords(ImDrawListPtr drawList, float cx, float cy, float s, uint color)
    {
        var thick = MathF.Max(1.8f, s * 0.06f);
        var arm = s * 0.40f;
        const float k = 0.70710677f;

        // Two blades crossing through the centre.
        drawList.AddLine(new Vector2(cx - k * arm, cy + k * arm), new Vector2(cx + k * arm, cy - k * arm), color, thick);
        drawList.AddLine(new Vector2(cx + k * arm, cy + k * arm), new Vector2(cx - k * arm, cy - k * arm), color, thick);

        // Short crossguards near the hilts (bottom corners).
        var g = s * 0.12f;
        drawList.AddLine(new Vector2(cx - k * arm - g, cy + k * arm - g),
            new Vector2(cx - k * arm + g, cy + k * arm + g), color, MathF.Max(1.2f, s * 0.035f));
        drawList.AddLine(new Vector2(cx + k * arm - g, cy + k * arm + g),
            new Vector2(cx + k * arm + g, cy + k * arm - g), color, MathF.Max(1.2f, s * 0.035f));
    }

    private static void DrawSparkle(ImDrawListPtr drawList, float cx, float cy, float s, uint color)
    {
        // Four tapered spikes from the centre — a heraldic mullet/sparkle (✦).
        var tip = s * 0.42f;
        var waist = s * 0.12f;
        var c = new Vector2(cx, cy);

        // N, E, S, W spikes, each a kite split into two triangles via the waist.
        DrawSpike(drawList, c, new Vector2(cx, cy - tip), waist, color, vertical: true);
        DrawSpike(drawList, c, new Vector2(cx, cy + tip), waist, color, vertical: true);
        DrawSpike(drawList, c, new Vector2(cx - tip, cy), waist, color, vertical: false);
        DrawSpike(drawList, c, new Vector2(cx + tip, cy), waist, color, vertical: false);
    }

    private static void DrawSpike(ImDrawListPtr drawList, Vector2 center, Vector2 tip, float waist, uint color,
        bool vertical)
    {
        var left = vertical
            ? new Vector2(center.X - waist, center.Y)
            : new Vector2(center.X, center.Y - waist);
        var right = vertical
            ? new Vector2(center.X + waist, center.Y)
            : new Vector2(center.X, center.Y + waist);
        drawList.AddTriangleFilled(left, right, tip, color);
    }

    private static void DrawSun(ImDrawListPtr drawList, float cx, float cy, float s, uint color)
    {
        var coreR = s * 0.16f;
        var center = new Vector2(cx, cy);
        drawList.AddCircleFilled(center, coreR, color);

        // Eight rays around the disc.
        var inner = coreR + s * 0.05f;
        var outer = s * 0.42f;
        var rayW = MathF.Max(1.2f, s * 0.035f);
        for (var i = 0; i < 8; i++)
        {
            var a = i * (MathF.PI / 4f);
            var dx = MathF.Cos(a);
            var dy = MathF.Sin(a);
            drawList.AddLine(
                new Vector2(cx + dx * inner, cy + dy * inner),
                new Vector2(cx + dx * outer, cy + dy * outer),
                color, rayW);
        }
    }
}
