using System.Collections.Generic;
using DivineAscension.GUI.Events.Civilization;
using DivineAscension.Network;

namespace DivineAscension.GUI.Models.Civilization.Milestones;

/// <summary>
///     Result from rendering the Milestones sub-tab
/// </summary>
public readonly struct CivilizationMilestoneRenderResult(
    IReadOnlyList<MilestoneEvent> events,
    float renderedHeight,
    MilestoneProgressDto? hoveredMilestone = null)
{
    public IReadOnlyList<MilestoneEvent> Events { get; } = events;
    public float RenderedHeight { get; } = renderedHeight;
    public MilestoneProgressDto? HoveredMilestone { get; } = hoveredMilestone;
}
