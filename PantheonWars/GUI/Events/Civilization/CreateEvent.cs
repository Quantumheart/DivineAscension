namespace PantheonWars.GUI.Events.Civilization;

public abstract record CreateEvent
{
    public sealed record NameChanged(string newName) : CreateEvent;

    public sealed record SubmitClicked : CreateEvent;

    public sealed record ClearClicked : CreateEvent;
}