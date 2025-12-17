using System.Collections.Generic;
using DivineAscension.GUI.Events.Civilization;

namespace DivineAscension.GUI.Models.Civilization.Detail;

public readonly struct CivilizationDetailRendererResult(IReadOnlyList<DetailEvent> events, float rendererHeight)
{
    public IReadOnlyList<DetailEvent> Events { get; } = events;
    public float RendererHeight { get; } = rendererHeight;
}