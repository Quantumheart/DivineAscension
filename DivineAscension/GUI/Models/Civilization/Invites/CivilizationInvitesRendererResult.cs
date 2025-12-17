using System.Collections.Generic;
using PantheonWars.GUI.Events.Civilization;

namespace PantheonWars.GUI.Models.Civilization.Invites;

public readonly struct CivilizationInvitesRendererResult(IReadOnlyList<InvitesEvent> events, float rendererHeight)
{
    public IReadOnlyList<InvitesEvent> Events { get; } = events;
    public float RendererHeight { get; } = rendererHeight;
}