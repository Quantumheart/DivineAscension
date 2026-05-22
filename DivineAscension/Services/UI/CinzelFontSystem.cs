using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using ImGuiNET;
using JetBrains.Annotations;
using Vintagestory.API.Common;
using VSImGui.API;

namespace DivineAscension.Services.UI;

// VSImGui's FontManager fires BeforeFontsLoaded inside its ImGuiModSystem.StartPre
// (Controller ctor -> LoadFonts -> FontManager.Load). To get our subscriber in
// before that fires we need our own ModSystem with a lower ExecuteOrder and the
// subscription wired from StartPre, not Start.
[UsedImplicitly]
public class CinzelFontSystem : ModSystem
{
    public const string RegularName = "cinzel-regular";
    public const string BoldName = "cinzel-bold";

    private const string RegularResource = "DivineAscension.Fonts.cinzel-regular.ttf";
    private const string BoldResource = "DivineAscension.Fonts.cinzel-bold.ttf";

    private static Dictionary<(string, int), ImFontPtr>? _loadedCache;

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

        var regularPath = Extract(api, RegularResource, cacheDir, $"{RegularName}.ttf");
        var boldPath = Extract(api, BoldResource, cacheDir, $"{BoldName}.ttf");

        FontManager.BeforeFontsLoaded += (fonts, _) =>
        {
            if (regularPath != null) fonts.Add(regularPath);
            if (boldPath != null) fonts.Add(boldPath);
        };
    }

    // VSImGui generates fonts at sizes registered in FontManager.Sizes
    // (defaults: 6, 8, 10, 14, 18, 24, 30, 36, 48, 60). Lookup returns null
    // until the atlas is built so callers can fall back to the default font.
    public static ImFontPtr? GetRegular(int size) => Lookup(RegularName, size);

    public static ImFontPtr? GetBold(int size) => Lookup(BoldName, size);

    private static ImFontPtr? Lookup(string fontName, int size)
    {
        var dict = _loadedCache ??= ResolveLoadedDictionary();
        return dict != null && dict.TryGetValue((fontName, size), out var ptr) ? ptr : null;
    }

    // FontManager.Loaded is internal in VSImGui 0.0.6; reach it once via
    // reflection. Falls through gracefully if a later version makes it public
    // or renames it — callers just get null and use the default font.
    private static Dictionary<(string, int), ImFontPtr>? ResolveLoadedDictionary()
    {
        var prop = typeof(FontManager).GetProperty("Loaded",
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
        return prop?.GetValue(null) as Dictionary<(string, int), ImFontPtr>;
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
