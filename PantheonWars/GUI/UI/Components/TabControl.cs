using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using ImGuiNET;
using PantheonWars.GUI.UI.Utilities;

namespace PantheonWars.GUI.UI.Components;

/// <summary>
///     Generic tab control component for switching between views
///     Handles tab rendering, hover, selection, and click detection
/// </summary>
[ExcludeFromCodeCoverage]
public static class TabControl
{
    /// <summary>
    ///     Draw a horizontal tab control
    /// </summary>
    /// <param name="drawList">ImGui draw list</param>
    /// <param name="x">X position</param>
    /// <param name="y">Y position</param>
    /// <param name="width">Total width for all tabs</param>
    /// <param name="height">Height of each tab</param>
    /// <param name="tabs">Array of tab labels</param>
    /// <param name="selectedIndex">Currently selected tab index</param>
    /// <param name="tabSpacing">Spacing between tabs (default 4f)</param>
    /// <returns>Updated selected tab index (may differ from input if tab clicked)</returns>
    public static int Draw(
        ImDrawListPtr drawList,
        float x,
        float y,
        float width,
        float height,
        string[] tabs,
        int selectedIndex,
        float tabSpacing = 4f)
    {
        if (tabs == null || tabs.Length == 0) return selectedIndex;

        // Calculate tab width
        var tabWidth = (width - tabSpacing * (tabs.Length - 1)) / tabs.Length;
        var tabX = x;
        var newSelectedIndex = selectedIndex;

        for (var i = 0; i < tabs.Length; i++)
        {
            var isSelected = selectedIndex == i;

            if (DrawTab(drawList, tabs[i], tabX, y, tabWidth, height, isSelected)) newSelectedIndex = i;

            tabX += tabWidth + tabSpacing;
        }

        return newSelectedIndex;
    }

    /// <summary>
    ///     Draw a single tab
    /// </summary>
    /// <returns>True if tab was clicked (and was not already selected)</returns>
    private static bool DrawTab(ImDrawListPtr drawList, string text, float x, float y, float width, float height,
        bool isSelected)
    {
        var tabStart = new Vector2(x, y);
        var tabEnd = new Vector2(x + width, y + height);

        var mousePos = ImGui.GetMousePos();
        var isHovering = mousePos.X >= x && mousePos.X <= x + width &&
                         mousePos.Y >= y && mousePos.Y <= y + height;

        // Determine background color
        Vector4 bgColor;
        if (isSelected)
        {
            bgColor = ColorPalette.Gold * 0.4f;
        }
        else if (isHovering)
        {
            bgColor = ColorPalette.LightBrown;
            ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
        }
        else
        {
            bgColor = ColorPalette.DarkBrown;
        }

        // Draw background
        var bgColorU32 = ImGui.ColorConvertFloat4ToU32(bgColor);
        drawList.AddRectFilled(tabStart, tabEnd, bgColorU32, 4f);

        // Draw border
        var borderColor = ImGui.ColorConvertFloat4ToU32(isSelected ? ColorPalette.Gold : ColorPalette.Grey * 0.5f);
        drawList.AddRect(tabStart, tabEnd, borderColor, 4f, ImDrawFlags.None, isSelected ? 2f : 1f);

        // Draw text (centered)
        var textSize = ImGui.CalcTextSize(text);
        var textPos = new Vector2(
            x + (width - textSize.X) / 2,
            y + (height - textSize.Y) / 2
        );
        var textColor = ImGui.ColorConvertFloat4ToU32(isSelected ? ColorPalette.White : ColorPalette.Grey);
        drawList.AddText(textPos, textColor, text);

        return !isSelected && isHovering && ImGui.IsMouseClicked(ImGuiMouseButton.Left);
    }
}