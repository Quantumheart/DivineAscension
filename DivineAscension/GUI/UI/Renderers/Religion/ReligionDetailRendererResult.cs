using System.Collections.Generic;
using DivineAscension.GUI.Events.Religion;

namespace DivineAscension.GUI.UI.Renderers.Religion;

/// <summary>
///     Result from rendering religion detail view
/// </summary>
// todo: migrate to correct path
public readonly struct ReligionDetailRendererResult(
    IReadOnlyList<DetailEvent> events,
    float height)
{
    public IReadOnlyList<DetailEvent> Events { get; } = events;
    public float Height { get; } = height;
}