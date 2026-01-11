namespace DivineAscension.Config;

/// <summary>
///     Main configuration class for the Divine Ascension mod.
///     Settings are persisted to ModConfig/divineascension.json in the game's data directory.
/// </summary>
public class ModConfig
{
    /// <summary>
    ///     The configuration file name (without path).
    /// </summary>
    public const string ConfigFileName = "divineascension.json";

    /// <summary>
    ///     The keybind configuration for opening the Divine Ascension dialog.
    ///     Default: Shift+G
    /// </summary>
    public KeybindConfig DialogKeybind { get; set; } = new();

    /// <summary>
    ///     Creates a default configuration with standard keybinds.
    /// </summary>
    public static ModConfig CreateDefault()
    {
        return new ModConfig
        {
            DialogKeybind = new KeybindConfig
            {
                Key = "G",
                Shift = true,
                Ctrl = false,
                Alt = false
            }
        };
    }
}
