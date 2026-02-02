using DivineAscension.Systems.Interfaces;

namespace DivineAscension.Systems.Altar.Pipeline.Steps;

/// <summary>
/// Awards favor and prestige progression to the player and religion.
/// Only executes if the prayer was successful and not a ritual contribution
/// (ritual contributions handle their own progression via RitualContributionService).
/// </summary>
public class ProgressionAwardStep(IPlayerProgressionService progressionService) : IPrayerStep
{
    public string Name => "ProgressionAward";

    public void Execute(PrayerContext context)
    {
        // Skip if prayer failed or was a ritual contribution (those handle their own progression)
        if (!context.Success || context.IsRitualContribution)
            return;

        // Build activity message based on offering status
        string activityMessage;
        if (context.OfferingBonus > 0)
        {
            activityMessage = $"{context.PlayerName} prayed with offering (+{context.OfferingBonus} bonus)";
        }
        else if (context.OfferingRejectedDomain)
        {
            activityMessage = $"{context.PlayerName} prayed (offering rejected - wrong domain)";
        }
        else
        {
            activityMessage = $"{context.PlayerName} prayed at holy site";
        }

        progressionService.AwardProgressionForPrayer(
            context.PlayerUID,
            context.HolySite!.ReligionUID,
            context.FavorAwarded,
            context.PrestigeAwarded,
            context.Domain,
            activityMessage);
    }
}