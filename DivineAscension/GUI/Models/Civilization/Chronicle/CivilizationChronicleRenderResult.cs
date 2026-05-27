using System.Collections.Generic;
using DivineAscension.GUI.Events.Civilization;

namespace DivineAscension.GUI.Models.Civilization.Chronicle;

public readonly struct CivilizationChronicleRenderResult(
    IReadOnlyList<CivilizationChronicleEvent> events, float rendererHeight)
{
    public IReadOnlyList<CivilizationChronicleEvent> Events { get; } = events;
    public float RendererHeight { get; } = rendererHeight;
}
