using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using HarmonyLib;
using ImGuiNET;
using JetBrains.Annotations;
using Vintagestory.API.Common;

namespace DivineAscension.Services.UI;

// VSImGui 1.2.5's FontManager static cctor calls ImGui.GetIO(), which requires
// cimgui's native library to be loadable. cimgui is only made loadable by
// VSImGui's own StartPre via NativesLoader.Load, which runs inside its
// ImGuiModSystem.StartPre right before it constructs Controller (which then
// triggers FontManager.Load, firing BeforeFontsLoaded).
//
// That means any reference to FontManager before VSImGui's StartPre poisons
// the type with a TypeInitializationException. We can't subscribe to
// BeforeFontsLoaded directly from a lower ExecuteOrder.
//
// Instead, we Harmony-prefix FontManager.Load. The patch is registered
// from our StartPre (ExecuteOrder = -1) using string-based reflection that
// never touches the FontManager type, so VSImGui's StartPre runs cleanly.
// When VSImGui later invokes FontManager.Load, our prefix executes between
// the cctor (succeeds because natives are loaded) and the body, adding our
// Cinzel paths to the static Fonts set before the atlas is built.
[UsedImplicitly]
public class CinzelFontSystem : ModSystem
{
    public const string RegularName = "cinzel-regular";
    public const string BoldName = "cinzel-bold";

    private const string RegularResource = "DivineAscension.Fonts.cinzel-regular.ttf";
    private const string BoldResource = "DivineAscension.Fonts.cinzel-bold.ttf";
    private const string HarmonyId = "com.divineascension.cinzel-fonts";

    private static string? _regularPath;
    private static string? _boldPath;
    private static Dictionary<(string, int), ImFontPtr>? _loadedCache;

    private Harmony? _harmony;

    public override double ExecuteOrder() => -1.0;

    public override bool ShouldLoad(EnumAppSide forSide) => forSide == EnumAppSide.Client;

    public override void StartPre(ICoreAPI api)
    {
        if (api.Side != EnumAppSide.Client) return;

        string cacheDir;
        try
        {
            cacheDir = api.GetOrCreateDataPath(Path.Combine("ModData", "divineascension", "fonts"));
        }
        catch (Exception ex)
        {
            api.Logger.Error($"[DivineAscension] Cinzel cache dir create failed: {ex.Message}");
            return;
        }

        _regularPath = Extract(api, RegularResource, cacheDir, $"{RegularName}.ttf");
        _boldPath = Extract(api, BoldResource, cacheDir, $"{BoldName}.ttf");
        if (_regularPath == null && _boldPath == null) return;

        // String-based reflection: must not trigger VSImGui.API.FontManager's
        // static initializer here. AccessTools.Method only reads metadata.
        var target = AccessTools.Method("VSImGui.API.FontManager:Load");
        if (target == null)
        {
            api.Logger.Warning("[DivineAscension] VSImGui.API.FontManager.Load not found; Cinzel not registered.");
            return;
        }

        var prefix = AccessTools.Method(typeof(CinzelFontSystem), nameof(AddCinzelFontsPrefix));
        try
        {
            _harmony = new Harmony(HarmonyId);
            _harmony.Patch(target, prefix: new HarmonyMethod(prefix));
        }
        catch (Exception ex)
        {
            api.Logger.Error($"[DivineAscension] Cinzel Harmony patch failed: {ex.Message}");
        }
    }

    public override void Dispose()
    {
        try
        {
            _harmony?.UnpatchAll(HarmonyId);
        }
        catch
        {
            // ignored — VSImGui may have already torn down
        }
        _harmony = null;
    }

    // Runs as a Harmony prefix on VSImGui.API.FontManager.Load. By the time
    // VSImGui invokes Load, NativesLoader has registered cimgui, so touching
    // FontManager here is safe — its static cctor will succeed.
    [HarmonyPriority(Priority.First)]
    private static void AddCinzelFontsPrefix()
    {
        try
        {
            var fontsProp = AccessTools.Property("VSImGui.API.FontManager:Fonts");
            if (fontsProp?.GetValue(null) is HashSet<string> fonts)
            {
                if (_regularPath != null) fonts.Add(_regularPath);
                if (_boldPath != null) fonts.Add(_boldPath);
            }
        }
        catch
        {
            // ignored — leave the atlas with just VSImGui's defaults
        }
    }

    // VSImGui generates fonts at sizes registered in FontManager.Sizes
    // (defaults below). Lookup returns null until the atlas is built so callers
    // can fall back to the default font.
    public static readonly int[] BakedSizes = { 6, 8, 10, 14, 18, 24, 30, 36, 48, 60 };

    /// <summary>
    ///     Snap a requested pixel size (e.g. a UI-scaled base size) to the nearest
    ///     size the atlas actually baked. Callers draw crisply at that size rather
    ///     than upscaling one baked glyph. Single source of truth for the ladder —
    ///     shared by the serif text helpers and the chrome's direct callers.
    /// </summary>
    public static int NearestBakedSize(int requested)
    {
        var best = BakedSizes[0];
        var bestDelta = int.MaxValue;
        foreach (var s in BakedSizes)
        {
            var delta = Math.Abs(s - requested);
            if (delta < bestDelta)
            {
                best = s;
                bestDelta = delta;
            }
        }
        return best;
    }

    public static ImFontPtr? GetRegular(int size) => Lookup(RegularName, size);

    public static ImFontPtr? GetBold(int size) => Lookup(BoldName, size);

    private static ImFontPtr? Lookup(string fontName, int size)
    {
        var dict = _loadedCache ??= ResolveLoadedDictionary();
        return dict != null && dict.TryGetValue((fontName, size), out var ptr) ? ptr : null;
    }

    private static Dictionary<(string, int), ImFontPtr>? ResolveLoadedDictionary()
    {
        try
        {
            var prop = AccessTools.Property("VSImGui.API.FontManager:Loaded");
            return prop?.GetValue(null) as Dictionary<(string, int), ImFontPtr>;
        }
        catch
        {
            return null;
        }
    }

    private static string? Extract(ICoreAPI api, string resourceName, string cacheDir, string outFileName)
    {
        try
        {
            using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName);
            if (stream == null)
            {
                api.Logger.Error($"[DivineAscension] Cinzel embedded resource missing: {resourceName}");
                return null;
            }

            var outPath = Path.Combine(cacheDir, outFileName);
            using (var fs = File.Create(outPath))
            {
                stream.CopyTo(fs);
            }
            return outPath;
        }
        catch (Exception ex)
        {
            api.Logger.Error($"[DivineAscension] Cinzel extract '{resourceName}' failed: {ex.Message}");
            return null;
        }
    }
}
