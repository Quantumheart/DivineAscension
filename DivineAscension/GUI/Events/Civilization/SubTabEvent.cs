using DivineAscension.GUI.State;

namespace DivineAscension.GUI.Events.Civilization;

public abstract record SubTabEvent
{
    public sealed record DismissActionError : SubTabEvent;

    public sealed record DismissContextError(SidebarNavId Nav) : SubTabEvent;

    public sealed record RetryRequested(SidebarNavId Nav) : SubTabEvent;
}
