using System.Collections.Generic;
using DivineAscension.GUI.Events.Religion;

namespace DivineAscension.GUI.Models.Religion.Roles;

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