using System.Collections.Generic;
using DivineAscension.GUI.Events.Civilization;

namespace DivineAscension.GUI.Models.Civilization.Browse;

public readonly struct CivilizationBrowseRenderResult(IReadOnlyList<BrowseEvent> events, float renderedHeight)
{
    public IReadOnlyList<BrowseEvent> Events { get; } = events;
    public float RenderedHeight { get; } = renderedHeight;
}