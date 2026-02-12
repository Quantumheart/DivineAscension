namespace DivineAscension.GUI.State;

/// <summary>
///     State management for BlessingDialog
/// </summary>
public class GuiDialogState
{
    /// <summary>
    ///     Index of the main tab in BlessingDialog (0=Blessings, 1=Religion, 2=Civilization)
    /// </summary>
    public MainDialogTab CurrentMainTab { get; set; }

    /// <summary>
    ///     Whether the dialog is currently open
    /// </summary>
    public bool IsOpen { get; set; }

    /// <summary>
    ///     Whether the dialog data is ready (loaded from server)
    /// </summary>
    public bool IsReady { get; set; }

    /// <summary>
    ///     UI requested to close the dialog (e.g., via X button)
    /// </summary>
    public bool RequestClose { get; set; }

    /// <summary>
    ///     Current window X position
    /// </summary>
    public float WindowPosX { get; set; }

    /// <summary>
    ///     Current window Y position
    /// </summary>
    public float WindowPosY { get; set; }

    public NotificationState? NotificationState { get; set; }
    public string? PreviousFavorRank { get; set; }
    public string? PreviousPrestigeRank { get; set; }

    /// <summary>
    ///     State for the rank progress HUD overlay
    /// </summary>
    public RankProgressHudState HudState { get; set; } = new();

    /// <summary>
    ///     Initialize/reset all state to defaults
    /// </summary>
    public void Reset()
    {
        IsOpen = false;
        IsReady = false;
        RequestClose = false;
        CurrentMainTab = MainDialogTab.Religion;
        WindowPosX = 0f;
        WindowPosY = 0f;
        NotificationState?.Reset();
        HudState.Reset();
    }
}

public enum MainDialogTab
{
    Religion = 0,
    Blessings = 1,
    Civilization = 2
}