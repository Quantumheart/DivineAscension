using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
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
    ///     Hook into VSImGui's font load. Idempotent. Must be called from
    ///     our <c>StartPre</c> with <c>ExecuteOrder() = -1.0</c> — VSImGui's
    ///     own <c>StartPre</c> calls <c>TryOpen()</c> on its dialog, which
    ///     triggers <c>FontManager.Load()</c> and fires
    ///     <see cref="FontManager.BeforeFontsLoaded" /> synchronously. We
    ///     must subscribe before that runs.
    ///
    ///     Touching <see cref="FontManager" /> normally throws
    ///     <c>DllNotFoundException</c> when called before VSImGui's natives
    ///     are loaded, so we pre-load <c>cimgui</c> ourselves from VSImGui's
    ///     mod folder.
    /// </summary>
    public static void Register(ICoreAPI api)
    {
        if (_registered) return;
        if (api.Side != EnumAppSide.Client) return;
        api.Logger.Notification("[DivineAscension] CinzelFontService.Register: pre-loading cimgui and subscribing to BeforeFontsLoaded");

        if (!PreloadCimgui(api))
        {
            api.Logger.Error("[DivineAscension] Could not pre-load cimgui — Cinzel registration skipped");
            return;
        }

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

    /// <summary>
    ///     Load <c>cimgui</c> from VSImGui's mod folder so subsequent calls
    ///     to <see cref="FontManager" /> don't blow up the static cctor.
    ///     Idempotent — re-loading an already-loaded library is a no-op
    ///     for both Windows and POSIX dlopen.
    /// </summary>
    private static bool PreloadCimgui(ICoreAPI api)
    {
        try
        {
            var vsImGuiMod = api.ModLoader.GetMod("vsimgui");
            if (vsImGuiMod == null)
            {
                api.Logger.Error("[DivineAscension] vsimgui mod not found via ModLoader");
                return false;
            }

            // FolderPath lives on the internal ModContainer subclass; reach
            // it by name so we don't take a hard dep on the internal type.
            var folderPath = vsImGuiMod.GetType()
                .GetProperty("FolderPath", BindingFlags.Public | BindingFlags.Instance)
                ?.GetValue(vsImGuiMod) as string
                ?? vsImGuiMod.GetType()
                .GetProperty("SourcePath", BindingFlags.Public | BindingFlags.Instance)
                ?.GetValue(vsImGuiMod) as string;

            if (string.IsNullOrEmpty(folderPath))
            {
                api.Logger.Error("[DivineAscension] Could not resolve VSImGui mod folder path");
                return false;
            }

            string subdir, ext;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) { subdir = "win"; ext = ".dll"; }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) { subdir = "mac"; ext = ".dylib"; }
            else { subdir = "linux"; ext = ".so"; }

            var cimguiPath = Path.Combine(folderPath, "native", subdir, $"cimgui{ext}");
            if (!File.Exists(cimguiPath))
            {
                api.Logger.Error($"[DivineAscension] cimgui native not found at expected path: {cimguiPath}");
                return false;
            }

            NativeLibrary.Load(cimguiPath);
            api.Logger.Notification($"[DivineAscension] Pre-loaded cimgui from {cimguiPath}");
            return true;
        }
        catch (Exception ex)
        {
            api.Logger.Error($"[DivineAscension] PreloadCimgui failed: {ex}");
            return false;
        }
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
