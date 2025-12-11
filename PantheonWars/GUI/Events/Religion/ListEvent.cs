namespace PantheonWars.GUI.Events.Religion;

public abstract record ListEvent
{
    // Fired when the scroll position changes due to wheel or drag
    public record ScrollChanged(float NewScrollY) : ListEvent;

    // Fired when a list item (religion) is clicked
    public record ItemClicked(string ReligionUID, float NewScrollY) : ListEvent;
}
