using System.Collections.Generic;
using PantheonWars.GUI.Events.Religion;

namespace PantheonWars.GUI.Models.Religion.Roles;

/// <summary>
///     Render outcome for ReligionRoles renderer: carries emitted UI events and the rendered height.
/// </summary>
public readonly struct ReligionRolesRenderResult(
    IReadOnlyList<RolesEvent> events,
    float renderedHeight)
{
    public IReadOnlyList<RolesEvent> Events { get; } = events;
    public float RenderedHeight { get; } = renderedHeight;
}