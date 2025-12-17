namespace PantheonWars.GUI.Events.Civilization;

public abstract record EditEvent
{
    public sealed record IconSelected(string icon) : EditEvent;

    public sealed record SubmitClicked : EditEvent;

    public sealed record CancelClicked : EditEvent;
}