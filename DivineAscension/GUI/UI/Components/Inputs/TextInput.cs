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
        var inputStart = new Vector2(x, y);
        var inputEnd = new Vector2(x + width, y + height);

        var bgColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.DarkBrown * 0.7f);
        drawList.AddRectFilled(inputStart, inputEnd, bgColor, 4f);

        var borderColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.Grey * 0.5f);
        drawList.AddRect(inputStart, inputEnd, borderColor, 4f, ImDrawFlags.None, 1f);

        ImGui.SetCursorScreenPos(inputStart);
        ImGui.InvisibleButton(id, new Vector2(width, height));
        var isActive = ImGui.IsItemActive() || ImGui.IsItemFocused();
        var wasClicked = ImGui.IsItemClicked();

        if (isActive || ImGui.IsItemHovered()) ImGui.SetMouseCursor(ImGuiMouseCursor.TextInput);

        // Set keyboard focus when clicked
        if (wasClicked) ImGui.SetKeyboardFocusHere(-1);

        if (isActive)
        {
            var io = ImGui.GetIO();
            io.WantCaptureKeyboard = true; // Block game input while typing

            if (ImGui.IsKeyPressed(ImGuiKey.Backspace) && currentValue.Length > 0)
                currentValue = currentValue.Substring(0, currentValue.Length - 1);

            for (var i = 0; i < io.InputQueueCharacters.Size; i++)
            {
                var c = (char)io.InputQueueCharacters[i];
                if (c >= 32 && c < 127 && currentValue.Length < maxLength) currentValue += c;
            }
        }

        var displayText = string.IsNullOrEmpty(currentValue) ? placeholder : currentValue;
        var textColor = string.IsNullOrEmpty(currentValue)
            ? ImGui.ColorConvertFloat4ToU32(ColorPalette.Grey * 0.7f)
            : ImGui.ColorConvertFloat4ToU32(ColorPalette.White);

        var textPos = new Vector2(x + 8f, y + (height - 16f) / 2);
        drawList.AddText(textPos, textColor, displayText);

        // Draw blinking cursor when active
        if (isActive && (int)(ImGui.GetTime() * 2) % 2 == 0)
        {
            var textWidth = string.IsNullOrEmpty(currentValue) ? 0f : ImGui.CalcTextSize(currentValue).X;
            var cursorX = x + 8f + textWidth;
            drawList.AddLine(new Vector2(cursorX, y + 6f), new Vector2(cursorX, y + height - 6f),
                ImGui.ColorConvertFloat4ToU32(ColorPalette.White), 2f);
        }

        return currentValue ?? "";
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
        var inputStart = new Vector2(x, y);
        var inputEnd = new Vector2(x + width, y + height);

        var bgColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.DarkBrown * 0.7f);
        drawList.AddRectFilled(inputStart, inputEnd, bgColor, 4f);

        var borderColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.Grey * 0.5f);
        drawList.AddRect(inputStart, inputEnd, borderColor, 4f, ImDrawFlags.None, 1f);

        ImGui.SetCursorScreenPos(inputStart);
        ImGui.InvisibleButton(id, new Vector2(width, height));
        var isActive = ImGui.IsItemActive() || ImGui.IsItemFocused();
        var wasClicked = ImGui.IsItemClicked();

        if (isActive || ImGui.IsItemHovered()) ImGui.SetMouseCursor(ImGuiMouseCursor.TextInput);

        // Set keyboard focus when clicked
        if (wasClicked) ImGui.SetKeyboardFocusHere(-1);

        if (isActive)
        {
            var io = ImGui.GetIO();
            io.WantCaptureKeyboard = true; // Block game input while typing

            if (ImGui.IsKeyPressed(ImGuiKey.Backspace) && currentValue.Length > 0)
                currentValue = currentValue.Substring(0, currentValue.Length - 1);

            if (ImGui.IsKeyPressed(ImGuiKey.Enter)) currentValue += "\n";

            for (var i = 0; i < io.InputQueueCharacters.Size; i++)
            {
                var c = (char)io.InputQueueCharacters[i];
                if (c >= 32 && c < 127 && currentValue.Length < maxLength) currentValue += c;
            }
        }

        // Draw text (simple word wrap)
        var textPos = new Vector2(x + 8f, y + 8f);
        var textColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.White);
        if (!string.IsNullOrEmpty(currentValue)) drawList.AddText(textPos, textColor, currentValue);

        // Draw blinking cursor when active
        if (isActive && (int)(ImGui.GetTime() * 2) % 2 == 0)
        {
            // Calculate cursor position at end of text
            var text = currentValue ?? "";
            var lines = text.Split('\n');
            var lastLine = lines.Length > 0 ? lines[lines.Length - 1] : "";

            // Calculate cursor position - handle empty strings
            var lastLineWidth = 0f;
            if (!string.IsNullOrEmpty(lastLine)) lastLineWidth = ImGui.CalcTextSize(lastLine).X;

            var cursorY = y + 8f + (lines.Length - 1) * 16f; // Approximate line height
            var cursorX = x + 8f + lastLineWidth;

            // Draw cursor line
            drawList.AddLine(new Vector2(cursorX, cursorY), new Vector2(cursorX, cursorY + 16f),
                ImGui.ColorConvertFloat4ToU32(ColorPalette.White), 2f);
        }

        return currentValue ?? "";
    }
}