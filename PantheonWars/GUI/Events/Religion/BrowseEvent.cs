using PantheonWars.Network;

namespace PantheonWars.GUI.Events.Religion;

public abstract record BrowseEvent
{
    // User clicked a deity filter tab
    public record DeityFilterChanged(string NewFilter) : BrowseEvent;

    // User selected a different religion from the list
    public record Selected(string? ReligionUID, float NewScrollY)
        : BrowseEvent;

    // User scrolled the religion list
    public record ScrollChanged(float NewScrollY) : BrowseEvent;

    // User clicked "Create Religion" button
    public record CreateClicked : BrowseEvent;

    // User clicked "Join Religion" button
    public record JoinClicked(string ReligionUID) : BrowseEvent;

    // User hovered over a religion (for tooltip)
    public record Hovered(ReligionListResponsePacket.ReligionInfo? Religion) : BrowseEvent;
}