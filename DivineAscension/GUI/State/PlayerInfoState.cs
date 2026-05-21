namespace DivineAscension.GUI.State;

/// <summary>
///     Runtime state for the "You" content page (former right rail).
///     Tracks scroll position for the notification feed and an
///     unread-only filter toggle.
/// </summary>
public class PlayerInfoState
{
    public float ScrollY { get; set; }
    public bool ShowUnreadOnly { get; set; }

    public void Reset()
    {
        ScrollY = 0f;
        ShowUnreadOnly = false;
    }
}
