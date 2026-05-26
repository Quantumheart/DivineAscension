using System.Collections.Generic;
using DivineAscension.GUI.Events.Civilization;

namespace DivineAscension.GUI.Models.Civilization.Leaderboard;

public readonly struct CivilizationLeaderboardRenderResult(
    IReadOnlyList<LeaderboardEvent> events,
    float renderedHeight)
{
    public IReadOnlyList<LeaderboardEvent> Events { get; } = events;
    public float RenderedHeight { get; } = renderedHeight;
}
