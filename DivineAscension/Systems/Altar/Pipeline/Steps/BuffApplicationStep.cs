using System.Collections.Generic;
using DivineAscension.Constants;
using DivineAscension.Systems.BuffSystem.Interfaces;

namespace DivineAscension.Systems.Altar.Pipeline.Steps;

/// <summary>
/// Applies the holy site prayer buff to the player.
/// Only executes if the prayer was successful.
/// </summary>
public class BuffApplicationStep(IBuffManager buffManager) : IPrayerStep
{
    private const float BUFF_DURATION_SECONDS = 3600f; // 1 hour

    public string Name => "BuffApplication";

    public void Execute(PrayerContext context)
    {
        if (!context.Success)
            return;

        var statModifiers = new Dictionary<string, float>
        {
            { VintageStoryStats.HolySiteFavorMultiplier, context.BuffMultiplier },
            { VintageStoryStats.HolySitePrestigeMultiplier, context.BuffMultiplier }
        };

        buffManager.ApplyEffect(
            context.Player.Entity,
            "holy_site_prayer_buff",
            BUFF_DURATION_SECONDS,
            "altar_prayer",
            context.PlayerUID,
            statModifiers,
            isBuff: true);
    }
}