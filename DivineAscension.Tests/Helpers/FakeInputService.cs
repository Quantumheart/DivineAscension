using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using DivineAscension.API.Interfaces;
using Vintagestory.API.Client;

namespace DivineAscension.Tests.Helpers;

/// <summary>
/// Fake implementation of IInputService for testing.
/// Tracks registered hotkeys and allows tests to simulate key presses.
/// </summary>
[ExcludeFromCodeCoverage]
public class FakeInputService : IInputService
{
    private readonly Dictionary<string, HotKeyInfo> _registeredHotKeys = new();
    private readonly Dictionary<string, Func<KeyCombination, bool>> _handlers = new();

    /// <summary>
    /// Gets all registered hotkeys.
    /// </summary>
    public IReadOnlyDictionary<string, HotKeyInfo> RegisteredHotKeys => _registeredHotKeys;

    public void RegisterHotKey(
        string code,
        string description,
        GlKeys keyCode,
        HotkeyType type,
        bool shiftPressed = false,
        bool ctrlPressed = false,
        bool altPressed = false)
    {
        if (string.IsNullOrEmpty(code)) return;

        _registeredHotKeys[code] = new HotKeyInfo(
            code,
            description,
            keyCode,
            type,
            shiftPressed,
            ctrlPressed,
            altPressed);
    }

    public void SetHotKeyHandler(string code, Func<KeyCombination, bool> handler)
    {
        if (string.IsNullOrEmpty(code) || handler == null)
            return;

        _handlers[code] = handler;
    }

    public void UnregisterHotKey(string code)
    {
        if (string.IsNullOrEmpty(code)) return;

        _registeredHotKeys.Remove(code);
        _handlers.Remove(code);
    }

    /// <summary>
    /// Simulates a hotkey press for testing.
    /// </summary>
    /// <param name="code">The hotkey code to trigger</param>
    /// <param name="combination">The key combination (optional, will create default if null)</param>
    /// <returns>True if the handler consumed the key press, false otherwise</returns>
    public bool SimulateHotKeyPress(string code, KeyCombination? combination = null)
    {
        if (string.IsNullOrEmpty(code) || !_handlers.ContainsKey(code))
            return false;

        var keyComb = combination ?? new KeyCombination { KeyCode = (int)GlKeys.G };
        return _handlers[code].Invoke(keyComb);
    }

    /// <summary>
    /// Clears all registered hotkeys and handlers.
    /// </summary>
    public void Clear()
    {
        _registeredHotKeys.Clear();
        _handlers.Clear();
    }

    /// <summary>
    /// Information about a registered hotkey.
    /// </summary>
    public record HotKeyInfo(
        string Code,
        string Description,
        GlKeys KeyCode,
        HotkeyType Type,
        bool ShiftPressed,
        bool CtrlPressed,
        bool AltPressed);
}
