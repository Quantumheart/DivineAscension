using System;
using System.Numerics;
using ImGuiNET;
using Vintagestory.API.Config;

namespace DivineAscension.GUI.UI.Utilities;

/// <summary>
///     Global UI scale lever. All font sizes and layout offsets in the dialog
///     are authored at a base (1.0) scale; <see cref="Scaled(float)" /> multiplies
///     them through <see cref="Factor" /> so the whole UI grows proportionally on
///     high-resolution / high-DPI screens.
///
///     This type is intentionally just the lever — call sites are converted to
///     route through <see cref="Scaled(float)" /> in follow-up work. The scale
///     source (VS <c>ClientSettings.GUIScale</c> vs. a dedicated mod slider) is
///     wired separately; until then <see cref="Factor" /> stays at 1.0, making
///     every <see cref="Scaled(float)" /> call a no-op.
///
///     Scaling the <em>inputs</em> to layout (positions, sizes, font sizes) — not
///     the rendered output — keeps drawn geometry and manual hit-rects consistent,
///     so mouse clicks stay aligned. See epic #584 / spike #585 for why a single
///     post-render viewport transform is not viable.
/// </summary>
internal static class UiScale
{
    /// <summary>Smallest accepted scale; below this the UI is unusably small.</summary>
    public const float MinFactor = 0.5f;

    /// <summary>Largest accepted scale; above this the dialog overflows most screens.</summary>
    public const float MaxFactor = 4.0f;

    private static float _factor = 1.0f;

    /// <summary>
    ///     Current UI scale multiplier. Defaults to 1.0 (no scaling). Assignments
    ///     are clamped to [<see cref="MinFactor" />, <see cref="MaxFactor" />];
    ///     NaN/infinity are rejected and leave the previous value unchanged.
    /// </summary>
    public static float Factor
    {
        get => _factor;
        set
        {
            if (float.IsNaN(value) || float.IsInfinity(value)) return;
            _factor = Math.Clamp(value, MinFactor, MaxFactor);
        }
    }

    /// <summary>
    ///     Scale a base (1.0-scale) pixel value by the current <see cref="Factor" />,
    ///     rounded to the nearest whole pixel to keep text and edges crisp.
    /// </summary>
    public static float Scaled(float baseValue) =>
        MathF.Round(baseValue * _factor, MidpointRounding.AwayFromZero);

    /// <summary>
    ///     Scale a base (1.0-scale) 2D offset/size by the current <see cref="Factor" />,
    ///     rounding each component independently.
    /// </summary>
    public static Vector2 Scaled(Vector2 baseValue) => new(Scaled(baseValue.X), Scaled(baseValue.Y));

    /// <summary>
    ///     Mirror the player's chosen GUI scale onto <see cref="Factor" />. ImGui
    ///     renders independently of VS's GuiElement system, so
    ///     <see cref="RuntimeEnv.GUIScale" /> — the live value vanilla uses to scale
    ///     its own GUI — is not applied to our dialogs automatically. Call this each
    ///     frame before drawing so changing the setting takes effect without
    ///     reopening. Non-positive values (uninitialised / bogus) are ignored;
    ///     <see cref="Factor" /> clamps the rest to its supported range.
    /// </summary>
    public static void SyncFromGameSettings()
    {
        var scale = RuntimeEnv.GUIScale;
        if (scale > 0f) Factor = scale;
    }

    /// <summary>
    ///     Apply <see cref="Factor" /> to ImGui's global font scale for the
    ///     duration of the returned scope, then restore the previous value on
    ///     dispose. Use with <c>using</c> around a dialog's draw so all
    ///     <em>implicit-font</em> text (3-arg <c>AddText</c>, <c>ImGui.Button</c>/
    ///     <c>Text</c>/<c>Selectable</c>) and <c>CalcTextSize</c> measurements in
    ///     that window — and its child windows — scale with the UI.
    ///
    ///     <para>
    ///     Explicit-size <c>AddText(font, size, …)</c> ignores the global scale, so
    ///     text already sized via <see cref="FontSizes" /> (which is scaled by
    ///     <see cref="Factor" />) is not double-scaled. Width ratios of the form
    ///     <c>fontSize / ImGui.GetFontSize()</c> are unaffected: both terms move
    ///     by the same factor and cancel.
    ///     </para>
    ///
    ///     <para>
    ///     The scale is context-global, so saving and restoring keeps it from
    ///     leaking onto other VSImGui windows (debug tools, other mods) drawn in
    ///     the same frame.
    ///     </para>
    /// </summary>
    public static FontScaleScope BeginFontScale() => new(_factor);

    /// <summary>RAII scope that sets and restores <c>io.FontGlobalScale</c>. See <see cref="BeginFontScale" />.</summary>
    public readonly ref struct FontScaleScope
    {
        private readonly float _previous;

        internal FontScaleScope(float scale)
        {
            var io = ImGui.GetIO();
            _previous = io.FontGlobalScale;
            io.FontGlobalScale = scale;
        }

        public void Dispose() => ImGui.GetIO().FontGlobalScale = _previous;
    }
}
