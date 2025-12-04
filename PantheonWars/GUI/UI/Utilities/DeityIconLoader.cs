using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using PantheonWars.Models.Enum;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace PantheonWars.GUI.UI.Utilities;

/// <summary>
///     Manages loading and caching of deity icon textures for ImGui rendering
///     Provides texture IDs for use with ImGui.Image() calls
/// </summary>
[ExcludeFromCodeCoverage]
internal static class DeityIconLoader
{
    private static readonly Dictionary<DeityType, LoadedTexture?> _deityTextures = new();
    private static readonly Dictionary<DeityType, IntPtr> _textureIds = new();
    private static bool _initialized;
    private static ICoreClientAPI? _api;

    /// <summary>
    ///     Initialize the deity icon loader with the client API
    ///     This must be called before any textures can be loaded
    /// </summary>
    public static void Initialize(ICoreClientAPI api)
    {
        _api = api;
        _initialized = true;
    }

    /// <summary>
    ///     Load deity icon texture from assets
    /// </summary>
    private static LoadedTexture? LoadDeityTexture(DeityType deity)
    {
        if (_api == null) return null;

        var deityName = deity.ToString().ToLowerInvariant();
        var assetPath = new AssetLocation($"pantheonwars:textures/icons/deities/{deityName}.png");

        try
        {
            // Check if asset exists
            var asset = _api.Assets.TryGet(assetPath);
            if (asset == null)
            {
                _api.Logger.Warning($"[PantheonWars] Deity icon not found: {assetPath}");
                return null;
            }

            // Load texture through Vintage Story's texture manager
            var textureId = _api.Render.GetOrLoadTexture(assetPath);
            if (textureId == 0)
            {
                _api.Logger.Warning($"[PantheonWars] Failed to load deity texture: {assetPath}");
                return null;
            }

            var texture = new LoadedTexture(_api)
            {
                TextureId = textureId,
                Width = 32, // Deity icons are 32x32
                Height = 32
            };

            _api.Logger.Debug($"[PantheonWars] Loaded deity icon: {deityName} (ID: {texture.TextureId})");
            return texture;
        }
        catch (Exception ex)
        {
            _api.Logger.Error($"[PantheonWars] Error loading deity texture {deityName}: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    ///     Get the texture ID for a deity icon (for use with ImGui.Image)
    ///     Returns IntPtr.Zero if texture couldn't be loaded
    /// </summary>
    public static IntPtr GetDeityTextureId(DeityType deity)
    {
        if (!_initialized || _api == null) return IntPtr.Zero;

        // Return cached texture ID if available
        if (_textureIds.TryGetValue(deity, out var cachedId)) return cachedId;

        // Load texture if not already loaded
        if (!_deityTextures.ContainsKey(deity))
        {
            var texture = LoadDeityTexture(deity);
            _deityTextures[deity] = texture;
        }

        var loadedTexture = _deityTextures[deity];
        if (loadedTexture != null && loadedTexture.TextureId != 0)
        {
            var textureId = new IntPtr(loadedTexture.TextureId);
            _textureIds[deity] = textureId;
            return textureId;
        }

        return IntPtr.Zero;
    }

    /// <summary>
    ///     Check if a deity has a valid texture loaded
    /// </summary>
    public static bool HasTexture(DeityType deity)
    {
        return GetDeityTextureId(deity) != IntPtr.Zero;
    }

    /// <summary>
    ///     Preload all deity textures (optional - call during dialog initialization)
    /// </summary>
    public static void PreloadAllTextures()
    {
        if (!_initialized) return;

        foreach (DeityType deity in Enum.GetValues(typeof(DeityType)))
        {
            if (deity == DeityType.None) continue;
            GetDeityTextureId(deity);
        }
    }

    /// <summary>
    ///     Dispose all loaded textures (call when dialog is closed/disposed)
    /// </summary>
    public static void Dispose()
    {
        foreach (var texture in _deityTextures.Values) texture?.Dispose();

        _deityTextures.Clear();
        _textureIds.Clear();
        _initialized = false;
        _api = null;
    }
}