using System.Collections.Generic;
using DivineAscension.GUI.Events.Religion;
using DivineAscension.Network;

namespace DivineAscension.GUI.Models.Religion.Browse;

public readonly struct ReligionBrowseRenderResult(
    IReadOnlyList<BrowseEvent> events,
    ReligionListResponsePacket.ReligionInfo? hoveredReligion,
    float renderedHeight)
{
    public IReadOnlyList<BrowseEvent> Events { get; } = events;
    public ReligionListResponsePacket.ReligionInfo? HoveredReligion { get; } = hoveredReligion;
    public float RenderedHeight { get; } = renderedHeight;
}