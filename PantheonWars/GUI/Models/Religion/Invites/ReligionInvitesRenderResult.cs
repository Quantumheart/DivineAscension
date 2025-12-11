using System.Collections.Generic;
using PantheonWars.GUI.Events.Religion;

namespace PantheonWars.GUI.Models.Religion.Invites;

public readonly struct ReligionInvitesRenderResult(
    IReadOnlyList<InvitesEvent> events,
    float renderedHeight)
{
    public IReadOnlyList<InvitesEvent> Events { get; } = events;
    public float RenderedHeight { get; } = renderedHeight;
}