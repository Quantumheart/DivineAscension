using System.Collections.Generic;
using PantheonWars.GUI.Events.Religion;

namespace PantheonWars.GUI.Models.Religion.Tab;

/// <summary>
/// The result of rendering the religion sub tab.
/// </summary>
public readonly struct ReligionSubTabRenderResult(
    IReadOnlyList<SubTabEvent> events,
    float renderedHeight)
{
    public IReadOnlyList<SubTabEvent> Events { get; } = events;
    public float RenderedHeight { get; } = renderedHeight;
}