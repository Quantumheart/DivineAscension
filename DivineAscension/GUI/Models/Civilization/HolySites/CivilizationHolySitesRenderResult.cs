using System.Collections.Generic;
using DivineAscension.GUI.Events.Civilization;

namespace DivineAscension.GUI.Models.Civilization.HolySites;

public readonly struct CivilizationHolySitesRenderResult(
    IReadOnlyList<HolySitesEvent> events,
    float renderedHeight)
{
    public IReadOnlyList<HolySitesEvent> Events { get; } = events;
    public float RenderedHeight { get; } = renderedHeight;
}
