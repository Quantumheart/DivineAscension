using System.Collections.Generic;
using PantheonWars.GUI.Events;
using PantheonWars.Network;

namespace PantheonWars.GUI.Models.Religion.List;

public readonly struct ReligionListRenderResult(
    IReadOnlyList<ReligionListEvent> events,
    ReligionListResponsePacket.ReligionInfo? hoveredReligion,
    float renderedHeight)
{
    public IReadOnlyList<ReligionListEvent> Events { get; } = events;
    public ReligionListResponsePacket.ReligionInfo? HoveredReligion { get; } = hoveredReligion;
    public float RenderedHeight { get; } = renderedHeight;
}
