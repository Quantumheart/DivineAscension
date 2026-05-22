namespace DivineAscension.GUI.Events.Religion;

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

    public record EditDescriptionOpen : InfoEvent;

    public record EditDescriptionCancel : InfoEvent;

    // Motto
    public record MottoChanged(string Text) : InfoEvent;
    public record SaveMottoClicked(string Text) : InfoEvent;
    public record EditMottoOpen : InfoEvent;
    public record EditMottoCancel : InfoEvent;

    // Founding myth
    public record FoundingMythChanged(string Text) : InfoEvent;
    public record SaveFoundingMythClicked(string Text) : InfoEvent;
    public record EditFoundingMythOpen : InfoEvent;
    public record EditFoundingMythCancel : InfoEvent;

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

    // Deity name editing
    public record EditDeityNameOpen : InfoEvent;

    public record EditDeityNameChanged(string Text) : InfoEvent;

    public record EditDeityNameSave(string NewDeityName) : InfoEvent;

    public record EditDeityNameCancel : InfoEvent;
}