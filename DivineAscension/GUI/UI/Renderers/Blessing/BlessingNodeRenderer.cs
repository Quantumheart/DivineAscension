using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using DivineAscension.GUI.UI.Utilities;
using DivineAscension.Models;
using ImGuiNET;
using static DivineAscension.GUI.UI.Utilities.FontSizes;

namespace DivineAscension.GUI.UI.Renderers.Blessing;

/// <summary>
///     Renders individual blessing nodes in the tree
///     Displays states: locked, unlockable (glowing), unlocked (gold)
/// </summary>
[ExcludeFromCodeCoverage]
internal static class BlessingNodeRenderer
{
    private const float CornerRadius = 4f;
    private static readonly Vector4 ColorLocked = new(0.573f, 0.502f, 0.416f, 1.0f); // #92806a grey
    private static readonly Vector4 ColorUnlockable = new(0.478f, 0.776f, 0.184f, 1.0f); // #7ac62f lime
    private static readonly Vector4 ColorUnlocked = new(0.996f, 0.682f, 0.204f, 1.0f); // #feae34 gold
    private static readonly Vector4 ColorBranchLocked = new(0.698f, 0.133f, 0.133f, 1.0f); // #b22222 firebrick red
    private static readonly Vector4 ColorSelected = new(1.0f, 1.0f, 1.0f, 1.0f); // White border
    private static readonly Vector4 ColorHover = new(0.8f, 0.8f, 1.0f, 1.0f); // Light blue tint

    // Static field for glow animation timing
    private static float _glowAnimationTime;

    /// <summary>
    ///     Draw a single blessing node
    /// </summary>
    /// <param name="state">Blessing node state</param>
    /// <param name="offsetX">Scroll offset X</param>
    /// <param name="offsetY">Scroll offset Y</param>
    /// <param name="mouseX">Mouse X position (world space)</param>
    /// <param name="mouseY">Mouse Y position (world space)</param>
    /// <param name="deltaTime">Time elapsed since last frame (for animations)</param>
    /// <param name="isSelected">Whether this node is currently selected</param>
    /// <returns>True if mouse is hovering over this node</returns>
    public static bool DrawNode(
        BlessingNodeState state,
        float offsetX, float offsetY,
        float mouseX, float mouseY,
        float deltaTime,
        bool isSelected)
    {
        var drawList = ImGui.GetWindowDrawList();

        // Calculate screen position (with scroll offset)
        var screenX = state.PositionX + offsetX;
        var screenY = state.PositionY + offsetY;

        // Check if mouse is hovering
        var isHovering = mouseX >= screenX && mouseX <= screenX + state.Width &&
                         mouseY >= screenY && mouseY <= screenY + state.Height;

        // Animate glow effect for unlockable blessings
        if (state.VisualState == BlessingNodeVisualState.Unlockable)
        {
            // Update animation time (shared across all unlockable blessings for synchronized pulse)
            _glowAnimationTime += deltaTime;

            // Pulse glow alpha between 0.3 and 1.0 using sine wave
            // Period of 2 seconds for smooth, noticeable pulsing
            var glowAlpha = 0.5f + 0.5f * (float)Math.Sin(_glowAnimationTime * Math.PI); // 0 to 1

            var glowPadding = 8f;
            var glowPos1 = new Vector2(screenX - glowPadding, screenY - glowPadding);
            var glowPos2 = new Vector2(screenX + state.Width + glowPadding, screenY + state.Height + glowPadding);
            var glowColor = ColorUnlockable * new Vector4(1, 1, 1, glowAlpha * 0.4f);
            var glowColorU32 = ImGui.ColorConvertFloat4ToU32(glowColor);
            drawList.AddRectFilled(glowPos1, glowPos2, glowColorU32, 8f);
        }

        // Draw node background as rounded square
        var nodePos1 = new Vector2(screenX, screenY);
        var nodePos2 = new Vector2(screenX + state.Width, screenY + state.Height);
        var center = new Vector2(screenX + state.Width / 2, screenY + state.Height / 2);

        var nodeColor = state.VisualState switch
        {
            BlessingNodeVisualState.Locked => ColorLocked,
            BlessingNodeVisualState.Unlockable => ColorUnlockable,
            BlessingNodeVisualState.Unlocked => ColorUnlocked,
            BlessingNodeVisualState.BranchLocked => ColorBranchLocked,
            _ => ColorLocked
        };

        // Apply hover tint
        if (isHovering) nodeColor = Vector4.Lerp(nodeColor, ColorHover, 0.3f);

        var nodeColorU32 = ImGui.ColorConvertFloat4ToU32(nodeColor);
        drawList.AddRectFilled(nodePos1, nodePos2, nodeColorU32, CornerRadius);

        // Draw border
        var borderColor = isSelected ? ColorSelected : nodeColor * 0.7f;
        var borderColorU32 = ImGui.ColorConvertFloat4ToU32(borderColor);
        var borderThickness = isSelected ? 3f : 2f;
        drawList.AddRect(nodePos1, nodePos2, borderColorU32, CornerRadius, ImDrawFlags.None, borderThickness);

        // Determine text color based on visual state (used for both icon and text modes)
        var textColor = state.VisualState == BlessingNodeVisualState.Unlocked
            ? new Vector4(0.2f, 0.15f, 0.1f, 1.0f) // Dark text on gold
            : new Vector4(0.9f, 0.9f, 0.9f, 1.0f); // Light text on dark

        // Try to load blessing icon
        var iconTextureId = BlessingIconLoader.GetBlessingTextureId(state.Blessing);
        var hasIcon = iconTextureId != IntPtr.Zero;

        // Debug logging
        var debugIconName = state.Blessing?.IconName ?? "null";
        var debugBlessingName = state.Blessing?.Name ?? "unknown";
        Debug.WriteLine(
            $"[BlessingIcon] Blessing: {debugBlessingName}, IconName: '{debugIconName}', HasIcon: {hasIcon}, TextureId: {iconTextureId}");

        if (hasIcon)
        {
            // Draw icon centered in node
            const float iconSize = 48f;
            var iconPos = new Vector2(
                center.X - iconSize / 2,
                center.Y - iconSize / 2
            );
            var iconMin = iconPos;
            var iconMax = new Vector2(iconPos.X + iconSize, iconPos.Y + iconSize);

            // Apply color tint based on state
            var iconTint = state.VisualState switch
            {
                BlessingNodeVisualState.Unlocked => new Vector4(1f, 1f, 1f, 1f), // Full color
                BlessingNodeVisualState.Unlockable => new Vector4(0.9f, 1f, 0.9f, 1f), // Slight green tint
                BlessingNodeVisualState.BranchLocked => new Vector4(0.5f, 0.3f, 0.3f, 0.6f), // Red-tinted and dimmed
                _ => new Vector4(0.6f, 0.6f, 0.6f, 0.7f) // Desaturated/dimmed for locked
            };
            var iconTintU32 = ImGui.ColorConvertFloat4ToU32(iconTint);

            drawList.AddImage(iconTextureId, iconMin, iconMax, Vector2.Zero, Vector2.One, iconTintU32);
        }
        else
        {
            // Fallback: Show initials (first 2-3 letters)
            var blessingName = state.Blessing!.Name;
            var initials = blessingName.Length <= 3 ? blessingName : blessingName.Substring(0, 3);
            const float fontSize = TableHeader; // Larger font for initials

            var textSize = ImGui.CalcTextSize(initials);
            var textPos = new Vector2(
                center.X - textSize.X / 2,
                center.Y - textSize.Y / 2
            );

            var textColorU32 = ImGui.ColorConvertFloat4ToU32(textColor);
            drawList.AddText(ImGui.GetFont(), fontSize, textPos, textColorU32, initials);
        }

        // Draw tier indicator (small number at bottom)
        var tierText = $"T{state.Tier}";
        var tierSize = ImGui.CalcTextSize(tierText);
        var tierPos = new Vector2(
            center.X - tierSize.X / 2,
            screenY + state.Height - tierSize.Y - 4f
        );
        var tierColorU32 = ImGui.ColorConvertFloat4ToU32(textColor * 0.7f);
        drawList.AddText(ImGui.GetFont(), Compact, tierPos, tierColorU32, tierText);

        return isHovering;
    }

    /// <summary>
    ///     Draw connection line between prerequisite nodes
    /// </summary>
    public static void DrawConnectionLine(
        BlessingNodeState fromNode,
        BlessingNodeState toNode,
        float offsetX, float offsetY)
    {
        var drawList = ImGui.GetWindowDrawList();

        // Calculate centers of both nodes (with scroll offset)
        var fromCenter = new Vector2(
            fromNode.PositionX + fromNode.Width / 2 + offsetX,
            fromNode.PositionY + fromNode.Height / 2 + offsetY
        );

        var toCenter = new Vector2(
            toNode.PositionX + toNode.Width / 2 + offsetX,
            toNode.PositionY + toNode.Height / 2 + offsetY
        );

        // Line color based on unlock status
        Vector4 lineColor;
        if (toNode.IsUnlocked)
            lineColor = ColorUnlocked * 0.6f; // Gold for unlocked chains
        else if (fromNode.IsUnlocked)
            lineColor = ColorUnlockable * 0.6f; // Green if prerequisite unlocked
        else
            lineColor = ColorLocked * 0.5f; // Grey for locked chains

        var lineColorU32 = ImGui.ColorConvertFloat4ToU32(lineColor);
        drawList.AddLine(fromCenter, toCenter, lineColorU32, 2f);

        // Draw small arrow head at destination
        DrawArrowHead(drawList, fromCenter, toCenter, lineColorU32);
    }

    /// <summary>
    ///     Draw small arrow head pointing from source to destination
    /// </summary>
    private static void DrawArrowHead(ImDrawListPtr drawList, Vector2 from, Vector2 to, uint color)
    {
        const float arrowSize = 6f;
        const float arrowAngle = 0.4f; // radians

        // Calculate direction vector
        var direction = Vector2.Normalize(to - from);

        // Calculate arrow points
        var angle = (float)Math.Atan2(direction.Y, direction.X);
        var arrowPoint1 = to - new Vector2(
            (float)Math.Cos(angle - arrowAngle) * arrowSize,
            (float)Math.Sin(angle - arrowAngle) * arrowSize
        );
        var arrowPoint2 = to - new Vector2(
            (float)Math.Cos(angle + arrowAngle) * arrowSize,
            (float)Math.Sin(angle + arrowAngle) * arrowSize
        );

        drawList.AddTriangleFilled(to, arrowPoint1, arrowPoint2, color);
    }
}