using DivineAscension.Models;

namespace DivineAscension.Systems.Interfaces;

/// <summary>
///     Provides civilization-wide bonus multipliers for favor and prestige calculations
/// </summary>
public interface ICivilizationBonusSystem
{
    /// <summary>
    ///     Gets the favor multiplier for a civilization (1.0 = no bonus)
    /// </summary>
    /// <param name="civId">Civilization ID</param>
    /// <returns>Favor multiplier (e.g., 1.10 = +10%)</returns>
    float GetFavorMultiplier(string civId);

    /// <summary>
    ///     Gets the prestige multiplier for a civilization (1.0 = no bonus)
    /// </summary>
    /// <param name="civId">Civilization ID</param>
    /// <returns>Prestige multiplier (e.g., 1.05 = +5%)</returns>
    float GetPrestigeMultiplier(string civId);

    /// <summary>
    ///     Gets the conquest/PvP reward multiplier for a civilization (1.0 = no bonus)
    /// </summary>
    /// <param name="civId">Civilization ID</param>
    /// <returns>Conquest multiplier (e.g., 1.05 = +5%)</returns>
    float GetConquestMultiplier(string civId);

    /// <summary>
    ///     Gets all active bonuses for a civilization
    /// </summary>
    /// <param name="civId">Civilization ID</param>
    /// <returns>All civilization bonuses</returns>
    CivilizationBonuses GetAllBonuses(string civId);

    /// <summary>
    ///     Gets the favor multiplier for a player based on their civilization membership
    /// </summary>
    /// <param name="playerUID">Player UID</param>
    /// <returns>Favor multiplier (1.0 if player is not in a civilization)</returns>
    float GetFavorMultiplierForPlayer(string playerUID);

    /// <summary>
    ///     Gets the prestige multiplier for a player based on their civilization membership
    /// </summary>
    /// <param name="playerUID">Player UID</param>
    /// <returns>Prestige multiplier (1.0 if player is not in a civilization)</returns>
    float GetPrestigeMultiplierForPlayer(string playerUID);

    /// <summary>
    ///     Gets the bonus holy site slots for a religion based on civilization milestone bonuses
    /// </summary>
    /// <param name="religionUID">Religion UID</param>
    /// <returns>Bonus holy site slots (0 if religion is not in a civilization)</returns>
    int GetBonusHolySiteSlotsForReligion(string religionUID);
}
