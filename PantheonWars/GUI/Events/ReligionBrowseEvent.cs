using PantheonWars.Network;

namespace PantheonWars.GUI.Events;

public abstract record ReligionBrowseEvent
{
    // User clicked a deity filter tab
    public record DeityFilterChanged(string NewFilter) : ReligionBrowseEvent;

    // User selected a different religion from the list
    public record ReligionSelected(string? ReligionUID, float NewScrollY)
        : ReligionBrowseEvent;

    // User scrolled the religion list
    public record ScrollChanged(float NewScrollY) : ReligionBrowseEvent;

    // User clicked "Create Religion" button
    public record CreateReligionClicked() : ReligionBrowseEvent;

    // User clicked "Join Religion" button
    public record JoinReligionClicked(string ReligionUID) : ReligionBrowseEvent;

    // User hovered over a religion (for tooltip)
    public record ReligionHovered(ReligionListResponsePacket.ReligionInfo? Religion) : ReligionBrowseEvent;
}