using System.Collections.Generic;
using DivineAscension.GUI.Events.Civilization;

namespace DivineAscension.GUI.Models.Civilization.Milestones;

public readonly struct CivilizationMilestoneRenderResult(
    IReadOnlyList<MilestoneEvent> events,
    float renderedHeight)
{
    public IReadOnlyList<MilestoneEvent> Events { get; } = events;
    public float RenderedHeight { get; } = renderedHeight;
}
