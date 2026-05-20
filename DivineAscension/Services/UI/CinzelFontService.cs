using System;
using System.Collections.Generic;
using System.IO;
using ImGuiNET;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace DivineAscension.Services.UI;

/// <summary>
///     Loads the Cinzel serif (Regular + Bold) into the ImGui font atlas at
///     the sizes we care about for codex chrome (title ribbon, chapter
///     headings, drop caps).
///
///     We do NOT go through VSImGui's <c>FontManager</c>. Its
///     <c>BeforeFontsLoaded</c> event fires synchronously inside VSImGui's
///     own <c>StartPre</c>, before any earlier-order mod can reasonably
///     touch <c>FontManager</c> (whose static cctor requires the ImGui
///     context, which VSImGui itself creates in that same <c>StartPre</c>).
///
///     Instead, run after VSImGui's <c>StartPre</c> has set up the context
///     but before the first frame bakes the font atlas into a GPU texture.
///     <see cref="LoadDirectly" /> is called from our <c>AssetsLoaded</c>,
///     which gives us:
///       • a valid ImGui context (VSImGui created it),
///       • legal asset reads (VS allows it from AssetsLoaded onward),
///       • the atlas not yet uploaded (first frame renders later).
///
///     Sizes are pre-baked up front. Renderers can fetch a face with
///     <see cref="GetRegular" /> / <see cref="GetBold" />; both return
///     <c>null</c> if the size wasn't pre-baked or the load failed.
/// </summary>
public static class CinzelFontService
{
    public const string RegularName = "cinzel-regular";
    public const string BoldName = "cinzel-bold";

    private const string RegularAssetPath = "textures/gui/codex/fonts/cinzel-regular.ttf";
    private const string BoldAssetPath = "textures/gui/codex/fonts/cinzel-bold.ttf";

    // Pre-bake these sizes: ribbon (~30-36), chapter heading (~24-30),
    // drop cap (60), small accent (18). Keep the set small — every entry
    // costs atlas space.
    private static readonly int[] SizesToLoad = { 18, 24, 30, 36, 48, 60 };

    private static readonly Dictionary<(string, int), ImFontPtr> _loaded = new();
    private static bool _loadedOnce;

    /// <summary>
    ///     Read the bundled Cinzel TTFs, write them to a stable cache path
    ///     so ImGui can mmap them, and register each size with the atlas.
    ///     Call from <c>AssetsLoaded</c> on the client side.
    /// </summary>
    public static void LoadDirectly(ICoreAPI api)
    {
        if (_loadedOnce) return;
        if (api.Side != EnumAppSide.Client) return;
        _loadedOnce = true;

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

        var io = ImGui.GetIO();
        var regularLoaded = AddSizes(api, io, regularPath, RegularName);
        var boldLoaded = AddSizes(api, io, boldPath, BoldName);

        api.Logger.Notification(
            $"[DivineAscension] Cinzel loaded into ImGui atlas: regular {regularLoaded}/{SizesToLoad.Length}, " +
            $"bold {boldLoaded}/{SizesToLoad.Length}, cache dir {cacheDir}");
    }

    /// <summary>
    ///     Look up a pre-baked Cinzel face. Returns <c>null</c> when the
    ///     size wasn't loaded (see <see cref="SizesToLoad" />) or when
    ///     <see cref="LoadDirectly" /> hasn't run yet.
    /// </summary>
    public static ImFontPtr? GetRegular(int size) => Lookup(RegularName, size);

    public static ImFontPtr? GetBold(int size) => Lookup(BoldName, size);

    private static ImFontPtr? Lookup(string fontName, int size)
    {
        return _loaded.TryGetValue((fontName, size), out var ptr) ? ptr : null;
    }

    private static unsafe int AddSizes(ICoreAPI api, ImGuiIOPtr io, string? path, string fontName)
    {
        if (path == null) return 0;
        var count = 0;
        foreach (var sz in SizesToLoad)
        {
            try
            {
                var ptr = io.Fonts.AddFontFromFileTTF(path, sz);
                if (ptr.NativePtr != null)
                {
                    _loaded[(fontName, sz)] = ptr;
                    count++;
                }
            }
            catch (Exception ex)
            {
                api.Logger.Error($"[DivineAscension] AddFontFromFileTTF failed for {fontName}@{sz}: {ex.Message}");
            }
        }
        return count;
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
