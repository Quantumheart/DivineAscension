using System;
using System.Runtime.InteropServices;
using DivineAscension.Services;
using ImGuiNET;
using Vintagestory.API.Common;

namespace DivineAscension.GUI;

/// <summary>
///     Helper class to configure ImGui clipboard callbacks.
///     Call SetupClipboardCallbacks() once during ImGui initialization.
/// </summary>
internal static class ImGuiClipboardHelper
{
    private static bool _isConfigured;

    // Keep delegate references alive to prevent garbage collection
    private static ImGuiGetClipboardTextCallback? _getClipboardDelegate;
    private static ImGuiSetClipboardTextCallback? _setClipboardDelegate;

    /// <summary>
    ///     Configure ImGui clipboard callbacks using ClipboardService.
    ///     This should be called once after ImGui is initialized.
    /// </summary>
    /// <param name="api">The Core API for logging</param>
    public static void SetupClipboardCallbacks(ICoreAPI api)
    {
        if (_isConfigured)
        {
            return;
        }

        try
        {
            // Initialize the clipboard bridge with API for logging
            ImGuiClipboardBridge.Initialize(api);

            // Create delegates and keep them alive in static fields
            _getClipboardDelegate = GetClipboardTextCallback;
            _setClipboardDelegate = SetClipboardTextCallback;

            // Get ImGuiIO and set clipboard callbacks
            var io = ImGui.GetIO();
            io.GetClipboardTextFn = Marshal.GetFunctionPointerForDelegate(_getClipboardDelegate);
            io.SetClipboardTextFn = Marshal.GetFunctionPointerForDelegate(_setClipboardDelegate);

            _isConfigured = true;
            api.Logger.Notification("[DivineAscension] ImGui clipboard callbacks configured");
        }
        catch (Exception ex)
        {
            api.Logger.Error($"[DivineAscension] Failed to configure ImGui clipboard callbacks: {ex}");
        }
    }

    /// <summary>
    ///     ImGui GetClipboardTextFn callback.
    ///     Called by ImGui when it needs to read clipboard content.
    /// </summary>
    private static IntPtr GetClipboardTextCallback(IntPtr userData)
    {
        return ImGuiClipboardBridge.GetClipboardTextCallback(userData);
    }

    /// <summary>
    ///     ImGui SetClipboardTextFn callback.
    ///     Called by ImGui when it needs to write clipboard content.
    /// </summary>
    private static void SetClipboardTextCallback(IntPtr userData, IntPtr text)
    {
        ImGuiClipboardBridge.SetClipboardTextCallback(userData, text);
    }

    /// <summary>
    ///     Cleanup method to free resources.
    /// </summary>
    public static void Cleanup()
    {
        ImGuiClipboardBridge.Cleanup();
        _getClipboardDelegate = null;
        _setClipboardDelegate = null;
        _isConfigured = false;
    }
}

/// <summary>
///     Delegate for ImGui GetClipboardTextFn callback.
/// </summary>
[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
internal delegate IntPtr ImGuiGetClipboardTextCallback(IntPtr userData);

/// <summary>
///     Delegate for ImGui SetClipboardTextFn callback.
/// </summary>
[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
internal delegate void ImGuiSetClipboardTextCallback(IntPtr userData, IntPtr text);
