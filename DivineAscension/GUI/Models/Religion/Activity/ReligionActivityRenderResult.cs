using System.Collections.Generic;
using DivineAscension.GUI.Events.Religion;

namespace DivineAscension.GUI.Models.Religion.Activity;

public readonly struct ReligionActivityRenderResult(IReadOnlyList<ActivityEvent> events, float rendererHeight)
{
    public IReadOnlyList<ActivityEvent> Events { get; } = events;
    public float RendererHeight { get; } = rendererHeight;
}