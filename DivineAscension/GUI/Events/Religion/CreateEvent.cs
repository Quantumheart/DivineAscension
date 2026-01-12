namespace DivineAscension.GUI.Events.Religion;

public abstract record CreateEvent
{
    // User edited the religion name input
    public record NameChanged(string NewName) : CreateEvent;

    // User changed the selected domain (via tabs)
    public record DeityChanged(string NewDeity) : CreateEvent;

    // User edited the deity name input
    public record DeityNameChanged(string NewDeityName) : CreateEvent;

    // User toggled the Public/Private checkbox
    public record IsPublicChanged(bool IsPublic) : CreateEvent;

    // User clicked the Create Religion button
    public record SubmitClicked : CreateEvent;
}