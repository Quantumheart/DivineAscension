using System.Collections.Generic;
using PantheonWars.GUI.Events;

namespace PantheonWars.GUI.Models.Religion.Invites;

public readonly struct ReligionInvitesRenderResult(
    IReadOnlyList<ReligionInvitesEvent> events,
    float renderedHeight)
{
    public IReadOnlyList<ReligionInvitesEvent> Events { get; } = events;
    public float RenderedHeight { get; } = renderedHeight;
}