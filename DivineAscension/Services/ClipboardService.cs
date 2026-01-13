using System;
using System.Diagnostics;
using Vintagestory.API.Common;

namespace DivineAscension.Services;

/// <summary>
///     Clipboard backend type
/// </summary>
internal enum ClipboardBackend
{
    InMemory,
    Wayland,
    TextCopy
}

/// <summary>
///     Clipboard service for Divine Ascension mod.
///     Provides hybrid clipboard support with platform detection:
///     - Wayland: Uses wl-copy/wl-paste
///     - X11/Windows/macOS: Uses TextCopy library
///     - Fallback: In-memory clipboard
///     Thread-safe singleton pattern.
/// </summary>
public class ClipboardService
{
    private static readonly Lazy<ClipboardService> _instance = new(() => new ClipboardService());
    private static readonly object _clipboardLock = new();
    private static readonly object _initLock = new();

    private string _inMemoryClipboard = string.Empty;
    private ICoreAPI? _api;
    private bool _systemClipboardAvailable;
    private bool _isInitialized;
    private ClipboardBackend _backend = ClipboardBackend.InMemory;

    private ClipboardService()
    {
    }

    /// <summary>
    ///     Gets the singleton instance of the ClipboardService.
    /// </summary>
    public static ClipboardService Instance => _instance.Value;

    /// <summary>
    ///     Gets whether the system clipboard is available.
    /// </summary>
    public bool IsSystemClipboardAvailable => _systemClipboardAvailable;

    /// <summary>
    ///     Initialize the clipboard service.
    ///     Tests system clipboard availability and logs the mode (system/fallback).
    /// </summary>
    /// <param name="api">The API instance (client or server)</param>
    public void Initialize(ICoreAPI api)
    {
        if (_isInitialized)
        {
            return;
        }

        lock (_initLock)
        {
            if (_isInitialized) // Double-check after acquiring lock
            {
                return;
            }

            _api = api ?? throw new ArgumentNullException(nameof(api));
            DetectAndConfigureClipboard();
            _isInitialized = true;

            var mode = _backend switch
            {
                ClipboardBackend.Wayland => "system (Wayland wl-clipboard)",
                ClipboardBackend.TextCopy => "system (TextCopy)",
                _ => "in-memory fallback"
            };
            api.Logger.Notification($"[DivineAscension] Clipboard mode: {mode}");
        }
    }

    /// <summary>
    ///     Get text from the clipboard.
    ///     Attempts system clipboard first, falls back to in-memory clipboard.
    /// </summary>
    /// <returns>Clipboard text, or empty string if clipboard is empty</returns>
    public string GetText()
    {
        lock (_clipboardLock)
        {
            if (_systemClipboardAvailable)
            {
                try
                {
                    return _backend switch
                    {
                        ClipboardBackend.Wayland => GetTextWayland(),
                        ClipboardBackend.TextCopy => TextCopy.ClipboardService.GetText() ?? string.Empty,
                        _ => _inMemoryClipboard
                    };
                }
                catch (Exception ex)
                {
                    // System clipboard failed, fall through to in-memory
                    _api?.Logger.Debug($"[DivineAscension] System clipboard read failed: {ex.Message}");
                }
            }

            return _inMemoryClipboard;
        }
    }

    /// <summary>
    ///     Set text to the clipboard.
    ///     Attempts system clipboard, but always updates in-memory clipboard as fallback.
    /// </summary>
    /// <param name="text">The text to set in the clipboard</param>
    public void SetText(string text)
    {
        if (text == null)
        {
            text = string.Empty;
        }

        lock (_clipboardLock)
        {
            // Always update in-memory clipboard
            _inMemoryClipboard = text;

            // Also try system clipboard if available
            if (_systemClipboardAvailable)
            {
                try
                {
                    switch (_backend)
                    {
                        case ClipboardBackend.Wayland:
                            SetTextWayland(text);
                            break;
                        case ClipboardBackend.TextCopy:
                            TextCopy.ClipboardService.SetText(text);
                            break;
                    }
                }
                catch (Exception ex)
                {
                    // System clipboard failed, but in-memory is already updated
                    _api?.Logger.Debug($"[DivineAscension] System clipboard write failed: {ex.Message}");
                }
            }
        }
    }

    /// <summary>
    ///     Detect platform and configure appropriate clipboard backend.
    /// </summary>
    private void DetectAndConfigureClipboard()
    {
        // Try TextCopy first - it handles most platforms including Wayland
        // wl-paste can cause focus issues when called during ImGui input processing
        try
        {
            _ = TextCopy.ClipboardService.GetText();
            _backend = ClipboardBackend.TextCopy;
            _systemClipboardAvailable = true;
            return;
        }
        catch (Exception ex)
        {
            _api?.Logger.Debug($"[DivineAscension] TextCopy not available: {ex.Message}");
        }

        // Fallback to wl-paste on Wayland if TextCopy failed
        var waylandDisplay = Environment.GetEnvironmentVariable("WAYLAND_DISPLAY");
        if (!string.IsNullOrEmpty(waylandDisplay))
        {
            if (TestWaylandClipboard())
            {
                _backend = ClipboardBackend.Wayland;
                _systemClipboardAvailable = true;
                return;
            }
        }

        // Last resort: in-memory fallback
        _backend = ClipboardBackend.InMemory;
        _systemClipboardAvailable = false;
        _api?.Logger.Debug("[DivineAscension] System clipboard not available, using in-memory fallback");
    }

    /// <summary>
    ///     Test if Wayland clipboard (wl-paste) is available.
    /// </summary>
    private bool TestWaylandClipboard()
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "wl-paste",
                Arguments = "--no-newline",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            if (process == null) return false;

            process.WaitForExit(1000); // 1 second timeout
            return process.ExitCode == 0 || process.ExitCode == 1; // 0 = success, 1 = empty clipboard
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    ///     Get clipboard text using Wayland wl-paste.
    /// </summary>
    private string GetTextWayland()
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "wl-paste",
                Arguments = "--no-newline",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            if (process == null) return string.Empty;

            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit(1000);

            return process.ExitCode == 0 ? output : string.Empty;
        }
        catch (Exception ex)
        {
            _api?.Logger.Debug($"[DivineAscension] wl-paste failed: {ex.Message}");
            return string.Empty;
        }
    }

    /// <summary>
    ///     Set clipboard text using Wayland wl-copy.
    /// </summary>
    private void SetTextWayland(string text)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "wl-copy",
                RedirectStandardInput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            if (process == null) return;

            process.StandardInput.Write(text);
            process.StandardInput.Close();
            process.WaitForExit(1000);
        }
        catch (Exception ex)
        {
            _api?.Logger.Debug($"[DivineAscension] wl-copy failed: {ex.Message}");
        }
    }

    /// <summary>
    ///     Reset the service for testing purposes. Only use in unit tests.
    /// </summary>
    internal void ResetForTesting()
    {
        lock (_initLock)
        {
            _inMemoryClipboard = string.Empty;
            _isInitialized = false;
            _systemClipboardAvailable = false;
            _backend = ClipboardBackend.InMemory;
            _api = null;
        }
    }
}
