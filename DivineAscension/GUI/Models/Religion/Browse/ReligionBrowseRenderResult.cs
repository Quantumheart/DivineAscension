using System.Collections.Generic;
using PantheonWars.GUI.Events.Religion;
using PantheonWars.Network;

namespace PantheonWars.GUI.Models.Religion.Browse;

public readonly struct ReligionBrowseRenderResult(
    IReadOnlyList<BrowseEvent> events,
    ReligionListResponsePacket.ReligionInfo? hoveredReligion,
    float renderedHeight)
{
    public IReadOnlyList<BrowseEvent> Events { get; } = events;
    public ReligionListResponsePacket.ReligionInfo? HoveredReligion { get; } = hoveredReligion;
    public float RenderedHeight { get; } = renderedHeight;
}