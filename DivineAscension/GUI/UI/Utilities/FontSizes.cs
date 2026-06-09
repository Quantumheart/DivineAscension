namespace DivineAscension.GUI.UI.Utilities;

/// <summary>
///     Centralized font size definitions for all UI components.
///     Ensures consistent typography across all overlays and dialogs.
///
///     Each member returns its base (1.0-scale) size multiplied by
///     <see cref="UiScale.Factor" /> via <see cref="UiScale.Scaled(float)" />, so
///     all text grows proportionally on high-resolution / high-DPI screens (#587).
///     The base sizes below are the unscaled source of truth; at the default
///     <see cref="UiScale.Factor" /> of 1.0 every member returns its base value.
///
///     These are properties rather than consts so the live scale is applied at
///     read time. Consequently they cannot be used as default parameter values or
///     other compile-time constants — pass the value explicitly (or use a nullable
///     sentinel resolved in the method body).
/// </summary>
internal static class FontSizes
{
    // Page/Window level
    /// <summary>Top-level screen titles (e.g., "Create Religion", "Blessing Info")</summary>
    public static float PageTitle => UiScale.Scaled(20f);

    // Section level
    /// <summary>Major section headers (e.g., activity sections, civilization sections)</summary>
    public static float SectionHeader => UiScale.Scaled(18f);

    /// <summary>Table column headers, detail section titles, religion names</summary>
    public static float TableHeader => UiScale.Scaled(17f);

    /// <summary>Field labels, requirement headers, ritual details</summary>
    public static float SubsectionLabel => UiScale.Scaled(16f);

    // Content level
    /// <summary>Primary body text, table data, list items</summary>
    public static float Body => UiScale.Scaled(15f);

    /// <summary>Info text, descriptions, metadata</summary>
    public static float Secondary => UiScale.Scaled(14f);

    /// <summary>Hints, timestamps, minor labels</summary>
    public static float Small => UiScale.Scaled(14f);

    /// <summary>Tier labels, very small text</summary>
    public static float Compact => UiScale.Scaled(14f);

    /// <summary>Densely packed summary text (cross-deity counts, micro badges)</summary>
    public static float Micro => UiScale.Scaled(12f);

    // Spacing helpers
    /// <summary>
    ///     Pixels added between wrapped text lines (line-height = fontSize + LinePadding).
    ///     Scales with the font so line spacing tracks text growth.
    /// </summary>
    public static float LinePadding => UiScale.Scaled(6f);
}
