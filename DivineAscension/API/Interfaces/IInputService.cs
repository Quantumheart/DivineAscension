using System;
using Vintagestory.API.Client;

namespace DivineAscension.API.Interfaces;

/// <summary>
/// Service for handling input and hotkey registration (client-side only).
/// Wraps IInputAPI for testability.
/// </summary>
public interface IInputService
{
    /// <summary>
    /// Registers a hotkey with the game's input system.
    /// </summary>
    /// <param name="code">Unique identifier for the hotkey</param>
    /// <param name="description">Human-readable description of what the hotkey does</param>
    /// <param name="keyCode">The key code (e.g., GlKeys.G)</param>
    /// <param name="type">The type of hotkey (GUI, gameplay, etc.)</param>
    /// <param name="shiftPressed">Whether Shift must be pressed</param>
    /// <param name="ctrlPressed">Whether Ctrl must be pressed</param>
    /// <param name="altPressed">Whether Alt must be pressed</param>
    void RegisterHotKey(
        string code,
        string description,
        GlKeys keyCode,
        HotkeyType type,
        bool shiftPressed = false,
        bool ctrlPressed = false,
        bool altPressed = false);

    /// <summary>
    /// Sets the handler callback for a registered hotkey.
    /// </summary>
    /// <param name="code">The hotkey code to attach the handler to</param>
    /// <param name="handler">The callback function to invoke when the hotkey is pressed (returns true if consumed)</param>
    void SetHotKeyHandler(string code, Func<KeyCombination, bool> handler);

    /// <summary>
    /// Unregisters a hotkey.
    /// </summary>
    /// <param name="code">The hotkey code to unregister</param>
    void UnregisterHotKey(string code);
}
