using System.Collections.Generic;
using DivineAscension.GUI.Events.Religion;

namespace DivineAscension.GUI.Models.Religion.Invites;

public readonly struct ReligionInvitesRenderResult(
    IReadOnlyList<InvitesEvent> events,
    float renderedHeight)
{
    public IReadOnlyList<InvitesEvent> Events { get; } = events;
    public float RenderedHeight { get; } = renderedHeight;
}