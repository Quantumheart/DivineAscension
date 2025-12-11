using System.Collections.Generic;
using PantheonWars.GUI.Events.Blessing;

namespace PantheonWars.GUI.Models.Blessing.Actions;

public readonly struct BlessingActionsRendererResult(IReadOnlyList<ActionsEvent> events, float height)
{
    public IReadOnlyList<ActionsEvent> Events { get; } = events;
    public float Height { get; } = height;
}