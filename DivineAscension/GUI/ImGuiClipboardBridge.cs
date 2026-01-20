using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using DivineAscension.Services;
using Vintagestory.API.Common;

namespace DivineAscension.GUI;

/// <summary>
///     Bridge between ImGui's native clipboard callbacks and ClipboardService.
///     Provides static callback methods that handle marshaling between ImGui's unmanaged memory and .NET strings.
/// </summary>
[ExcludeFromCodeCoverage]
internal static class ImGuiClipboardBridge
{
    // Cache the last clipboard text to prevent premature garbage collection
    // ImGui expects the pointer returned by GetClipboardTextCallback to remain valid until the next call
    private static string? _lastClipboardText;
    private static IntPtr _lastClipboardTextPtr = IntPtr.Zero;
    private static ICoreAPI? _api;

    /// <summary>
    ///     Initialize the clipboard bridge with API for logging.
    /// </summary>
    public static void Initialize(ICoreAPI api)
    {
        _api = api;
    }

    /// <summary>
    ///     ImGui GetClipboardTextFn callback.
    ///     Called by ImGui when it needs to read clipboard content.
    /// </summary>
    /// <param name="userData">User data pointer (not used)</param>
    /// <returns>Pointer to UTF-8 encoded clipboard text</returns>
    public static IntPtr GetClipboardTextCallback(IntPtr userData)
    {
        _api?.Logger.Debug("[DivineAscension] GetClipboardTextCallback invoked");
        try
        {
            // Free previous allocation if any
            if (_lastClipboardTextPtr != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(_lastClipboardTextPtr);
                _lastClipboardTextPtr = IntPtr.Zero;
            }

            // Get text from ClipboardService
            string text = ClipboardService.Instance.GetText();
            _api?.Logger.Debug($"[DivineAscension] GetClipboardTextCallback got text: '{text}' (length={text.Length})");

            // Cache the string to prevent garbage collection
            _lastClipboardText = text;

            // Marshal to unmanaged UTF-8 string
            // ImGui expects UTF-8 encoded text
            _lastClipboardTextPtr = Marshal.StringToHGlobalAnsi(text);
            return _lastClipboardTextPtr;
        }
        catch (Exception ex)
        {
            _api?.Logger.Error($"[DivineAscension] GetClipboardTextCallback error: {ex}");
            return IntPtr.Zero;
        }
    }

    /// <summary>
    ///     ImGui SetClipboardTextFn callback.
    ///     Called by ImGui when it needs to write clipboard content.
    /// </summary>
    /// <param name="userData">User data pointer (not used)</param>
    /// <param name="text">Pointer to UTF-8 encoded text to set</param>
    public static void SetClipboardTextCallback(IntPtr userData, IntPtr text)
    {
        _api?.Logger.Debug("[DivineAscension] SetClipboardTextCallback invoked");
        try
        {
            // Marshal from unmanaged UTF-8 string to .NET string
            string? clipboardText = Marshal.PtrToStringAnsi(text);
            _api?.Logger.Debug($"[DivineAscension] SetClipboardTextCallback setting text: '{clipboardText}' (length={clipboardText?.Length ?? 0})");

            if (clipboardText != null)
            {
                ClipboardService.Instance.SetText(clipboardText);
                _api?.Logger.Debug("[DivineAscension] SetClipboardTextCallback completed successfully");
            }
        }
        catch (Exception ex)
        {
            _api?.Logger.Error($"[DivineAscension] SetClipboardTextCallback error: {ex}");
        }
    }

    /// <summary>
    ///     Cleanup method to free any allocated memory.
    ///     Should be called when the application exits.
    /// </summary>
    public static void Cleanup()
    {
        if (_lastClipboardTextPtr != IntPtr.Zero)
        {
            Marshal.FreeHGlobal(_lastClipboardTextPtr);
            _lastClipboardTextPtr = IntPtr.Zero;
        }

        _lastClipboardText = null;
    }
}
