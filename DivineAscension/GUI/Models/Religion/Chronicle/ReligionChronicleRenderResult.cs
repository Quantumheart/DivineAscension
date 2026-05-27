using System.Collections.Generic;
using DivineAscension.GUI.Events.Religion;

namespace DivineAscension.GUI.Models.Religion.Chronicle;

public readonly struct ReligionChronicleRenderResult(IReadOnlyList<ChronicleEvent> events, float rendererHeight)
{
    public IReadOnlyList<ChronicleEvent> Events { get; } = events;
    public float RendererHeight { get; } = rendererHeight;
}
