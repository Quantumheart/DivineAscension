namespace DivineAscension.GUI.State;

/// <summary>
///     Runtime state for the right-hand rail introduced by the UI refactor.
///     Currently tracks scroll position for the notification feed and an
///     unread-only filter toggle.
/// </summary>
public class RightRailState
{
    public float ScrollY { get; set; }
    public bool ShowUnreadOnly { get; set; }

    public void Reset()
    {
        ScrollY = 0f;
        ShowUnreadOnly = false;
    }
}
