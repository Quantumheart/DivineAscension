using System;
using DivineAscension.Config;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace DivineAscension.Services;

/// <summary>
///     Service for loading and managing mod configuration.
///     Handles keybind configuration persistence using Vintage Story's ModConfig API.
/// </summary>
public class ModConfigService
{
    private readonly ICoreAPI _api;
    private ModConfig _config;

    /// <summary>
    ///     Initializes the ModConfigService with the given API.
    /// </summary>
    /// <param name="api">The Vintage Story API (client or server)</param>
    public ModConfigService(ICoreAPI api)
    {
        _api = api ?? throw new ArgumentNullException(nameof(api));
        _config = ModConfig.CreateDefault();
    }

    /// <summary>
    ///     Gets the current mod configuration.
    /// </summary>
    public ModConfig Config => _config;

    /// <summary>
    ///     Loads the configuration from disk, creating a default if it doesn't exist.
    /// </summary>
    public void LoadConfig()
    {
        try
        {
            var loadedConfig = _api.LoadModConfig<ModConfig>(ModConfig.ConfigFileName);

            if (loadedConfig != null)
            {
                _config = loadedConfig;
                _api.Logger.Notification(
                    $"[DivineAscension] Loaded configuration. Dialog keybind: {_config.DialogKeybind.GetDisplayString()}");
            }
            else
            {
                // Config doesn't exist, create default and save it
                _config = ModConfig.CreateDefault();
                SaveConfig();
                _api.Logger.Notification(
                    $"[DivineAscension] Created default configuration. Dialog keybind: {_config.DialogKeybind.GetDisplayString()}");
            }

            // Validate the keybind configuration
            ValidateKeybindConfig();
        }
        catch (Exception ex)
        {
            _api.Logger.Error($"[DivineAscension] Error loading configuration: {ex.Message}");
            _config = ModConfig.CreateDefault();
        }
    }

    /// <summary>
    ///     Saves the current configuration to disk.
    /// </summary>
    public void SaveConfig()
    {
        try
        {
            _api.StoreModConfig(_config, ModConfig.ConfigFileName);
        }
        catch (Exception ex)
        {
            _api.Logger.Error($"[DivineAscension] Error saving configuration: {ex.Message}");
        }
    }

    /// <summary>
    ///     Parses the configured key string to a GlKeys enum value.
    /// </summary>
    /// <param name="keybind">The keybind configuration to parse</param>
    /// <returns>The GlKeys value, or GlKeys.G as default if parsing fails</returns>
    public GlKeys ParseKey(KeybindConfig keybind)
    {
        if (string.IsNullOrWhiteSpace(keybind.Key))
        {
            _api.Logger.Warning("[DivineAscension] Keybind key is empty, using default 'G'");
            return GlKeys.G;
        }

        // Try to parse the key name to GlKeys enum
        if (Enum.TryParse<GlKeys>(keybind.Key, ignoreCase: true, out var glKey))
        {
            return glKey;
        }

        _api.Logger.Warning(
            $"[DivineAscension] Invalid key '{keybind.Key}' in configuration, using default 'G'. " +
            "Valid keys include: A-Z, F1-F12, Num0-Num9, etc.");
        return GlKeys.G;
    }

    /// <summary>
    ///     Validates the keybind configuration and logs warnings for invalid settings.
    /// </summary>
    private void ValidateKeybindConfig()
    {
        var keybind = _config.DialogKeybind;

        // Try parsing the key to validate it
        var key = ParseKey(keybind);

        // Check if no modifiers are set for commonly used keys
        if (!keybind.Shift && !keybind.Ctrl && !keybind.Alt)
        {
            // Warn about potential conflicts with vanilla keybinds
            if (key is >= GlKeys.A and <= GlKeys.Z)
            {
                _api.Logger.Warning(
                    $"[DivineAscension] Keybind '{keybind.Key}' has no modifiers (Shift/Ctrl/Alt). " +
                    "This may conflict with typing in chat or other game functions. " +
                    "Consider adding a modifier key in the config file.");
            }
        }
    }
}
