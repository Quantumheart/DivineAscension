using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace DivineAscension.GUI.UI.Utilities;

/// <summary>
///     Loads the single parchment background tile applied to the content pane.
///     Separate from <see cref="GuiIconLoader" /> because that helper hardcodes
///     the <c>textures/icons/</c> prefix.
/// </summary>
public static class ParchmentTexture
{
    private const string AssetPath = "divineascension:textures/gui/codex/parchment.png";

    private static ICoreClientAPI? _api;
    private static LoadedTexture? _texture;
    private static IntPtr _textureId = IntPtr.Zero;
    private static bool _attempted;

    public static void Initialize(ICoreClientAPI api)
    {
        _api = api;
    }

    public static IntPtr GetTextureId()
    {
        if (_textureId != IntPtr.Zero) return _textureId;
        if (_attempted || _api == null) return IntPtr.Zero;
        _attempted = true;

        try
        {
            var location = new AssetLocation(AssetPath);
            var id = _api.Render.GetOrLoadTexture(location);
            if (id == 0)
            {
                GuiDialog.Logger?.Warning($"[DivineAscension] Failed to load parchment texture: {location}");
                return IntPtr.Zero;
            }

            _texture = new LoadedTexture(_api)
            {
                TextureId = id,
                Width = 512,
                Height = 512
            };
            _textureId = new IntPtr(id);
            return _textureId;
        }
        catch (Exception ex)
        {
            GuiDialog.Logger?.Error($"[DivineAscension] Error loading parchment texture: {ex.Message}");
            return IntPtr.Zero;
        }
    }

    public static void Dispose()
    {
        _texture?.Dispose();
        _texture = null;
        _textureId = IntPtr.Zero;
        _attempted = false;
        _api = null;
    }
}
