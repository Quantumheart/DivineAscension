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
        // Default to the domain's manuscript-ink hue (palette §3) so the glyph
        // reads on parchment. Callers can override for dark surfaces.
        var color = ImGui.ColorConvertFloat4ToU32(
            colorOverride ?? DomainHelper.GetDeityColor(domain));
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
        // Antlers — heraldic stag mark. Symmetric pair rising from a small
        // skull plate at the base, each antler a curved beam with two
        // outward-up tines. More legible at 32px than the old leaf, which
        // read as just a diamond when domain-tinted.
        var beam = MathF.Max(2f, s * 0.07f);
        var tine = MathF.Max(1.5f, s * 0.055f);
        var baseY = cy + s * 0.30f;

        // Skull plate.
        drawList.AddTriangleFilled(
            new Vector2(cx - s * 0.06f, baseY + s * 0.03f),
            new Vector2(cx + s * 0.06f, baseY + s * 0.03f),
            new Vector2(cx, baseY - s * 0.06f),
            color);

        for (var side = -1; side <= 1; side += 2)
        {
            var p0 = new Vector2(cx, baseY - s * 0.04f);
            var p1 = new Vector2(cx + side * s * 0.10f, cy + s * 0.10f);
            var p2 = new Vector2(cx + side * s * 0.20f, cy - s * 0.08f);
            var p3 = new Vector2(cx + side * s * 0.28f, cy - s * 0.34f);

            drawList.AddLine(p0, p1, color, beam);
            drawList.AddLine(p1, p2, color, beam);
            drawList.AddLine(p2, p3, color, beam);

            // Inner-facing tines branching off the beam — each tine angles
            // up and slightly outward so the rack reads as a fan.
            var tine1 = new Vector2(cx + side * s * 0.30f, cy + s * 0.00f);
            var tine2 = new Vector2(cx + side * s * 0.42f, cy - s * 0.22f);
            drawList.AddLine(p1, tine1, color, tine);
            drawList.AddLine(p2, tine2, color, tine);
        }
    }

    private static void DrawCrossedSwords(ImDrawListPtr drawList, float cx, float cy, float s, uint color)
    {
        // Heraldic crossed-swords: both blades point upward, hilts down. Each
        // blade is a filled tapered triangle from a wide base near the guard
        // to a sharp tip, with a perpendicular crossguard and a circular
        // pommel at the grip's end. Two swords drawn along the SW→NE and
        // SE→NW diagonals so the tips reach the upper corners.
        const float invSqrt2 = 0.70710677f;
        // Tip = arm out along sword axis; HiltEnd = arm out the opposite way.
        var arm = s * 0.42f;
        var bladeHalf = MathF.Max(1.5f, s * 0.045f);
        var guardArm = s * 0.10f;
        var guardThick = MathF.Max(1.5f, s * 0.035f);
        var pommelR = MathF.Max(2f, s * 0.06f);
        // Where the blade ends and the grip begins along the sword axis.
        var bladeBaseFromCenter = -arm * 0.35f;

        // Sword A — tip top-left, hilt bottom-right.
        DrawOneSword(drawList, cx, cy, -invSqrt2, -invSqrt2,
            arm, bladeHalf, bladeBaseFromCenter, guardArm, guardThick, pommelR, color);
        // Sword B — tip top-right, hilt bottom-left.
        DrawOneSword(drawList, cx, cy, invSqrt2, -invSqrt2,
            arm, bladeHalf, bladeBaseFromCenter, guardArm, guardThick, pommelR, color);
    }

    private static void DrawOneSword(
        ImDrawListPtr drawList, float cx, float cy,
        float dx, float dy,
        float arm, float bladeHalf, float bladeBaseAlong,
        float guardArm, float guardThick, float pommelR, uint color)
    {
        // Axis vector (toward tip) and perpendicular vector.
        var px = -dy;
        var py = dx;

        var tip = new Vector2(cx + dx * arm, cy + dy * arm);
        var baseX = cx + dx * bladeBaseAlong;
        var baseY = cy + dy * bladeBaseAlong;
        var baseLeft = new Vector2(baseX + px * bladeHalf, baseY + py * bladeHalf);
        var baseRight = new Vector2(baseX - px * bladeHalf, baseY - py * bladeHalf);
        drawList.AddTriangleFilled(baseLeft, baseRight, tip, color);

        // Crossguard — short perpendicular bar straddling the guard line.
        var guardA = new Vector2(baseX + px * guardArm, baseY + py * guardArm);
        var guardB = new Vector2(baseX - px * guardArm, baseY - py * guardArm);
        drawList.AddLine(guardA, guardB, color, guardThick);

        // Grip + pommel at the hilt end (opposite the tip).
        var hiltEnd = new Vector2(cx - dx * arm * 0.65f, cy - dy * arm * 0.65f);
        drawList.AddLine(new Vector2(baseX, baseY), hiltEnd, color, guardThick * 0.85f);
        drawList.AddCircleFilled(hiltEnd, pommelR, color);
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
