using System.Collections.Generic;
using DivineAscension.GUI.Events.Religion;

namespace DivineAscension.GUI.Models.Religion.Create;

public readonly struct ReligionCreateRenderResult(
    IReadOnlyList<CreateEvent> events,
    float renderedHeight)
{
    public IReadOnlyList<CreateEvent> Events { get; } = events;
    public float RenderedHeight { get; } = renderedHeight;
}