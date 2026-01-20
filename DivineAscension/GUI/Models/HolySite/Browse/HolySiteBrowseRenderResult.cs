using System.Collections.Generic;
using DivineAscension.GUI.Events.HolySite;

namespace DivineAscension.GUI.Models.HolySite.Browse;

/// <summary>
///     Immutable result from holy site browse rendering containing events and state.
/// </summary>
public readonly struct HolySiteBrowseRenderResult(
    IReadOnlyList<BrowseEvent> events,
    float renderedHeight)
{
    public IReadOnlyList<BrowseEvent> Events { get; } = events;
    public float RenderedHeight { get; } = renderedHeight;
}
