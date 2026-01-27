namespace DivineAscension.GUI.Events.Civilization;

public abstract record MilestoneEvent
{
    public sealed record RefreshClicked : MilestoneEvent;

    public sealed record ScrollChanged(float NewScrollY) : MilestoneEvent;
}
