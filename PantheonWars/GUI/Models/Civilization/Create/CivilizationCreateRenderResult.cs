using System.Collections.Generic;
using PantheonWars.GUI.Events.Civilization;

namespace PantheonWars.GUI.Models.Civilization.Create;

public readonly struct CivilizationCreateRenderResult(IReadOnlyList<CreateEvent> events, float renderedHeight)
{
    public IReadOnlyList<CreateEvent> Events { get; } = events;
    public float RenderedHeight { get; } = renderedHeight;
}
