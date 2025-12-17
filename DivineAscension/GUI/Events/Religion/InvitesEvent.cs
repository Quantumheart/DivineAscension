namespace DivineAscension.GUI.Events.Religion;

/// <summary>
/// Events representing user interactions with the invites renderer
/// </summary>
public abstract record InvitesEvent
{
    /// <summary>
    /// User clicked Accept button for an invite
    /// </summary>
    public record AcceptInviteClicked(string InviteId) : InvitesEvent;

    /// <summary>
    /// User clicked Decline button for an invite
    /// </summary>
    public record DeclineInviteClicked(string InviteId) : InvitesEvent;

    /// <summary>
    /// User scrolled the invites list
    /// </summary>
    public record ScrollChanged(float NewScrollY) : InvitesEvent;
}