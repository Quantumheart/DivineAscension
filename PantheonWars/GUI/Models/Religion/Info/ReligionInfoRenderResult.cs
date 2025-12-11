using System.Collections.Generic;
using PantheonWars.GUI.Events.Religion;

namespace PantheonWars.GUI.Models.Religion.Info;

/// <summary>
/// Render outcome for ReligionInfo renderer: carries emitted UI events and the rendered height.
/// </summary>
public readonly struct ReligionInfoRenderResult(
    IReadOnlyList<InfoEvent> events,
    float renderedHeight)
{
    public IReadOnlyList<InfoEvent> Events { get; } = events;
    public float RenderedHeight { get; } = renderedHeight;
}
