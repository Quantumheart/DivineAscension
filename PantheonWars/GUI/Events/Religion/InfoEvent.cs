namespace PantheonWars.GUI.Events.Religion;

/// <summary>
/// Events representing user interactions within the My Religion info renderer.
/// Pure UI intents that the state manager will handle.
/// </summary>
public abstract record InfoEvent
{
    // Scrolling
    public record ScrollChanged(float NewScrollY) : InfoEvent;

    public record MemberScrollChanged(float NewScrollY) : InfoEvent;

    public record BanListScrollChanged(float NewScrollY) : InfoEvent;

    // Description
    public record DescriptionChanged(string Text) : InfoEvent;

    public record SaveDescriptionClicked(string Text) : InfoEvent;

    // Invites
    public record InviteNameChanged(string Text) : InfoEvent;

    public record InviteClicked(string PlayerName) : InfoEvent;

    // Membership actions
    public record LeaveClicked : InfoEvent;

    // Disband flow
    public record DisbandOpen : InfoEvent;

    public record DisbandConfirm : InfoEvent;

    public record DisbandCancel : InfoEvent;

    // Kick flow
    public record KickOpen(string PlayerUID, string PlayerName) : InfoEvent;

    public record KickConfirm(string PlayerUID) : InfoEvent;

    public record KickCancel : InfoEvent;

    // Ban flow
    public record BanOpen(string PlayerUID, string PlayerName) : InfoEvent;

    public record BanConfirm(string PlayerUID) : InfoEvent;

    public record BanCancel : InfoEvent;

    // Unban
    public record UnbanClicked(string PlayerUID) : InfoEvent;
}
