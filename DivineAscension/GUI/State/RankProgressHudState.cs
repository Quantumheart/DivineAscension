namespace DivineAscension.GUI.State;

/// <summary>
///     State container for the rank progress HUD overlay
/// </summary>
public class RankProgressHudState
{
    /// <summary>
    ///     Whether the HUD is currently visible (player has a religion)
    /// </summary>
    public bool IsVisible { get; set; }

    /// <summary>
    ///     When true, render only the patron deity row instead of all five.
    /// </summary>
    public bool CollapsedToPatron { get; set; }

    /// <summary>
    ///     Reset state to defaults
    /// </summary>
    public void Reset()
    {
        IsVisible = false;
        CollapsedToPatron = false;
    }
}
