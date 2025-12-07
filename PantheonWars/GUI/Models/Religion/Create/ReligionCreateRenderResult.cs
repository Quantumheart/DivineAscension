using System.Collections.Generic;
using PantheonWars.GUI.Events;

namespace PantheonWars.GUI.Models.Religion.Create;

public readonly struct ReligionCreateRenderResult(
    IReadOnlyList<ReligionCreateEvent> events,
    float renderedHeight)
{
    public IReadOnlyList<ReligionCreateEvent> Events { get; } = events;
    public float RenderedHeight { get; } = renderedHeight;
}