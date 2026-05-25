namespace DivineAscension.GUI.Events.Civilization;

public abstract record InfoEvent
{
    public sealed record ScrollChanged(float y) : InfoEvent;

    public sealed record MemberScrollChanged(float y) : InfoEvent;

    public sealed record InviteReligionNameChanged(string text) : InfoEvent;

    public sealed record InviteReligionClicked(string religionName) : InfoEvent;

    public sealed record DescriptionChanged(string newDescription) : InfoEvent;

    public sealed record EditDescriptionOpen : InfoEvent;

    public sealed record EditDescriptionCancel : InfoEvent;

    public sealed record SaveDescriptionClicked : InfoEvent;

    public sealed record EditCapitalOpen : InfoEvent;

    public sealed record EditCapitalCancel : InfoEvent;

    public sealed record CapitalNameChanged(string text) : InfoEvent;

    public sealed record CapitalBindingChanged(string siteId) : InfoEvent;

    public sealed record SaveCapitalClicked : InfoEvent;

    public sealed record ToggleCapitalSiteDropdown(bool isOpen) : InfoEvent;

    public sealed record LeaveClicked : InfoEvent;

    public sealed record EditIconClicked : InfoEvent;

    public sealed record DisbandOpened : InfoEvent;

    public sealed record DisbandConfirmed : InfoEvent, IModalControlEvent;

    public sealed record DisbandCancel : InfoEvent, IModalControlEvent;

    public sealed record KickOpen(string religionId, string religionName) : InfoEvent;

    public sealed record KickConfirm(string religionName) : InfoEvent, IModalControlEvent;

    public sealed record KickCancel : InfoEvent, IModalControlEvent;
}