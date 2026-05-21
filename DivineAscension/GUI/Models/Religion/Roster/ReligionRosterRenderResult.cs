using System.Collections.Generic;
using DivineAscension.GUI.Events.Religion;

namespace DivineAscension.GUI.Models.Religion.Roster;

public readonly struct ReligionRosterRenderResult(IReadOnlyList<RosterEvent> events, float renderedHeight)
{
    public IReadOnlyList<RosterEvent> Events { get; } = events;
    public float RenderedHeight { get; } = renderedHeight;
}
