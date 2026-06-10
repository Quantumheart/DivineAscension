using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using DivineAscension.GUI.UI.Utilities;
using ImGuiNET;

namespace DivineAscension.GUI.UI.Renderers.Components;

[ExcludeFromCodeCoverage]
internal static class CheckboxRenderer
{
    /// <summary>
    /// Draws a checkbox with label (pure version without side effects)
    /// </summary>
    public static bool DrawCheckbox(
        ImDrawListPtr drawList,
        string label,
        float x,
        float y,
        bool isChecked,
        float checkboxSize = -1f,
        float labelPadding = -1f)
    {
        if (checkboxSize < 0f) checkboxSize = UiScale.Scaled(20f);
        if (labelPadding < 0f) labelPadding = UiScale.Scaled(8f);

        var checkboxStart = new Vector2(x, y);
        var checkboxEnd = new Vector2(x + checkboxSize, y + checkboxSize);

        var mousePos = ImGui.GetMousePos();
        var isHovering = mousePos.X >= x && mousePos.X <= x + checkboxSize &&
                         mousePos.Y >= y && mousePos.Y <= y + checkboxSize;

        // Draw checkbox background
        var bgColor = isHovering
            ? ImGui.ColorConvertFloat4ToU32(ColorPalette.LightBrown * 0.7f)
            : ImGui.ColorConvertFloat4ToU32(ColorPalette.DarkBrown * 0.7f);
        drawList.AddRectFilled(checkboxStart, checkboxEnd, bgColor, UiScale.Scaled(3f));

        // Draw border
        var borderColor = ImGui.ColorConvertFloat4ToU32(isChecked ? ColorPalette.Gold : ColorPalette.Grey * 0.5f);
        drawList.AddRect(checkboxStart, checkboxEnd, borderColor, UiScale.Scaled(3f), ImDrawFlags.None, UiScale.Scaled(1.5f));

        if (isHovering) ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);

        // Draw checkmark if checked
        if (isChecked)
        {
            var checkColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.Gold);
            drawList.AddLine(
                new Vector2(x + UiScale.Scaled(4f), y + checkboxSize / 2),
                new Vector2(x + checkboxSize / 2 - UiScale.Scaled(1f), y + checkboxSize - UiScale.Scaled(5f)),
                checkColor, UiScale.Scaled(2f)
            );
            drawList.AddLine(
                new Vector2(x + checkboxSize / 2 - UiScale.Scaled(1f), y + checkboxSize - UiScale.Scaled(5f)),
                new Vector2(x + checkboxSize - UiScale.Scaled(4f), y + UiScale.Scaled(4f)),
                checkColor, UiScale.Scaled(2f)
            );
        }

        // Draw label
        var labelPos = new Vector2(x + checkboxSize + labelPadding, y + (checkboxSize - UiScale.Scaled(14f)) / 2);
        var labelColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.White);
        drawList.AddText(labelPos, labelColor, label);

        // Return new state if clicked (no sound - that's a side effect)
        if (isHovering && ImGui.IsMouseClicked(ImGuiMouseButton.Left))
        {
            return !isChecked;
        }

        return isChecked;
    }
}