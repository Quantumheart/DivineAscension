namespace PantheonWars.GUI.Events;

/// <summary>
/// Events representing user interactions with the invites renderer
/// </summary>
public abstract record ReligionInvitesEvent
{
    /// <summary>
    /// User clicked Accept button for an invite
    /// </summary>
    public record AcceptInviteClicked(string InviteId) : ReligionInvitesEvent;

    /// <summary>
    /// User clicked Decline button for an invite
    /// </summary>
    public record DeclineInviteClicked(string InviteId) : ReligionInvitesEvent;

    /// <summary>
    /// User scrolled the invites list
    /// </summary>
    public record ScrollChanged(float NewScrollY) : ReligionInvitesEvent;
}