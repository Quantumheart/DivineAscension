namespace DivineAscension.GUI.Events.Religion;

public abstract record RosterEvent
{
    public record ScrollChanged(float NewScrollY) : RosterEvent;

    public record RowToggled(string PlayerUID) : RosterEvent;

    public record KickClicked(string PlayerUID, string PlayerName) : RosterEvent;
    public record KickConfirm(string PlayerUID) : RosterEvent, IModalControlEvent;
    public record KickCancel : RosterEvent, IModalControlEvent;

    public record StrikeClicked(string PlayerUID, string PlayerName) : RosterEvent;
    public record StrikeConfirm(string PlayerUID) : RosterEvent, IModalControlEvent;
    public record StrikeCancel : RosterEvent, IModalControlEvent;

    public record InviteNameChanged(string Text) : RosterEvent;
    public record InviteClicked(string PlayerName) : RosterEvent;
}
