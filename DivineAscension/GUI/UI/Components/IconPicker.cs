using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using DivineAscension.GUI.UI.Utilities;
using ImGuiNET;

namespace DivineAscension.GUI.UI.Components;

/// <summary>
///     Reusable icon picker component for selecting civilization icons
///     Displays icons in a grid layout with hover and selection states
/// </summary>
[ExcludeFromCodeCoverage]
public static class IconPicker
{
    /// <summary>
    ///     Draw an icon picker grid
    /// </summary>
    /// <param name="drawList">ImGui draw list</param>
    /// <param name="availableIcons">List of icon names to display</param>
    /// <param name="selectedIcon">Currently selected icon name</param>
    /// <param name="x">X position</param>
    /// <param name="y">Y position</param>
    /// <param name="width">Total width of the picker</param>
    /// <param name="columns">Number of columns in the grid (default 4)</param>
    /// <param name="iconSize">Size of each icon in pixels (default 40)</param>
    /// <param name="spacing">Spacing between icons (default 8)</param>
    /// <returns>Tuple of (clicked icon name if any, total height used)</returns>
    public static (string? clickedIcon, float height) Draw(
        ImDrawListPtr drawList,
        List<string> availableIcons,
        string selectedIcon,
        float x,
        float y,
        float width,
        int columns = 4,
        float iconSize = 40f,
        float spacing = 8f)
    {
        if (availableIcons == null || availableIcons.Count == 0) return (null, 0f);

        string? clickedIcon = null;
        var iconX = x;
        var iconY = y;
        var currentColumn = 0;
        var maxHeight = 0f;

        // Draw icons in a grid
        foreach (var iconName in availableIcons)
        {
            var wasClicked = DrawIconSlot(
                drawList,
                iconName,
                selectedIcon,
                iconX,
                iconY,
                iconSize
            );

            if (wasClicked) clickedIcon = iconName;

            // Update position for next icon
            currentColumn++;
            if (currentColumn >= columns)
            {
                // Move to next row
                currentColumn = 0;
                iconX = x;
                iconY += iconSize + spacing;
                maxHeight = iconY + iconSize - y;
            }
            else
            {
                // Move to next column
                iconX += iconSize + spacing;
            }
        }

        // Calculate final height (include the last row)
        var rows = (int)Math.Ceiling((double)availableIcons.Count / columns);
        var totalHeight = rows * iconSize + (rows - 1) * spacing;

        return (clickedIcon, totalHeight);
    }

    /// <summary>
    ///     Draw a single icon slot with hover and selection states
    /// </summary>
    /// <returns>True if icon was clicked</returns>
    private static bool DrawIconSlot(
        ImDrawListPtr drawList,
        string iconName,
        string selectedIcon,
        float x,
        float y,
        float size)
    {
        var slotStart = new Vector2(x, y);
        var slotEnd = new Vector2(x + size, y + size);

        var mousePos = ImGui.GetMousePos();
        var isHovering = mousePos.X >= x && mousePos.X <= x + size &&
                         mousePos.Y >= y && mousePos.Y <= y + size;
        var isSelected = string.Equals(iconName, selectedIcon, StringComparison.OrdinalIgnoreCase);

        // Determine background color
        Vector4 bgColor;
        if (isSelected)
        {
            bgColor = ColorPalette.Gold * 0.4f;
        }
        else if (isHovering)
        {
            bgColor = ColorPalette.LightBrown * 0.7f;
            ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
        }
        else
        {
            bgColor = ColorPalette.DarkBrown * 0.8f;
        }

        // Draw background
        var bgColorU32 = ImGui.ColorConvertFloat4ToU32(bgColor);
        drawList.AddRectFilled(slotStart, slotEnd, bgColorU32, 4f);

        // Draw border
        var borderColor = ImGui.ColorConvertFloat4ToU32(isSelected ? ColorPalette.Gold : ColorPalette.Grey * 0.5f);
        drawList.AddRect(slotStart, slotEnd, borderColor, 4f, ImDrawFlags.None, isSelected ? 2f : 1f);

        // Draw icon texture
        var iconTextureId = CivilizationIconLoader.GetIconTextureId(iconName);
        if (iconTextureId != IntPtr.Zero)
        {
            // Center the 32x32 icon within the slot
            const float iconTextureSize = 32f;
            var padding = (size - iconTextureSize) / 2f;
            var iconMin = new Vector2(x + padding, y + padding);
            var iconMax = new Vector2(iconMin.X + iconTextureSize, iconMin.Y + iconTextureSize);

            // Draw icon with full color (no tint)
            var tintColorU32 = ImGui.ColorConvertFloat4ToU32(new Vector4(1f, 1f, 1f, 1f));
            drawList.AddImage(iconTextureId, iconMin, iconMax, Vector2.Zero, Vector2.One, tintColorU32);
        }
        else
        {
            // Fallback: Draw placeholder text if texture not available
            var placeholderText = "?";
            var textSize = ImGui.CalcTextSize(placeholderText);
            var textPos = new Vector2(
                x + (size - textSize.X) / 2,
                y + (size - textSize.Y) / 2
            );
            var textColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.Grey);
            drawList.AddText(textPos, textColor, placeholderText);
        }

        // Handle click
        return isHovering && ImGui.IsMouseClicked(ImGuiMouseButton.Left);
    }

    /// <summary>
    ///     Draw a preview of a single selected icon with label
    /// </summary>
    /// <param name="drawList">ImGui draw list</param>
    /// <param name="iconName">Icon name to preview</param>
    /// <param name="label">Label text to display</param>
    /// <param name="x">X position</param>
    /// <param name="y">Y position</param>
    /// <param name="iconSize">Size of the icon (default 48)</param>
    /// <returns>Total height used by the preview</returns>
    public static float DrawPreview(
        ImDrawListPtr drawList,
        string iconName,
        string label,
        float x,
        float y,
        float iconSize = 48f)
    {
        const float padding = 8f;
        const float spacing = 8f;

        // Draw label
        var labelSize = ImGui.CalcTextSize(label);
        var labelPos = new Vector2(x, y);
        var labelColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.White);
        drawList.AddText(labelPos, labelColor, label);

        var iconY = y + labelSize.Y + spacing;

        // Draw icon background
        var slotStart = new Vector2(x, iconY);
        var slotEnd = new Vector2(x + iconSize, iconY + iconSize);
        var bgColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.DarkBrown * 0.8f);
        drawList.AddRectFilled(slotStart, slotEnd, bgColor, 4f);

        // Draw icon border
        var borderColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.Gold);
        drawList.AddRect(slotStart, slotEnd, borderColor, 4f, ImDrawFlags.None, 2f);

        // Draw icon texture
        var iconTextureId = CivilizationIconLoader.GetIconTextureId(iconName);
        if (iconTextureId != IntPtr.Zero)
        {
            // Center the 32x32 icon within the slot
            const float iconTextureSize = 32f;
            var iconPadding = (iconSize - iconTextureSize) / 2f;
            var iconMin = new Vector2(x + iconPadding, iconY + iconPadding);
            var iconMax = new Vector2(iconMin.X + iconTextureSize, iconMin.Y + iconTextureSize);

            // Draw icon with full color (no tint)
            var tintColorU32 = ImGui.ColorConvertFloat4ToU32(new Vector4(1f, 1f, 1f, 1f));
            drawList.AddImage(iconTextureId, iconMin, iconMax, Vector2.Zero, Vector2.One, tintColorU32);
        }

        // Draw icon name below
        var iconNameSize = ImGui.CalcTextSize(iconName);
        var iconNamePos = new Vector2(x + (iconSize - iconNameSize.X) / 2, iconY + iconSize + spacing);
        var iconNameColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.Grey);
        drawList.AddText(iconNamePos, iconNameColor, iconName);

        return labelSize.Y + spacing + iconSize + spacing + iconNameSize.Y;
    }
}