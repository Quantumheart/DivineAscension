using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

namespace DivineAscension.Services;

/// <summary>
///     Centralized localization service for Divine Ascension mod.
///     Supports both client-side (using Vintage Story's Lang API) and server-side (loading JSON directly) contexts.
///     Thread-safe singleton with caching for performance.
/// </summary>
public class LocalizationService
{
    private static readonly Lazy<LocalizationService> _instance = new(() => new LocalizationService());
    private readonly ConcurrentDictionary<string, string> _cache = new();

    private ICoreClientAPI? _capi;
    private bool _isInitialized;
    private ICoreServerAPI? _sapi;
    private Dictionary<string, string>? _serverTranslations;

    private LocalizationService()
    {
    }

    /// <summary>
    ///     Gets the singleton instance of the LocalizationService.
    /// </summary>
    public static LocalizationService Instance => _instance.Value;

    /// <summary>
    ///     Initialize the localization service for client-side usage.
    ///     Uses Vintage Story's Lang.Get() API for translations.
    /// </summary>
    /// <param name="capi">The client API instance</param>
    public void InitializeClient(ICoreClientAPI capi)
    {
        _capi = capi ?? throw new ArgumentNullException(nameof(capi));
        _isInitialized = true;
        capi.Logger.Notification("[DivineAscension Localization] Client-side localization initialized");
    }

    /// <summary>
    ///     Initialize the localization service for server-side usage.
    ///     Loads JSON language files directly since server API doesn't have Lang helper.
    /// </summary>
    /// <param name="sapi">The server API instance</param>
    public void InitializeServer(ICoreServerAPI sapi)
    {
        _sapi = sapi ?? throw new ArgumentNullException(nameof(sapi));
        LoadServerTranslations();
        _isInitialized = true;
        sapi.Logger.Notification("[DivineAscension Localization] Server-side localization initialized");
    }

    /// <summary>
    ///     Get a localized string by key.
    /// </summary>
    /// <param name="key">The localization key (e.g., "divineascension:ui.tab.religion")</param>
    /// <returns>The localized string, or the key itself if not found</returns>
    public string Get(string key)
    {
        if (!_isInitialized)
        {
            return key; // Fallback if not initialized
        }

        // Check cache first
        if (_cache.TryGetValue(key, out var cachedValue))
        {
            return cachedValue;
        }

        string result;

        if (_capi != null)
        {
            // Client-side: use Vintage Story's Lang.GetUnformatted() to avoid format exceptions
            // when the translation contains {0}, {1} placeholders but no args are provided
            try
            {
                result = Lang.GetUnformatted(key);
            }
            catch
            {
                // Fallback if GetUnformatted fails
                result = key;
            }

            // If it returns the key itself, the translation wasn't found
            if (result == key)
            {
                result = key; // Keep the key as fallback
            }
        }
        else if (_serverTranslations != null)
        {
            // Server-side or test mode: look up in loaded translations
            if (!_serverTranslations.TryGetValue(key, out result))
            {
                result = key; // Fallback to key
            }
        }
        else
        {
            result = key; // Fallback if neither is initialized
        }

        // Cache the result
        _cache.TryAdd(key, result);
        return result;
    }

    /// <summary>
    ///     Get a localized string with parameter substitution (string.Format style).
    /// </summary>
    /// <param name="key">The localization key</param>
    /// <param name="args">Arguments to substitute into the format string</param>
    /// <returns>The formatted localized string</returns>
    public string Get(string key, params object[] args)
    {
        if (!_isInitialized)
        {
            return key;
        }

        try
        {
            if (_capi != null)
            {
                // Client-side: use Vintage Story's Lang.Get with args for proper formatting
                return Lang.Get(key, args);
            }

            // Server-side or test mode: get template and format manually
            var template = Get(key);
            return string.Format(template, args);
        }
        catch (FormatException)
        {
            // If formatting fails, return the template with args appended
            var template = Get(key);
            return $"{template} [{string.Join(", ", args)}]";
        }
    }

    /// <summary>
    ///     Clear the translation cache. Useful when language changes or for testing.
    /// </summary>
    public void ClearCache()
    {
        _cache.Clear();
    }

    /// <summary>
    ///     Check if a localization key exists.
    /// </summary>
    /// <param name="key">The localization key to check</param>
    /// <returns>True if the key exists, false otherwise</returns>
    public bool HasKey(string key)
    {
        if (!_isInitialized)
        {
            return false;
        }

        if (_capi != null)
        {
            // For client, check if Lang.GetUnformatted returns something other than the key
            try
            {
                var result = Lang.GetUnformatted(key);
                return result != key;
            }
            catch
            {
                return false;
            }
        }

        if (_serverTranslations != null)
        {
            return _serverTranslations.ContainsKey(key);
        }

        return false;
    }

    /// <summary>
    ///     Load translations from JSON files for server-side use.
    ///     Loads English (en.json) as the base language.
    /// </summary>
    private void LoadServerTranslations()
    {
        if (_sapi == null)
        {
            return;
        }

        _serverTranslations = new Dictionary<string, string>();

        try
        {
            // Get the mod's asset directory
            var modAsset = _sapi.Assets.Get(new AssetLocation("divineascension", "lang/en.json"));

            if (modAsset == null)
            {
                _sapi.Logger.Warning("[DivineAscension Localization] Could not find en.json language file");
                return;
            }

            // Read and parse the JSON
            var json = Encoding.UTF8.GetString(modAsset.Data);

            var translations = JsonSerializer.Deserialize<Dictionary<string, string>>(json);

            if (translations != null)
            {
                _serverTranslations = translations;
                _sapi.Logger.Notification(
                    $"[DivineAscension Localization] Loaded {translations.Count} translations from en.json");
            }
        }
        catch (Exception ex)
        {
            _sapi.Logger.Error($"[DivineAscension Localization] Failed to load server translations: {ex.Message}");
        }
    }

    /// <summary>
    ///     Set the instance for testing purposes. Only use in unit tests.
    /// </summary>
    internal void SetInstanceForTesting(ICoreClientAPI? capi = null, ICoreServerAPI? sapi = null)
    {
        _capi = capi;
        _sapi = sapi;
        _cache.Clear();
        _serverTranslations = null;
        _isInitialized = capi != null || sapi != null;
    }

    /// <summary>
    ///     Initialize with a dictionary of translations for testing purposes. Only use in unit tests.
    /// </summary>
    internal void InitializeForTesting(Dictionary<string, string> translations)
    {
        _serverTranslations = translations;
        _cache.Clear();
        _isInitialized = true;
        _capi = null;
        _sapi = null;
    }
}