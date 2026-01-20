using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using DivineAscension.GUI.UI.Utilities;
using ImGuiNET;

namespace DivineAscension.GUI.UI.Components.Buttons;

/// <summary>
///     Reusable button rendering component
///     Provides consistent button styles across all UI overlays
/// </summary>
[ExcludeFromCodeCoverage]
internal static class ButtonRenderer
{
    /// <summary>
    ///     Draw a standard button with text
    /// </summary>
    /// <param name="drawList">ImGui draw list</param>
    /// <param name="text">Button text</param>
    /// <param name="x">X position</param>
    /// <param name="y">Y position</param>
    /// <param name="width">Button width</param>
    /// <param name="height">Button height</param>
    /// <param name="isPrimary">If true, uses gold color scheme</param>
    /// <param name="enabled">If false, button is grayed out and non-clickable</param>
    /// <param name="customColor">Optional custom color override</param>
    /// <returns>True if button was clicked</returns>
    public static bool DrawButton(
        ImDrawListPtr drawList,
        string text,
        float x,
        float y,
        float width,
        float height,
        bool isPrimary = false,
        bool enabled = true,
        Vector4? customColor = null,
        string directoryPath = "",
        string iconName = "")
    {
        var buttonStart = new Vector2(x, y);
        var buttonEnd = new Vector2(x + width, y + height);

        var mousePos = ImGui.GetMousePos();
        var isHovering = enabled && mousePos.X >= x && mousePos.X <= x + width &&
                         mousePos.Y >= y && mousePos.Y <= y + height;

        var baseColor = customColor ?? (isPrimary ? ColorPalette.Gold : ColorPalette.DarkBrown);

        Vector4 bgColor;
        if (!enabled)
        {
            bgColor = ColorPalette.DarkBrown * 0.5f;
        }
        else if (isHovering && ImGui.IsMouseDown(ImGuiMouseButton.Left))
        {
            bgColor = baseColor * 0.7f;
        }
        else if (isHovering)
        {
            bgColor = baseColor * 1.2f;
            ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
        }
        else
        {
            bgColor = baseColor * 0.8f;
        }

        var bgColorU32 = ImGui.ColorConvertFloat4ToU32(bgColor);
        drawList.AddRectFilled(buttonStart, buttonEnd, bgColorU32, 4f);

        // Issue #71: Use BorderColor from palette, consistent 2px border width
        var borderColor = ImGui.ColorConvertFloat4ToU32(enabled ? ColorPalette.Gold * 0.7f : ColorPalette.BorderColor);
        drawList.AddRect(buttonStart, buttonEnd, borderColor, 4f, ImDrawFlags.None, 2f);
        // Icon rendering constants
        const float iconSize = 20f;
        const float leftPadding = 8f;
        const float iconSpacing = 6f;
        const float rightPadding = 8f;

        if (directoryPath != "" && iconName != "")
        {
            var textureId = GuiIconLoader.GetTextureId(directoryPath, iconName);
            var iconY = y + (height - iconSize) / 2f; // Center vertically
            var iconX = x + leftPadding; // Position from left with padding
            var iconMin = new Vector2(iconX, iconY);
            var iconMax = new Vector2(iconX + iconSize, iconY + iconSize);
            drawList.AddImage(textureId, iconMin, iconMax, Vector2.Zero, Vector2.One);
        }

        // Only render text if it's not null or empty
        if (!string.IsNullOrEmpty(text))
        {
            var textSize = ImGui.CalcTextSize(text);

            // Calculate horizontal text position accounting for icon
            float textX;
            if (directoryPath != "" && iconName != "")
            {
                // Icon is present: offset text to the right with padding on both sides
                var iconTotalWidth = leftPadding + iconSize + iconSpacing;
                var availableWidth = width - iconTotalWidth - rightPadding;
                textX = x + iconTotalWidth + (availableWidth - textSize.X) / 2;
            }
            else
            {
                // No icon: center text in full button width
                textX = x + (width - textSize.X) / 2;
            }

            var textPos = new Vector2(textX, y + (height - textSize.Y) / 2);
            var textColor = ImGui.ColorConvertFloat4ToU32(enabled ? ColorPalette.White : ColorPalette.Grey * 0.7f);
            drawList.AddText(textPos, textColor, text);
        }

        return enabled && isHovering && ImGui.IsMouseReleased(ImGuiMouseButton.Left);
    }

    /// <summary>
    ///     Draw a close button (X)
    /// </summary>
    /// <param name="drawList">ImGui draw list</param>
    /// <param name="x">X position</param>
    /// <param name="y">Y position</param>
    /// <param name="size">Button size (square)</param>
    /// <returns>True if button was clicked</returns>
    public static bool DrawCloseButton(ImDrawListPtr drawList, float x, float y, float size)
    {
        var buttonStart = new Vector2(x, y);
        var buttonEnd = new Vector2(x + size, y + size);

        var mousePos = ImGui.GetMousePos();
        var isHovering = mousePos.X >= x && mousePos.X <= x + size &&
                         mousePos.Y >= y && mousePos.Y <= y + size;

        var bgColor = isHovering ? ColorPalette.LightBrown : ColorPalette.DarkBrown;
        var bgColorU32 = ImGui.ColorConvertFloat4ToU32(bgColor);
        drawList.AddRectFilled(buttonStart, buttonEnd, bgColorU32, 4f);

        var xColor = ImGui.ColorConvertFloat4ToU32(isHovering ? ColorPalette.White : ColorPalette.Grey);
        var xPadding = size * 0.25f;
        drawList.AddLine(new Vector2(x + xPadding, y + xPadding),
            new Vector2(x + size - xPadding, y + size - xPadding), xColor, 2f);
        drawList.AddLine(new Vector2(x + size - xPadding, y + xPadding),
            new Vector2(x + xPadding, y + size - xPadding), xColor, 2f);

        if (isHovering) ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);

        return isHovering && ImGui.IsMouseClicked(ImGuiMouseButton.Left);
    }

    /// <summary>
    ///     Draw a small button (typically used in list items)
    /// </summary>
    /// <param name="drawList">ImGui draw list</param>
    /// <param name="text">Button text</param>
    /// <param name="x">X position</param>
    /// <param name="y">Y position</param>
    /// <param name="width">Button width</param>
    /// <param name="height">Button height</param>
    /// <param name="color">Optional color override (defaults to dark brown, red on hover)</param>
    /// <returns>True if button was clicked</returns>
    public static bool DrawSmallButton(
        ImDrawListPtr drawList,
        string text,
        float x,
        float y,
        float width,
        float height,
        Vector4? color = null)
    {
        var buttonStart = new Vector2(x, y);
        var buttonEnd = new Vector2(x + width, y + height);

        var mousePos = ImGui.GetMousePos();
        var isHovering = mousePos.X >= x && mousePos.X <= x + width &&
                         mousePos.Y >= y && mousePos.Y <= y + height;

        var defaultColor = color ?? ColorPalette.DarkBrown;
        var hoverColor = color.HasValue ? ColorPalette.Lighten(color.Value) : ColorPalette.Red * 0.8f;
        var bgColor = isHovering ? hoverColor : defaultColor;
        var bgColorU32 = ImGui.ColorConvertFloat4ToU32(bgColor);
        drawList.AddRectFilled(buttonStart, buttonEnd, bgColorU32, 4f);

        var borderColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.Grey * 0.5f);
        drawList.AddRect(buttonStart, buttonEnd, borderColor, 4f, ImDrawFlags.None, 1f);

        var textSize = ImGui.CalcTextSize(text);
        var textPos = new Vector2(x + (width - textSize.X) / 2, y + (height - textSize.Y) / 2);
        var textColor = ImGui.ColorConvertFloat4ToU32(isHovering ? ColorPalette.White : ColorPalette.Grey);
        drawList.AddText(textPos, textColor, text);

        if (isHovering) ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);

        return isHovering && ImGui.IsMouseReleased(ImGuiMouseButton.Left);
    }

    /// <summary>
    ///     Draw an action button (typically for dangerous actions like "Kick", "Delete", etc.)
    /// </summary>
    /// <param name="drawList">ImGui draw list</param>
    /// <param name="text">Button text</param>
    /// <param name="x">X position</param>
    /// <param name="y">Y position</param>
    /// <param name="width">Button width</param>
    /// <param name="height">Button height</param>
    /// <param name="isDangerous">If true, uses red color scheme</param>
    /// <returns>True if button was clicked</returns>
    public static bool DrawActionButton(
        ImDrawListPtr drawList,
        string text,
        float x,
        float y,
        float width,
        float height,
        bool isDangerous = false)
    {
        var color = isDangerous ? ColorPalette.Red * 0.6f : ColorPalette.Gold * 0.6f;
        return DrawButton(drawList, text, x, y, width, height, true, true, color);
    }

    /// <summary>
    ///     Draw a small icon button (typically used for inline edit actions)
    /// </summary>
    /// <param name="drawList">ImGui draw list</param>
    /// <param name="icon">Icon character (e.g., "✎" for edit)</param>
    /// <param name="x">X position</param>
    /// <param name="y">Y position</param>
    /// <param name="width">Button width</param>
    /// <param name="height">Button height</param>
    /// <param name="enabled">If false, button is grayed out and non-clickable</param>
    /// <returns>True if button was clicked</returns>
    public static bool DrawIconButton(
        ImDrawListPtr drawList,
        string icon,
        float x,
        float y,
        float width,
        float height,
        bool enabled = true)
    {
        var buttonStart = new Vector2(x, y);
        var buttonEnd = new Vector2(x + width, y + height);

        var mousePos = ImGui.GetMousePos();
        var isHovering = enabled && mousePos.X >= x && mousePos.X <= x + width &&
                         mousePos.Y >= y && mousePos.Y <= y + height;

        Vector4 bgColor;
        if (!enabled)
        {
            bgColor = ColorPalette.DarkBrown * 0.3f;
        }
        else if (isHovering && ImGui.IsMouseDown(ImGuiMouseButton.Left))
        {
            bgColor = ColorPalette.Gold * 0.5f;
        }
        else if (isHovering)
        {
            bgColor = ColorPalette.Gold * 0.3f;
            ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
        }
        else
        {
            bgColor = ColorPalette.DarkBrown * 0.5f;
        }

        var bgColorU32 = ImGui.ColorConvertFloat4ToU32(bgColor);
        drawList.AddRectFilled(buttonStart, buttonEnd, bgColorU32, 3f);

        var textSize = ImGui.CalcTextSize(icon);
        var textPos = new Vector2(x + (width - textSize.X) / 2, y + (height - textSize.Y) / 2);
        var textColor = ImGui.ColorConvertFloat4ToU32(enabled ? ColorPalette.White : ColorPalette.Grey * 0.5f);
        drawList.AddText(textPos, textColor, icon);

        return enabled && isHovering && ImGui.IsMouseReleased(ImGuiMouseButton.Left);
    }
}