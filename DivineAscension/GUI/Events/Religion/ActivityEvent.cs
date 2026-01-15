namespace DivineAscension.GUI.Events.Religion;

public abstract record ActivityEvent
{
    public record ScrollChanged(float NewScrollY) : ActivityEvent;

    public record RefreshRequested : ActivityEvent;
}