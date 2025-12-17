namespace PantheonWars.GUI.Events.Civilization;

public abstract record InfoEvent
{
    public sealed record ScrollChanged(float y) : InfoEvent;

    public sealed record MemberScrollChanged(float y) : InfoEvent;

    public sealed record InviteReligionNameChanged(string text) : InfoEvent;

    public sealed record InviteReligionClicked(string religionName) : InfoEvent;

    public sealed record LeaveClicked : InfoEvent;

    public sealed record EditIconClicked : InfoEvent;

    public sealed record DisbandOpened : InfoEvent;

    public sealed record DisbandConfirmed : InfoEvent;

    public sealed record DisbandCancel : InfoEvent;

    public sealed record KickOpen(string religionId, string religionName) : InfoEvent;

    public sealed record KickConfirm(string religionName) : InfoEvent;

    public sealed record KickCancel : InfoEvent;
}