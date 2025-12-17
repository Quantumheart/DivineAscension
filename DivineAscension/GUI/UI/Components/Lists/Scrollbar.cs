using System;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using DivineAscension.GUI.UI.Utilities;
using ImGuiNET;

namespace DivineAscension.GUI.UI.Components.Lists;

/// <summary>
///     Reusable scrollbar component
///     Provides consistent scrollbar styling for scrollable content
/// </summary>
[ExcludeFromCodeCoverage]
internal static class Scrollbar
{
    /// <summary>
    ///     Draw a vertical scrollbar
    /// </summary>
    /// <param name="drawList">ImGui draw list</param>
    /// <param name="x">X position</param>
    /// <param name="y">Y position</param>
    /// <param name="width">Scrollbar width</param>
    /// <param name="height">Scrollbar height</param>
    /// <param name="scrollY">Current scroll position</param>
    /// <param name="maxScroll">Maximum scroll value</param>
    public static void Draw(
        ImDrawListPtr drawList,
        float x,
        float y,
        float width,
        float height,
        float scrollY,
        float maxScroll)
    {
        // Draw track
        var trackStart = new Vector2(x, y);
        var trackEnd = new Vector2(x + width, y + height);
        var trackColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.DarkBrown * 0.7f);
        drawList.AddRectFilled(trackStart, trackEnd, trackColor, 4f);

        // Calculate thumb size and position
        if (maxScroll > 0)
        {
            var thumbHeight = Math.Max(20f, height * (height / (height + maxScroll)));
            var thumbY = y + scrollY / maxScroll * (height - thumbHeight);

            var thumbStart = new Vector2(x + 2f, thumbY);
            var thumbEnd = new Vector2(x + width - 2f, thumbY + thumbHeight);
            var thumbColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.Grey);
            drawList.AddRectFilled(thumbStart, thumbEnd, thumbColor, 4f);
        }
    }

    /// <summary>
    ///     Handle mouse wheel scrolling for a scrollable area
    /// </summary>
    /// <param name="currentScrollY">Current scroll position</param>
    /// <param name="maxScroll">Maximum scroll value</param>
    /// <param name="mouseX">Mouse X position</param>
    /// <param name="mouseY">Mouse Y position</param>
    /// <param name="areaX">Scrollable area X position</param>
    /// <param name="areaY">Scrollable area Y position</param>
    /// <param name="areaWidth">Scrollable area width</param>
    /// <param name="areaHeight">Scrollable area height</param>
    /// <param name="scrollSpeed">Scroll speed multiplier (default 20)</param>
    /// <returns>Updated scroll position</returns>
    public static float HandleMouseWheel(
        float currentScrollY,
        float maxScroll,
        float mouseX,
        float mouseY,
        float areaX,
        float areaY,
        float areaWidth,
        float areaHeight,
        float scrollSpeed = 20f)
    {
        // Check if mouse is over the scrollable area
        var isMouseOver = mouseX >= areaX && mouseX <= areaX + areaWidth &&
                          mouseY >= areaY && mouseY <= areaY + areaHeight;

        if (isMouseOver && maxScroll > 0)
        {
            var mouseWheel = ImGui.GetIO().MouseWheel;
            if (mouseWheel != 0) currentScrollY = Math.Clamp(currentScrollY - mouseWheel * scrollSpeed, 0f, maxScroll);
        }

        return currentScrollY;
    }

    /// <summary>
    ///     Handle scrollbar dragging
    /// </summary>
    /// <param name="currentScrollY">Current scroll position</param>
    /// <param name="maxScroll">Maximum scroll value</param>
    /// <param name="scrollbarX">Scrollbar X position</param>
    /// <param name="scrollbarY">Scrollbar Y position</param>
    /// <param name="scrollbarWidth">Scrollbar width</param>
    /// <param name="scrollbarHeight">Scrollbar height</param>
    /// <returns>Updated scroll position</returns>
    public static float HandleDragging(
        float currentScrollY,
        float maxScroll,
        float scrollbarX,
        float scrollbarY,
        float scrollbarWidth,
        float scrollbarHeight)
    {
        if (maxScroll <= 0) return currentScrollY;

        var mousePos = ImGui.GetMousePos();
        var thumbHeight = Math.Max(20f, scrollbarHeight * (scrollbarHeight / (scrollbarHeight + maxScroll)));
        var thumbY = scrollbarY + currentScrollY / maxScroll * (scrollbarHeight - thumbHeight);

        // Check if clicking on thumb
        var isOverThumb = mousePos.X >= scrollbarX && mousePos.X <= scrollbarX + scrollbarWidth &&
                          mousePos.Y >= thumbY && mousePos.Y <= thumbY + thumbHeight;

        if (isOverThumb && ImGui.IsMouseDown(ImGuiMouseButton.Left))
        {
            var dragDelta = ImGui.GetMouseDragDelta(ImGuiMouseButton.Left, 0f);
            var scrollDelta = dragDelta.Y / (scrollbarHeight - thumbHeight) * maxScroll;
            currentScrollY = Math.Clamp(currentScrollY + scrollDelta, 0f, maxScroll);
            ImGui.ResetMouseDragDelta(ImGuiMouseButton.Left);
        }

        return currentScrollY;
    }
}