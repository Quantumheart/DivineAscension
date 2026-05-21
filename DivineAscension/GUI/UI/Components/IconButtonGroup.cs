using System;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using DivineAscension.GUI.UI.Utilities;
using ImGuiNET;

namespace DivineAscension.GUI.UI.Components;

/// <summary>
///     Renders a horizontal group of icon + label buttons with single-selection
///     semantics. Used today for the deity-domain selector in the Religion Create
///     form. Not a navigation control — the main dialog navigation lives in
///     <c>SidebarRenderer</c>.
/// </summary>
[ExcludeFromCodeCoverage]
public static class IconButtonGroup
{
    /// <summary>
    ///     Draw a horizontal button group with icons and hover tracking.
    /// </summary>
    /// <returns>Tuple of (selectedIndex, hoveredIndex). hoveredIndex is -1 if no hover.</returns>
    public static (int selectedIndex, int hoveredIndex) DrawWithHover(
        ImDrawListPtr drawList,
        float x,
        float y,
        float width,
        float height,
        string[] buttons,
        int selectedIndex,
        float buttonSpacing = 4f,
        string iconDirectory = "gui",
        string[]? iconNames = null)
    {
        if (buttons == null || buttons.Length == 0) return (selectedIndex, -1);

        var buttonWidth = (width - buttonSpacing * (buttons.Length - 1)) / buttons.Length;
        var buttonX = x;
        var newSelectedIndex = selectedIndex;
        var hoveredIndex = -1;

        for (var i = 0; i < buttons.Length; i++)
        {
            var isSelected = selectedIndex == i;
            var iconName = iconNames != null && i < iconNames.Length ? iconNames[i] : "";

            var (clicked, hovering) = DrawButton(
                drawList, buttons[i], buttonX, y, buttonWidth, height,
                isSelected, iconDirectory, iconName);

            if (clicked) newSelectedIndex = i;
            if (hovering) hoveredIndex = i;

            buttonX += buttonWidth + buttonSpacing;
        }

        return (newSelectedIndex, hoveredIndex);
    }

    private static (bool clicked, bool hovering) DrawButton(
        ImDrawListPtr drawList,
        string text,
        float x,
        float y,
        float width,
        float height,
        bool isSelected,
        string iconDirectory,
        string iconName)
    {
        const float iconSize = 20f;
        const float leftPadding = 8f;
        const float iconSpacing = 6f;
        const float rightPadding = 8f;

        var topLeft = new Vector2(x, y);
        var bottomRight = new Vector2(x + width, y + height);

        var mousePos = ImGui.GetMousePos();
        var isHovering = mousePos.X >= x && mousePos.X <= x + width &&
                         mousePos.Y >= y && mousePos.Y <= y + height;

        var bgColor = isSelected
            ? ColorPalette.Gold * 0.4f
            : isHovering
                ? ColorPalette.LightBrown
                : ColorPalette.DarkBrown;
        if (isHovering && !isSelected) ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);

        drawList.AddRectFilled(topLeft, bottomRight,
            ImGui.ColorConvertFloat4ToU32(bgColor), 4f);
        drawList.AddRect(topLeft, bottomRight,
            ImGui.ColorConvertFloat4ToU32(isSelected ? ColorPalette.Gold : ColorPalette.BorderColor),
            4f, ImDrawFlags.None, 2f);

        var hasIcon = !string.IsNullOrEmpty(iconDirectory) && !string.IsNullOrEmpty(iconName);
        if (hasIcon)
        {
            var textureId = GuiIconLoader.GetTextureId(iconDirectory, iconName);
            if (textureId != IntPtr.Zero)
            {
                var iconY = y + (height - iconSize) / 2f;
                var iconX = x + leftPadding;
                drawList.AddImage(textureId,
                    new Vector2(iconX, iconY),
                    new Vector2(iconX + iconSize, iconY + iconSize),
                    Vector2.Zero, Vector2.One,
                    ImGui.ColorConvertFloat4ToU32(new Vector4(1f, 1f, 1f, 1f)));
            }
        }

        var textSize = ImGui.CalcTextSize(text);
        float textX;
        if (hasIcon)
        {
            var iconTotalWidth = leftPadding + iconSize + iconSpacing;
            var availableWidth = width - iconTotalWidth - rightPadding;
            textX = x + iconTotalWidth + (availableWidth - textSize.X) / 2;
        }
        else
        {
            textX = x + (width - textSize.X) / 2;
        }

        var textPos = new Vector2(textX, y + (height - textSize.Y) / 2);
        var textColor = ImGui.ColorConvertFloat4ToU32(isSelected ? ColorPalette.LightText : ColorPalette.MutedText);
        drawList.AddText(textPos, textColor, text);

        var clicked = !isSelected && isHovering && ImGui.IsMouseClicked(ImGuiMouseButton.Left);
        return (clicked, isHovering);
    }
}
