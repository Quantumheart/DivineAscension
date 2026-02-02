using System;
using DivineAscension.Configuration;
using DivineAscension.Constants;
using DivineAscension.Services;

namespace DivineAscension.Systems.Altar.Pipeline.Steps;

/// <summary>
/// Calculates favor, prestige, and buff rewards based on prayer and offering.
/// Sets FavorAwarded, PrestigeAwarded, BuffMultiplier, Success, and Message on the context.
/// </summary>
public class RewardCalculationStep(GameBalanceConfig config) : IPrayerStep
{
    private const int BASE_PRAYER_FAVOR = 5;

    public string Name => "RewardCalculation";

    public void Execute(PrayerContext context)
    {
        context.FavorAwarded = (int)Math.Round(
            (BASE_PRAYER_FAVOR + context.OfferingBonus) * context.PrayerMultiplier);
        context.PrestigeAwarded = context.FavorAwarded; // 1:1 ratio with favor

        context.BuffMultiplier = context.HolySiteTier switch
        {
            1 => config.HolySiteTier1Multiplier,
            2 => config.HolySiteTier2Multiplier,
            3 => config.HolySiteTier3Multiplier,
            _ => 1.0f
        };

        context.Success = true;
        context.Message = BuildMessage(context);
    }

    private static string BuildMessage(PrayerContext ctx)
    {
        if (ctx.OfferingBonus > 0)
        {
            return LocalizationService.Instance.Get(
                LocalizationKeys.PRAYER_SUCCESS_WITH_OFFERING,
                ctx.FavorAwarded,
                ctx.PrestigeAwarded,
                ctx.OfferingBonus,
                ctx.HolySiteTier,
                ctx.PrayerMultiplier,
                ctx.BuffMultiplier);
        }

        if (ctx.OfferingRejectedDomain)
        {
            return LocalizationService.Instance.Get(
                LocalizationKeys.PRAYER_SUCCESS_OFFERING_REJECTED,
                ctx.FavorAwarded,
                ctx.PrestigeAwarded,
                ctx.HolySiteTier,
                ctx.PrayerMultiplier,
                ctx.BuffMultiplier);
        }

        return LocalizationService.Instance.Get(
            LocalizationKeys.PRAYER_SUCCESS_NO_OFFERING,
            ctx.FavorAwarded,
            ctx.PrestigeAwarded,
            ctx.HolySiteTier,
            ctx.PrayerMultiplier,
            ctx.BuffMultiplier);
    }
}