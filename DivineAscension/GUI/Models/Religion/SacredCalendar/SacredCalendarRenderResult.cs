using System.Collections.Generic;
using DivineAscension.GUI.Events.Religion;

namespace DivineAscension.GUI.Models.Religion.SacredCalendar;

public readonly struct SacredCalendarRenderResult(IReadOnlyList<SacredCalendarEvent> events, float rendererHeight)
{
    public IReadOnlyList<SacredCalendarEvent> Events { get; } = events;
    public float RendererHeight { get; } = rendererHeight;
}
