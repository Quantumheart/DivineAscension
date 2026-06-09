using System;
using System.Numerics;

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
}
