using DivineAscension.GUI.State;

namespace DivineAscension.GUI.Events.Religion;

public abstract record SubTabEvent
{
    // User dismissed the top error banner for the last action (global)
    public record DismissActionError : SubTabEvent;

    // User dismissed the context-specific error for the given destination
    public record DismissContextError(SidebarNavId Nav) : SubTabEvent;

    // User clicked retry on the error banner for the given destination
    public record RetryRequested(SidebarNavId Nav) : SubTabEvent;
}
