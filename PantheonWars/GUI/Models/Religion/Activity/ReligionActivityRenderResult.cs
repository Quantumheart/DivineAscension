using System.Collections.Generic;
using PantheonWars.GUI.Events;

namespace PantheonWars.GUI.Models.Religion.Activity;

public readonly struct ReligionActivityRenderResult(IReadOnlyList<ReligionActivityEvent>  events, float rendererHeight)
{
    public IReadOnlyList<ReligionActivityEvent> Events { get; } =  events;
    public float RendererHeight { get; } = rendererHeight;
}