namespace PantheonWars.GUI.Events.Civilization;

public abstract record DetailEvent
{
    public sealed record MemberScrollChanged(float NewScrollY) : DetailEvent;

    public sealed record BackToBrowseClicked : DetailEvent;

    public sealed record RequestToJoinClicked(string CivId) : DetailEvent;
}