using System.Collections.Generic;
using PantheonWars.GUI.Events.Civilization;

namespace PantheonWars.GUI.Models.Civilization.Tab;

public readonly struct CivilizationTabRendererResult(IReadOnlyList<InfoEvent> events, float rendererHeight)
{
    public IReadOnlyList<InfoEvent> Events { get; } = events;
    public float RendererHeight { get; } = rendererHeight;
}