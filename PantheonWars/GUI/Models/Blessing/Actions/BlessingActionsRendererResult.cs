using System.Collections.Generic;
using PantheonWars.GUI.Events;

namespace PantheonWars.GUI.Models.Blessing.Actions;

public readonly struct BlessingActionsRendererResult(IReadOnlyList<BlessingActionsEvent> events, float height)
{
    public IReadOnlyList<BlessingActionsEvent> Events { get; } = events;
    public float Height { get; } = height;
}