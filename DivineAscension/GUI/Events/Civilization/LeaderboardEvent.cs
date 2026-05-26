namespace DivineAscension.GUI.Events.Civilization;

public abstract record LeaderboardEvent
{
    public sealed record RefreshClicked : LeaderboardEvent;

    public sealed record ScrollChanged(float NewScrollY) : LeaderboardEvent;
}
