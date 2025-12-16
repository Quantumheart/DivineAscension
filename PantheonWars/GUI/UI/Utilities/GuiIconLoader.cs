using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace PantheonWars.GUI.UI.Utilities;

public static class GuiIconLoader
{
    private static readonly Dictionary<string, LoadedTexture?> _dictionary = new();
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
    ///     Load deity icon texture from assets
    /// </summary>
    private static LoadedTexture? LoadTexture(string directory, string name)
    {
        if (_api == null) return null;

        var iconName = name.ToLowerInvariant();
        var assetPath = new AssetLocation($"pantheonwars:textures/icons/{directory}/{iconName}.png");

        try
        {
            // Check if asset exists
            var asset = _api.Assets.TryGet(assetPath);
            if (asset == null)
            {
                _api.Logger.Warning($"[PantheonWars] Icon not found: {assetPath}");
                return null;
            }

            // Load texture through Vintage Story's texture manager
            var textureId = _api.Render.GetOrLoadTexture(assetPath);
            if (textureId == 0)
            {
                _api.Logger.Warning($"[PantheonWars] Failed to load icon texture: {assetPath}");
                return null;
            }

            var texture = new LoadedTexture(_api)
            {
                TextureId = textureId,
                Width = 32,
                Height = 32
            };

            _api.Logger.Debug($"[PantheonWars] Loaded GUI icon: {iconName} (ID: {texture.TextureId})");
            return texture;
        }
        catch (Exception ex)
        {
            _api.Logger.Error($"[PantheonWars] Error loading GUI texture {iconName}: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    ///     Get the texture ID for a deity icon (for use with ImGui.Image)
    ///     Returns IntPtr.Zero if texture couldn't be loaded
    /// </summary>
    public static IntPtr GetTextureId(string directoryName, string iconName)
    {
        if (!_initialized || _api == null) return IntPtr.Zero;

        // Return cached texture ID if available
        if (_textureIds.TryGetValue(iconName, out var cachedId)) return cachedId;

        // Load texture if not already loaded
        if (!_dictionary.ContainsKey(iconName))
        {
            var texture = LoadTexture(directoryName, iconName);
            _dictionary[iconName] = texture;
        }

        var loadedTexture = _dictionary[iconName];
        if (loadedTexture != null && loadedTexture.TextureId != 0)
        {
            var textureId = new IntPtr(loadedTexture.TextureId);
            _textureIds[iconName] = textureId;
            return textureId;
        }

        return IntPtr.Zero;
    }
}