namespace DivineAscension.GUI.Events.Civilization;

public abstract record InvitesEvent
{
    public sealed record ScrollChanged(float y) : InvitesEvent;

    public sealed record AcceptInviteClicked(string inviteId) : InvitesEvent;

    public sealed record DeclineInviteClicked(string inviteId) : InvitesEvent;
}