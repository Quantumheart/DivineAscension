namespace DivineAscension.GUI.UI.Utilities;

/// <summary>
///     Centralized spacing scale for content page renderers.
///     Use these in preference to magic numbers so padding stays
///     consistent across Religion / Civilization / Blessing / HolySites.
/// </summary>
internal static class Spacing
{
    // Horizontal padding inside a content panel (gap from the panel edge).
    public const float ContentPadding = 16f;

    // Gap between the back-button row and the panel content below it.
    public const float BackButtonRow = 44f;

    // Vertical rhythm — pick one of these for "blank space between things".
    public const float Tight = 8f;
    public const float Compact = 12f;
    public const float Comfortable = 16f;
    public const float Section = 24f;
    public const float Block = 32f;

    // Spacing between a section header label and the first content row underneath it.
    public const float HeaderToContent = 28f;

    // Gap between repeated rows in a vertical list (members, milestones, etc.).
    public const float ListItemGap = 8f;
}
