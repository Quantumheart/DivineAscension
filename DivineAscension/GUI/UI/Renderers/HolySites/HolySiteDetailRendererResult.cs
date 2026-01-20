using System.Collections.Generic;
using DivineAscension.GUI.Events.HolySite;

namespace DivineAscension.GUI.UI.Renderers.HolySites;

/// <summary>
///     Result from rendering holy site detail view
/// </summary>
// todo: migrate to correct path
public readonly struct HolySiteDetailRendererResult(
    IReadOnlyList<DetailEvent> events,
    float height)
{
    public IReadOnlyList<DetailEvent> Events { get; } = events;
    public float Height { get; } = height;
}
