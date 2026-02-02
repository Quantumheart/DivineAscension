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
        var cooldownExpiry = progressionDataManager.GetPrayerCooldownExpiry(context.PlayerUID);

        if (cooldownExpiry > 0 && context.CurrentTime < cooldownExpiry)
        {
            var remainingMs = cooldownExpiry - context.CurrentTime;

            // Round to nearest minute (adds 30s before dividing)
            var remainingMinutes = (int)((remainingMs + 30000) / 60000);
            if (remainingMinutes == 0 && remainingMs > 0)
                remainingMinutes = 1;

            context.Success = false;
            context.Message = LocalizationService.Instance.Get(LocalizationKeys.PRAYER_COOLDOWN, remainingMinutes);
            context.IsComplete = true;
        }
    }
}