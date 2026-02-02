using System;
using DivineAscension.Systems.Interfaces;

namespace DivineAscension.Systems.Altar.Pipeline.Steps;

/// <summary>
/// Updates the player's prayer cooldown after a successful prayer.
/// Only executes if the prayer was successful and ShouldUpdateCooldown is true.
/// </summary>
public class CooldownUpdateStep : IPrayerStep
{
    private static readonly TimeSpan PrayerCooldown = TimeSpan.FromHours(1);

    private readonly IPlayerProgressionDataManager _progressionDataManager;

    public CooldownUpdateStep(IPlayerProgressionDataManager progressionDataManager)
    {
        _progressionDataManager = progressionDataManager;
    }

    public string Name => "CooldownUpdate";

    public void Execute(PrayerContext context)
    {
        if (!context.Success || !context.ShouldUpdateCooldown)
            return;

        _progressionDataManager.SetPrayerCooldownExpiryUtc(
            context.PlayerUID,
            DateTime.UtcNow + PrayerCooldown);
    }
}