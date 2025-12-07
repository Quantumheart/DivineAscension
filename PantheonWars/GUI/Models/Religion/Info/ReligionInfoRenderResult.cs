using System.Collections.Generic;
using PantheonWars.GUI.Events;

namespace PantheonWars.GUI.Models.Religion.Info;

/// <summary>
/// Render outcome for ReligionInfo renderer: carries emitted UI events and the rendered height.
/// </summary>
public readonly struct ReligionInfoRenderResult(
    IReadOnlyList<ReligionInfoEvent> events,
    float renderedHeight)
{
    public IReadOnlyList<ReligionInfoEvent> Events { get; } = events;
    public float RenderedHeight { get; } = renderedHeight;
}
