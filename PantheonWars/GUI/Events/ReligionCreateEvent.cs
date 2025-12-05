namespace PantheonWars.GUI.Events;

public abstract record ReligionCreateEvent
{
    // User edited the religion name input
    public record NameChanged(string NewName) : ReligionCreateEvent;

    // User changed the selected deity (via tabs)
    public record DeityChanged(string NewDeity) : ReligionCreateEvent;

    // User toggled the Public/Private checkbox
    public record IsPublicChanged(bool IsPublic) : ReligionCreateEvent;

    // User clicked the Create Religion button
    public record SubmitClicked() : ReligionCreateEvent;
}