namespace PantheonWars.GUI.Events.Civilization;

public abstract record BrowseEvent
{
    public sealed record DeityFilterChanged(string newFilter) : BrowseEvent;

    public sealed record ScrollChanged(float y) : BrowseEvent;

    public sealed record ViewDetailedsClicked(string civId) : BrowseEvent;

    public sealed record RefreshClicked : BrowseEvent;

    public sealed record DeityDropDownToggled(bool isOpen) : BrowseEvent;
}