using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using ImGuiNET;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using VSImGui.API;

namespace DivineAscension.Services.UI;

/// <summary>
///     Registers the Cinzel serif (Regular + Bold) with VSImGui's
///     <see cref="FontManager" /> so chrome renderers can push it for title
///     ribbons, chapter headings, and drop caps.
///
///     VSImGui only accepts disk file paths for fonts. The font ships inside
///     the mod's asset zip, so on first call we extract the bytes to a known
///     cache directory and register that path via
///     <see cref="FontManager.BeforeFontsLoaded" />.
/// </summary>
public static class CinzelFontService
{
    public const string RegularName = "cinzel-regular";
    public const string BoldName = "cinzel-bold";

    private const string RegularAssetPath = "textures/gui/codex/fonts/cinzel-regular.ttf";
    private const string BoldAssetPath = "textures/gui/codex/fonts/cinzel-bold.ttf";

    private static bool _registered;
    private static Dictionary<(string, int), ImFontPtr>? _loadedCache;

    /// <summary>
    ///     Hook into VSImGui's font load. Idempotent. Must be called before
    ///     VSImGui builds its font atlas (AssetsLoaded), so wire from
    ///     <c>Start(api)</c>, not <c>StartClientSide</c>.
    /// </summary>
    public static void Register(ICoreAPI api)
    {
        if (_registered) return;
        if (api.Side != EnumAppSide.Client) return;
        api.Logger.Notification("[DivineAscension] CinzelFontService.Register: subscribing to BeforeFontsLoaded");

        // VS forbids asset reads until AssetsLoaded, but VSImGui fires
        // BeforeFontsLoaded during its own AssetsLoaded — so do the
        // extraction lazily inside the callback, where asset access is
        // guaranteed to be legal.
        FontManager.BeforeFontsLoaded += (fonts, _) =>
        {
            var cacheDir = Path.Combine(GamePaths.Cache, "divineascension", "fonts");
            try
            {
                Directory.CreateDirectory(cacheDir);
            }
            catch (Exception ex)
            {
                api.Logger.Error($"[DivineAscension] Could not create font cache dir '{cacheDir}': {ex.Message}");
                return;
            }

            var regularPath = ExtractFontToCache(api, RegularAssetPath, cacheDir, $"{RegularName}.ttf");
            var boldPath = ExtractFontToCache(api, BoldAssetPath, cacheDir, $"{BoldName}.ttf");

            if (regularPath != null) fonts.Add(regularPath);
            if (boldPath != null) fonts.Add(boldPath);

            var registered = (regularPath != null ? 1 : 0) + (boldPath != null ? 1 : 0);
            api.Logger.Notification(
                $"[DivineAscension] Registered Cinzel with VSImGui: {registered}/2 faces, cache dir {cacheDir}");
        };

        _registered = true;
    }

    /// <summary>
    ///     Look up a loaded Cinzel face at the requested size. VSImGui only
    ///     generates fonts at the sizes registered in
    ///     <c>FontManager.Sizes</c> (defaults: 6, 8, 10, 14, 18, 24, 30, 36,
    ///     48, 60); ask for one of those. Returns <c>null</c> when the font
    ///     isn't loaded yet so callers can fall back to the default.
    /// </summary>
    public static ImFontPtr? GetRegular(int size) => Lookup(RegularName, size);

    public static ImFontPtr? GetBold(int size) => Lookup(BoldName, size);

    private static ImFontPtr? Lookup(string fontName, int size)
    {
        var dict = _loadedCache ??= ResolveLoadedDictionary();
        return dict != null && dict.TryGetValue((fontName, size), out var ptr) ? ptr : null;
    }

    // FontManager.Loaded is internal in VSImGui 0.0.6; reach it once via
    // reflection and cache the reference. Tolerant of VSImGui exposing a
    // public accessor later.
    private static Dictionary<(string, int), ImFontPtr>? ResolveLoadedDictionary()
    {
        var prop = typeof(FontManager).GetProperty("Loaded",
            BindingFlags.NonPublic | BindingFlags.Static);
        return prop?.GetValue(null) as Dictionary<(string, int), ImFontPtr>;
    }

    private static string? ExtractFontToCache(ICoreAPI api, string assetPath, string cacheDir,
        string outFileName)
    {
        try
        {
            var asset = api.Assets.TryGet(new AssetLocation("divineascension", assetPath));
            if (asset == null)
            {
                api.Logger.Error($"[DivineAscension] Cinzel font asset missing: {assetPath}");
                return null;
            }

            var outPath = Path.Combine(cacheDir, outFileName);
            File.WriteAllBytes(outPath, asset.Data);
            return outPath;
        }
        catch (Exception ex)
        {
            api.Logger.Error($"[DivineAscension] Failed to extract '{assetPath}': {ex.Message}");
            return null;
        }
    }
}
