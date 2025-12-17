using System.Collections.Generic;
using PantheonWars.GUI.Events.Religion;

namespace PantheonWars.GUI.Models.Religion.Roles;

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