using System.Collections.Generic;
using DivineAscension.GUI.Events.Blessing;

namespace DivineAscension.GUI.Models.Blessing.Actions;

public readonly struct BlessingActionsRendererResult(IReadOnlyList<ActionsEvent> events, float height)
{
    public IReadOnlyList<ActionsEvent> Events { get; } = events;
    public float Height { get; } = height;
}