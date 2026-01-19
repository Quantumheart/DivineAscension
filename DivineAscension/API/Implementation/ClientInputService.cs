using System;
using DivineAscension.API.Interfaces;
using Vintagestory.API.Client;

namespace DivineAscension.API.Implementation;

/// <summary>
/// Client-side implementation of IInputService that wraps IInputAPI.
/// Provides thin abstraction over Vintage Story's input handling for improved testability.
/// </summary>
public class ClientInputService : IInputService
{
    private readonly IInputAPI _inputApi;

    /// <summary>
    /// Constructor for dependency injection
    /// </summary>
    /// <param name="inputApi">The client input API</param>
    public ClientInputService(IInputAPI inputApi)
    {
        _inputApi = inputApi ?? throw new ArgumentNullException(nameof(inputApi));
    }

    public void RegisterHotKey(
        string code,
        string description,
        GlKeys keyCode,
        HotkeyType type,
        bool shiftPressed = false,
        bool ctrlPressed = false,
        bool altPressed = false)
    {
        if (string.IsNullOrEmpty(code))
            throw new ArgumentException("Hotkey code cannot be null or empty", nameof(code));

        _inputApi.RegisterHotKey(code, description, keyCode, type, shiftPressed, ctrlPressed, altPressed);
    }

    public void SetHotKeyHandler(string code, Func<KeyCombination, bool> handler)
    {
        if (string.IsNullOrEmpty(code))
            throw new ArgumentException("Hotkey code cannot be null or empty", nameof(code));

        if (handler == null)
            throw new ArgumentNullException(nameof(handler));

        _inputApi.SetHotKeyHandler(code, (KeyCombination keyComb) => handler(keyComb));
    }

    public void UnregisterHotKey(string code)
    {
        if (string.IsNullOrEmpty(code))
            throw new ArgumentException("Hotkey code cannot be null or empty", nameof(code));

        // Note: IInputAPI doesn't have an UnregisterHotKey method in the current VS API
        // This is a placeholder for future API support or workaround
        // For now, we just ensure the code is valid
    }
}
