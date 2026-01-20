using System;
using DivineAscension.Models.Enum;
using DivineAscension.Systems.Interfaces;

namespace DivineAscension.Systems;

/// <summary>
/// Facade service that coordinates player and religion progression rewards.
/// Simplifies the common pattern of awarding favor, prestige, and logging activity.
/// </summary>
internal class PlayerProgressionService(
    IFavorSystem favorSystem,
    IReligionPrestigeManager prestigeManager,
    IActivityLogManager activityLogManager)
    : IPlayerProgressionService
{
    private readonly IActivityLogManager _activityLogManager =
        activityLogManager ?? throw new ArgumentNullException(nameof(activityLogManager));

    private readonly IFavorSystem _favorSystem = favorSystem ?? throw new ArgumentNullException(nameof(favorSystem));

    private readonly IReligionPrestigeManager _prestigeManager =
        prestigeManager ?? throw new ArgumentNullException(nameof(prestigeManager));

    public void AwardProgressionForPrayer(
        string playerUID,
        string religionUID,
        int favor,
        int prestige,
        DeityDomain domain,
        string activityMessage)
    {
        // Award favor to player (player progression)
        _favorSystem.AwardFavorForAction(playerUID, "prayer", favor, domain);

        // Award prestige to religion (religion progression)
        _prestigeManager.AddPrestige(religionUID, prestige, "prayer");

        // Log the activity in the religion's feed
        _activityLogManager.LogActivity(
            religionUID,
            playerUID,
            activityMessage,
            favor,
            prestige,
            domain);
    }
}