using DivineAscension.Models.Enum;

namespace DivineAscension.GUI.Events.Civilization;

public abstract record LeaderboardEvent
{
    public sealed record RefreshClicked : LeaderboardEvent;

    public sealed record ScrollChanged(float NewScrollY) : LeaderboardEvent;

    public sealed record BoardSelected(LeaderboardMetric Board) : LeaderboardEvent;
}
