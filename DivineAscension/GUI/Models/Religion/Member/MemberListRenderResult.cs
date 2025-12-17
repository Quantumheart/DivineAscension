using System.Collections.Generic;
using DivineAscension.GUI.Events.Religion;

namespace DivineAscension.GUI.Models.Religion.Member;

public readonly struct MemberListRenderResult(
    IReadOnlyList<MemberListEvent> events,
    float renderedHeight)
{
    public IReadOnlyList<MemberListEvent> Events { get; } = events;
    public float RenderedHeight { get; } = renderedHeight;
}