using System.Collections.Generic;
using PantheonWars.GUI.Events;

namespace PantheonWars.GUI.Models.Religion.Header;

public readonly struct ReligionHeaderRendererResult(IReadOnlyList<ReligionHeaderEvent> events, float height)
{
    public IReadOnlyList<ReligionHeaderEvent> Events { get; } = events;
    public float Height { get; } = height;
}