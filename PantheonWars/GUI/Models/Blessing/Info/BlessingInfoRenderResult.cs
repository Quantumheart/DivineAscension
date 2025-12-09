using System.Collections.Generic;
using PantheonWars.GUI.Events;

namespace PantheonWars.GUI.Models.Blessing.Info;

internal readonly struct BlessingInfoRenderResult(IReadOnlyList<BlessingInfoEvent> events, float heightUsed)
{
    public IReadOnlyList<BlessingInfoEvent> Events { get; } = events;
    public float HeightUsed { get; } = heightUsed;

    public static BlessingInfoRenderResult Empty(float height)
    {
        return new BlessingInfoRenderResult(new List<BlessingInfoEvent>(0), height);
    }
}