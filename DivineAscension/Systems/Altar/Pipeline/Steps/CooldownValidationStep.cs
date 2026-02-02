using System;
using DivineAscension.Constants;
using DivineAscension.Services;
using DivineAscension.Systems.Interfaces;

namespace DivineAscension.Systems.Altar.Pipeline.Steps;

/// <summary>
/// Validates that the player's prayer cooldown has expired.
/// </summary>
public class CooldownValidationStep(IPlayerProgressionDataManager progressionDataManager) : IPrayerStep
{
    public string Name => "CooldownValidation";

    public void Execute(PrayerContext context)
    {
        var cooldownExpiry = progressionDataManager.GetPrayerCooldownExpiryUtc(context.PlayerUID);

        if (cooldownExpiry.HasValue && DateTime.UtcNow < cooldownExpiry.Value)
        {
            var remainingTime = cooldownExpiry.Value - DateTime.UtcNow;

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