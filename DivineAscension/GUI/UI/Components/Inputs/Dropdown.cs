using System;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using DivineAscension.GUI.UI.Utilities;
using ImGuiNET;

namespace DivineAscension.GUI.UI.Components.Inputs;

/// <summary>
///     Reusable dropdown component
///     Provides consistent dropdown styling across all UI overlays
/// </summary>
[ExcludeFromCodeCoverage]
internal static class Dropdown
{
    // Shared scroll state for the single open dropdown menu. Only one menu is
    // open at a time across the dialog (clicking another closes the first), so
    // a single anchor + offset suffices. Anchor is the menu's top-left so the
    // offset resets the moment a different dropdown opens.
    private static Vector2 _scrollAnchor;
    private static float _scrollY;

    private static float ScrollbarGutterWidth => UiScale.Scaled(6f);
    private static float ScrollbarTrackPadding => UiScale.Scaled(2f);

    /// <summary>
    ///     Draw a dropdown button (without the menu)
    /// </summary>
    /// <param name="drawList">ImGui draw list</param>
    /// <param name="x">X position</param>
    /// <param name="y">Y position</param>
    /// <param name="width">Dropdown width</param>
    /// <param name="height">Dropdown height</param>
    /// <param name="selectedText">Text to display for the selected item</param>
    /// <param name="isOpen">Whether the dropdown is currently open</param>
    /// <param name="fontSize">Font size for the text (default FontSizes.Body)</param>
    /// <returns>True if the dropdown was clicked</returns>
    public static bool DrawButton(
        ImDrawListPtr drawList,
        float x,
        float y,
        float width,
        float height,
        string selectedText,
        bool isOpen,
        float fontSize = -1f)
    {
        if (fontSize < 0f) fontSize = FontSizes.Body;
        var dropdownStart = new Vector2(x, y);
        var dropdownEnd = new Vector2(x + width, y + height);

        var mousePos = ImGui.GetMousePos();
        var isHovering = mousePos.X >= x && mousePos.X <= x + width &&
                         mousePos.Y >= y && mousePos.Y <= y + height;

        // Draw dropdown background
        var bgColor = isHovering
            ? ImGui.ColorConvertFloat4ToU32(ColorPalette.LightBrown * 0.7f)
            : ImGui.ColorConvertFloat4ToU32(ColorPalette.DarkBrown * 0.7f);
        drawList.AddRectFilled(dropdownStart, dropdownEnd, bgColor, 4f);

        // Draw border
        var borderColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.Grey * 0.5f);
        drawList.AddRect(dropdownStart, dropdownEnd, borderColor, 4f, ImDrawFlags.None, 1f);

        if (isHovering) ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);

        // Draw selected text with font scaling
        var fontScale = fontSize / FontSizes.Body;
        ImGui.SetWindowFontScale(fontScale);
        var scaledTextSize = ImGui.CalcTextSize(selectedText);
        ImGui.SetWindowFontScale(1f);

        var textPos = new Vector2(x + UiScale.Scaled(12f), y + (height - scaledTextSize.Y) / 2);
        var textColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.LightText);

        ImGui.SetWindowFontScale(fontScale);
        drawList.AddText(textPos, textColor, selectedText);
        ImGui.SetWindowFontScale(1f);

        // Draw dropdown arrow
        var arrowX = x + width - UiScale.Scaled(20f);
        var arrowY = y + height / 2;
        var arrowH = UiScale.Scaled(4f);
        var arrowHalf = UiScale.Scaled(2f);
        var arrowColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.Grey);
        if (isOpen)
            // Arrow pointing up when open
            drawList.AddTriangleFilled(
                new Vector2(arrowX, arrowY - arrowH),
                new Vector2(arrowX - arrowH, arrowY + arrowHalf),
                new Vector2(arrowX + arrowH, arrowY + arrowHalf),
                arrowColor
            );
        else
            // Arrow pointing down when closed
            drawList.AddTriangleFilled(
                new Vector2(arrowX - arrowH, arrowY - arrowHalf),
                new Vector2(arrowX + arrowH, arrowY - arrowHalf),
                new Vector2(arrowX, arrowY + arrowH),
                arrowColor
            );

        // Check if clicked
        return isHovering && ImGui.IsMouseClicked(ImGuiMouseButton.Left);
    }

    /// <summary>
    ///     Draw dropdown menu and handle interactions
    /// </summary>
    /// <param name="x">X position (same as button)</param>
    /// <param name="y">Y position (same as button)</param>
    /// <param name="width">Dropdown width</param>
    /// <param name="height">Dropdown button height</param>
    /// <param name="items">Array of menu items to display</param>
    /// <param name="selectedIndex">Currently selected index</param>
    /// <param name="itemHeight">Height of each menu item (default 40)</param>
    /// <param name="maxVisibleItems">Cap on visible rows; menu becomes scrollable past this (default 8)</param>
    /// <returns>Tuple of (newSelectedIndex, shouldClose, clickConsumed)</returns>
    public static (int selectedIndex, bool shouldClose, bool clickConsumed) DrawMenuAndHandleInteraction(float x,
        float y,
        float width,
        float height,
        string[] items,
        int selectedIndex,
        float itemHeight = 40f,
        int maxVisibleItems = 8)
    {
        var mousePos = ImGui.GetMousePos();
        var menuTop = y + height + 2f;
        var menuStart = new Vector2(x, menuTop);
        var contentHeight = items.Length * itemHeight;
        var visibleHeight = MathF.Min(items.Length, MathF.Max(1, maxVisibleItems)) * itemHeight;
        var menuEnd = new Vector2(x + width, menuTop + visibleHeight);
        var maxScroll = MathF.Max(0f, contentHeight - visibleHeight);

        var clickConsumed = false;
        var shouldClose = false;
        var newSelectedIndex = selectedIndex;

        var isMouseOverMenu = mousePos.X >= menuStart.X && mousePos.X <= menuEnd.X &&
                              mousePos.Y >= menuStart.Y && mousePos.Y <= menuEnd.Y;

        // Drive the shared scroll cache. Reset when the anchor moves (different
        // dropdown opened) so each menu starts at the top.
        if (_scrollAnchor != menuStart)
        {
            _scrollAnchor = menuStart;
            _scrollY = 0f;
        }
        if (isMouseOverMenu && maxScroll > 0f)
        {
            var wheel = ImGui.GetIO().MouseWheel;
            if (wheel != 0f)
                _scrollY = Math.Clamp(_scrollY - wheel * itemHeight, 0f, maxScroll);
        }
        _scrollY = Math.Clamp(_scrollY, 0f, maxScroll);

        // Handle item clicks against the scrolled, clipped row layout.
        for (var i = 0; i < items.Length; i++)
        {
            var itemY = menuTop + i * itemHeight - _scrollY;
            var rowTop = itemY;
            var rowBottom = itemY + itemHeight;
            if (rowBottom <= menuTop || rowTop >= menuEnd.Y) continue;

            var isItemHovering = mousePos.X >= x && mousePos.X <= x + width &&
                                 mousePos.Y >= MathF.Max(rowTop, menuTop) &&
                                 mousePos.Y <= MathF.Min(rowBottom, menuEnd.Y);

            if (isItemHovering)
            {
                ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
                if (ImGui.IsMouseClicked(ImGuiMouseButton.Left))
                {
                    newSelectedIndex = i;
                    shouldClose = true;
                    clickConsumed = true;
                }
            }
        }

        // Close dropdown if clicked outside
        var isHoveringButton = mousePos.X >= x && mousePos.X <= x + width &&
                               mousePos.Y >= y && mousePos.Y <= y + height;
        if (!clickConsumed && ImGui.IsMouseClicked(ImGuiMouseButton.Left) && !isHoveringButton)
        {
            if (!isMouseOverMenu)
                shouldClose = true;
            else
                // Clicked in menu but not on an item - consume the click
                clickConsumed = true;
        }

        return (newSelectedIndex, shouldClose, clickConsumed || isMouseOverMenu);
    }

    /// <summary>
    ///     Draw dropdown menu visual (no interaction)
    /// </summary>
    /// <param name="drawList">ImGui draw list</param>
    /// <param name="x">X position (same as button)</param>
    /// <param name="y">Y position (same as button)</param>
    /// <param name="width">Dropdown width</param>
    /// <param name="height">Dropdown button height</param>
    /// <param name="items">Array of menu items to display</param>
    /// <param name="selectedIndex">Currently selected index</param>
    /// <param name="itemHeight">Height of each menu item (default 40)</param>
    /// <param name="fontSize">Font size for the text (default FontSizes.Body)</param>
    /// <param name="maxVisibleItems">Cap on visible rows; menu becomes scrollable past this (default 8)</param>
    public static void DrawMenuVisual(
        ImDrawListPtr drawList,
        float x,
        float y,
        float width,
        float height,
        string[] items,
        int selectedIndex,
        float itemHeight = -1f,
        float fontSize = -1f,
        int maxVisibleItems = 8)
    {
        if (fontSize < 0f) fontSize = FontSizes.Body;
        if (itemHeight < 0f) itemHeight = UiScale.Scaled(40f);
        var mousePos = ImGui.GetMousePos();
        var menuTop = y + height + UiScale.Scaled(2f);
        var menuStart = new Vector2(x, menuTop);
        var contentHeight = items.Length * itemHeight;
        var visibleHeight = MathF.Min(items.Length, MathF.Max(1, maxVisibleItems)) * itemHeight;
        var menuEnd = new Vector2(x + width, menuTop + visibleHeight);
        var maxScroll = MathF.Max(0f, contentHeight - visibleHeight);
        var scrollY = _scrollAnchor == menuStart ? Math.Clamp(_scrollY, 0f, maxScroll) : 0f;

        // Menu background + border (sized to the visible window, not the full
        // content height — the rest scrolls inside).
        var menuBgColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.Background);
        drawList.AddRectFilled(menuStart, menuEnd, menuBgColor, UiScale.Scaled(4f));
        var menuBorderColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.Gold * 0.7f);
        drawList.AddRect(menuStart, menuEnd, menuBorderColor, UiScale.Scaled(4f), ImDrawFlags.None, UiScale.Scaled(2f));

        // Clip rendering to the visible menu rect so off-screen items don't
        // bleed outside the border when scrolled.
        drawList.PushClipRect(menuStart, menuEnd, true);

        for (var i = 0; i < items.Length; i++)
        {
            var itemY = menuTop + i * itemHeight - scrollY;
            if (itemY + itemHeight <= menuTop || itemY >= menuEnd.Y) continue;

            var itemStart = new Vector2(x, itemY);
            var itemEnd = new Vector2(x + width, itemY + itemHeight);

            var isItemHovering = mousePos.X >= x && mousePos.X <= x + width &&
                                 mousePos.Y >= MathF.Max(itemY, menuTop) &&
                                 mousePos.Y <= MathF.Min(itemY + itemHeight, menuEnd.Y);

            if (isItemHovering || i == selectedIndex)
            {
                var itemBgColor = isItemHovering
                    ? ImGui.ColorConvertFloat4ToU32(ColorPalette.LightBrown * 0.6f)
                    : ImGui.ColorConvertFloat4ToU32(ColorPalette.DarkBrown * 0.8f);
                drawList.AddRectFilled(itemStart, itemEnd, itemBgColor);
            }

            var fontScale = fontSize / FontSizes.Body;
            ImGui.SetWindowFontScale(fontScale);
            var itemTextSize = ImGui.CalcTextSize(items[i]);
            ImGui.SetWindowFontScale(1f);

            var itemTextPos = new Vector2(x + UiScale.Scaled(12f), itemY + (itemHeight - itemTextSize.Y) / 2);
            var itemOnDark = isItemHovering || i == selectedIndex;
            var itemTextColor = ImGui.ColorConvertFloat4ToU32(
                itemOnDark ? ColorPalette.LightText : ColorPalette.White);

            ImGui.SetWindowFontScale(fontScale);
            drawList.AddText(itemTextPos, itemTextColor, items[i]);
            ImGui.SetWindowFontScale(1f);
        }

        drawList.PopClipRect();

        // Scrollbar gutter on the right edge of the menu, drawn outside the
        // clip so it sits on top of the border.
        if (maxScroll > 0f)
        {
            var trackLeft = menuEnd.X - ScrollbarGutterWidth - ScrollbarTrackPadding;
            var trackRight = menuEnd.X - ScrollbarTrackPadding;
            var trackTop = menuTop + ScrollbarTrackPadding;
            var trackBottom = menuEnd.Y - ScrollbarTrackPadding;
            var trackHeight = trackBottom - trackTop;
            var thumbHeight = MathF.Max(UiScale.Scaled(16f), trackHeight * (visibleHeight / contentHeight));
            var thumbTop = trackTop + (trackHeight - thumbHeight) * (scrollY / maxScroll);

            drawList.AddRectFilled(new Vector2(trackLeft, trackTop), new Vector2(trackRight, trackBottom),
                ImGui.ColorConvertFloat4ToU32(ColorPalette.DarkBrown * 0.4f), UiScale.Scaled(2f));
            drawList.AddRectFilled(new Vector2(trackLeft, thumbTop),
                new Vector2(trackRight, thumbTop + thumbHeight),
                ImGui.ColorConvertFloat4ToU32(ColorPalette.Gold * 0.7f), UiScale.Scaled(2f));
        }
    }
}
