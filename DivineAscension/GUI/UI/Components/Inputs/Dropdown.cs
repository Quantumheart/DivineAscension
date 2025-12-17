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
    /// <param name="fontSize">Font size for the text (default 13f)</param>
    /// <returns>True if the dropdown was clicked</returns>
    public static bool DrawButton(
        ImDrawListPtr drawList,
        float x,
        float y,
        float width,
        float height,
        string selectedText,
        bool isOpen,
        float fontSize = 13f)
    {
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
        var fontScale = fontSize / 13f;
        ImGui.SetWindowFontScale(fontScale);
        var scaledTextSize = ImGui.CalcTextSize(selectedText);
        ImGui.SetWindowFontScale(1f);

        var textPos = new Vector2(x + 12f, y + (height - scaledTextSize.Y) / 2);
        var textColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.White);

        ImGui.SetWindowFontScale(fontScale);
        drawList.AddText(textPos, textColor, selectedText);
        ImGui.SetWindowFontScale(1f);

        // Draw dropdown arrow
        var arrowX = x + width - 20f;
        var arrowY = y + height / 2;
        var arrowColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.Grey);
        if (isOpen)
            // Arrow pointing up when open
            drawList.AddTriangleFilled(
                new Vector2(arrowX, arrowY - 4f),
                new Vector2(arrowX - 4f, arrowY + 2f),
                new Vector2(arrowX + 4f, arrowY + 2f),
                arrowColor
            );
        else
            // Arrow pointing down when closed
            drawList.AddTriangleFilled(
                new Vector2(arrowX - 4f, arrowY - 2f),
                new Vector2(arrowX + 4f, arrowY - 2f),
                new Vector2(arrowX, arrowY + 4f),
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
    /// <returns>Tuple of (newSelectedIndex, shouldClose, clickConsumed)</returns>
    public static (int selectedIndex, bool shouldClose, bool clickConsumed) DrawMenuAndHandleInteraction(float x,
        float y,
        float width,
        float height,
        string[] items,
        int selectedIndex,
        float itemHeight = 40f)
    {
        var mousePos = ImGui.GetMousePos();
        var menuHeight = items.Length * itemHeight;
        var menuStart = new Vector2(x, y + height + 2f);
        var menuEnd = new Vector2(x + width, y + height + 2f + menuHeight);

        var clickConsumed = false;
        var shouldClose = false;
        var newSelectedIndex = selectedIndex;

        // Check if mouse is over the menu area
        var isMouseOverMenu = mousePos.X >= menuStart.X && mousePos.X <= menuEnd.X &&
                              mousePos.Y >= menuStart.Y && mousePos.Y <= menuEnd.Y;

        // Handle item clicks
        for (var i = 0; i < items.Length; i++)
        {
            var itemY = y + height + 2f + i * itemHeight;
            var isItemHovering = mousePos.X >= x && mousePos.X <= x + width &&
                                 mousePos.Y >= itemY && mousePos.Y <= itemY + itemHeight;

            // Handle item click
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
    /// <param name="fontSize">Font size for the text (default 13f)</param>
    public static void DrawMenuVisual(
        ImDrawListPtr drawList,
        float x,
        float y,
        float width,
        float height,
        string[] items,
        int selectedIndex,
        float itemHeight = 40f,
        float fontSize = 13f)
    {
        var mousePos = ImGui.GetMousePos();
        var menuHeight = items.Length * itemHeight;
        var menuStart = new Vector2(x, y + height + 2f);
        var menuEnd = new Vector2(x + width, y + height + 2f + menuHeight);

        // Draw menu background
        var menuBgColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.Background);
        drawList.AddRectFilled(menuStart, menuEnd, menuBgColor, 4f);

        // Draw menu border
        var menuBorderColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.Gold * 0.7f);
        drawList.AddRect(menuStart, menuEnd, menuBorderColor, 4f, ImDrawFlags.None, 2f);

        // Draw each item
        for (var i = 0; i < items.Length; i++)
        {
            var itemY = y + height + 2f + i * itemHeight;
            var itemStart = new Vector2(x, itemY);
            var itemEnd = new Vector2(x + width, itemY + itemHeight);

            var isItemHovering = mousePos.X >= x && mousePos.X <= x + width &&
                                 mousePos.Y >= itemY && mousePos.Y <= itemY + itemHeight;

            // Draw item background if hovering or selected
            if (isItemHovering || i == selectedIndex)
            {
                var itemBgColor = isItemHovering
                    ? ImGui.ColorConvertFloat4ToU32(ColorPalette.LightBrown * 0.6f)
                    : ImGui.ColorConvertFloat4ToU32(ColorPalette.DarkBrown * 0.8f);
                drawList.AddRectFilled(itemStart, itemEnd, itemBgColor);
            }

            // Draw item text with font scaling
            var fontScale = fontSize / 13f;
            ImGui.SetWindowFontScale(fontScale);
            var itemTextSize = ImGui.CalcTextSize(items[i]);
            ImGui.SetWindowFontScale(1f);

            var itemTextPos = new Vector2(x + 12f, itemY + (itemHeight - itemTextSize.Y) / 2);
            var itemTextColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.White);

            ImGui.SetWindowFontScale(fontScale);
            drawList.AddText(itemTextPos, itemTextColor, items[i]);
            ImGui.SetWindowFontScale(1f);
        }
    }
}