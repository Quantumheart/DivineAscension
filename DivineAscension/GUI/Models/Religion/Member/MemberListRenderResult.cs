using System.Collections.Generic;
using PantheonWars.GUI.Events.Religion;

namespace PantheonWars.GUI.Models.Religion.Member;

public readonly struct MemberListRenderResult(
    IReadOnlyList<MemberListEvent> events,
    float renderedHeight)
{
    public IReadOnlyList<MemberListEvent> Events { get; } = events;
    public float RenderedHeight { get; } = renderedHeight;
}