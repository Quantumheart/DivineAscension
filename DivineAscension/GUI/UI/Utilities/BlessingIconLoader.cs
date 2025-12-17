using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using PantheonWars.Models;
using PantheonWars.Models.Enum;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace PantheonWars.GUI.UI.Utilities;

/// <summary>
///     Manages loading and caching of blessing icon textures for ImGui rendering
///     Provides texture IDs for use with ImGui drawList.AddImage() calls
///     Icons are organized by deity: assets/pantheonwars/textures/icons/perks/{deity}/{iconName}.png
/// </summary>
[ExcludeFromCodeCoverage]
internal static class BlessingIconLoader
{
    // Cache textures by full key: "{deity}_{iconName}"
    private static readonly Dictionary<string, LoadedTexture?> _blessingTextures = new();
    private static readonly Dictionary<string, IntPtr> _textureIds = new();
    private static bool _initialized;
    private static ICoreClientAPI? _api;

    /// <summary>
    ///     Initialize the blessing icon loader with the client API
    ///     This must be called before any textures can be loaded
    /// </summary>
    public static void Initialize(ICoreClientAPI api)
    {
        _api = api;
        _initialized = true;
    }

    /// <summary>
    ///     Load blessing icon texture from assets
    /// </summary>
    private static LoadedTexture? LoadBlessingTexture(DeityType deity, string iconName)
    {
        if (_api == null) return null;

        var deityName = deity.ToString().ToLowerInvariant();
        var normalizedIconName = iconName.ToLowerInvariant();
        var assetPath = new AssetLocation($"pantheonwars:textures/icons/perks/{deityName}/{normalizedIconName}.png");

        try
        {
            // Check if asset exists
            var asset = _api.Assets.TryGet(assetPath);
            if (asset == null)
            {
                _api.Logger.Warning($"[DivineAscension] Blessing icon not found: {assetPath}");
                return null;
            }

            // Load texture through Vintage Story's texture manager
            var textureId = _api.Render.GetOrLoadTexture(assetPath);
            if (textureId == 0)
            {
                _api.Logger.Warning($"[DivineAscension] Failed to load blessing texture: {assetPath}");
                return null;
            }

            var texture = new LoadedTexture(_api)
            {
                TextureId = textureId,
                Width = 48, // Blessing icons rendered at 48x48
                Height = 48
            };

            _api.Logger.Debug(
                $"[DivineAscension] Loaded blessing icon: {deityName}/{normalizedIconName} (ID: {texture.TextureId})");
            return texture;
        }
        catch (Exception ex)
        {
            _api.Logger.Error(
                $"[DivineAscension] Error loading blessing texture {deityName}/{normalizedIconName}: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    ///     Get the texture ID for a blessing icon (for use with ImGui drawList.AddImage)
    ///     Returns IntPtr.Zero if texture couldn't be loaded
    /// </summary>
    public static IntPtr GetBlessingTextureId(Blessing? blessing)
    {
        if (!_initialized) return IntPtr.Zero;

        if (_api == null) return IntPtr.Zero;

        if (blessing == null) return IntPtr.Zero;

        if (string.IsNullOrWhiteSpace(blessing.IconName)) return IntPtr.Zero;

        return GetBlessingTextureId(blessing.Deity, blessing.IconName);
    }

    /// <summary>
    ///     Get the texture ID for a blessing icon by deity and icon name
    ///     Returns IntPtr.Zero if texture couldn't be loaded
    /// </summary>
    public static IntPtr GetBlessingTextureId(DeityType deity, string iconName)
    {
        if (!_initialized || _api == null) return IntPtr.Zero;
        if (string.IsNullOrWhiteSpace(iconName)) return IntPtr.Zero;

        // Create cache key
        var deityName = deity.ToString().ToLowerInvariant();
        var normalizedIconName = iconName.ToLowerInvariant();
        var cacheKey = $"{deityName}_{normalizedIconName}";

        // Return cached texture ID if available
        if (_textureIds.TryGetValue(cacheKey, out var cachedId)) return cachedId;

        // Load texture if not already loaded
        if (!_blessingTextures.ContainsKey(cacheKey))
        {
            var texture = LoadBlessingTexture(deity, normalizedIconName);
            _blessingTextures[cacheKey] = texture;
        }

        var loadedTexture = _blessingTextures[cacheKey];
        if (loadedTexture != null && loadedTexture.TextureId != 0)
        {
            var textureId = new IntPtr(loadedTexture.TextureId);
            _textureIds[cacheKey] = textureId;
            return textureId;
        }

        return IntPtr.Zero;
    }

    /// <summary>
    ///     Check if a blessing has a valid texture loaded
    /// </summary>
    public static bool HasTexture(Blessing blessing)
    {
        return GetBlessingTextureId(blessing) != IntPtr.Zero;
    }

    /// <summary>
    ///     Preload blessing texture (call during initialization for better performance)
    /// </summary>
    public static void PreloadTexture(Blessing blessing)
    {
        if (!_initialized || blessing == null) return;
        GetBlessingTextureId(blessing);
    }

    /// <summary>
    ///     Preload all blessing textures for a specific deity
    /// </summary>
    public static void PreloadDeityTextures(List<Blessing> blessings, DeityType deity)
    {
        if (!_initialized || blessings == null) return;

        foreach (var blessing in blessings)
            if (blessing.Deity == deity)
                GetBlessingTextureId(blessing);
    }

    /// <summary>
    ///     Dispose all loaded textures (call when dialog is closed/disposed)
    /// </summary>
    public static void Dispose()
    {
        foreach (var texture in _blessingTextures.Values)
            texture?.Dispose();

        _blessingTextures.Clear();
        _textureIds.Clear();
        _initialized = false;
        _api = null;
    }
}