using PantheonWars.Network;

namespace PantheonWars.GUI.Events;

public abstract record ReligionListEvent
{
    // Fired when the scroll position changes due to wheel or drag
    public record ScrollChanged(float NewScrollY) : ReligionListEvent;

    // Fired when a list item (religion) is clicked
    public record ItemClicked(string ReligionUID, float NewScrollY) : ReligionListEvent;
}
