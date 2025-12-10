using System.Collections.Generic;
using PantheonWars.GUI.Events.Civilization;

namespace PantheonWars.GUI.Models.Civilization.Create;

public readonly struct CivilizationDetailRenderResult(IReadOnlyList<DetailEvent> events, float renderedHeight)
{
    public IReadOnlyList<DetailEvent> Events { get; } = events;
    public float RenderedHeight { get; } = renderedHeight;
}