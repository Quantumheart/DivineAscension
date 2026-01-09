namespace DivineAscension.GUI.Events.Religion;

/// <summary>
///     Events emitted from religion detail view renderer
/// </summary>
public abstract record DetailEvent
{
    /// <summary>
    ///     User clicked back button to return to browse
    /// </summary>
    public sealed record BackToBrowseClicked : DetailEvent;

    /// <summary>
    ///     User scrolled the member list
    /// </summary>
    public sealed record MemberScrollChanged(float NewScrollY) : DetailEvent;

    /// <summary>
    ///     User clicked join button
    /// </summary>
    public sealed record JoinClicked(string ReligionUID) : DetailEvent;
}