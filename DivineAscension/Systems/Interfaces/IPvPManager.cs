using Vintagestory.API.Server;

namespace PantheonWars.Systems.Interfaces;

/// <summary>
///     Interface for managing PvP interactions, favor, and prestige rewards
/// </summary>
public interface IPvPManager
{
    /// <summary>
    ///     Initializes the PvP manager and hooks into game events
    /// </summary>
    void Initialize();

    /// <summary>
    ///     Awards favor and prestige for deity-aligned actions (extensible for future features)
    /// </summary>
    void AwardRewardsForAction(IServerPlayer player, string actionType, int favorAmount, int prestigeAmount);
}