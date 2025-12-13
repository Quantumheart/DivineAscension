using System.Collections.Generic;
using PantheonWars.GUI.Events.Religion;

namespace PantheonWars.GUI.Models.Religion.Roles;

/// <summary>
///     Render outcome for ReligionRolesBrowse renderer: carries emitted UI events and the rendered height.
/// </summary>
public readonly struct ReligionRolesBrowseRenderResult(
    IReadOnlyList<RolesBrowseEvent> events,
    float renderedHeight)
{
    public IReadOnlyList<RolesBrowseEvent> Events { get; } = events;
    public float RenderedHeight { get; } = renderedHeight;
}