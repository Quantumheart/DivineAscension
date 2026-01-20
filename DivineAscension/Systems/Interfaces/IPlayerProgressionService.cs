using DivineAscension.Models.Enum;

namespace DivineAscension.Systems.Interfaces;

/// <summary>
/// Facade service that coordinates player and religion progression rewards.
/// Encapsulates favor awarding, prestige awarding, and activity logging as a single operation.
/// </summary>
public interface IPlayerProgressionService
{
    /// <summary>
    /// Awards progression (favor + prestige) and logs the activity for a prayer action.
    /// </summary>
    /// <param name="playerUID">The unique identifier of the player</param>
    /// <param name="religionUID">The religion receiving prestige</param>
    /// <param name="favor">Amount of favor to award to the player</param>
    /// <param name="prestige">Amount of prestige to award to the religion</param>
    /// <param name="domain">The deity domain for activity logging</param>
    /// <param name="activityMessage">The message to log in the activity feed</param>
    void AwardProgressionForPrayer(
        string playerUID,
        string religionUID,
        int favor,
        int prestige,
        DeityDomain domain,
        string activityMessage);
}