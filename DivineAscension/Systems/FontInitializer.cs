using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using ImGuiNET;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using VSImGui.API;

namespace DivineAscension.Systems;

/// <summary>
///     Loads a serif font (DejaVu Serif) with extended glyph ranges as the
///     default ImGui font for the mod.
///
///     VSImGui's <see cref="FontManager.Load" /> restricts loaded fonts to
///     <c>GetGlyphRangesDefault()</c> (Basic Latin + Latin-1) per the player's
///     locale, which silently drops ornament glyphs in the Dingbats and
///     Miscellaneous Symbols blocks (✦ ⚜ ✉ ★ etc.). We bypass that by
///     subscribing to <see cref="FontManager.BeforeFontsLoaded" /> and directly
///     calling <c>AddFontFromMemoryTTF</c> with our own font config and a
///     broader glyph range table. Setting <c>io.FontDefault</c> makes our font
///     the implicit default for every <c>ImGui.Text</c> / <c>drawList.AddText</c>
///     call.
///
///     <see cref="ModSystem.ExecuteOrder" /> is negative so this runs before
///     VSImGui (order 0) initializes and fires the event.
/// </summary>
[ExcludeFromCodeCoverage]
public class FontInitializer : ModSystem
{
    private const float DefaultSizePixels = 18f;

    // Long-lived references so ImGui's atlas can read these pointers without
    // worrying about GC moves or our memory disappearing.
    private static GCHandle _fontBytesHandle;
    private static IntPtr _glyphRangesPtr = IntPtr.Zero;

    public override double ExecuteOrder()
    {
        return -1.0;
    }

    public override bool ShouldLoad(EnumAppSide forSide)
    {
        return forSide == EnumAppSide.Client;
    }

    public override void StartClientSide(ICoreClientAPI api)
    {
        base.StartClientSide(api);
        FontManager.BeforeFontsLoaded += (_, _) => RegisterCustomFont(api);
    }

    private static unsafe void RegisterCustomFont(ICoreClientAPI api)
    {
        var loc = new AssetLocation("divineascension", "gui/fonts/dejavuserif.ttf");
        var asset = api.Assets.TryGet(loc);
        if (asset == null)
        {
            api.Logger.Error($"[DivineAscension] Font asset not found: {loc}");
            return;
        }

        // Pin the byte[] so its address stays stable while ImGui reads it
        // (FontDataOwnedByAtlas = 0). The pin is held for the lifetime of the
        // process — fine since the font lives as long as the atlas does.
        _fontBytesHandle = GCHandle.Alloc(asset.Data, GCHandleType.Pinned);
        var fontDataPtr = _fontBytesHandle.AddrOfPinnedObject();
        _glyphRangesPtr = AllocateGlyphRanges();

        var config = new ImFontConfig
        {
            FontDataOwnedByAtlas = 0,
            OversampleH = 2,
            OversampleV = 1,
            PixelSnapH = 1,
            GlyphMaxAdvanceX = float.MaxValue,
            RasterizerMultiply = 1f,
            EllipsisChar = ushort.MaxValue
        };

        var io = ImGui.GetIO();
        var fontPtr = io.Fonts.AddFontFromMemoryTTF(
            fontDataPtr,
            asset.Data.Length,
            DefaultSizePixels,
            new ImFontConfigPtr(&config),
            _glyphRangesPtr);

        // FontDefault is read-only via the C# property wrapper; reach through
        // to the native struct field to assign it.
        io.NativePtr->FontDefault = fontPtr.NativePtr;
        api.Logger.Notification("[DivineAscension] Registered DejaVu Serif as default ImGui font");
    }

    /// <summary>
    ///     Allocate the glyph range table ImGui copies pointers from when
    ///     rasterizing the atlas. Pairs of <c>(start, end)</c> codepoints
    ///     terminated by <c>0</c>. The ranges below cover everything we need
    ///     for codex chrome — Basic Latin, common punctuation, geometric
    ///     shapes, Miscellaneous Symbols, and Dingbats.
    /// </summary>
    private static IntPtr AllocateGlyphRanges()
    {
        ushort[] ranges =
        {
            0x0020, 0x00FF, // Basic Latin + Latin-1 Supplement
            0x2000, 0x206F, // General Punctuation
            0x2100, 0x214F, // Letterlike Symbols
            0x2190, 0x21FF, // Arrows
            0x2500, 0x259F, // Box Drawing + Block Elements
            0x25A0, 0x25FF, // Geometric Shapes
            0x2600, 0x26FF, // Miscellaneous Symbols (✦ ⚜ ★)
            0x2700, 0x27BF, // Dingbats (✉)
            0x0
        };
        var bytes = ranges.Length * sizeof(ushort);
        var ptr = Marshal.AllocHGlobal(bytes);
        var shortRanges = new short[ranges.Length];
        for (var i = 0; i < ranges.Length; i++)
        {
            shortRanges[i] = unchecked((short)ranges[i]);
        }
        Marshal.Copy(shortRanges, 0, ptr, ranges.Length);
        return ptr;
    }
}
