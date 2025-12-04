namespace PantheonWars.GUI.UI.State;

/// <summary>
///     State management for Create Religion overlay
/// </summary>
public class CreateReligionState
{
    /// <summary>
    ///     Name for the new religion
    /// </summary>
    public string ReligionName { get; set; } = "";

    /// <summary>
    ///     Selected deity index (from DeityHelper.DeityNames array)
    /// </summary>
    public int SelectedDeityIndex { get; set; }

    /// <summary>
    ///     Whether the religion should be public (true) or private (false)
    /// </summary>
    public bool IsPublic { get; set; } = true;

    /// <summary>
    ///     Error message to display (if any)
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    ///     Whether the deity dropdown menu is currently open
    /// </summary>
    public bool DropdownOpen { get; set; }

    /// <summary>
    ///     Initialize/reset all state to defaults
    /// </summary>
    public void Reset()
    {
        ReligionName = "";
        SelectedDeityIndex = 0;
        IsPublic = true;
        ErrorMessage = null;
        DropdownOpen = false;
    }
}