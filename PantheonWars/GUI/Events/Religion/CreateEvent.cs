namespace PantheonWars.GUI.Events.Religion;

public abstract record CreateEvent
{
    // User edited the religion name input
    public record NameChanged(string NewName) : CreateEvent;

    // User changed the selected deity (via tabs)
    public record DeityChanged(string NewDeity) : CreateEvent;

    // User toggled the Public/Private checkbox
    public record IsPublicChanged(bool IsPublic) : CreateEvent;

    // User clicked the Create Religion button
    public record SubmitClicked : CreateEvent;
}