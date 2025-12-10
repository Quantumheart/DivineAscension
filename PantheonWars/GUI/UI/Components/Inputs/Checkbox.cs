using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using ImGuiNET;
using PantheonWars.GUI.UI.Utilities;
using Vintagestory.API.Client;

namespace PantheonWars.GUI.UI.Components.Inputs;

/// <summary>
///     Checkbox input component with label
///     Handles rendering, hover, and click interaction
/// </summary>
[ExcludeFromCodeCoverage]
public static class Checkbox
{
    /// <summary>
    ///     Draw a checkbox with label
    /// </summary>
    /// <param name="drawList">ImGui draw list</param>
    /// <param name="api">Client API for sound playback</param>
    /// <param name="label">Label text to display next to checkbox</param>
    /// <param name="x">X position</param>
    /// <param name="y">Y position</param>
    /// <param name="isChecked">Current checked state</param>
    /// <param name="checkboxSize">Size of the checkbox square (default 20f)</param>
    /// <param name="labelPadding">Padding between checkbox and label (default 8f)</param>
    /// <returns>Updated checked state</returns>
    public static bool Draw(
        ImDrawListPtr drawList,
        ICoreClientAPI api,
        string label,
        float x,
        float y,
        bool isChecked,
        float checkboxSize = 20f,
        float labelPadding = 8f)
    {
        // todo: why not usages?
        var checkboxStart = new Vector2(x, y);
        var checkboxEnd = new Vector2(x + checkboxSize, y + checkboxSize);

        var mousePos = ImGui.GetMousePos();
        var isHovering = mousePos.X >= x && mousePos.X <= x + checkboxSize &&
                         mousePos.Y >= y && mousePos.Y <= y + checkboxSize;

        // Draw checkbox background
        var bgColor = isHovering
            ? ImGui.ColorConvertFloat4ToU32(ColorPalette.LightBrown * 0.7f)
            : ImGui.ColorConvertFloat4ToU32(ColorPalette.DarkBrown * 0.7f);
        drawList.AddRectFilled(checkboxStart, checkboxEnd, bgColor, 3f);

        // Draw border
        var borderColor = ImGui.ColorConvertFloat4ToU32(isChecked ? ColorPalette.Gold : ColorPalette.Grey * 0.5f);
        drawList.AddRect(checkboxStart, checkboxEnd, borderColor, 3f, ImDrawFlags.None, 1.5f);

        if (isHovering) ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);

        // Draw checkmark if checked
        if (isChecked)
        {
            var checkColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.Gold);
            drawList.AddLine(
                new Vector2(x + 4f, y + checkboxSize / 2),
                new Vector2(x + checkboxSize / 2 - 1f, y + checkboxSize - 5f),
                checkColor, 2f
            );
            drawList.AddLine(
                new Vector2(x + checkboxSize / 2 - 1f, y + checkboxSize - 5f),
                new Vector2(x + checkboxSize - 4f, y + 4f),
                checkColor, 2f
            );
        }

        // Draw label
        var labelPos = new Vector2(x + checkboxSize + labelPadding, y + (checkboxSize - 14f) / 2);
        var labelColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.White);
        drawList.AddText(labelPos, labelColor, label);

        // Handle click
        if (isHovering && ImGui.IsMouseClicked(ImGuiMouseButton.Left))
        {
            // todo: rethink this
            // api.World.PlaySoundAt(new AssetLocation("pantheonwars:sounds/click"),
            // api.World.Player.Entity, null, false, 8f, 0.3f);
            return !isChecked;
        }

        return isChecked;
    }
}