namespace DivineAscension.GUI.Events.Civilization;

public abstract record CreateEvent
{
    public sealed record NameChanged(string newName) : CreateEvent;

    public sealed record DescriptionChanged(string newDescription) : CreateEvent;

    public sealed record IconSelected(string icon) : CreateEvent;

    public sealed record SubmitClicked : CreateEvent;

    public sealed record ClearClicked : CreateEvent;
}