using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace PantheonWars.GUI.UI.Utilities;

/// <summary>
///     Manages loading and caching of civilization icon textures for ImGui rendering
///     Provides texture IDs for use with ImGui.Image() calls
/// </summary>
[ExcludeFromCodeCoverage]
internal static class CivilizationIconLoader
{
    private static readonly Dictionary<string, LoadedTexture?> _civilizationTextures = new();
    private static readonly Dictionary<string, IntPtr> _textureIds = new();
    private static bool _initialized;
    private static ICoreClientAPI? _api;

    /// <summary>
    ///     Initialize the civilization icon loader with the client API
    ///     This must be called before any textures can be loaded
    /// </summary>
    public static void Initialize(ICoreClientAPI api)
    {
        _api = api;
        _initialized = true;
    }

    /// <summary>
    ///     Load civilization icon texture from assets
    /// </summary>
    private static LoadedTexture? LoadCivilizationTexture(string iconName)
    {
        if (_api == null) return null;

        var normalizedIconName = iconName.ToLowerInvariant();
        var assetPath = new AssetLocation($"pantheonwars:textures/icons/civilizations/{normalizedIconName}.png");

        try
        {
            // Check if asset exists
            var asset = _api.Assets.TryGet(assetPath);
            if (asset == null)
            {
                _api.Logger.Warning($"[DivineAscension] Civilization icon not found: {assetPath}");

                // Try to load default icon as fallback
                if (normalizedIconName != "default")
                {
                    _api.Logger.Debug("[DivineAscension] Attempting to load default civilization icon as fallback");
                    return LoadCivilizationTexture("default");
                }

                return null;
            }

            // Load texture through Vintage Story's texture manager
            var textureId = _api.Render.GetOrLoadTexture(assetPath);
            if (textureId == 0)
            {
                _api.Logger.Warning($"[DivineAscension] Failed to load civilization texture: {assetPath}");

                // Try to load default icon as fallback
                if (normalizedIconName != "default") return LoadCivilizationTexture("default");

                return null;
            }

            var texture = new LoadedTexture(_api)
            {
                TextureId = textureId,
                Width = 48, // Civilization icons are 48x48
                Height = 48
            };

            _api.Logger.Debug(
                $"[DivineAscension] Loaded civilization icon: {normalizedIconName} (ID: {texture.TextureId})");
            return texture;
        }
        catch (Exception ex)
        {
            _api.Logger.Error(
                $"[DivineAscension] Error loading civilization texture {normalizedIconName}: {ex.Message}");

            // Try to load default icon as fallback
            if (normalizedIconName != "default") return LoadCivilizationTexture("default");

            return null;
        }
    }

    /// <summary>
    ///     Get the texture ID for a civilization icon (for use with ImGui.Image)
    ///     Returns IntPtr.Zero if texture couldn't be loaded
    /// </summary>
    public static IntPtr GetIconTextureId(string iconName)
    {
        if (!_initialized || _api == null) return IntPtr.Zero;

        // Use default icon if none specified
        if (string.IsNullOrWhiteSpace(iconName)) iconName = "default";

        var normalizedIconName = iconName.ToLowerInvariant();

        // Return cached texture ID if available
        if (_textureIds.TryGetValue(normalizedIconName, out var cachedId)) return cachedId;

        // Load texture if not already loaded
        if (!_civilizationTextures.ContainsKey(normalizedIconName))
        {
            var texture = LoadCivilizationTexture(normalizedIconName);
            _civilizationTextures[normalizedIconName] = texture;
        }

        var loadedTexture = _civilizationTextures[normalizedIconName];
        if (loadedTexture != null && loadedTexture.TextureId != 0)
        {
            var textureId = new IntPtr(loadedTexture.TextureId);
            _textureIds[normalizedIconName] = textureId;
            return textureId;
        }

        return IntPtr.Zero;
    }

    /// <summary>
    ///     Check if a civilization icon has a valid texture loaded
    /// </summary>
    public static bool HasTexture(string iconName)
    {
        return GetIconTextureId(iconName) != IntPtr.Zero;
    }

    /// <summary>
    ///     Get list of all available civilization icon names from assets
    /// </summary>
    public static List<string> GetAvailableIcons()
    {
        if (_api == null) return new List<string> { "default" };

        var icons = new List<string>();

        // Known civilization icon names to check for
        var knownIcons = new[]
        {
            "default", "congress", "byzantin-temple", "egyptian-temple", "granary", "huts-village",
            "indian-palace", "moai", "pagoda", "saint-basil-cathedral", "viking-church", "village",
            "scales", "yin-yang", "peace-dove", "freemasonry", "cursed-star"
        };

        // Check which icons actually exist in the assets
        foreach (var iconName in knownIcons)
        {
            var assetPath = new AssetLocation($"pantheonwars:textures/icons/civilizations/{iconName}.png");
            var asset = _api.Assets.TryGet(assetPath);

            if (asset != null) icons.Add(iconName);
        }

        // Ensure default is always first
        if (icons.Contains("default"))
        {
            icons.Remove("default");
            icons.Insert(0, "default");
        }

        return icons;
    }

    /// <summary>
    ///     Preload specific civilization icon texture (call during dialog initialization)
    /// </summary>
    public static void PreloadTexture(string iconName)
    {
        if (!_initialized) return;
        GetIconTextureId(iconName);
    }

    /// <summary>
    ///     Dispose all loaded textures (call when dialog is closed/disposed)
    /// </summary>
    public static void Dispose()
    {
        foreach (var texture in _civilizationTextures.Values) texture?.Dispose();

        _civilizationTextures.Clear();
        _textureIds.Clear();
        _initialized = false;
        _api = null;
    }
}