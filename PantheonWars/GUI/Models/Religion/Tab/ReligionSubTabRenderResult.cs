using System.Collections.Generic;
using PantheonWars.GUI.Events;

namespace PantheonWars.GUI.Models.Religion.Tab;

/// <summary>
/// The result of rendering the religion sub tab.
/// </summary>
public readonly struct ReligionSubTabRenderResult(
    IReadOnlyList<ReligionSubTabEvent> events,
    float renderedHeight)
{
    public IReadOnlyList<ReligionSubTabEvent> Events { get; } = events;
    public float RenderedHeight { get; } = renderedHeight;
}