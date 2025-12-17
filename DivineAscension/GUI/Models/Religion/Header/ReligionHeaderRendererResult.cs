using System.Collections.Generic;
using DivineAscension.GUI.Events.Religion;

namespace DivineAscension.GUI.Models.Religion.Header;

public readonly struct ReligionHeaderRendererResult(IReadOnlyList<HeaderEvent> events, float height)
{
    public IReadOnlyList<HeaderEvent> Events { get; } = events;
    public float Height { get; } = height;
}