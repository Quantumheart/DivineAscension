using System;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using DivineAscension.Constants;
using DivineAscension.GUI.UI.Utilities;
using DivineAscension.Models;
using ImGuiNET;
using static DivineAscension.GUI.UI.Utilities.FontSizes;

namespace DivineAscension.GUI.UI.Renderers.Blessing;

/// <summary>
///     Renders individual blessing nodes as parchment-style wax-seal medallions
///     using the codex palette. Outer ring + rim show unlock state; inner disc
///     hosts the blessing's icon (or its initials if no icon is registered).
///     Tier number and modern fills/glows have been dropped in favour of the
///     ledger language used by the surrounding chapter.
/// </summary>
[ExcludeFromCodeCoverage]
internal static class BlessingNodeRenderer
{
    private static float SealRadius => UiScale.Scaled(22f);
    private static float RimThickness => UiScale.Scaled(2.0f);
    private static float SelectedRimThickness => UiScale.Scaled(3.0f);
    private static float IconSize => UiScale.Scaled(28f);

    // Shared animation clock for the "available" gentle pulse.
    private static float _glowAnimationTime;

    public static bool DrawNode(
        BlessingNodeState state,
        float offsetX, float offsetY,
        float mouseX, float mouseY,
        float deltaTime,
        bool isSelected)
    {
        var drawList = ImGui.GetWindowDrawList();

        // Centre of the node within the (Width × Height) hit-box reserved by the layout.
        var screenX = state.PositionX + offsetX;
        var screenY = state.PositionY + offsetY;
        var center = new Vector2(screenX + state.Width / 2f, screenY + state.Height / 2f);

        var isHovering = mouseX >= screenX && mouseX <= screenX + state.Width &&
                         mouseY >= screenY && mouseY <= screenY + state.Height;

        var (fillColor, rimColor, iconTint, textColor) = StyleFor(state.VisualState);

        // Pulse the available seal so the eye can find it without the lime glow.
        if (state.VisualState == BlessingNodeVisualState.Unlockable)
        {
            _glowAnimationTime += deltaTime;
            var pulse = 0.5f + 0.5f * (float)Math.Sin(_glowAnimationTime * Math.PI);
            var glowAlpha = 0.18f + 0.22f * pulse;
            var glowColor = ColorPalette.Gold;
            glowColor.W = glowAlpha;
            drawList.AddCircleFilled(center, SealRadius + UiScale.Scaled(5f),
                ImGui.ColorConvertFloat4ToU32(glowColor), 32);
        }

        // Wax-seal disc + faded-ink rim.
        drawList.AddCircleFilled(center, SealRadius,
            ImGui.ColorConvertFloat4ToU32(fillColor), 32);
        var rim = isSelected
            ? ImGui.ColorConvertFloat4ToU32(ColorPalette.Gold)
            : ImGui.ColorConvertFloat4ToU32(rimColor);
        drawList.AddCircle(center, SealRadius, rim, 32,
            isSelected ? SelectedRimThickness : RimThickness);

        // Hover halo — single thin gold ring, no blue tint.
        if (isHovering && !isSelected)
        {
            drawList.AddCircle(center, SealRadius + UiScale.Scaled(2f),
                ImGui.ColorConvertFloat4ToU32(ColorPalette.Gold * 0.5f), 32, UiScale.Scaled(1f));
        }

        // Inner symbol — icon if registered, else initials in iron-gall ink.
        var iconTextureId = BlessingIconLoader.GetBlessingTextureId(state.Blessing);
        if (iconTextureId != IntPtr.Zero)
        {
            var iconMin = new Vector2(center.X - IconSize / 2f, center.Y - IconSize / 2f);
            var iconMax = new Vector2(iconMin.X + IconSize, iconMin.Y + IconSize);
            drawList.AddImage(iconTextureId, iconMin, iconMax,
                Vector2.Zero, Vector2.One,
                ImGui.ColorConvertFloat4ToU32(iconTint));
        }
        else
        {
            var name = state.Blessing?.Name ?? string.Empty;
            var initials = name.Length switch
            {
                0 => "?",
                1 => name,
                2 => name,
                _ => name.Substring(0, 2)
            };
            initials = initials.ToUpperInvariant();
            var size = ImGui.CalcTextSize(initials);
            var pos = new Vector2(center.X - size.X / 2f, center.Y - size.Y / 2f);
            drawList.AddText(ImGui.GetFont(), Body, pos,
                ImGui.ColorConvertFloat4ToU32(textColor), initials);
        }

        // Blessing name caption below the seal so the tree reads like a labelled diagram.
        var caption = state.Blessing?.Name ?? string.Empty;
        if (!string.IsNullOrEmpty(caption))
        {
            var captionSize = ImGui.CalcTextSize(caption);
            var captionPos = new Vector2(
                center.X - captionSize.X / 2f,
                center.Y + SealRadius + UiScale.Scaled(6f));
            var captionColor = state.VisualState switch
            {
                BlessingNodeVisualState.Unlocked => ColorPalette.Gold,
                BlessingNodeVisualState.Unlockable => ColorPalette.White,
                _ => ColorPalette.MutedText
            };
            drawList.AddText(ImGui.GetFont(), Compact, captionPos,
                ImGui.ColorConvertFloat4ToU32(captionColor), caption);
        }

        return isHovering;
    }

    /// <summary>
    /// Draw the prerequisite connection between two seals. Single sepia ink
    /// stroke for met chains; faded ink for unmet ones. No arrowhead — flow
    /// is implied by tier (top → bottom).
    /// </summary>
    public static void DrawConnectionLine(
        BlessingNodeState fromNode,
        BlessingNodeState toNode,
        float offsetX, float offsetY)
    {
        var drawList = ImGui.GetWindowDrawList();

        var fromCenter = new Vector2(
            fromNode.PositionX + fromNode.Width / 2f + offsetX,
            fromNode.PositionY + fromNode.Height / 2f + offsetY);
        var toCenter = new Vector2(
            toNode.PositionX + toNode.Width / 2f + offsetX,
            toNode.PositionY + toNode.Height / 2f + offsetY);

        var color = toNode.IsUnlocked
            ? ColorPalette.Gold * 0.7f
            : (fromNode.IsUnlocked ? ColorPalette.Grey : ColorPalette.BorderColor * 0.7f);
        var col32 = ImGui.ColorConvertFloat4ToU32(color);

        // Same-lane (vertical) prereqs draw as a straight stroke. Cross-lane
        // prereqs route as a short vertical-tangent bezier so the curve clears
        // sibling seals instead of slicing through them.
        if (Math.Abs(fromCenter.X - toCenter.X) < 0.5f)
        {
            drawList.AddLine(fromCenter, toCenter, col32, UiScale.Scaled(1.25f));
            return;
        }

        var dy = toCenter.Y - fromCenter.Y;
        var tangent = Math.Max(UiScale.Scaled(24f), Math.Abs(dy) * 0.55f);
        var cp1 = new Vector2(fromCenter.X, fromCenter.Y + tangent);
        var cp2 = new Vector2(toCenter.X, toCenter.Y - tangent);
        drawList.AddBezierCubic(fromCenter, cp1, cp2, toCenter, col32, UiScale.Scaled(1.25f), 24);
    }

    private static (Vector4 Fill, Vector4 Rim, Vector4 IconTint, Vector4 Text) StyleFor(
        BlessingNodeVisualState visual)
    {
        return visual switch
        {
            BlessingNodeVisualState.Unlocked => (
                ColorPalette.Gold,            // gilt wax fill
                ColorPalette.Gold * 0.7f,     // darker gold rim
                new Vector4(1f, 1f, 1f, 1f),  // full-colour icon
                ColorPalette.White),          // iron-gall ink initials
            BlessingNodeVisualState.Unlockable => (
                ColorPalette.Background,                          // parchment fill
                ColorPalette.Gold,                                // gilt-ink rim invites
                new Vector4(0.95f, 0.85f, 0.55f, 1f),             // warm tinted icon
                ColorPalette.White),
            BlessingNodeVisualState.BranchLocked => (
                ColorPalette.Background,
                ColorPalette.Vermilion,                           // rubric red rim
                new Vector4(0.5f, 0.3f, 0.3f, 0.6f),
                ColorPalette.MutedText),
            _ => (
                ColorPalette.Background,
                ColorPalette.BorderColor,                         // faded ink rim
                new Vector4(0.6f, 0.55f, 0.45f, 0.7f),
                ColorPalette.MutedText)
        };
    }
}
