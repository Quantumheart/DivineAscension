namespace DivineAscension.GUI.Events.Civilization;

public abstract record HolySitesEvent
{
    public sealed record RefreshClicked : HolySitesEvent;

    public sealed record ScrollChanged(float NewScrollY) : HolySitesEvent;

    public sealed record ReligionToggled(string ReligionUID) : HolySitesEvent;

    public sealed record SiteSelected(string SiteUID) : HolySitesEvent;
}
