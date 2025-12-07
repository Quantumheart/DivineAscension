using System.Collections.Generic;
using PantheonWars.GUI.Events;
using PantheonWars.Network;

namespace PantheonWars.GUI.Models.Religion.Browse;

public readonly struct RenderResult(
    IReadOnlyList<ReligionBrowseEvent> events,
    ReligionListResponsePacket.ReligionInfo? hoveredReligion,
    float renderedHeight)
{
    public IReadOnlyList<ReligionBrowseEvent> Events { get; } = events;
    public ReligionListResponsePacket.ReligionInfo? HoveredReligion { get; } = hoveredReligion;
    public float RenderedHeight { get; } = renderedHeight;
}