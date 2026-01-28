namespace DivineAscension.GUI.UI.Utilities;

/// <summary>
///     Centralized font size definitions for all UI components.
///     Ensures consistent typography across all overlays and dialogs.
/// </summary>
internal static class FontSizes
{
    // Page/Window level
    /// <summary>Top-level screen titles (e.g., "Create Religion", "Blessing Info")</summary>
    public const float PageTitle = 20f;

    // Section level
    /// <summary>Major section headers (e.g., activity sections, civilization sections)</summary>
    public const float SectionHeader = 18f;

    /// <summary>Table column headers, detail section titles, religion names</summary>
    public const float TableHeader = 16f;

    /// <summary>Field labels, requirement headers, ritual details</summary>
    public const float SubsectionLabel = 14f;

    // Content level
    /// <summary>Primary body text, table data, list items</summary>
    public const float Body = 13f;

    /// <summary>Info text, descriptions, metadata</summary>
    public const float Secondary = 12f;

    /// <summary>Hints, timestamps, minor labels</summary>
    public const float Small = 11f;

    /// <summary>Tier labels, very small text</summary>
    public const float Compact = 10f;
}
