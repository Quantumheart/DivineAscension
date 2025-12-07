using System.Collections.Generic;
using PantheonWars.GUI.Events;

namespace PantheonWars.GUI.Models.Religion.Member;

public readonly struct MemberListRenderResult(
    IReadOnlyList<ReligionMemberListEvent> events,
    float renderedHeight)
{
    public IReadOnlyList<ReligionMemberListEvent> Events { get; } = events;
    public float RenderedHeight { get; } = renderedHeight;
}
