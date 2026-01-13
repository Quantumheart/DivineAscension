using System;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.InteropServices;
using DivineAscension.GUI.UI.Utilities;
using DivineAscension.Services;
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
    ///     InputText callback to handle clipboard operations manually.
    ///     VSImGui's OpenTK backend may not properly configure ImGui clipboard callbacks on Linux,
    ///     so we intercept Ctrl+C/Ctrl+V and handle them explicitly.
    /// </summary>
    private static unsafe int ClipboardCallback(ImGuiInputTextCallbackData* data)
    {
        if (data == null) return 0;

        // Use IsKeyDown instead of io.KeyCtrl - VSImGui's OpenTK backend doesn't properly
        // forward keyboard modifier state to ImGui's IO structure on Linux
        var ctrlHeld = ImGui.IsKeyDown(ImGuiKey.LeftCtrl) || ImGui.IsKeyDown(ImGuiKey.RightCtrl);

        // Check if there's a selection
        var hasSelection = data->SelectionStart != data->SelectionEnd;

        // Handle Ctrl+V (paste)
        if (ctrlHeld && ImGui.IsKeyPressed(ImGuiKey.V))
        {
            var clipboardText = ClipboardService.Instance.GetText();
            if (!string.IsNullOrEmpty(clipboardText))
            {
                // If there's a selection, delete it first by modifying cursor position
                if (hasSelection)
                {
                    var selStart = Math.Min(data->SelectionStart, data->SelectionEnd);
                    var selEnd = Math.Max(data->SelectionStart, data->SelectionEnd);

                    // Move remaining text over deleted selection
                    var bytesToMove = data->BufTextLen - selEnd;
                    if (bytesToMove > 0)
                    {
                        Buffer.MemoryCopy(
                            data->Buf + selEnd,
                            data->Buf + selStart,
                            data->BufSize - selStart,
                            bytesToMove);
                    }

                    data->BufTextLen -= selEnd - selStart;
                    data->CursorPos = selStart;
                    data->SelectionStart = data->SelectionEnd = selStart;
                    data->BufDirty = 1;
                }

                // Insert clipboard text at cursor
                var clipBytes = System.Text.Encoding.UTF8.GetBytes(clipboardText);
                var insertLen = clipBytes.Length;
                var availableSpace = data->BufSize - data->BufTextLen - 1; // -1 for null terminator

                if (insertLen > availableSpace)
                    insertLen = availableSpace;

                if (insertLen > 0)
                {
                    // Shift existing text to make room
                    var bytesToShift = data->BufTextLen - data->CursorPos;
                    if (bytesToShift > 0)
                    {
                        Buffer.MemoryCopy(
                            data->Buf + data->CursorPos,
                            data->Buf + data->CursorPos + insertLen,
                            data->BufSize - data->CursorPos - insertLen,
                            bytesToShift);
                    }

                    // Copy clipboard text
                    fixed (byte* clipPtr = clipBytes)
                    {
                        Buffer.MemoryCopy(clipPtr, data->Buf + data->CursorPos, insertLen, insertLen);
                    }

                    data->CursorPos += insertLen;
                    data->BufTextLen += insertLen;
                    data->Buf[data->BufTextLen] = 0; // Null terminate
                    data->BufDirty = 1;
                }
            }
        }

        // Handle Ctrl+C (copy)
        if (ctrlHeld && ImGui.IsKeyPressed(ImGuiKey.C) && hasSelection)
        {
            var selStart = Math.Min(data->SelectionStart, data->SelectionEnd);
            var selEnd = Math.Max(data->SelectionStart, data->SelectionEnd);
            var length = selEnd - selStart;

            if (length > 0)
            {
                var selectedText = Marshal.PtrToStringUTF8((IntPtr)(data->Buf + selStart), length);
                if (!string.IsNullOrEmpty(selectedText))
                {
                    ClipboardService.Instance.SetText(selectedText);
                }
            }
        }

        // Handle Ctrl+X (cut)
        if (ctrlHeld && ImGui.IsKeyPressed(ImGuiKey.X) && hasSelection)
        {
            var selStart = Math.Min(data->SelectionStart, data->SelectionEnd);
            var selEnd = Math.Max(data->SelectionStart, data->SelectionEnd);
            var length = selEnd - selStart;

            if (length > 0)
            {
                // Copy to clipboard
                var selectedText = Marshal.PtrToStringUTF8((IntPtr)(data->Buf + selStart), length);
                if (!string.IsNullOrEmpty(selectedText))
                {
                    ClipboardService.Instance.SetText(selectedText);
                }

                // Delete selected text
                var bytesToMove = data->BufTextLen - selEnd;
                if (bytesToMove > 0)
                {
                    Buffer.MemoryCopy(
                        data->Buf + selEnd,
                        data->Buf + selStart,
                        data->BufSize - selStart,
                        bytesToMove);
                }

                data->BufTextLen -= length;
                data->CursorPos = selStart;
                data->SelectionStart = data->SelectionEnd = selStart;
                data->Buf[data->BufTextLen] = 0; // Null terminate
                data->BufDirty = 1;
            }
        }

        return 0;
    }

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

        // Use ImGui's native single-line text input with manual clipboard callback
        var buffer = currentValue ?? string.Empty;
        unsafe
        {
            ImGui.InputTextWithHint(id, placeholder, ref buffer, (uint)maxLength,
                ImGuiInputTextFlags.CallbackAlways, ClipboardCallback);
        }

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

        // Use ImGui's native multiline text input with manual clipboard callback
        var buffer = currentValue ?? string.Empty;
        unsafe
        {
            ImGui.InputTextMultiline(id, ref buffer, (uint)maxLength, new Vector2(width, height),
                ImGuiInputTextFlags.CallbackAlways, ClipboardCallback);
        }

        // Restore previous colors
        style.Colors[(int)ImGuiCol.FrameBg] = prevFrameBg;
        style.Colors[(int)ImGuiCol.FrameBgHovered] = prevFrameBgHovered;
        style.Colors[(int)ImGuiCol.FrameBgActive] = prevFrameBgActive;
        style.Colors[(int)ImGuiCol.Text] = prevText;
        style.Colors[(int)ImGuiCol.Border] = prevBorder;

        return buffer;
    }
}