namespace PantheonWars.GUI.Events;

/// <summary>
/// Events representing user interactions within the My Religion info renderer.
/// Pure UI intents that the state manager will handle.
/// </summary>
public abstract record ReligionInfoEvent
{
    // Scrolling
    public record ScrollChanged(float NewScrollY) : ReligionInfoEvent;
    public record MemberScrollChanged(float NewScrollY) : ReligionInfoEvent;
    public record BanListScrollChanged(float NewScrollY) : ReligionInfoEvent;

    // Description
    public record DescriptionChanged(string Text) : ReligionInfoEvent;
    public record SaveDescriptionClicked(string Text) : ReligionInfoEvent;

    // Invites
    public record InviteNameChanged(string Text) : ReligionInfoEvent;
    public record InviteClicked(string PlayerName) : ReligionInfoEvent;

    // Membership actions
    public record LeaveClicked() : ReligionInfoEvent;

    // Disband flow
    public record DisbandOpen() : ReligionInfoEvent;
    public record DisbandConfirm() : ReligionInfoEvent;
    public record DisbandCancel() : ReligionInfoEvent;

    // Kick flow
    public record KickOpen(string PlayerUID, string PlayerName) : ReligionInfoEvent;
    public record KickConfirm(string PlayerUID) : ReligionInfoEvent;
    public record KickCancel() : ReligionInfoEvent;

    // Ban flow
    public record BanOpen(string PlayerUID, string PlayerName) : ReligionInfoEvent;
    public record BanConfirm(string PlayerUID) : ReligionInfoEvent;
    public record BanCancel() : ReligionInfoEvent;

    // Unban
    public record UnbanClicked(string PlayerUID) : ReligionInfoEvent;
}
