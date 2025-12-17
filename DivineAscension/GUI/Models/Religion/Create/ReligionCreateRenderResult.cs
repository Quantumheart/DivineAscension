using System.Collections.Generic;
using PantheonWars.GUI.Events.Religion;

namespace PantheonWars.GUI.Models.Religion.Create;

public readonly struct ReligionCreateRenderResult(
    IReadOnlyList<CreateEvent> events,
    float renderedHeight)
{
    public IReadOnlyList<CreateEvent> Events { get; } = events;
    public float RenderedHeight { get; } = renderedHeight;
}