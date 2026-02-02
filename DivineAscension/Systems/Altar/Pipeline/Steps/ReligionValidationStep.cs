using DivineAscension.Constants;
using DivineAscension.Models.Enum;
using DivineAscension.Services;
using DivineAscension.Systems.Interfaces;

namespace DivineAscension.Systems.Altar.Pipeline.Steps;

/// <summary>
/// Validates that the player belongs to a religion and can pray at this holy site.
/// Players can pray if they're members of the religion OR worship the same deity domain.
/// Sets Religion and Domain on the context.
/// </summary>
public class ReligionValidationStep(IReligionManager religionManager) : IPrayerStep
{
    public string Name => "ReligionValidation";

    public void Execute(PrayerContext context)
    {
        context.Religion = religionManager.GetPlayerReligion(context.PlayerUID);

        if (context.Religion == null)
        {
            context.Success = false;
            context.Message = LocalizationService.Instance.Get(LocalizationKeys.PRAYER_NO_RELIGION);
            context.IsComplete = true;
            return;
        }

        context.Domain = context.Religion.Domain;

        // Check if player can pray at this holy site:
        // 1. Same religion (member of the religion that owns the holy site)
        // 2. Same domain (player's religion worships the same deity domain)
        if (context.Religion.ReligionUID != context.HolySite!.ReligionUID)
        {
            // Not a member - check if same domain allows prayer
            var holySiteOwnerReligion = religionManager.GetReligion(context.HolySite.ReligionUID);
            var holySiteDomain = holySiteOwnerReligion?.Domain ?? DeityDomain.None;

            if (context.Religion.Domain != holySiteDomain || holySiteDomain == DeityDomain.None)
            {
                context.Success = false;
                context.Message = LocalizationService.Instance.Get(LocalizationKeys.PRAYER_WRONG_DOMAIN);
                context.IsComplete = true;
            }
        }
    }
}