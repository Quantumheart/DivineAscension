using System.Collections.Generic;
using PantheonWars.GUI.Events.Blessing;

namespace PantheonWars.GUI.Models.Blessing.Info;

internal readonly struct BlessingInfoRenderResult(IReadOnlyList<InfoEvent> events, float heightUsed)
{
    public IReadOnlyList<InfoEvent> Events { get; } = events;
    public float HeightUsed { get; } = heightUsed;

    public static BlessingInfoRenderResult Empty(float height)
    {
        return new BlessingInfoRenderResult(new List<InfoEvent>(0), height);
    }
}