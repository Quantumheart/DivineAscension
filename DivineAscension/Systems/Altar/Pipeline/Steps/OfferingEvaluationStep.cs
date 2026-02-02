using DivineAscension.Constants;
using DivineAscension.Services;
using DivineAscension.Services.Interfaces;

namespace DivineAscension.Systems.Altar.Pipeline.Steps;

/// <summary>
/// Evaluates the offering and calculates its favor bonus.
/// May short-circuit if the offering is rejected due to tier requirements.
/// Sets OfferingBonus, ShouldConsumeOffering, and OfferingRejectedDomain on the context.
/// </summary>
public class OfferingEvaluationStep(IOfferingEvaluator offeringEvaluator) : IPrayerStep
{
    public string Name => "OfferingEvaluation";

    public void Execute(PrayerContext context)
    {
        if (context.Offering == null || context.Offering.StackSize <= 0)
            return;

        var value = offeringEvaluator.CalculateOfferingValue(
            context.Offering,
            context.Religion!.Domain,
            context.HolySiteTier);

        if (value == -1)
        {
            // Offering rejected due to insufficient holy site tier
            context.Success = false;
            context.Message = LocalizationService.Instance.Get(LocalizationKeys.PRAYER_OFFERING_TIER_REJECTED);
            context.IsComplete = true;
            return;
        }

        if (value > 0)
        {
            context.OfferingBonus = value;
            context.ShouldConsumeOffering = true;
        }
        else
        {
            // value == 0 means domain mismatch, offering rejected but prayer continues
            context.OfferingRejectedDomain = true;
        }
    }
}