using System.Collections.Generic;
using DivineAscension.GUI.Events.Civilization;

namespace DivineAscension.GUI.Models.Civilization.Create;

public readonly struct CivilizationCreateRenderResult(IReadOnlyList<CreateEvent> events, float renderedHeight)
{
    public IReadOnlyList<CreateEvent> Events { get; } = events;
    public float RenderedHeight { get; } = renderedHeight;
}