using DivineAscension.Network;

namespace DivineAscension.GUI.State.Religion;

/// <summary>
///     State for religion detail view (when viewing details of a specific religion from browse)
/// </summary>
public class DetailState
{
    /// <summary>
    ///     UID of the religion currently being viewed (null = not viewing detail)
    /// </summary>
    public string? ViewingReligionUID { get; set; }

    /// <summary>
    ///     Details of the religion being viewed
    /// </summary>
    public ReligionDetailResponsePacket? ViewingReligionDetails { get; set; }

    /// <summary>
    ///     Whether the detail data is currently loading
    /// </summary>
    public bool IsLoading { get; set; }

    /// <summary>
    ///     Scroll position for the member list
    /// </summary>
    public float MemberScrollY { get; set; }

    /// <summary>
    ///     Reset all detail state
    /// </summary>
    public void Reset()
    {
        ViewingReligionUID = null;
        ViewingReligionDetails = null;
        IsLoading = false;
        MemberScrollY = 0f;
    }
}