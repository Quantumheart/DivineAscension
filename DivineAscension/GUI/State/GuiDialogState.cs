namespace DivineAscension.GUI.State;

/// <summary>
///     State management for BlessingDialog
/// </summary>
public class GuiDialogState
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

    /// <summary>
    ///     Live window width snapshot, captured each frame inside <c>DrawWindow</c>.
    ///     Persisted on <c>Close()</c> via <c>UiPrefs</c>.
    /// </summary>
    public float WindowWidth { get; set; }

    /// <summary>
    ///     Live window height snapshot, captured each frame inside <c>DrawWindow</c>.
    ///     Persisted on <c>Close()</c> via <c>UiPrefs</c>.
    /// </summary>
    public float WindowHeight { get; set; }

    public NotificationState? NotificationState { get; set; }
    public string? PreviousFavorRank { get; set; }
    public string? PreviousPrestigeRank { get; set; }

    /// <summary>
    ///     Sidebar nav state (Phase 1 scaffold; wired in Phase 3).
    /// </summary>
    public SidebarState Sidebar { get; } = new();

    /// <summary>
    ///     "You" content page state — notification feed scroll + unread-only
    ///     filter toggle. Previously lived as the right rail.
    /// </summary>
    public PlayerInfoState PlayerInfo { get; } = new();

    /// <summary>
    ///     Initialize/reset all state to defaults
    /// </summary>
    public void Reset()
    {
        IsOpen = false;
        IsReady = false;
        RequestClose = false;
        WindowPosX = 0f;
        WindowPosY = 0f;
        WindowWidth = 0f;
        WindowHeight = 0f;
        NotificationState?.Reset();
        Sidebar.Reset();
        PlayerInfo.Reset();
    }
}