using ImGuiNET;

namespace DivineAscension.Services.UI;

/// <summary>
///     Placeholder for Cinzel font integration. The TTFs ship under
///     <c>assets/divineascension/textures/gui/codex/fonts/</c>, but runtime
///     atlas registration is currently disabled — see issue #287.
///
///     <para>
///     Summary of the blocker: VSImGui 1.2.5 bakes its font atlas to GPU
///     exactly once during its own <c>StartPre</c> and exposes no public
///     reload. Adding fonts later either crashes the renderer (glyph data
///     never built) or, if we force <c>iO.Fonts.Build()</c> + reflect into
///     the private <c>ImGuiRenderer.RecreateFontDeviceTexture</c>, corrupts
///     every other font in the atlas — VS's native UI text included.
///     </para>
///
///     <para>
///     <see cref="GetRegular" /> and <see cref="GetBold" /> always return
///     <c>null</c> so callers fall back to the default font cleanly. When
///     VSImGui exposes a reload API (or we adopt a second ImGui context),
///     the load + lookup wiring lives here.
///     </para>
/// </summary>
public static class CinzelFontService
{
    public const string RegularName = "cinzel-regular";
    public const string BoldName = "cinzel-bold";

    public static ImFontPtr? GetRegular(int size) => null;

    public static ImFontPtr? GetBold(int size) => null;
}
