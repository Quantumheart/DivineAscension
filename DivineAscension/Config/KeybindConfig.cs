using System.Collections.Generic;

namespace DivineAscension.Config;

/// <summary>
///     Configuration for a single keybind.
///     Allows users to customize keyboard shortcuts via the mod config file.
/// </summary>
public class KeybindConfig
{
    /// <summary>
    ///     The key to bind. This should be a valid GlKeys enum value name (e.g., "G", "B", "F1", "Num1").
    ///     See https://docs.gl/keys for a reference of available keys.
    /// </summary>
    public string Key { get; set; } = "G";

    /// <summary>
    ///     Whether the Shift modifier key must be held.
    /// </summary>
    public bool Shift { get; set; } = true;

    /// <summary>
    ///     Whether the Ctrl modifier key must be held.
    /// </summary>
    public bool Ctrl { get; set; }

    /// <summary>
    ///     Whether the Alt modifier key must be held.
    /// </summary>
    public bool Alt { get; set; }

    /// <summary>
    ///     Gets a human-readable description of this keybind (e.g., "Shift+G", "Ctrl+Alt+B").
    /// </summary>
    public string GetDisplayString()
    {
        var parts = new List<string>();

        if (Ctrl) parts.Add("Ctrl");
        if (Alt) parts.Add("Alt");
        if (Shift) parts.Add("Shift");
        parts.Add(Key);

        return string.Join("+", parts);
    }
}
