using System.Collections.Generic;
using DivineAscension.GUI.Events.Religion;
using DivineAscension.Network;

namespace DivineAscension.GUI.Models.Religion.List;

public readonly struct ReligionListRenderResult(
    IReadOnlyList<ListEvent> events,
    ReligionListResponsePacket.ReligionInfo? hoveredReligion,
    float renderedHeight)
{
    public IReadOnlyList<ListEvent> Events { get; } = events;
    public ReligionListResponsePacket.ReligionInfo? HoveredReligion { get; } = hoveredReligion;
    public float RenderedHeight { get; } = renderedHeight;
}