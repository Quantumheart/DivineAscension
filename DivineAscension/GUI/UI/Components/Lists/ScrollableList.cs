using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using DivineAscension.GUI.UI.Utilities;
using ImGuiNET;

namespace DivineAscension.GUI.UI.Components.Lists;

/// <summary>
///     Generic scrollable list component with item renderer callback
///     Handles scrolling, clipping, mouse wheel, and scrollbar rendering
/// </summary>
[ExcludeFromCodeCoverage]
public static class ScrollableList
{
    /// <summary>
    ///     Draw a scrollable list with custom item rendering
    /// </summary>
    /// <typeparam name="T">Type of items in the list</typeparam>
    /// <param name="drawList">ImGui draw list</param>
    /// <param name="x">X position</param>
    /// <param name="y">Y position</param>
    /// <param name="width">Width of list</param>
    /// <param name="height">Height of list</param>
    /// <param name="items">List of items to display</param>
    /// <param name="itemHeight">Height of each item</param>
    /// <param name="itemSpacing">Spacing between items</param>
    /// <param name="scrollY">Current scroll position</param>
    /// <param name="itemRenderer">Callback to render each item (item, x, y, itemWidth, itemHeight)</param>
    /// <param name="emptyText">Text to show when list is empty (null to skip)</param>
    /// <param name="loadingText">Text to show when loading (null if not loading)</param>
    /// <param name="backgroundColor">Optional background color (uses default if null)</param>
    /// <param name="scrollbarWidth">Width of scrollbar (default 16f)</param>
    /// <param name="wheelSpeed">Mouse wheel scroll speed (default 30f)</param>
    /// <returns>Updated scroll position</returns>
    public static float Draw<T>(
        ImDrawListPtr drawList,
        float x,
        float y,
        float width,
        float height,
        List<T> items,
        float itemHeight,
        float itemSpacing,
        float scrollY,
        Action<T, float, float, float, float> itemRenderer,
        string? emptyText = null,
        string? loadingText = null,
        Vector4? backgroundColor = null,
        float scrollbarWidth = 16f,
        float wheelSpeed = 30f)
    {
        // Draw background
        var listStart = new Vector2(x, y);
        var listEnd = new Vector2(x + width, y + height);
        var bgColor = backgroundColor ?? ColorPalette.DarkBrown * 0.5f;
        var listBgColor = ImGui.ColorConvertFloat4ToU32(bgColor);
        drawList.AddRectFilled(listStart, listEnd, listBgColor, 4f);

        // Loading state
        if (loadingText != null)
        {
            DrawCenteredText(drawList, loadingText, x, y, width, height, ColorPalette.Grey);
            return scrollY;
        }

        // Empty state
        if (items.Count == 0 && emptyText != null)
        {
            DrawCenteredText(drawList, emptyText, x, y, width, height, ColorPalette.Grey);
            return scrollY;
        }

        // No items to render
        if (items.Count == 0) return scrollY;

        // Calculate scroll limits
        var contentHeight = items.Count * (itemHeight + itemSpacing);
        var maxScroll = Math.Max(0f, contentHeight - height);

        // Handle mouse wheel scrolling
        var mousePos = ImGui.GetMousePos();
        var isMouseOver = mousePos.X >= x && mousePos.X <= x + width &&
                          mousePos.Y >= y && mousePos.Y <= y + height;
        if (isMouseOver)
        {
            var wheel = ImGui.GetIO().MouseWheel;
            if (wheel != 0) scrollY = Math.Clamp(scrollY - wheel * wheelSpeed, 0f, maxScroll);
        }

        // Clip to list bounds
        drawList.PushClipRect(listStart, listEnd, true);

        // Draw items
        var itemY = y - scrollY;
        var itemWidth = width - scrollbarWidth;

        foreach (var item in items)
        {
            // Skip if not visible (culling optimization)
            if (itemY + itemHeight < y || itemY > y + height)
            {
                itemY += itemHeight + itemSpacing;
                continue;
            }

            // Render the item using callback
            itemRenderer(item, x, itemY, itemWidth, itemHeight);

            itemY += itemHeight + itemSpacing;
        }

        drawList.PopClipRect();

        // Draw scrollbar if needed
        if (contentHeight > height)
            Scrollbar.Draw(drawList, x + width - scrollbarWidth, y, scrollbarWidth, height, scrollY, maxScroll);

        return scrollY;
    }

    /// <summary>
    ///     Draw centered text in a rectangle
    /// </summary>
    private static void DrawCenteredText(ImDrawListPtr drawList, string text, float x, float y, float width,
        float height, Vector4 color)
    {
        var textSize = ImGui.CalcTextSize(text);
        var textPos = new Vector2(
            x + (width - textSize.X) / 2,
            y + (height - textSize.Y) / 2
        );
        var textColor = ImGui.ColorConvertFloat4ToU32(color);
        drawList.AddText(textPos, textColor, text);
    }
}