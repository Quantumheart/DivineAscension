namespace DivineAscension.GUI.UI.Utilities;

/// <summary>
///     Centralized spacing scale for content page renderers.
///     Use these in preference to magic numbers so padding stays
///     consistent across Religion / Civilization / Blessing / HolySites.
/// </summary>
internal static class Spacing
{
    // All values scale with UiScale.Factor so spacing tracks the UI scale (#600).

    // Horizontal padding inside a content panel (gap from the panel edge).
    public static float ContentPadding => UiScale.Scaled(16f);

    // Gap between the back-button row and the panel content below it.
    public static float BackButtonRow => UiScale.Scaled(44f);

    // Vertical rhythm — pick one of these for "blank space between things".
    public static float Tight => UiScale.Scaled(8f);
    public static float Compact => UiScale.Scaled(12f);
    public static float Comfortable => UiScale.Scaled(16f);
    public static float Section => UiScale.Scaled(24f);
    public static float Block => UiScale.Scaled(32f);

    // Spacing between a section header label and the first content row underneath it.
    public static float HeaderToContent => UiScale.Scaled(28f);

    // Gap between repeated rows in a vertical list (members, milestones, etc.).
    public static float ListItemGap => UiScale.Scaled(8f);
}
