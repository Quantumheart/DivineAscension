using DivineAscension.Constants;
using DivineAscension.Services;
using DivineAscension.Systems.Interfaces;

namespace DivineAscension.Systems.Altar.Pipeline.Steps;

/// <summary>
/// Validates that the altar is part of a consecrated holy site.
/// Sets HolySite, HolySiteTier, and PrayerMultiplier on the context.
/// </summary>
public class HolySiteValidationStep(IHolySiteManager holySiteManager) : IPrayerStep
{
    public string Name => "HolySiteValidation";

    public void Execute(PrayerContext context)
    {
        context.HolySite = holySiteManager.GetHolySiteByAltarPosition(context.AltarPosition);

        if (context.HolySite == null)
        {
            context.Success = false;
            context.Message = LocalizationService.Instance.Get(LocalizationKeys.PRAYER_ALTAR_NOT_CONSECRATED);
            context.IsComplete = true;
            return;
        }

        context.HolySiteTier = context.HolySite.GetTier();
        context.PrayerMultiplier = context.HolySite.GetPrayerMultiplier();
    }
}