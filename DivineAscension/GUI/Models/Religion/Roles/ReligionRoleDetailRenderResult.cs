using System.Collections.Generic;
using DivineAscension.GUI.Events.Religion;

namespace DivineAscension.GUI.Models.Religion.Roles;

/// <summary>
///     Render outcome for ReligionRoleDetail renderer: carries emitted UI events and the rendered height.
/// </summary>
public readonly struct ReligionRoleDetailRenderResult(
    IReadOnlyList<RoleDetailEvent> events,
    float renderedHeight)
{
    public IReadOnlyList<RoleDetailEvent> Events { get; } = events;
    public float RenderedHeight { get; } = renderedHeight;
}