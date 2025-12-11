using System.Collections.Generic;
using PantheonWars.GUI.Events.Civilization;

namespace PantheonWars.GUI.Models.Civilization.Tab;

public readonly struct CivilizationTabRendererResult(IReadOnlyList<SubTabEvent> events, float rendererHeight)
{
    public IReadOnlyList<SubTabEvent> Events { get; } = events;
    public float RendererHeight { get; } = rendererHeight;
}