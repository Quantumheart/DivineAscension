using System;
using DivineAscension.API.Interfaces;
using DivineAscension.Constants;
using DivineAscension.Services;
using DivineAscension.Systems.Interfaces;

namespace DivineAscension.Systems.Altar.Pipeline.Steps;

/// <summary>
/// Validates that the player's prayer cooldown has expired.
/// </summary>
public class CooldownValidationStep(
    IPlayerProgressionDataManager progressionDataManager,
    ITimeService timeService) : IPrayerStep
{
    public string Name => "CooldownValidation";

    public void Execute(PrayerContext context)
    {
        var cooldownExpiry = progressionDataManager.GetPrayerCooldownExpiryUtc(context.PlayerUID);
        var now = timeService.UtcNow;

        if (cooldownExpiry.HasValue && now < cooldownExpiry.Value)
        {
            var remainingTime = cooldownExpiry.Value - now;

            // Round to nearest minute (adds 30s before rounding)
            var remainingMinutes = (int)Math.Round(remainingTime.TotalMinutes);
            if (remainingMinutes == 0 && remainingTime.TotalMilliseconds > 0)
                remainingMinutes = 1;

            context.Success = false;
            context.Message = LocalizationService.Instance.Get(LocalizationKeys.PRAYER_COOLDOWN, remainingMinutes);
            context.IsComplete = true;
        }
    }
}