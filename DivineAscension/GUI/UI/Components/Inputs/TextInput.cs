using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using DivineAscension.GUI.UI.Utilities;
using ImGuiNET;

namespace DivineAscension.GUI.UI.Components.Inputs;

/// <summary>
///     Reusable text input component
///     Provides consistent single-line text input styling across all UI overlays
/// </summary>
[ExcludeFromCodeCoverage]
internal static class TextInput
{
    /// <summary>
    ///     Draw a single-line text input field
    /// </summary>
    /// <param name="drawList">ImGui draw list</param>
    /// <param name="id">Unique identifier for this input (must start with ##)</param>
    /// <param name="currentValue">Current text value</param>
    /// <param name="x">X position</param>
    /// <param name="y">Y position</param>
    /// <param name="width">Input width</param>
    /// <param name="height">Input height</param>
    /// <param name="placeholder">Placeholder text when empty</param>
    /// <param name="maxLength">Maximum character length (default 200)</param>
    /// <returns>Updated text value</returns>
    public static string Draw(
        ImDrawListPtr drawList,
        string id,
        string currentValue,
        float x,
        float y,
        float width,
        float height,
        string placeholder = "",
        int maxLength = 200)
    {
        // Position ImGui cursor for the input widget
        ImGui.SetCursorScreenPos(new Vector2(x, y));

        // Save current style colors
        var style = ImGui.GetStyle();
        var prevFrameBg = style.Colors[(int)ImGuiCol.FrameBg];
        var prevFrameBgHovered = style.Colors[(int)ImGuiCol.FrameBgHovered];
        var prevFrameBgActive = style.Colors[(int)ImGuiCol.FrameBgActive];
        var prevText = style.Colors[(int)ImGuiCol.Text];
        var prevBorder = style.Colors[(int)ImGuiCol.Border];

        // Set custom colors to match DivineAscension theme
        style.Colors[(int)ImGuiCol.FrameBg] = ColorPalette.DarkBrown * 0.7f;
        style.Colors[(int)ImGuiCol.FrameBgHovered] = ColorPalette.DarkBrown * 0.8f;
        style.Colors[(int)ImGuiCol.FrameBgActive] = ColorPalette.DarkBrown * 0.9f;
        style.Colors[(int)ImGuiCol.Text] = ColorPalette.White;
        style.Colors[(int)ImGuiCol.Border] = ColorPalette.Grey * 0.5f;

        // Push frame size to match requested dimensions
        ImGui.PushItemWidth(width);

        // Use ImGui's native single-line text input with clipboard support
        var buffer = currentValue ?? string.Empty;
        ImGui.InputTextWithHint(id, placeholder, ref buffer, (uint)maxLength);

        ImGui.PopItemWidth();

        // Restore previous colors
        style.Colors[(int)ImGuiCol.FrameBg] = prevFrameBg;
        style.Colors[(int)ImGuiCol.FrameBgHovered] = prevFrameBgHovered;
        style.Colors[(int)ImGuiCol.FrameBgActive] = prevFrameBgActive;
        style.Colors[(int)ImGuiCol.Text] = prevText;
        style.Colors[(int)ImGuiCol.Border] = prevBorder;

        return buffer;
    }

    /// <summary>
    ///     Draw a multiline text input field
    /// </summary>
    /// <param name="drawList">ImGui draw list</param>
    /// <param name="id">Unique identifier for this input (must start with ##)</param>
    /// <param name="currentValue">Current text value</param>
    /// <param name="x">X position</param>
    /// <param name="y">Y position</param>
    /// <param name="width">Input width</param>
    /// <param name="height">Input height</param>
    /// <param name="maxLength">Maximum character length (default 500)</param>
    /// <returns>Updated text value</returns>
    public static string DrawMultiline(
        ImDrawListPtr drawList,
        string id,
        string currentValue,
        float x,
        float y,
        float width,
        float height,
        int maxLength = 500)
    {
        // Position ImGui cursor for the input widget
        ImGui.SetCursorScreenPos(new Vector2(x, y));

        // Save current style colors
        var style = ImGui.GetStyle();
        var prevFrameBg = style.Colors[(int)ImGuiCol.FrameBg];
        var prevFrameBgHovered = style.Colors[(int)ImGuiCol.FrameBgHovered];
        var prevFrameBgActive = style.Colors[(int)ImGuiCol.FrameBgActive];
        var prevText = style.Colors[(int)ImGuiCol.Text];
        var prevBorder = style.Colors[(int)ImGuiCol.Border];

        // Set custom colors to match DivineAscension theme
        style.Colors[(int)ImGuiCol.FrameBg] = ColorPalette.DarkBrown * 0.7f;
        style.Colors[(int)ImGuiCol.FrameBgHovered] = ColorPalette.DarkBrown * 0.8f;
        style.Colors[(int)ImGuiCol.FrameBgActive] = ColorPalette.DarkBrown * 0.9f;
        style.Colors[(int)ImGuiCol.Text] = ColorPalette.White;
        style.Colors[(int)ImGuiCol.Border] = ColorPalette.Grey * 0.5f;

        // Use ImGui's native multiline text input with clipboard support
        var buffer = currentValue ?? string.Empty;
        ImGui.InputTextMultiline(id, ref buffer, (uint)maxLength, new Vector2(width, height),
            ImGuiInputTextFlags.None);

        // Restore previous colors
        style.Colors[(int)ImGuiCol.FrameBg] = prevFrameBg;
        style.Colors[(int)ImGuiCol.FrameBgHovered] = prevFrameBgHovered;
        style.Colors[(int)ImGuiCol.FrameBgActive] = prevFrameBgActive;
        style.Colors[(int)ImGuiCol.Text] = prevText;
        style.Colors[(int)ImGuiCol.Border] = prevBorder;

        return buffer;
    }
}