using System;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using DivineAscension.GUI.UI.Utilities;
using DivineAscension.Models.Enum;
using ImGuiNET;

namespace DivineAscension.GUI.UI.Renderers.Utilities;

/// <summary>
///     Paints a per-domain emblem inside a square slot using drawList
///     primitives — no font glyph dependency, no texture asset. Used in
///     pane headers where the spec calls for a domain mark rather than the
///     bundled deity icon. Glyphs are deliberately silhouette-level
///     simple (hammer / leaf / sword / wheat / mountain) so they read at
///     32 px.
/// </summary>
[ExcludeFromCodeCoverage]
internal static class DomainGlyphRenderer
{
    /// <summary>
    ///     Paint the domain emblem centered inside the square defined by
    ///     <paramref name="min"/> / <paramref name="max"/>. Caller controls
    ///     the frame (border / background) — this routine only paints the
    ///     glyph strokes and fills.
    /// </summary>
    public static void Draw(ImDrawListPtr drawList, DeityDomain domain,
        Vector2 min, Vector2 max, Vector4? colorOverride = null)
    {
        var size = MathF.Min(max.X - min.X, max.Y - min.Y);
        if (size <= 0f) return;

        var cx = (min.X + max.X) / 2f;
        var cy = (min.Y + max.Y) / 2f;
        var half = size / 2f;
        var color = colorOverride ?? DomainHelper.GetDeityColor(domain);
        var u32 = ImGui.ColorConvertFloat4ToU32(color);

        switch (domain)
        {
            case DeityDomain.Craft: DrawHammer(drawList, cx, cy, half, u32); break;
            case DeityDomain.Wild: DrawLeaf(drawList, cx, cy, half, u32); break;
            case DeityDomain.Conquest: DrawSwords(drawList, cx, cy, half, u32); break;
            case DeityDomain.Harvest: DrawWheat(drawList, cx, cy, half, u32); break;
            case DeityDomain.Stone: DrawMountain(drawList, cx, cy, half, u32); break;
            default: DrawCircleMark(drawList, cx, cy, half, u32); break;
        }
    }

    // Hammer: rectangular head with a centered handle running down-right.
    private static void DrawHammer(ImDrawListPtr drawList, float cx, float cy, float half, uint color)
    {
        var headHalfW = half * 0.55f;
        var headHalfH = half * 0.22f;
        var headCx = cx - half * 0.15f;
        var headCy = cy - half * 0.35f;
        drawList.AddRectFilled(
            new Vector2(headCx - headHalfW, headCy - headHalfH),
            new Vector2(headCx + headHalfW, headCy + headHalfH),
            color, 2f);

        // Handle from the underside of the head down to the lower-right.
        var handleStart = new Vector2(headCx + half * 0.1f, headCy + headHalfH);
        var handleEnd = new Vector2(cx + half * 0.55f, cy + half * 0.75f);
        drawList.AddLine(handleStart, handleEnd, color, MathF.Max(2f, half * 0.18f));
    }

    // Leaf: filled quad shaped like a pointed oval with a central vein.
    private static void DrawLeaf(ImDrawListPtr drawList, float cx, float cy, float half, uint color)
    {
        var top = new Vector2(cx + half * 0.6f, cy - half * 0.7f);
        var right = new Vector2(cx + half * 0.45f, cy + half * 0.1f);
        var bottom = new Vector2(cx - half * 0.6f, cy + half * 0.7f);
        var left = new Vector2(cx - half * 0.45f, cy - half * 0.1f);
        drawList.AddQuadFilled(top, right, bottom, left, color);

        // Central vein.
        drawList.AddLine(top, bottom, color, MathF.Max(1f, half * 0.08f));
    }

    // Crossed swords: two diagonal blades with a small pommel each end.
    private static void DrawSwords(ImDrawListPtr drawList, float cx, float cy, float half, uint color)
    {
        var thickness = MathF.Max(2f, half * 0.16f);
        var a1 = new Vector2(cx - half * 0.7f, cy - half * 0.7f);
        var a2 = new Vector2(cx + half * 0.7f, cy + half * 0.7f);
        var b1 = new Vector2(cx + half * 0.7f, cy - half * 0.7f);
        var b2 = new Vector2(cx - half * 0.7f, cy + half * 0.7f);
        drawList.AddLine(a1, a2, color, thickness);
        drawList.AddLine(b1, b2, color, thickness);

        // Hilt cross-guards near the lower (handle) ends of each blade.
        DrawGuard(drawList, a2, a1, half, color);
        DrawGuard(drawList, b2, b1, half, color);
    }

    private static void DrawGuard(ImDrawListPtr drawList, Vector2 handle, Vector2 tip,
        float half, uint color)
    {
        var dir = Vector2.Normalize(tip - handle);
        var perp = new Vector2(-dir.Y, dir.X);
        var center = handle - dir * (half * 0.15f);
        var len = half * 0.25f;
        drawList.AddLine(center - perp * len, center + perp * len, color, MathF.Max(1.5f, half * 0.1f));
    }

    // Wheat stalk: vertical stem with paired grain triangles up each side.
    private static void DrawWheat(ImDrawListPtr drawList, float cx, float cy, float half, uint color)
    {
        var stalkTop = new Vector2(cx, cy - half * 0.8f);
        var stalkBot = new Vector2(cx, cy + half * 0.8f);
        drawList.AddLine(stalkTop, stalkBot, color, MathF.Max(1.5f, half * 0.1f));

        // Three pairs of grain leaves along the stalk.
        for (var i = 0; i < 3; i++)
        {
            var t = -0.55f + i * 0.4f;
            var yy = cy + t * half;
            var leafLen = half * 0.45f;
            var leafTipY = yy - half * 0.18f;
            // Left grain.
            drawList.AddTriangleFilled(
                new Vector2(cx, yy),
                new Vector2(cx, yy - half * 0.22f),
                new Vector2(cx - leafLen, leafTipY),
                color);
            // Right grain.
            drawList.AddTriangleFilled(
                new Vector2(cx, yy),
                new Vector2(cx, yy - half * 0.22f),
                new Vector2(cx + leafLen, leafTipY),
                color);
        }
    }

    // Mountain: two overlapping triangles, the rear one slightly taller.
    private static void DrawMountain(ImDrawListPtr drawList, float cx, float cy, float half, uint color)
    {
        var baseY = cy + half * 0.65f;
        var rearPeak = new Vector2(cx - half * 0.2f, cy - half * 0.7f);
        var rearLeft = new Vector2(cx - half * 0.85f, baseY);
        var rearRight = new Vector2(cx + half * 0.45f, baseY);
        drawList.AddTriangleFilled(rearLeft, rearPeak, rearRight, color);

        var frontPeak = new Vector2(cx + half * 0.3f, cy - half * 0.35f);
        var frontLeft = new Vector2(cx - half * 0.3f, baseY);
        var frontRight = new Vector2(cx + half * 0.85f, baseY);
        drawList.AddTriangleFilled(frontLeft, frontPeak, frontRight, color);
    }

    // Fallback: a hollow ring so an unknown domain still reads as "an icon
    // was here" instead of nothing.
    private static void DrawCircleMark(ImDrawListPtr drawList, float cx, float cy, float half, uint color)
    {
        drawList.AddCircle(new Vector2(cx, cy), half * 0.65f, color, 24, MathF.Max(1.5f, half * 0.12f));
    }
}
