using PantheonWars.GUI.State.Religion;

namespace PantheonWars.GUI.Events;

public abstract record ReligionSubTabEvent
{
    // User clicked a different sub tab header
    public record TabChanged(SubTab SubTab) : ReligionSubTabEvent;

    // User dismissed the top error banner for the last action (global)
    public record DismissActionError() : ReligionSubTabEvent;

    // User dismissed the context-specific error for the given tab (Browse/Info/Create)
    public record DismissContextError(SubTab SubTab) : ReligionSubTabEvent;

    // User clicked retry on the error banner for a specific tab (Browse/Info)
    public record RetryRequested(SubTab SubTab) : ReligionSubTabEvent;
}