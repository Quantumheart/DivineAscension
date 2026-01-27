using System;
using Vintagestory.API.Server;

namespace DivineAscension.Systems.Interfaces;

/// <summary>
///     Interface for managing PvP interactions, favor, and prestige rewards
/// </summary>
public interface IPvPManager
{
    /// <summary>
    ///     Event fired when a PvP kill occurs against an enemy civilization during war.
    ///     Parameters: attackerCivId
    /// </summary>
    event Action<string>? OnWarKill;

    /// <summary>
    ///     Initializes the PvP manager and hooks into game events
    /// </summary>
    void Initialize();

    /// <summary>
    ///     Awards favor and prestige for deity-aligned actions (extensible for future features)
    /// </summary>
    void AwardRewardsForAction(IServerPlayer player, string actionType, int favorAmount, int prestigeAmount);

    /// <summary>
    ///     Sets the civilization bonus system for applying conquest multipliers.
    ///     Uses late binding to avoid circular dependencies during initialization.
    /// </summary>
    void SetCivilizationBonusSystem(ICivilizationBonusSystem civilizationBonusSystem);
}