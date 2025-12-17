using System.Collections.Generic;
using DivineAscension.GUI.Events.Civilization;

namespace DivineAscension.GUI.Models.Civilization.Info;

public readonly struct CivilizationInfoRendererResult(IReadOnlyList<InfoEvent> events, float rendererHeight)
{
    public IReadOnlyList<InfoEvent> Events { get; } = events;
    public float RendererHeight { get; } = rendererHeight;
}