using DivineAscension.GUI.State;

namespace DivineAscension.GUI.Events.Civilization;

public abstract record SubTabEvent
{
    public sealed record TabChanged(CivilizationSubTab NewSubTab) : SubTabEvent;

    public sealed record DismissActionError : SubTabEvent;

    public sealed record DismissContextError(CivilizationSubTab SubTab) : SubTabEvent;

    public sealed record RetryRequested(CivilizationSubTab SubTab) : SubTabEvent;
}