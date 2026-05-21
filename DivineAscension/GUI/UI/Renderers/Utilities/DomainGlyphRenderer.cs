using System;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using DivineAscension.GUI.UI.Utilities;
using DivineAscension.Models.Enum;
using ImGuiNET;

namespace DivineAscension.GUI.UI.Renderers.Utilities;

/// <summary>
///     Paint a domain mark inside the rect <c>(min, max)</c> using drawList
///     primitives only — hammer (Craft), leaf (Wild), crossed swords
///     (Conquest), wheat (Harvest), mountain (Stone). Used in place of the
///     PNG deity icons so the chapter chrome reads as ink-on-vellum and
///     stays glyph-coverage-independent.
/// </summary>
[ExcludeFromCodeCoverage]
internal static class DomainGlyphRenderer
{
    public static void Draw(ImDrawListPtr drawList, DeityDomain domain,
        Vector2 min, Vector2 max, Vector4? colorOverride = null)
    {
        var color = ImGui.ColorConvertFloat4ToU32(colorOverride ?? ColorPalette.LightText);
        var w = max.X - min.X;
        var h = max.Y - min.Y;
        if (w <= 2f || h <= 2f) return;

        var cx = (min.X + max.X) * 0.5f;
        var cy = (min.Y + max.Y) * 0.5f;
        var s = MathF.Min(w, h);

        switch (domain)
        {
            case DeityDomain.Craft:
                DrawHammer(drawList, cx, cy, s, color);
                return;
            case DeityDomain.Wild:
                DrawLeaf(drawList, cx, cy, s, color);
                return;
            case DeityDomain.Conquest:
                DrawCrossedSwords(drawList, cx, cy, s, color);
                return;
            case DeityDomain.Harvest:
                DrawWheat(drawList, cx, cy, s, color);
                return;
            case DeityDomain.Stone:
                DrawMountain(drawList, cx, cy, s, color);
                return;
            default:
                drawList.AddCircle(new Vector2(cx, cy), s * 0.3f, color, 16, 1.5f);
                return;
        }
    }

    private static void DrawHammer(ImDrawListPtr drawList, float cx, float cy, float s, uint color)
    {
        var headW = s * 0.55f;
        var headH = s * 0.22f;
        var handleW = s * 0.10f;
        var handleH = s * 0.55f;
        // Total vertical extent = head + handle; center the whole shape on cy.
        var totalH = headH + handleH;
        var topY = cy - totalH * 0.5f;

        var headMin = new Vector2(cx - headW * 0.5f, topY);
        var headMax = new Vector2(cx + headW * 0.5f, topY + headH);
        drawList.AddRectFilled(headMin, headMax, color, 2f);

        var handleMin = new Vector2(cx - handleW * 0.5f, headMax.Y);
        var handleMax = new Vector2(cx + handleW * 0.5f, headMax.Y + handleH);
        drawList.AddRectFilled(handleMin, handleMax, color, 1f);
    }

    private static void DrawLeaf(ImDrawListPtr drawList, float cx, float cy, float s, uint color)
    {
        var halfW = s * 0.25f;
        var halfH = s * 0.40f;
        var top = new Vector2(cx, cy - halfH);
        var right = new Vector2(cx + halfW, cy);
        var bottom = new Vector2(cx, cy + halfH);
        var left = new Vector2(cx - halfW, cy);
        drawList.AddQuadFilled(top, right, bottom, left, color);
        // Midrib.
        drawList.AddLine(top, bottom, ImGui.ColorConvertFloat4ToU32(ColorPalette.DarkBrown), 1.5f);
    }

    private static void DrawCrossedSwords(ImDrawListPtr drawList, float cx, float cy, float s, uint color)
    {
        var arm = s * 0.40f;
        var thick = MathF.Max(2f, s * 0.06f);
        // Two diagonal lines forming an X.
        drawList.AddLine(new Vector2(cx - arm, cy - arm), new Vector2(cx + arm, cy + arm), color, thick);
        drawList.AddLine(new Vector2(cx - arm, cy + arm), new Vector2(cx + arm, cy - arm), color, thick);
        // Hilts as little perpendicular ticks near the bottom of each blade.
        var tick = s * 0.10f;
        drawList.AddLine(new Vector2(cx - arm - tick * 0.5f, cy + arm - tick * 0.5f),
            new Vector2(cx - arm + tick * 0.5f, cy + arm + tick * 0.5f), color, thick * 0.6f);
        drawList.AddLine(new Vector2(cx + arm - tick * 0.5f, cy + arm + tick * 0.5f),
            new Vector2(cx + arm + tick * 0.5f, cy + arm - tick * 0.5f), color, thick * 0.6f);
    }

    private static void DrawWheat(ImDrawListPtr drawList, float cx, float cy, float s, uint color)
    {
        var stemTop = new Vector2(cx, cy - s * 0.40f);
        var stemBot = new Vector2(cx, cy + s * 0.40f);
        drawList.AddLine(stemTop, stemBot, color, MathF.Max(1.5f, s * 0.04f));
        // Three pairs of angled grain marks centered on cy.
        var spread = s * 0.18f;
        var step = s * 0.18f;
        var startY = cy - step; // marks at cy - step, cy, cy + step
        for (var i = 0; i < 3; i++)
        {
            var y = startY + i * step;
            drawList.AddLine(new Vector2(cx, y),
                new Vector2(cx - spread, y - step * 0.5f), color, MathF.Max(1.2f, s * 0.03f));
            drawList.AddLine(new Vector2(cx, y),
                new Vector2(cx + spread, y - step * 0.5f), color, MathF.Max(1.2f, s * 0.03f));
        }
    }

    private static void DrawMountain(ImDrawListPtr drawList, float cx, float cy, float s, uint color)
    {
        // Symmetric vertical extent so the silhouette centers on cy.
        var baseY = cy + s * 0.325f;
        var peakY = cy - s * 0.325f;
        // Main peak triangle.
        var leftBase = new Vector2(cx - s * 0.42f, baseY);
        var rightBase = new Vector2(cx + s * 0.42f, baseY);
        var peak = new Vector2(cx, peakY);
        drawList.AddTriangleFilled(leftBase, rightBase, peak, color);
        // Smaller foreground peak to the right.
        var fLeft = new Vector2(cx + s * 0.10f, baseY);
        var fRight = new Vector2(cx + s * 0.45f, baseY);
        var fPeak = new Vector2(cx + s * 0.28f, cy);
        drawList.AddTriangleFilled(fLeft, fRight, fPeak, color);
    }
}
