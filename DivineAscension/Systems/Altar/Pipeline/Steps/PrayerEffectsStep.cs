using DivineAscension.Services.Interfaces;

namespace DivineAscension.Systems.Altar.Pipeline.Steps;

/// <summary>
/// Plays visual and audio effects for a successful prayer.
/// Only executes if the prayer was successful.
/// </summary>
public class PrayerEffectsStep : IPrayerStep
{
    private readonly IPrayerEffectsService _effectsService;

    public PrayerEffectsStep(IPrayerEffectsService effectsService)
    {
        _effectsService = effectsService;
    }

    public string Name => "PrayerEffects";

    public void Execute(PrayerContext context)
    {
        if (!context.Success)
            return;

        _effectsService.PlayPrayerEffects(
            context.Player,
            context.AltarPosition,
            context.HolySiteTier,
            context.Domain);
    }
}