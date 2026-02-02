using DivineAscension.Services.Interfaces;

namespace DivineAscension.Systems.Altar.Pipeline.Steps;

/// <summary>
/// Attempts to contribute an offering to an active or new ritual.
/// If successful, short-circuits the pipeline (ritual contributions bypass normal prayer).
/// </summary>
public class RitualContributionStep(IRitualContributionService ritualService) : IPrayerStep
{
    public string Name => "RitualContribution";

    public void Execute(PrayerContext context)
    {
        if (context.Offering == null || context.Offering.StackSize <= 0)
            return;

        var result = ritualService.TryContributeToRitual(
            context.HolySite!,
            context.Offering,
            context.Religion!,
            context.PlayerUID,
            context.PlayerName);

        if (result.Success)
        {
            context.IsRitualContribution = true;
            context.Success = true;
            context.Message = result.Message;
            context.FavorAwarded = result.FavorAwarded;
            context.PrestigeAwarded = result.PrestigeAwarded;
            context.ShouldConsumeOffering = result.ShouldConsumeOffering;
            context.ShouldUpdateCooldown = false; // Ritual contributions bypass cooldown
            context.IsComplete = true;
        }
        // If ritual contribution failed (not a ritual item), continue with normal prayer flow
    }
}