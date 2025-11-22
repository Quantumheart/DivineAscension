namespace PantheonWars.GUI.State;

/// <summary>
///     State management for GuildManagementDialog
/// </summary>
public class GuildDialogState
{
    /// <summary>
    ///     Whether the dialog is currently open
    /// </summary>
    public bool IsOpen { get; set; }

    /// <summary>
    ///     Whether the dialog data is ready (loaded from server)
    /// </summary>
    public bool IsReady { get; set; }

    /// <summary>
    ///     Current window X position
    /// </summary>
    public float WindowPosX { get; set; }

    /// <summary>
    ///     Current window Y position
    /// </summary>
    public float WindowPosY { get; set; }

    /// <summary>
    ///     Initialize/reset all state to defaults
    /// </summary>
    public void Reset()
    {
        IsOpen = false;
        IsReady = false;
        WindowPosX = 0f;
        WindowPosY = 0f;
    }
}
