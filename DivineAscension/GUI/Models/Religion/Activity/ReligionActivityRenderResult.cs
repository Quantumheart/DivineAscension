using System.Collections.Generic;
using PantheonWars.GUI.Events.Religion;

namespace PantheonWars.GUI.Models.Religion.Activity;

public readonly struct ReligionActivityRenderResult(IReadOnlyList<ActivityEvent> events, float rendererHeight)
{
    public IReadOnlyList<ActivityEvent> Events { get; } = events;
    public float RendererHeight { get; } = rendererHeight;
}