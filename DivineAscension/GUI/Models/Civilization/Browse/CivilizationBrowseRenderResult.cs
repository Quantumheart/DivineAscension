using System.Collections.Generic;
using PantheonWars.GUI.Events.Civilization;

namespace PantheonWars.GUI.Models.Civilization.Browse;

public readonly struct CivilizationBrowseRenderResult(IReadOnlyList<BrowseEvent> events, float renderedHeight)
{
    public IReadOnlyList<BrowseEvent> Events { get; } = events;
    public float RenderedHeight { get; } = renderedHeight;
}